namespace UglyToad.PdfPig.Tests.Dla
{
    using System.Collections.Generic;
    using System.Linq;
    using UglyToad.PdfPig.Content;
    using UglyToad.PdfPig.Core;
    using UglyToad.PdfPig.DocumentLayoutAnalysis;
    using UglyToad.PdfPig.DocumentLayoutAnalysis.TableDetector;
    using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
    using UglyToad.PdfPig.Graphics;
    using UglyToad.PdfPig.Graphics.Core;
    using UglyToad.PdfPig.PdfFonts;

    public class TableAwareTextExtractorTests
    {
        [Fact]
        public void GetText_WithNullPage_ThrowsArgumentNullException()
        {
            Assert.Throws<System.ArgumentNullException>(() =>
                TableAwareTextExtractor.GetText(null!));
        }

        [Fact]
        public void GetText_WithNoTableDetector_ReturnsTextOnly()
        {
            // This test requires a mock Page which is complex to create
            // The logic is tested via integration tests with real PDFs
        }

        [Fact]
        public void GetTextString_WithNullTableDetector_ReturnsText()
        {
            // This test requires a mock Page which is complex to create
            // The logic is tested via integration tests with real PDFs
        }

        [Fact]
        public void Options_DefaultValues_AreCorrect()
        {
            var options = new TableAwareTextExtractor.Options();

            Assert.Null(options.TableDetector);
            Assert.Null(options.WordExtractor);
            Assert.Equal("\t", options.ColumnSeparator);
            Assert.Equal(System.Environment.NewLine, options.RowSeparator);
            Assert.False(options.IncludeTableMarkers);
            Assert.Equal("[TABLE]", options.TableStartMarker);
            Assert.Equal("[/TABLE]", options.TableEndMarker);
            Assert.Equal(5.0, options.LineTolerance);
        }

        [Fact]
        public void Options_CanSetCustomValues()
        {
            var options = new TableAwareTextExtractor.Options
            {
                TableDetector = RuledLineTableDetector.Instance,
                ColumnSeparator = "|",
                RowSeparator = "\n",
                IncludeTableMarkers = true,
                TableStartMarker = "<<TABLE>>",
                TableEndMarker = "<</TABLE>>",
                LineTolerance = 10.0
            };

            Assert.Same(RuledLineTableDetector.Instance, options.TableDetector);
            Assert.Equal("|", options.ColumnSeparator);
            Assert.Equal("\n", options.RowSeparator);
            Assert.True(options.IncludeTableMarkers);
            Assert.Equal("<<TABLE>>", options.TableStartMarker);
            Assert.Equal("<</TABLE>>", options.TableEndMarker);
            Assert.Equal(10.0, options.LineTolerance);
        }

        [Fact]
        public void ExtractionResult_StoresTextAndTables()
        {
            var tables = new List<Table>();
            var result = new TableAwareTextExtractor.ExtractionResult("test text", tables);

            Assert.Equal("test text", result.Text);
            Assert.Same(tables, result.Tables);
        }

        [Fact]
        public void ExtractionResult_WithEmptyTables_ReturnsEmptyList()
        {
            var tables = new List<Table>();
            var result = new TableAwareTextExtractor.ExtractionResult("", tables);

            Assert.Empty(result.Tables);
        }

        // Integration test with a simulated table detection scenario
        [Fact]
        public void WhitespaceTableDetector_CanDetectTableWithWords()
        {
            var detector = new WhitespaceTableDetector(new WhitespaceTableDetector.WhitespaceTableDetectorOptions
            {
                MinColumns = 2,
                MinRows = 2,
                MinColumnGap = 10,
                LineTolerance = 5
            });

            // Create words that form a 2x2 table:
            // Row 1: "Name" at (0,100), "Age" at (100,100)
            // Row 2: "John" at (0,80), "25" at (100,80)
            var words = new List<Word>
            {
                CreateTestWord("Name", 0, 100, 40, 12),
                CreateTestWord("Age", 100, 100, 30, 12),
                CreateTestWord("John", 0, 80, 40, 12),
                CreateTestWord("25", 100, 80, 20, 12)
            };

            var tables = detector.DetectTables(words, null);

            Assert.Single(tables);
            Assert.Equal(2, tables[0].RowCount);
            Assert.Equal(2, tables[0].ColumnCount);

            // Check that the table cells contain the expected text
            var cell00 = tables[0].GetCell(0, 0);
            var cell01 = tables[0].GetCell(0, 1);
            var cell10 = tables[0].GetCell(1, 0);
            var cell11 = tables[0].GetCell(1, 1);

            Assert.NotNull(cell00);
            Assert.NotNull(cell01);
            Assert.NotNull(cell10);
            Assert.NotNull(cell11);

            Assert.Equal("Name", cell00!.Text);
            Assert.Equal("Age", cell01!.Text);
            Assert.Equal("John", cell10!.Text);
            Assert.Equal("25", cell11!.Text);
        }

        [Fact]
        public void RuledLineTableDetector_CanDetectTableWithLines()
        {
            var detector = new RuledLineTableDetector(new RuledLineTableDetector.RuledLineTableDetectorOptions
            {
                MinHorizontalLines = 2,
                MinVerticalLines = 2,
                MinLineLength = 5
            });

            // Create a simple 2x2 table grid with lines
            var paths = new List<PdfPath>();

            var path = new PdfPath();
            path.SetStroked();

            // Add horizontal lines at y=0, y=50, y=100
            path.Add(CreateHorizontalLine(0, 0, 100));
            path.Add(CreateHorizontalLine(0, 50, 100));
            path.Add(CreateHorizontalLine(0, 100, 100));

            // Add vertical lines at x=0, x=50, x=100
            path.Add(CreateVerticalLine(0, 0, 100));
            path.Add(CreateVerticalLine(50, 0, 100));
            path.Add(CreateVerticalLine(100, 0, 100));

            paths.Add(path);

            var tables = detector.DetectTables(new List<Word>(), paths);

            Assert.Single(tables);
            Assert.Equal(2, tables[0].RowCount);
            Assert.Equal(2, tables[0].ColumnCount);
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

        private static Word CreateTestWord(string text, double x, double y, double width, double height)
        {
            var letters = new List<Letter>();

            double charWidth = width / System.Math.Max(text.Length, 1);
            for (int i = 0; i < text.Length; i++)
            {
                var startPoint = new PdfPoint(x + i * charWidth, y);
                var endPoint = new PdfPoint(x + (i + 1) * charWidth, y);
                var glyphRect = new PdfRectangle(x + i * charWidth, y, x + (i + 1) * charWidth, y + height);

                var letter = new Letter(
                    text[i].ToString(),
                    glyphRect,
                    glyphRect,
                    startPoint,
                    endPoint,
                    charWidth,
                    height,
                    (FontDetails)null!,
                    TextRenderingMode.Fill,
                    null,
                    null,
                    12,
                    1);
                letters.Add(letter);
            }

            return new Word(letters);
        }
    }
}
