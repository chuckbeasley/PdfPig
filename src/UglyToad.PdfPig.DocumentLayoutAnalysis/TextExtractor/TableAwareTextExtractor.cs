#nullable enable

namespace UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using UglyToad.PdfPig.Content;
    using UglyToad.PdfPig.Core;
    using UglyToad.PdfPig.DocumentLayoutAnalysis.TableDetector;
    using UglyToad.PdfPig.Util;

    /// <summary>
    /// Extracts text from a PDF page with awareness of table structures.
    /// Tables are detected and their content is formatted inline with regular text,
    /// maintaining the natural reading order of the document.
    /// </summary>
    public static class TableAwareTextExtractor
    {
        /// <summary>
        /// Options for table-aware text extraction.
        /// </summary>
        public class Options
        {
            /// <summary>
            /// The table detector to use. If null, no table detection is performed.
            /// </summary>
            public ITableDetector? TableDetector { get; set; }

            /// <summary>
            /// The word extractor to use. If null, uses the default word extractor.
            /// </summary>
            public IWordExtractor? WordExtractor { get; set; }

            /// <summary>
            /// The column separator to use when formatting table cells.
            /// Default is "\t" (tab).
            /// </summary>
            public string ColumnSeparator { get; set; } = "\t";

            /// <summary>
            /// The row separator to use when formatting table rows.
            /// Default is newline.
            /// </summary>
            public string RowSeparator { get; set; } = Environment.NewLine;

            /// <summary>
            /// Whether to include a marker before and after table content.
            /// Default is false.
            /// </summary>
            public bool IncludeTableMarkers { get; set; }

            /// <summary>
            /// The marker to place before table content when <see cref="IncludeTableMarkers"/> is true.
            /// Default is "[TABLE]".
            /// </summary>
            public string TableStartMarker { get; set; } = "[TABLE]";

            /// <summary>
            /// The marker to place after table content when <see cref="IncludeTableMarkers"/> is true.
            /// Default is "[/TABLE]".
            /// </summary>
            public string TableEndMarker { get; set; } = "[/TABLE]";

            /// <summary>
            /// The tolerance (in points) for considering two Y positions as being on the same line.
            /// Default is 5 points.
            /// </summary>
            public double LineTolerance { get; set; } = 5.0;
        }

        /// <summary>
        /// The result of table-aware text extraction.
        /// </summary>
        public class ExtractionResult
        {
            /// <summary>
            /// The full extracted text with tables formatted inline in document reading order.
            /// </summary>
            public string Text { get; }

            /// <summary>
            /// The tables detected on the page.
            /// </summary>
            public IReadOnlyList<Table> Tables { get; }

            /// <summary>
            /// Creates a new <see cref="ExtractionResult"/>.
            /// </summary>
            public ExtractionResult(string text, IReadOnlyList<Table> tables)
            {
                Text = text;
                Tables = tables;
            }
        }

        /// <summary>
        /// Represents a content element in the document flow (either a text line or a table).
        /// </summary>
        private abstract class ContentElement
        {
            public double Top { get; }
            public double Bottom { get; }

            protected ContentElement(double top, double bottom)
            {
                Top = top;
                Bottom = bottom;
            }

            public abstract void AppendTo(StringBuilder sb, Options options);
        }

        /// <summary>
        /// Represents a line of text in the document flow.
        /// </summary>
        private class TextLineElement : ContentElement
        {
            public List<Word> Words { get; }

            public TextLineElement(List<Word> words, double top, double bottom)
                : base(top, bottom)
            {
                Words = words;
            }

            public override void AppendTo(StringBuilder sb, Options options)
            {
                for (int i = 0; i < Words.Count; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(' ');
                    }
                    sb.Append(Words[i].Text);
                }
            }
        }

        /// <summary>
        /// Represents a table in the document flow.
        /// </summary>
        private class TableElement : ContentElement
        {
            public Table Table { get; }

            public TableElement(Table table)
                : base(table.BoundingBox.Top, table.BoundingBox.Bottom)
            {
                Table = table;
            }

            public override void AppendTo(StringBuilder sb, Options options)
            {
                if (options.IncludeTableMarkers)
                {
                    sb.AppendLine(options.TableStartMarker);
                }

                for (int row = 0; row < Table.RowCount; row++)
                {
                    if (row > 0)
                    {
                        sb.Append(options.RowSeparator);
                    }

                    var cellTexts = new List<string>();
                    var outputColumns = new HashSet<int>();

                    for (int col = 0; col < Table.ColumnCount; col++)
                    {
                        if (outputColumns.Contains(col))
                        {
                            continue;
                        }

                        var cell = Table.GetCell(row, col);
                        if (cell != null)
                        {
                            cellTexts.Add(cell.Text);
                            for (int c = col; c < col + cell.ColumnSpan; c++)
                            {
                                outputColumns.Add(c);
                            }
                        }
                        else
                        {
                            cellTexts.Add(string.Empty);
                            outputColumns.Add(col);
                        }
                    }

                    sb.Append(string.Join(options.ColumnSeparator, cellTexts));
                }

                if (options.IncludeTableMarkers)
                {
                    sb.AppendLine();
                    sb.Append(options.TableEndMarker);
                }
            }
        }

        /// <summary>
        /// Extracts text from a page with awareness of table structures.
        /// Tables are integrated inline with the document text flow.
        /// </summary>
        /// <param name="page">The page to extract text from.</param>
        /// <param name="options">Options controlling the extraction. If null, defaults are used.</param>
        /// <returns>The extraction result containing formatted text and detected tables.</returns>
        public static ExtractionResult GetText(Page page, Options? options = null)
        {
            if (page == null)
            {
                throw new ArgumentNullException(nameof(page));
            }

            options ??= new Options();

            // Get words from the page
            var wordExtractor = options.WordExtractor ?? DefaultWordExtractor.Instance;
            var words = page.GetWords(wordExtractor).ToList();

            // Get paths for ruled-line table detection
            var paths = page.Paths;

            // Detect tables
            var tables = new List<Table>();
            if (options.TableDetector != null)
            {
                tables.AddRange(options.TableDetector.DetectTables(words, paths));
            }

            // Create a set of words that belong to tables
            var tableWords = new HashSet<Word>();
            foreach (var table in tables)
            {
                foreach (var cell in table.Cells)
                {
                    foreach (var word in cell.Words)
                    {
                        tableWords.Add(word);
                    }
                }
            }

            // Build a unified list of content elements (text lines and tables)
            var contentElements = new List<ContentElement>();

            // Add tables as content elements
            foreach (var table in tables)
            {
                contentElements.Add(new TableElement(table));
            }

            // Group non-table words into lines and add them as content elements
            var nonTableWords = words.Where(w => !tableWords.Contains(w)).ToList();
            var textLines = GroupWordsIntoLines(nonTableWords, options.LineTolerance);

            foreach (var line in textLines)
            {
                var top = line.Max(w => w.BoundingBox.Top);
                var bottom = line.Min(w => w.BoundingBox.Bottom);
                contentElements.Add(new TextLineElement(line, top, bottom));
            }

            // Sort all content elements by position (top to bottom in reading order)
            // PDF Y increases upward, so higher Y values come first
            contentElements.Sort((a, b) => b.Top.CompareTo(a.Top));

            // Build the final text by appending elements in reading order
            var sb = new StringBuilder();
            for (int i = 0; i < contentElements.Count; i++)
            {
                if (i > 0)
                {
                    sb.AppendLine();
                }
                contentElements[i].AppendTo(sb, options);
            }

            return new ExtractionResult(sb.ToString(), tables);
        }

        /// <summary>
        /// Gets just the text from a page with table awareness.
        /// This is a convenience method that returns only the formatted text.
        /// </summary>
        /// <param name="page">The page to extract text from.</param>
        /// <param name="tableDetector">The table detector to use. If null, no table detection is performed.</param>
        /// <returns>The formatted text.</returns>
        public static string GetTextString(Page page, ITableDetector? tableDetector = null)
        {
            return GetText(page, new Options { TableDetector = tableDetector }).Text;
        }

        /// <summary>
        /// Groups words into lines based on their Y position.
        /// </summary>
        private static List<List<Word>> GroupWordsIntoLines(List<Word> words, double lineTolerance)
        {
            if (words.Count == 0)
            {
                return new List<List<Word>>();
            }

            // Sort words by Y position (descending, since PDF Y increases upward)
            var sortedWords = words.OrderByDescending(w => w.BoundingBox.Bottom).ToList();

            var lines = new List<List<Word>>();
            List<Word>? currentLine = null;
            double currentY = double.MinValue;

            foreach (var word in sortedWords)
            {
                var wordY = word.BoundingBox.Bottom;

                if (currentLine == null || Math.Abs(wordY - currentY) > lineTolerance)
                {
                    currentLine = new List<Word>();
                    lines.Add(currentLine);
                    currentY = wordY;
                }

                currentLine.Add(word);
            }

            // Sort words within each line left to right
            foreach (var line in lines)
            {
                line.Sort((a, b) => a.BoundingBox.Left.CompareTo(b.BoundingBox.Left));
            }

            return lines;
        }
    }
}
