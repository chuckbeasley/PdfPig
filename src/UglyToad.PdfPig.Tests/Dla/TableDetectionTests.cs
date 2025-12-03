namespace UglyToad.PdfPig.Tests.Dla
{
    using System.Collections.Generic;
    using UglyToad.PdfPig.Content;
    using UglyToad.PdfPig.Core;
    using UglyToad.PdfPig.DocumentLayoutAnalysis.TableDetection;
    using UglyToad.PdfPig.Graphics;

    public class TableDetectionTests
    {
        [Fact]
        public void TableCell_CreatesWithValidParameters()
        {
            var boundingBox = new PdfRectangle(0, 0, 100, 50);
            var words = new List<Word>();

            var cell = new TableCell(boundingBox, 0, 0, words);

            Assert.Equal(boundingBox, cell.BoundingBox);
            Assert.Equal(0, cell.RowIndex);
            Assert.Equal(0, cell.ColumnIndex);
            Assert.Equal(1, cell.RowSpan);
            Assert.Equal(1, cell.ColumnSpan);
            Assert.Empty(cell.Words);
            Assert.Equal(string.Empty, cell.Text);
        }

        [Fact]
        public void TableCell_ThrowsOnNegativeRowIndex()
        {
            var boundingBox = new PdfRectangle(0, 0, 100, 50);
            var words = new List<Word>();

            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                new TableCell(boundingBox, -1, 0, words));
        }

        [Fact]
        public void TableCell_ThrowsOnNegativeColumnIndex()
        {
            var boundingBox = new PdfRectangle(0, 0, 100, 50);
            var words = new List<Word>();

            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                new TableCell(boundingBox, 0, -1, words));
        }

        [Fact]
        public void TableCell_ThrowsOnInvalidRowSpan()
        {
            var boundingBox = new PdfRectangle(0, 0, 100, 50);
            var words = new List<Word>();

            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                new TableCell(boundingBox, 0, 0, words, rowSpan: 0));
        }

        [Fact]
        public void TableCell_ThrowsOnInvalidColumnSpan()
        {
            var boundingBox = new PdfRectangle(0, 0, 100, 50);
            var words = new List<Word>();

            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                new TableCell(boundingBox, 0, 0, words, columnSpan: 0));
        }

        [Fact]
        public void TableCell_ToStringReturnsExpectedFormat()
        {
            var boundingBox = new PdfRectangle(0, 0, 100, 50);
            var cell = new TableCell(boundingBox, 1, 2, new List<Word>());

            var result = cell.ToString();

            Assert.Equal("Cell[1,2]: ", result);
        }

        [Fact]
        public void Table_CreatesWithValidCells()
        {
            var cells = new List<TableCell>
            {
                new TableCell(new PdfRectangle(0, 0, 50, 25), 0, 0, new List<Word>()),
                new TableCell(new PdfRectangle(50, 0, 100, 25), 0, 1, new List<Word>()),
                new TableCell(new PdfRectangle(0, 25, 50, 50), 1, 0, new List<Word>()),
                new TableCell(new PdfRectangle(50, 25, 100, 50), 1, 1, new List<Word>())
            };
            var boundingBox = new PdfRectangle(0, 0, 100, 50);

            var table = new Table(boundingBox, cells);

            Assert.Equal(2, table.RowCount);
            Assert.Equal(2, table.ColumnCount);
            Assert.Equal(4, table.Cells.Count);
        }

        [Fact]
        public void Table_ThrowsOnEmptyCells()
        {
            var boundingBox = new PdfRectangle(0, 0, 100, 50);

            Assert.Throws<System.ArgumentException>(() =>
                new Table(boundingBox, new List<TableCell>()));
        }

        [Fact]
        public void Table_GetCell_ReturnsCorrectCell()
        {
            var cells = new List<TableCell>
            {
                new TableCell(new PdfRectangle(0, 0, 50, 25), 0, 0, new List<Word>()),
                new TableCell(new PdfRectangle(50, 0, 100, 25), 0, 1, new List<Word>()),
                new TableCell(new PdfRectangle(0, 25, 50, 50), 1, 0, new List<Word>()),
                new TableCell(new PdfRectangle(50, 25, 100, 50), 1, 1, new List<Word>())
            };
            var boundingBox = new PdfRectangle(0, 0, 100, 50);
            var table = new Table(boundingBox, cells);

            var cell = table.GetCell(0, 0);

            Assert.NotNull(cell);
            Assert.Equal(0, cell.RowIndex);
            Assert.Equal(0, cell.ColumnIndex);
        }

        [Fact]
        public void Table_GetCell_ThrowsOnInvalidRow()
        {
            var cells = new List<TableCell>
            {
                new TableCell(new PdfRectangle(0, 0, 50, 25), 0, 0, new List<Word>())
            };
            var boundingBox = new PdfRectangle(0, 0, 100, 50);
            var table = new Table(boundingBox, cells);

            Assert.Throws<System.ArgumentOutOfRangeException>(() => table.GetCell(5, 0));
        }

        [Fact]
        public void Table_GetCell_ThrowsOnInvalidColumn()
        {
            var cells = new List<TableCell>
            {
                new TableCell(new PdfRectangle(0, 0, 50, 25), 0, 0, new List<Word>())
            };
            var boundingBox = new PdfRectangle(0, 0, 100, 50);
            var table = new Table(boundingBox, cells);

            Assert.Throws<System.ArgumentOutOfRangeException>(() => table.GetCell(0, 5));
        }

        [Fact]
        public void Table_GetRow_ReturnsCorrectCells()
        {
            var cells = new List<TableCell>
            {
                new TableCell(new PdfRectangle(0, 0, 50, 25), 0, 0, new List<Word>()),
                new TableCell(new PdfRectangle(50, 0, 100, 25), 0, 1, new List<Word>()),
                new TableCell(new PdfRectangle(0, 25, 50, 50), 1, 0, new List<Word>()),
                new TableCell(new PdfRectangle(50, 25, 100, 50), 1, 1, new List<Word>())
            };
            var boundingBox = new PdfRectangle(0, 0, 100, 50);
            var table = new Table(boundingBox, cells);

            var row = table.GetRow(0);

            Assert.Equal(2, row.Count);
            Assert.All(row, c => Assert.Equal(0, c.RowIndex));
        }

        [Fact]
        public void Table_GetColumn_ReturnsCorrectCells()
        {
            var cells = new List<TableCell>
            {
                new TableCell(new PdfRectangle(0, 0, 50, 25), 0, 0, new List<Word>()),
                new TableCell(new PdfRectangle(50, 0, 100, 25), 0, 1, new List<Word>()),
                new TableCell(new PdfRectangle(0, 25, 50, 50), 1, 0, new List<Word>()),
                new TableCell(new PdfRectangle(50, 25, 100, 50), 1, 1, new List<Word>())
            };
            var boundingBox = new PdfRectangle(0, 0, 100, 50);
            var table = new Table(boundingBox, cells);

            var column = table.GetColumn(0);

            Assert.Equal(2, column.Count);
            Assert.All(column, c => Assert.Equal(0, c.ColumnIndex));
        }

        [Fact]
        public void RuledLineTableDetector_ReturnsEmptyWhenNoPathsProvided()
        {
            var detector = new RuledLineTableDetector();
            var words = new List<Word>();

            var tables = detector.DetectTables(words, null);

            Assert.Empty(tables);
        }

        [Fact]
        public void RuledLineTableDetector_ReturnsEmptyWhenEmptyPathsProvided()
        {
            var detector = new RuledLineTableDetector();
            var words = new List<Word>();
            var paths = new List<PdfPath>();

            var tables = detector.DetectTables(words, paths);

            Assert.Empty(tables);
        }

        [Fact]
        public void RuledLineTableDetector_DetectsSimpleTable()
        {
            var detector = new RuledLineTableDetector(new RuledLineTableDetector.RuledLineTableDetectorOptions
            {
                MinHorizontalLines = 2,
                MinVerticalLines = 2,
                MinLineLength = 5
            });

            // Create a simple 2x2 table grid with lines
            var paths = new List<PdfPath>();

            // Add horizontal lines at y=0, y=50, y=100
            var path = new PdfPath();
            path.SetStroked();
            var horizontalSubpath1 = CreateHorizontalLine(0, 0, 100);
            var horizontalSubpath2 = CreateHorizontalLine(0, 50, 100);
            var horizontalSubpath3 = CreateHorizontalLine(0, 100, 100);
            path.Add(horizontalSubpath1);
            path.Add(horizontalSubpath2);
            path.Add(horizontalSubpath3);

            // Add vertical lines at x=0, x=50, x=100
            var verticalSubpath1 = CreateVerticalLine(0, 0, 100);
            var verticalSubpath2 = CreateVerticalLine(50, 0, 100);
            var verticalSubpath3 = CreateVerticalLine(100, 0, 100);
            path.Add(verticalSubpath1);
            path.Add(verticalSubpath2);
            path.Add(verticalSubpath3);

            paths.Add(path);

            var tables = detector.DetectTables(new List<Word>(), paths);

            Assert.Single(tables);
            Assert.Equal(2, tables[0].RowCount);
            Assert.Equal(2, tables[0].ColumnCount);
        }

        [Fact]
        public void RuledLineTableDetector_DefaultInstance_IsNotNull()
        {
            Assert.NotNull(RuledLineTableDetector.Instance);
        }

        private static PdfSubpath CreateHorizontalLine(double x, double y, double width)
        {
            var subpath = new PdfSubpath();
            subpath.MoveTo(x, y);
            subpath.LineTo(x + width, y);
            return subpath;
        }

        private static PdfSubpath CreateVerticalLine(double x, double y, double height)
        {
            var subpath = new PdfSubpath();
            subpath.MoveTo(x, y);
            subpath.LineTo(x, y + height);
            return subpath;
        }
    }
}
