#nullable enable

namespace UglyToad.PdfPig.DocumentLayoutAnalysis
{
    using UglyToad.PdfPig.Content;
    using UglyToad.PdfPig.DocumentLayoutAnalysis.TableDetection;
    using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

    /// <summary>
    /// Extension methods for integrating table detection with text extraction.
    /// Tables are detected and their content is formatted inline with regular text,
    /// maintaining the natural reading order of the document.
    /// </summary>
    public static class TableAwarePageExtensions
    {
        /// <summary>
        /// Extracts text from the page with awareness of table structures.
        /// Tables are integrated inline with the document text flow in reading order.
        /// </summary>
        /// <param name="page">The page to extract text from.</param>
        /// <param name="options">Options controlling the extraction. If null, defaults are used.</param>
        /// <returns>The extraction result containing formatted text and detected tables.</returns>
        public static TableAwareTextExtractor.ExtractionResult GetTextWithTables(
            this Page page,
            TableAwareTextExtractor.Options? options = null)
        {
            return TableAwareTextExtractor.GetText(page, options);
        }

        /// <summary>
        /// Extracts text from the page with awareness of table structures using the ruled-line table detector.
        /// Tables are integrated inline with the document text flow in reading order.
        /// </summary>
        /// <param name="page">The page to extract text from.</param>
        /// <param name="detectorOptions">Options for the ruled-line table detector. If null, defaults are used.</param>
        /// <returns>The extraction result containing formatted text and detected tables.</returns>
        public static TableAwareTextExtractor.ExtractionResult GetTextWithRuledLineTables(
            this Page page,
            RuledLineTableDetector.RuledLineTableDetectorOptions? detectorOptions = null)
        {
            var detector = detectorOptions != null
                ? new RuledLineTableDetector(detectorOptions)
                : RuledLineTableDetector.Instance;

            return TableAwareTextExtractor.GetText(page, new TableAwareTextExtractor.Options
            {
                TableDetector = detector
            });
        }

        /// <summary>
        /// Extracts text from the page with awareness of table structures using the whitespace table detector.
        /// Tables are integrated inline with the document text flow in reading order.
        /// </summary>
        /// <param name="page">The page to extract text from.</param>
        /// <param name="detectorOptions">Options for the whitespace table detector. If null, defaults are used.</param>
        /// <returns>The extraction result containing formatted text and detected tables.</returns>
        public static TableAwareTextExtractor.ExtractionResult GetTextWithWhitespaceTables(
            this Page page,
            WhitespaceTableDetector.WhitespaceTableDetectorOptions? detectorOptions = null)
        {
            var detector = detectorOptions != null
                ? new WhitespaceTableDetector(detectorOptions)
                : WhitespaceTableDetector.Instance;

            return TableAwareTextExtractor.GetText(page, new TableAwareTextExtractor.Options
            {
                TableDetector = detector
            });
        }
    }
}
