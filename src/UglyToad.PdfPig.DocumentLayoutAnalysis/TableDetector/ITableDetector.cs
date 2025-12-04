#nullable enable

namespace UglyToad.PdfPig.DocumentLayoutAnalysis.TableDetector
{
    using System.Collections.Generic;
    using UglyToad.PdfPig.Content;
    using UglyToad.PdfPig.Graphics;

    /// <summary>
    /// Interface for table detection algorithms.
    /// </summary>
    public interface ITableDetector
    {
        /// <summary>
        /// Detects tables in a PDF page.
        /// </summary>
        /// <param name="words">The words on the page.</param>
        /// <param name="paths">The paths (lines) on the page, used for ruled-line table detection.</param>
        /// <returns>A list of detected tables.</returns>
        IReadOnlyList<Table> DetectTables(IEnumerable<Word> words, IReadOnlyList<PdfPath>? paths = null);
    }
}
