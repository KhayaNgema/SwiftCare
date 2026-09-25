using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using UglyToad.PdfPig;
using SwiftCare.Services.VolunteerATS.CV;

namespace SwiftCare.Services.VolunteerATS.CV
{
    public class VolunteerCVExtractionEngine
        : IVolunteerCVExtractionEngine
    {
        private readonly IVolunteerCVDocumentEngine _documentEngine;

        public VolunteerCVExtractionEngine(
            IVolunteerCVDocumentEngine documentEngine)
        {
            _documentEngine = documentEngine
                ?? throw new ArgumentNullException(
                    nameof(documentEngine));
        }

        // ============================================================
        // EXTRACT TEXT
        // ============================================================

        public async Task<string> ExtractTextAsync(
            Stream documentStream,
            string fileName)
        {
            if (documentStream == null)
            {
                throw new ArgumentNullException(
                    nameof(documentStream));
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException(
                    "CV file name is required.",
                    nameof(fileName));
            }

            if (!_documentEngine.IsSupported(fileName))
            {
                throw new NotSupportedException(
                    $"Unsupported CV document type: {fileName}");
            }

            if (documentStream.CanSeek)
            {
                documentStream.Position = 0;
            }

            string extractedText;

            if (_documentEngine.IsPdf(fileName))
            {
                extractedText =
                    await ExtractPdfAsync(documentStream);
            }
            else if (_documentEngine.IsDocx(fileName))
            {
                extractedText =
                    await ExtractDocxAsync(documentStream);
            }
            else
            {
                throw new NotSupportedException(
                    $"Unsupported CV document type: " +
                    $"{_documentEngine.GetDocumentType(fileName)}");
            }

            return CleanExtractedText(extractedText);
        }

        // ============================================================
        // PDF EXTRACTION
        // ============================================================

        private static async Task<string> ExtractPdfAsync(
            Stream documentStream)
        {
            using var memoryStream =
                new MemoryStream();

            await documentStream.CopyToAsync(
                memoryStream);

            memoryStream.Position = 0;

            var builder =
                new StringBuilder();

            using var pdf =
                PdfDocument.Open(memoryStream);

            foreach (var page in pdf.GetPages())
            {
                var pageText =
                    page.Text;

                if (string.IsNullOrWhiteSpace(pageText))
                {
                    continue;
                }

                // ----------------------------------------------------
                // Keep the page as a logical section.
                //
                // We do NOT split the page into "experiences".
                // PDF extraction is only responsible for recovering
                // the document text.
                // ----------------------------------------------------

                builder.AppendLine(
                    pageText.Trim());

                builder.AppendLine();
            }

            return builder.ToString();
        }

        // ============================================================
        // DOCX EXTRACTION
        // ============================================================

        private static async Task<string> ExtractDocxAsync(
            Stream documentStream)
        {
            using var memoryStream =
                new MemoryStream();

            await documentStream.CopyToAsync(
                memoryStream);

            memoryStream.Position = 0;

            using var document =
                WordprocessingDocument.Open(
                    memoryStream,
                    false);

            var body =
                document.MainDocumentPart?
                    .Document?
                    .Body;

            if (body == null)
            {
                return string.Empty;
            }

            var builder =
                new StringBuilder();

            // --------------------------------------------------------
            // IMPORTANT
            //
            // Process paragraphs and tables in their actual document
            // order.
            //
            // A CV frequently stores work experience inside tables.
            // Flattening every table cell into an independent line can
            // destroy the relationship between:
            //
            // Job Title
            // Organisation
            // Dates
            // Description
            //
            // We therefore preserve table rows as logical records.
            // --------------------------------------------------------

            foreach (var element in body.Elements())
            {
                if (element is Paragraph paragraph)
                {
                    AppendParagraph(
                        builder,
                        paragraph);
                }
                else if (element is Table table)
                {
                    AppendTable(
                        builder,
                        table);
                }
            }

            return builder.ToString();
        }

        // ============================================================
        // PARAGRAPH
        // ============================================================

        private static void AppendParagraph(
            StringBuilder builder,
            Paragraph paragraph)
        {
            if (paragraph == null)
            {
                return;
            }

            var text =
                paragraph.InnerText;

            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            text =
                CleanLine(text);

            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            builder.AppendLine(text);
        }

        // ============================================================
        // TABLE
        // ============================================================

        private static void AppendTable(
            StringBuilder builder,
            Table table)
        {
            if (table == null)
            {
                return;
            }

            foreach (var row in table.Elements<TableRow>())
            {
                var cells =
                    new StringBuilder();

                foreach (var cell in row.Elements<TableCell>())
                {
                    var cellText =
                        ExtractTableCellText(cell);

                    if (string.IsNullOrWhiteSpace(cellText))
                    {
                        continue;
                    }

                    if (cells.Length > 0)
                    {
                        // ------------------------------------------------
                        // IMPORTANT:
                        //
                        // Keep cells from the SAME ROW together.
                        //
                        // Example:
                        //
                        // Care Assistant | Sunrise Care Home |
                        // January 2023 - December 2024
                        //
                        // instead of:
                        //
                        // Care Assistant
                        // Sunrise Care Home
                        // January 2023 - December 2024
                        //
                        // This preserves the relationship between the
                        // fields and prevents downstream parsers from
                        // interpreting every cell as an independent
                        // experience.
                        // ------------------------------------------------

                        cells.Append(" | ");
                    }

                    cells.Append(cellText);
                }

                var rowText =
                    cells.ToString().Trim();

                if (string.IsNullOrWhiteSpace(rowText))
                {
                    continue;
                }

                builder.AppendLine(rowText);
            }

            // --------------------------------------------------------
            // Separate this table from the next document section.
            // --------------------------------------------------------

            builder.AppendLine();
        }

        // ============================================================
        // TABLE CELL TEXT
        // ============================================================

        private static string ExtractTableCellText(
            TableCell cell)
        {
            if (cell == null)
            {
                return string.Empty;
            }

            var builder =
                new StringBuilder();

            // --------------------------------------------------------
            // A cell itself can contain multiple paragraphs.
            //
            // Those paragraphs belong to the SAME CELL.
            // They must not become separate experiences.
            // --------------------------------------------------------

            foreach (var paragraph in
                     cell.Elements<Paragraph>())
            {
                var paragraphText =
                    paragraph.InnerText;

                if (string.IsNullOrWhiteSpace(paragraphText))
                {
                    continue;
                }

                paragraphText =
                    CleanLine(paragraphText);

                if (string.IsNullOrWhiteSpace(paragraphText))
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append(" ");
                }

                builder.Append(
                    paragraphText);
            }

            // --------------------------------------------------------
            // Some DOCX files contain nested tables.
            // Include their content as part of the same cell.
            // --------------------------------------------------------

            foreach (var nestedTable in
                     cell.Elements<Table>())
            {
                foreach (var row in
                         nestedTable.Elements<TableRow>())
                {
                    var rowCells =
                        new StringBuilder();

                    foreach (var nestedCell in
                             row.Elements<TableCell>())
                    {
                        var nestedText =
                            ExtractTableCellText(
                                nestedCell);

                        if (string.IsNullOrWhiteSpace(nestedText))
                        {
                            continue;
                        }

                        if (rowCells.Length > 0)
                        {
                            rowCells.Append(" | ");
                        }

                        rowCells.Append(
                            nestedText);
                    }

                    var nestedRowText =
                        rowCells.ToString().Trim();

                    if (string.IsNullOrWhiteSpace(nestedRowText))
                    {
                        continue;
                    }

                    if (builder.Length > 0)
                    {
                        builder.Append(" ");
                    }

                    builder.Append(
                        nestedRowText);
                }
            }

            return builder
                .ToString()
                .Trim();
        }

