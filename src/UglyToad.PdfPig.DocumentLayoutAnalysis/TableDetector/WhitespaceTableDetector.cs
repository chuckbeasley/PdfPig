#nullable enable

namespace UglyToad.PdfPig.DocumentLayoutAnalysis.TableDetector
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UglyToad.PdfPig.Content;
    using UglyToad.PdfPig.Core;
    using UglyToad.PdfPig.Graphics;

    /// <summary>
    /// Detects tables in PDF pages by analyzing whitespace and text alignment patterns.
    /// This detector works without requiring ruled lines, making it suitable for tables
    /// that use only spacing to separate columns.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Tables are detected by identifying consistent vertical whitespace gaps between words
    /// on the same line. When multiple consecutive lines share similar gap patterns,
    /// they are grouped together as a table.
    /// </para>
    /// <para>
    /// Row indexing follows a top-to-bottom convention where row 0 is the topmost row.
    /// </para>
    /// </remarks>
    public class WhitespaceTableDetector : ITableDetector
    {
        /// <summary>
        /// Options for the whitespace table detector.
        /// </summary>
        public class WhitespaceTableDetectorOptions
        {
            /// <summary>
            /// The minimum number of columns required to form a table.
            /// Default is 2.
            /// </summary>
            public int MinColumns { get; set; } = 2;

            /// <summary>
            /// The minimum number of rows required to form a table.
            /// Default is 2.
            /// </summary>
            public int MinRows { get; set; } = 2;

            /// <summary>
            /// The minimum horizontal gap (in points) between words to consider as a column separator.
            /// Default is 15 points.
            /// </summary>
            public double MinColumnGap { get; set; } = 15.0;

            /// <summary>
            /// The tolerance (in points) for considering two Y positions as being on the same line.
            /// Default is 5 points.
            /// </summary>
            public double LineTolerance { get; set; } = 5.0;

            /// <summary>
            /// The tolerance (in points) for considering column positions as aligned.
            /// Default is 10 points.
            /// </summary>
            public double ColumnAlignmentTolerance { get; set; } = 10.0;

            /// <summary>
            /// The maximum vertical gap (in points) between rows to still consider them part of the same table.
            /// Default is 50 points.
            /// </summary>
            public double MaxRowGap { get; set; } = 50.0;
        }

        private readonly WhitespaceTableDetectorOptions options;

        /// <summary>
        /// The default instance of the <see cref="WhitespaceTableDetector"/>.
        /// </summary>
        public static WhitespaceTableDetector Instance { get; } = new WhitespaceTableDetector();

        /// <summary>
        /// Creates a new <see cref="WhitespaceTableDetector"/> with default options.
        /// </summary>
        public WhitespaceTableDetector() : this(new WhitespaceTableDetectorOptions())
        {
        }

        /// <summary>
        /// Creates a new <see cref="WhitespaceTableDetector"/> with the specified options.
        /// </summary>
        /// <param name="options">The detector options.</param>
        public WhitespaceTableDetector(WhitespaceTableDetectorOptions options)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <inheritdoc />
        public IReadOnlyList<Table> DetectTables(IEnumerable<Word> words, IReadOnlyList<PdfPath>? paths = null)
        {
            var wordsList = words?.ToList() ?? new List<Word>();
            if (wordsList.Count == 0)
            {
                return Array.Empty<Table>();
            }

            // Group words into lines based on Y position
            var lines = GroupWordsIntoLines(wordsList);
            if (lines.Count < options.MinRows)
            {
                return Array.Empty<Table>();
            }

            // Find column separators in each line
            var lineColumnData = lines.Select(line => AnalyzeLineColumns(line)).ToList();

            // Find table regions by grouping consecutive lines with similar column structure
            var tableRegions = FindTableRegions(lines, lineColumnData);

            // Build tables from regions
            var tables = new List<Table>();
            foreach (var region in tableRegions)
            {
                var table = BuildTable(region);
                if (table != null)
                {
                    tables.Add(table);
                }
            }

            return tables;
        }

        private List<List<Word>> GroupWordsIntoLines(List<Word> words)
        {
            // Sort words by Y position (descending, since PDF Y increases upward)
            var sortedWords = words.OrderByDescending(w => w.BoundingBox.Bottom).ToList();

            var lines = new List<List<Word>>();
            List<Word>? currentLine = null;
            double currentY = double.MinValue;

            foreach (var word in sortedWords)
            {
                var wordY = word.BoundingBox.Bottom;

                if (currentLine == null || Math.Abs(wordY - currentY) > options.LineTolerance)
                {
                    // Start a new line
                    currentLine = new List<Word>();
                    lines.Add(currentLine);
                    currentY = wordY;
                }

                currentLine.Add(word);
            }

            // Sort words within each line by X position (left to right)
            foreach (var line in lines)
            {
                line.Sort((a, b) => a.BoundingBox.Left.CompareTo(b.BoundingBox.Left));
            }

            return lines;
        }

        private class LineColumnInfo
        {
            public List<double> ColumnBoundaries { get; } = new List<double>();
            public int ColumnCount => ColumnBoundaries.Count > 0 ? ColumnBoundaries.Count + 1 : 1;
            public double MinX { get; set; }
            public double MaxX { get; set; }
        }

        private LineColumnInfo AnalyzeLineColumns(List<Word> lineWords)
        {
            var info = new LineColumnInfo();

            if (lineWords.Count == 0)
            {
                return info;
            }

            info.MinX = lineWords.Min(w => w.BoundingBox.Left);
            info.MaxX = lineWords.Max(w => w.BoundingBox.Right);

            if (lineWords.Count < 2)
            {
                return info;
            }

            // Find gaps between consecutive words
            for (int i = 0; i < lineWords.Count - 1; i++)
            {
                var gap = lineWords[i + 1].BoundingBox.Left - lineWords[i].BoundingBox.Right;
                if (gap >= options.MinColumnGap)
                {
                    // Mark the midpoint of the gap as a column boundary
                    var boundary = lineWords[i].BoundingBox.Right + (gap / 2);
                    info.ColumnBoundaries.Add(boundary);
                }
            }

            return info;
        }

        private class TableRegion
        {
            public List<List<Word>> Lines { get; } = new List<List<Word>>();
            public List<double> ColumnBoundaries { get; } = new List<double>();
            public PdfRectangle BoundingBox { get; set; }
        }

        private List<TableRegion> FindTableRegions(List<List<Word>> lines, List<LineColumnInfo> lineColumnData)
        {
            var regions = new List<TableRegion>();
            var usedLines = new HashSet<int>();

            for (int i = 0; i < lines.Count; i++)
            {
                if (usedLines.Contains(i))
                {
                    continue;
                }

                var lineInfo = lineColumnData[i];
                if (lineInfo.ColumnCount < options.MinColumns)
                {
                    continue;
                }

                // Start a new potential table region
                var region = new TableRegion();
                region.Lines.Add(lines[i]);
                region.ColumnBoundaries.AddRange(lineInfo.ColumnBoundaries);
                usedLines.Add(i);

                // Look for consecutive lines with similar column structure
                for (int j = i + 1; j < lines.Count; j++)
                {
                    if (usedLines.Contains(j))
                    {
                        continue;
                    }

                    var nextLineInfo = lineColumnData[j];

                    // Check if the line has a compatible column structure
                    if (AreColumnStructuresCompatible(region.ColumnBoundaries, nextLineInfo.ColumnBoundaries))
                    {
                        // Check if the row gap is acceptable
                        var lastLineY = region.Lines[region.Lines.Count - 1].Min(w => w.BoundingBox.Bottom);
                        var nextLineY = lines[j].Max(w => w.BoundingBox.Top);
                        var rowGap = lastLineY - nextLineY;

                        if (rowGap >= 0 && rowGap <= options.MaxRowGap)
                        {
                            region.Lines.Add(lines[j]);
                            usedLines.Add(j);

                            // Merge column boundaries
                            MergeColumnBoundaries(region.ColumnBoundaries, nextLineInfo.ColumnBoundaries);
                        }
                    }
                }

                // Only keep regions with enough rows
                if (region.Lines.Count >= options.MinRows)
                {
                    // Calculate bounding box
                    var allWords = region.Lines.SelectMany(l => l).ToList();
                    var minX = allWords.Min(w => w.BoundingBox.Left);
                    var maxX = allWords.Max(w => w.BoundingBox.Right);
                    var minY = allWords.Min(w => w.BoundingBox.Bottom);
                    var maxY = allWords.Max(w => w.BoundingBox.Top);
                    region.BoundingBox = new PdfRectangle(minX, minY, maxX, maxY);

                    regions.Add(region);
                }
            }

            return regions;
        }

        private bool AreColumnStructuresCompatible(List<double> boundaries1, List<double> boundaries2)
        {
            // Empty boundaries are not compatible with column boundaries
            if (boundaries1.Count == 0 || boundaries2.Count == 0)
            {
                return boundaries1.Count == boundaries2.Count;
            }

            // Allow some flexibility in the number of columns
            if (Math.Abs(boundaries1.Count - boundaries2.Count) > 1)
            {
                return false;
            }

            // Check if boundaries are approximately aligned
            int matched = 0;
            foreach (var b1 in boundaries1)
            {
                if (boundaries2.Any(b2 => Math.Abs(b1 - b2) <= options.ColumnAlignmentTolerance))
                {
                    matched++;
                }
            }

            // Require at least half of the boundaries to match
            return matched >= Math.Min(boundaries1.Count, boundaries2.Count) / 2.0;
        }

        private void MergeColumnBoundaries(List<double> target, List<double> source)
        {
            foreach (var boundary in source)
            {
                if (!target.Any(b => Math.Abs(b - boundary) <= options.ColumnAlignmentTolerance))
                {
                    target.Add(boundary);
                }
            }

            target.Sort();
        }

        private Table? BuildTable(TableRegion region)
        {
            if (region.Lines.Count < options.MinRows || region.ColumnBoundaries.Count < options.MinColumns - 1)
            {
                return null;
            }

            var cells = new List<TableCell>();
            int rowCount = region.Lines.Count;
            int columnCount = region.ColumnBoundaries.Count + 1;

            // Sort column boundaries
            var sortedBoundaries = region.ColumnBoundaries.OrderBy(b => b).ToList();

            for (int row = 0; row < rowCount; row++)
            {
                var lineWords = region.Lines[row];

                for (int col = 0; col < columnCount; col++)
                {
                    // Determine cell boundaries
                    double cellLeft = col == 0 ? region.BoundingBox.Left : sortedBoundaries[col - 1];
                    double cellRight = col == columnCount - 1 ? region.BoundingBox.Right : sortedBoundaries[col];

                    // Get the row's vertical bounds
                    double cellBottom = lineWords.Min(w => w.BoundingBox.Bottom);
                    double cellTop = lineWords.Max(w => w.BoundingBox.Top);

                    var cellBounds = new PdfRectangle(cellLeft, cellBottom, cellRight, cellTop);

                    // Find words in this cell
                    var cellWords = lineWords.Where(w => IsWordInColumn(w, cellLeft, cellRight)).ToList();

                    cells.Add(new TableCell(cellBounds, row, col, cellWords));
                }
            }

            if (cells.Count == 0)
            {
                return null;
            }

            return new Table(region.BoundingBox, cells);
        }

        private bool IsWordInColumn(Word word, double columnLeft, double columnRight)
        {
            var wordCenter = word.BoundingBox.Centroid.X;
            return wordCenter >= columnLeft && wordCenter <= columnRight;
        }
    }
}
