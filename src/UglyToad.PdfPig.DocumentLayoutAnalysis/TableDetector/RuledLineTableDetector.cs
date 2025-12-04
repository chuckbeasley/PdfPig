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
    /// Detects tables in PDF pages using ruled lines (stroked paths).
    /// This detector works by identifying horizontal and vertical lines that form table grids.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Tables are detected by analyzing the stroked paths in a PDF page. Horizontal and vertical
    /// lines are identified based on angle tolerance, and their intersections define the table grid.
    /// </para>
    /// <para>
    /// Row indexing follows a top-to-bottom convention where row 0 is the topmost row.
    /// This differs from PDF's coordinate system where the origin is at the bottom-left.
    /// </para>
    /// </remarks>
    public class RuledLineTableDetector : ITableDetector
    {
        /// <summary>
        /// Options for the ruled-line table detector.
        /// </summary>
        public class RuledLineTableDetectorOptions
        {
            /// <summary>
            /// The minimum number of horizontal lines required to form a table.
            /// Default is 2.
            /// </summary>
            public int MinHorizontalLines { get; set; } = 2;

            /// <summary>
            /// The minimum number of vertical lines required to form a table.
            /// Default is 2.
            /// </summary>
            public int MinVerticalLines { get; set; } = 2;

            /// <summary>
            /// The tolerance for considering lines as horizontal or vertical (in degrees).
            /// Default is 5 degrees.
            /// </summary>
            public double AngleTolerance { get; set; } = 5.0;

            /// <summary>
            /// The tolerance for grouping lines at the same position (in points).
            /// Default is 3 points.
            /// </summary>
            public double PositionTolerance { get; set; } = 3.0;

            /// <summary>
            /// The minimum length for a line to be considered as a table line (in points).
            /// Default is 10 points.
            /// </summary>
            public double MinLineLength { get; set; } = 10.0;
        }

        private readonly RuledLineTableDetectorOptions options;

        /// <summary>
        /// The default instance of the <see cref="RuledLineTableDetector"/>.
        /// </summary>
        public static RuledLineTableDetector Instance { get; } = new RuledLineTableDetector();

        /// <summary>
        /// Creates a new <see cref="RuledLineTableDetector"/> with default options.
        /// </summary>
        public RuledLineTableDetector() : this(new RuledLineTableDetectorOptions())
        {
        }

        /// <summary>
        /// Creates a new <see cref="RuledLineTableDetector"/> with the specified options.
        /// </summary>
        /// <param name="options">The detector options.</param>
        public RuledLineTableDetector(RuledLineTableDetectorOptions options)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <inheritdoc />
        public IReadOnlyList<Table> DetectTables(IEnumerable<Word> words, IReadOnlyList<PdfPath>? paths = null)
        {
            if (paths == null || paths.Count == 0)
            {
                return Array.Empty<Table>();
            }

            var wordsList = words?.ToList() ?? new List<Word>();

            // Extract all line segments from paths
            var lines = ExtractLines(paths);

            if (lines.Count == 0)
            {
                return Array.Empty<Table>();
            }

            // Classify lines as horizontal or vertical
            var horizontalLines = lines.Where(l => IsHorizontal(l)).ToList();
            var verticalLines = lines.Where(l => IsVertical(l)).ToList();

            if (horizontalLines.Count < options.MinHorizontalLines ||
                verticalLines.Count < options.MinVerticalLines)
            {
                return Array.Empty<Table>();
            }

            // Find table regions by clustering intersecting lines
            var tableRegions = FindTableRegions(horizontalLines, verticalLines);

            // Build tables from regions
            var tables = new List<Table>();
            foreach (var region in tableRegions)
            {
                var table = BuildTable(region, wordsList);
                if (table != null)
                {
                    tables.Add(table);
                }
            }

            return tables;
        }

        private List<PdfSubpath.Line> ExtractLines(IReadOnlyList<PdfPath> paths)
        {
            var lines = new List<PdfSubpath.Line>();

            foreach (var path in paths)
            {
                if (!path.IsStroked)
                {
                    continue;
                }

                foreach (var subpath in path)
                {
                    foreach (var command in subpath.Commands)
                    {
                        if (command is PdfSubpath.Line line && line.Length >= options.MinLineLength)
                        {
                            lines.Add(line);
                        }
                    }
                }
            }

            return lines;
        }

        private bool IsHorizontal(PdfSubpath.Line line)
        {
            var angle = Math.Abs(Math.Atan2(line.To.Y - line.From.Y, line.To.X - line.From.X) * 180 / Math.PI);
            return angle <= options.AngleTolerance || Math.Abs(angle - 180) <= options.AngleTolerance;
        }

        private bool IsVertical(PdfSubpath.Line line)
        {
            var angle = Math.Abs(Math.Atan2(line.To.Y - line.From.Y, line.To.X - line.From.X) * 180 / Math.PI);
            return Math.Abs(angle - 90) <= options.AngleTolerance;
        }

        private class TableRegion
        {
            public List<PdfSubpath.Line> HorizontalLines { get; } = new List<PdfSubpath.Line>();
            public List<PdfSubpath.Line> VerticalLines { get; } = new List<PdfSubpath.Line>();
            public PdfRectangle BoundingBox { get; set; }
        }

        private List<TableRegion> FindTableRegions(List<PdfSubpath.Line> horizontalLines, List<PdfSubpath.Line> verticalLines)
        {
            var regions = new List<TableRegion>();

            // Get the bounding box of all lines
            var allLines = horizontalLines.Concat(verticalLines).ToList();
            if (allLines.Count == 0)
            {
                return regions;
            }

            var minX = allLines.Min(l => Math.Min(l.From.X, l.To.X));
            var maxX = allLines.Max(l => Math.Max(l.From.X, l.To.X));
            var minY = allLines.Min(l => Math.Min(l.From.Y, l.To.Y));
            var maxY = allLines.Max(l => Math.Max(l.From.Y, l.To.Y));

            // For now, treat all lines as belonging to a single table region
            // A more sophisticated algorithm would cluster lines based on intersection patterns
            var region = new TableRegion
            {
                BoundingBox = new PdfRectangle(minX, minY, maxX, maxY)
            };
            region.HorizontalLines.AddRange(horizontalLines);
            region.VerticalLines.AddRange(verticalLines);

            if (region.HorizontalLines.Count >= options.MinHorizontalLines &&
                region.VerticalLines.Count >= options.MinVerticalLines)
            {
                regions.Add(region);
            }

            return regions;
        }

        private Table? BuildTable(TableRegion region, List<Word> words)
        {
            // Get unique Y positions for rows (horizontal lines)
            var rowPositions = GetUniquePositions(
                region.HorizontalLines.SelectMany(l => new[] { l.From.Y, l.To.Y }),
                isVertical: false);

            // Get unique X positions for columns (vertical lines)
            var columnPositions = GetUniquePositions(
                region.VerticalLines.SelectMany(l => new[] { l.From.X, l.To.X }),
                isVertical: true);

            if (rowPositions.Count < 2 || columnPositions.Count < 2)
            {
                return null;
            }

            // Sort positions
            rowPositions.Sort();
            columnPositions.Sort();

            // Create cells from grid
            var cells = new List<TableCell>();
            int rowCount = rowPositions.Count - 1;
            int columnCount = columnPositions.Count - 1;

            for (int row = 0; row < rowCount; row++)
            {
                for (int col = 0; col < columnCount; col++)
                {
                    var cellLeft = columnPositions[col];
                    var cellRight = columnPositions[col + 1];
                    var cellBottom = rowPositions[row];
                    var cellTop = rowPositions[row + 1];

                    var cellBounds = new PdfRectangle(cellLeft, cellBottom, cellRight, cellTop);

                    // Find words that belong to this cell
                    var cellWords = words.Where(w => IsWordInCell(w, cellBounds)).ToList();

                    // Note: Row index 0 is the bottom row in PDF coordinates,
                    // but we want 0 to be the top row for user convenience
                    var adjustedRowIndex = rowCount - 1 - row;

                    cells.Add(new TableCell(cellBounds, adjustedRowIndex, col, cellWords));
                }
            }

            if (cells.Count == 0)
            {
                return null;
            }

            return new Table(region.BoundingBox, cells);
        }

        private List<double> GetUniquePositions(IEnumerable<double> positions, bool isVertical)
        {
            var sorted = positions.OrderBy(p => p).ToList();
            var unique = new List<double>();

            foreach (var pos in sorted)
            {
                if (unique.Count == 0 || Math.Abs(pos - unique[unique.Count - 1]) > options.PositionTolerance)
                {
                    unique.Add(pos);
                }
            }

            return unique;
        }

        private bool IsWordInCell(Word word, PdfRectangle cellBounds)
        {
            // Check if the word's center is within the cell bounds
            var wordCenter = word.BoundingBox.Centroid;
            return wordCenter.X >= cellBounds.Left &&
                   wordCenter.X <= cellBounds.Right &&
                   wordCenter.Y >= cellBounds.Bottom &&
                   wordCenter.Y <= cellBounds.Top;
        }
    }
}
