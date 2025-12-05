#nullable enable

namespace UglyToad.PdfPig.DocumentLayoutAnalysis.TableDetector
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UglyToad.PdfPig.Content;
    using UglyToad.PdfPig.Graphics;

    /// <summary>
    /// Automatically selects the appropriate table detection algorithm based on the content.
    /// This detector first tries ruled-line detection (for tables with visible borders/lines),
    /// and falls back to whitespace-based detection if no ruled-line tables are found.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This approach prioritizes ruled-line detection because it typically provides more accurate
    /// results when table borders are present in the PDF. Whitespace detection is used as a fallback
    /// for tables that use only spacing to separate columns.
    /// </para>
    /// <para>
    /// Row indexing follows a top-to-bottom convention where row 0 is the topmost row.
    /// </para>
    /// </remarks>
    public class AutomaticTableDetector : ITableDetector
    {
        /// <summary>
        /// Options for the automatic table detector.
        /// </summary>
        public class AutomaticTableDetectorOptions
        {
            /// <summary>
            /// Options for the ruled-line table detector.
            /// If null, default options are used.
            /// </summary>
            public RuledLineTableDetector.RuledLineTableDetectorOptions? RuledLineOptions { get; set; }

            /// <summary>
            /// Options for the whitespace table detector.
            /// If null, default options are used.
            /// </summary>
            public WhitespaceTableDetector.WhitespaceTableDetectorOptions? WhitespaceOptions { get; set; }

            /// <summary>
            /// Whether to use whitespace detection as a fallback when no ruled-line tables are found.
            /// Default is true.
            /// </summary>
            public bool UseWhitespaceFallback { get; set; } = true;
        }

        private readonly RuledLineTableDetector ruledLineDetector;
        private readonly WhitespaceTableDetector whitespaceDetector;
        private readonly AutomaticTableDetectorOptions options;

        /// <summary>
        /// The default instance of the <see cref="AutomaticTableDetector"/>.
        /// </summary>
        public static AutomaticTableDetector Instance { get; } = new AutomaticTableDetector();

        /// <summary>
        /// Creates a new <see cref="AutomaticTableDetector"/> with default options.
        /// </summary>
        public AutomaticTableDetector() : this(new AutomaticTableDetectorOptions())
        {
        }

        /// <summary>
        /// Creates a new <see cref="AutomaticTableDetector"/> with the specified options.
        /// </summary>
        /// <param name="options">The detector options.</param>
        public AutomaticTableDetector(AutomaticTableDetectorOptions options)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));

            ruledLineDetector = options.RuledLineOptions != null
                ? new RuledLineTableDetector(options.RuledLineOptions)
                : RuledLineTableDetector.Instance;

            whitespaceDetector = options.WhitespaceOptions != null
                ? new WhitespaceTableDetector(options.WhitespaceOptions)
                : WhitespaceTableDetector.Instance;
        }

        /// <inheritdoc />
        public IReadOnlyList<Table> DetectTables(IEnumerable<Word> words, IReadOnlyList<PdfPath>? paths = null)
        {
            var wordsList = words?.ToList() ?? new List<Word>();

            // First, try ruled-line detection if paths are available
            if (paths != null && paths.Count > 0)
            {
                var ruledLineTables = ruledLineDetector.DetectTables(wordsList, paths);
                if (ruledLineTables.Count > 0)
                {
                    return ruledLineTables;
                }
            }

            // Fall back to whitespace detection if enabled and no ruled-line tables were found
            if (options.UseWhitespaceFallback)
            {
                return whitespaceDetector.DetectTables(wordsList, paths);
            }

            return Array.Empty<Table>();
        }
    }
}