        // ============================================================
        // CLEAN EXTRACTED TEXT
        // ============================================================

        private static string CleanExtractedText(
            string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            text = text
                .Replace("\r\n", "\n")
                .Replace("\r", "\n")
                .Replace("\u00A0", " ");

            var lines =
                text.Split(
                    '\n',
                    StringSplitOptions.None);

            var builder =
                new StringBuilder();

            foreach (var rawLine in lines)
            {
                if (string.IsNullOrWhiteSpace(rawLine))
                {
                    continue;
                }

                var line =
                    CleanLine(rawLine);

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                // ----------------------------------------------------
                // Avoid excessive duplicate blank separators.
                // ----------------------------------------------------

                if (line == "__SWIFTCARE_SECTION_BREAK__")
                {
                    if (builder.Length == 0)
                    {
                        continue;
                    }

                    var current =
                        builder.ToString();

                    if (current.EndsWith(
                            Environment.NewLine +
                            Environment.NewLine,
                            StringComparison.Ordinal))
                    {
                        continue;
                    }

                    builder.AppendLine();
                    continue;
                }

                builder.AppendLine(line);
            }

            return builder
                .ToString()
                .Trim();
        }

        // ============================================================
        // CLEAN SINGLE LINE
        // ============================================================

        private static string CleanLine(
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var line =
                value
                    .Replace("\r\n", " ")
                    .Replace("\r", " ")
                    .Replace("\n", " ")
                    .Replace("\u00A0", " ");

            // --------------------------------------------------------
            // Normalize common bullet characters.
            // --------------------------------------------------------

            line = line
                .Replace("▪", "•")
                .Replace("●", "•")
                .Replace("◦", "•")
                .Replace("‣", "•")
                .Replace("⁃", "•")
                .Replace("∙", "•");

            // --------------------------------------------------------
            // Normalize tabs.
            // --------------------------------------------------------

            line =
                line.Replace("\t", " ");

            // --------------------------------------------------------
            // Normalize repeated whitespace.
            // --------------------------------------------------------

            line =
                Regex.Replace(
                    line,
                    @"[ ]{2,}",
                    " ");

            return line.Trim();
        }
    }
}