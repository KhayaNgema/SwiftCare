using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SwiftCare.Services.VolunteerATS.CV
{
    // ============================================================
    // AZURE DOCUMENT INTELLIGENCE SERVICE
    // ============================================================

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
        // --------------------------------------------------------
        // EXTRACTION STATUS
        // --------------------------------------------------------

        public bool Success { get; set; }

        public string? ExtractionMessage { get; set; }

        public string? ErrorMessage { get; set; }

        // --------------------------------------------------------
        // RAW AZURE CONTENT
        // --------------------------------------------------------

        public string RawText { get; set; }
            = string.Empty;

        public string ContentFormat { get; set; }
            = string.Empty;

        // --------------------------------------------------------
        // CUSTOM MODEL FIELDS
        // --------------------------------------------------------
        //
        // Examples:
        //
        // FullName
        // Email
        // PhoneNumber
        // ProfessionalTitle
        // ProfessionalSummary
        // CareerObjective
        // Address
        // City
        // Province
        // Country
        // LinkedInUrl
        // PortfolioUrl
        // GitHubUrl
        //
        // Repeating:
        //
        // Skills
        // Experience
        // Education
        // Certifications
        // Languages
        //
        // Azure Document Intelligence returns named fields
        // from the custom model.
        // --------------------------------------------------------

        public Dictionary<string, AzureCvField> Fields { get; set; }
            = new(StringComparer.OrdinalIgnoreCase);

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
        // WARNINGS
        // --------------------------------------------------------

        public List<string> Warnings { get; set; }
            = new();
    }

    // ============================================================
    // CUSTOM AZURE FIELD
    // ============================================================

    public class AzureCvField
    {
        /// <summary>
        /// Human-readable content returned by Azure.
        ///
        /// Examples:
        /// "Thando Mkhize"
        /// "thando.mkhize@example.com"
        /// "Elderly Resident Support & Activities Volunteer"
        /// </summary>
        public string Content { get; set; }
            = string.Empty;

        /// <summary>
        /// Azure field type.
        ///
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

        // --------------------------------------------------------
        // NESTED OBJECT VALUES
        // --------------------------------------------------------
        //
        // Example:
        //
        // Experience
        //   Employer
        //   Position
        //   StartDate
        //   EndDate
        //   Description
        //
        // Education
        //   Institution
        //   Qualification
        //   FieldOfStudy
        //   StartDate
        //   EndDate
        // --------------------------------------------------------

        public Dictionary<string, AzureCvField> ObjectValues { get; set; }
            = new(StringComparer.OrdinalIgnoreCase);

        // --------------------------------------------------------
        // ARRAY / REPEATING VALUES
        // --------------------------------------------------------
        //
        // Examples:
        //
        // Skills
        // Experience
        // Education
        // Certifications
        // Languages
        // --------------------------------------------------------

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