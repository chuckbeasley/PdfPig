#nullable enable

namespace UglyToad.PdfPig.DocumentLayoutAnalysis.TableDetector
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UglyToad.PdfPig.Content;
    using UglyToad.PdfPig.Core;

    /// <summary>
    /// Represents a cell in a table.
    /// </summary>
    public class TableCell
    {
        /// <summary>
        /// The bounding box of the cell.
        /// </summary>
        public PdfRectangle BoundingBox { get; }

        /// <summary>
        /// The row index of the cell (0-based, where 0 is the topmost row).
        /// </summary>
        public int RowIndex { get; }

        /// <summary>
        /// The column index of the cell (0-based, where 0 is the leftmost column).
        /// </summary>
        public int ColumnIndex { get; }

        /// <summary>
        /// The number of rows this cell spans.
        /// </summary>
        public int RowSpan { get; }

        /// <summary>
        /// The number of columns this cell spans.
        /// </summary>
        public int ColumnSpan { get; }

        /// <summary>
        /// The words contained in this cell.
        /// </summary>
        public IReadOnlyList<Word> Words { get; }

        /// <summary>
        /// The text content of the cell.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Creates a new <see cref="TableCell"/>.
        /// </summary>
        /// <param name="boundingBox">The bounding box of the cell.</param>
        /// <param name="rowIndex">The row index (0-based).</param>
        /// <param name="columnIndex">The column index (0-based).</param>
        /// <param name="words">The words contained in the cell.</param>
        /// <param name="rowSpan">The number of rows this cell spans. Default is 1.</param>
        /// <param name="columnSpan">The number of columns this cell spans. Default is 1.</param>
        public TableCell(
            PdfRectangle boundingBox,
            int rowIndex,
            int columnIndex,
            IReadOnlyList<Word> words,
            int rowSpan = 1,
            int columnSpan = 1)
        {
            if (rowIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rowIndex), "Row index must be non-negative.");
            }

            if (columnIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(columnIndex), "Column index must be non-negative.");
            }

            if (rowSpan < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(rowSpan), "Row span must be at least 1.");
            }

            if (columnSpan < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(columnSpan), "Column span must be at least 1.");
            }

            BoundingBox = boundingBox;
            RowIndex = rowIndex;
            ColumnIndex = columnIndex;
            RowSpan = rowSpan;
            ColumnSpan = columnSpan;
            Words = words ?? Array.Empty<Word>();

            // Build text from words
            if (Words.Count == 0)
            {
                Text = string.Empty;
            }
            else if (Words.Count == 1)
            {
                Text = Words[0].Text;
            }
            else
            {
                var sortedWords = new List<Word>(Words);
                // Sort words by position (top to bottom, left to right)
                sortedWords.Sort((a, b) =>
                {
                    var yDiff = b.BoundingBox.Bottom - a.BoundingBox.Bottom;
                    if (Math.Abs(yDiff) > 5) // tolerance for same line
                    {
                        return yDiff > 0 ? 1 : -1;
                    }
                    return a.BoundingBox.Left.CompareTo(b.BoundingBox.Left);
                });

                Text = string.Join(" ", sortedWords.Select(w => w.Text));
            }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Cell[{RowIndex},{ColumnIndex}]: {Text}";
        }
    }
}
