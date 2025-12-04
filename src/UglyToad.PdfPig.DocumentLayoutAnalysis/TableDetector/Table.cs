#nullable enable

namespace UglyToad.PdfPig.DocumentLayoutAnalysis.TableDetector
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UglyToad.PdfPig.Core;

    /// <summary>
    /// Represents a table detected in a PDF page.
    /// </summary>
    public class Table
    {
        /// <summary>
        /// The bounding box of the entire table.
        /// </summary>
        public PdfRectangle BoundingBox { get; }

        /// <summary>
        /// The number of rows in the table.
        /// </summary>
        public int RowCount { get; }

        /// <summary>
        /// The number of columns in the table.
        /// </summary>
        public int ColumnCount { get; }

        /// <summary>
        /// All cells in the table.
        /// </summary>
        public IReadOnlyList<TableCell> Cells { get; }

        /// <summary>
        /// Creates a new <see cref="Table"/>.
        /// </summary>
        /// <param name="boundingBox">The bounding box of the table.</param>
        /// <param name="cells">The cells in the table.</param>
        public Table(PdfRectangle boundingBox, IReadOnlyList<TableCell> cells)
        {
            if (cells == null || cells.Count == 0)
            {
                throw new ArgumentException("Table must have at least one cell.", nameof(cells));
            }

            BoundingBox = boundingBox;
            Cells = cells;

            // Calculate row and column count
            RowCount = cells.Max(c => c.RowIndex + c.RowSpan);
            ColumnCount = cells.Max(c => c.ColumnIndex + c.ColumnSpan);
        }

        /// <summary>
        /// Gets the cell at the specified row and column.
        /// </summary>
        /// <param name="row">The row index (0-based).</param>
        /// <param name="column">The column index (0-based).</param>
        /// <returns>The cell at the specified position, or null if no cell exists there.</returns>
        public TableCell? GetCell(int row, int column)
        {
            if (row < 0 || row >= RowCount)
            {
                throw new ArgumentOutOfRangeException(nameof(row), $"Row must be between 0 and {RowCount - 1}.");
            }

            if (column < 0 || column >= ColumnCount)
            {
                throw new ArgumentOutOfRangeException(nameof(column), $"Column must be between 0 and {ColumnCount - 1}.");
            }

            return Cells.FirstOrDefault(c =>
                row >= c.RowIndex && row < c.RowIndex + c.RowSpan &&
                column >= c.ColumnIndex && column < c.ColumnIndex + c.ColumnSpan);
        }

        /// <summary>
        /// Gets all cells in the specified row.
        /// </summary>
        /// <param name="row">The row index (0-based).</param>
        /// <returns>The cells in the specified row.</returns>
        public IReadOnlyList<TableCell> GetRow(int row)
        {
            if (row < 0 || row >= RowCount)
            {
                throw new ArgumentOutOfRangeException(nameof(row), $"Row must be between 0 and {RowCount - 1}.");
            }

            return Cells.Where(c => row >= c.RowIndex && row < c.RowIndex + c.RowSpan)
                        .OrderBy(c => c.ColumnIndex)
                        .ToList();
        }

        /// <summary>
        /// Gets all cells in the specified column.
        /// </summary>
        /// <param name="column">The column index (0-based).</param>
        /// <returns>The cells in the specified column.</returns>
        public IReadOnlyList<TableCell> GetColumn(int column)
        {
            if (column < 0 || column >= ColumnCount)
            {
                throw new ArgumentOutOfRangeException(nameof(column), $"Column must be between 0 and {ColumnCount - 1}.");
            }

            return Cells.Where(c => column >= c.ColumnIndex && column < c.ColumnIndex + c.ColumnSpan)
                        .OrderBy(c => c.RowIndex)
                        .ToList();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Table[{RowCount}x{ColumnCount}] at {BoundingBox}";
        }
    }
}
