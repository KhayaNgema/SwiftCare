using Microsoft.AspNetCore.Http;

namespace SwiftCare.Services.VolunteerATS.CV
{
    public interface IAzureDocumentIntelligenceCvService
    {
        Task<AzureCvExtractionResult> AnalyzeAsync(
            IFormFile file,
            CancellationToken cancellationToken = default);
    }

    // ============================================================
    // MAIN AZURE CV EXTRACTION RESULT
    // ============================================================

    public class AzureCvExtractionResult
    {
        public bool Success { get; set; }

        public string RawText { get; set; }
            = string.Empty;

        public string ContentFormat { get; set; }
            = string.Empty;

        // --------------------------------------------------------
        // CUSTOM MODEL FIELDS
        // --------------------------------------------------------

        public Dictionary<string, AzureCvField> Fields { get; set; }
            = new();

        // --------------------------------------------------------
        // LAYOUT INFORMATION
        // --------------------------------------------------------

        public List<AzureCvParagraph> Paragraphs { get; set; }
            = new();

        public List<AzureCvSection> Sections { get; set; }
            = new();

        public List<AzureCvTable> Tables { get; set; }
            = new();

        // --------------------------------------------------------
        // WARNINGS / ERRORS
        // --------------------------------------------------------

        public List<string> Warnings { get; set; }
            = new();

        public string? ErrorMessage { get; set; }
    }

    // ============================================================
    // CUSTOM AZURE FIELD
    // ============================================================

    public class AzureCvField
    {
        /// <summary>
        /// Human-readable content returned by Azure.
        /// Example:
        /// "John Doe"
        /// "john@example.com"
        /// "C#"
        /// </summary>
        public string Content { get; set; }
            = string.Empty;

        /// <summary>
        /// Azure field type.
        /// Examples:
        /// string
        /// array
        /// object
        /// date
        /// phoneNumber
        /// </summary>
        public string FieldType { get; set; }
            = string.Empty;

        /// <summary>
        /// Azure confidence score.
        /// </summary>
        public float? Confidence { get; set; }

        /// <summary>
        /// Nested object fields.
        ///
        /// Example:
        ///
        /// Experience
        ///   Employer
        ///   Position
        ///   StartDate
        ///   EndDate
        ///   Description
        /// </summary>
        public Dictionary<string, AzureCvField> ObjectValues { get; set; }
            = new();

        /// <summary>
        /// Array/repeating fields.
        ///
        /// Example:
        ///
        /// Skills
        /// Experience
        /// Education
        /// Certifications
        /// Languages
        /// </summary>
        public List<AzureCvField> ArrayValues { get; set; }
            = new();
    }

    // ============================================================
    // PARAGRAPH
    // ============================================================

    public class AzureCvParagraph
    {
        public string Content { get; set; }
            = string.Empty;

        public string? Role { get; set; }
    }

    // ============================================================
    // SECTION
    // ============================================================

    public class AzureCvSection
    {
        public string? Role { get; set; }

        public List<string> Paragraphs { get; set; }
            = new();
    }

    // ============================================================
    // TABLE
    // ============================================================

    public class AzureCvTable
    {
        public int RowCount { get; set; }

        public int ColumnCount { get; set; }

        public List<AzureCvTableCell> Cells { get; set; }
            = new();
    }

    // ============================================================
    // TABLE CELL
    // ============================================================

    public class AzureCvTableCell
    {
        public int RowIndex { get; set; }

        public int ColumnIndex { get; set; }

        public string Content { get; set; }
            = string.Empty;
    }
}