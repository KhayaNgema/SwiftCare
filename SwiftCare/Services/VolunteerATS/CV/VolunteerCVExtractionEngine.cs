using System;
using System.IO;
using System.Text;
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

        public async Task<string> ExtractTextAsync(
            Stream documentStream,
            string fileName)
        {
            if (documentStream == null)
                throw new ArgumentNullException(
                    nameof(documentStream));

            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException(
                    "CV file name is required.",
                    nameof(fileName));

            if (!_documentEngine.IsSupported(fileName))
            {
                throw new NotSupportedException(
                    $"Unsupported CV document type: {fileName}");
            }

            if (documentStream.CanSeek)
                documentStream.Position = 0;

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
                var pageText = page.Text;

                if (string.IsNullOrWhiteSpace(pageText))
                    continue;

                builder.AppendLine(pageText);
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
                return string.Empty;

            var builder =
                new StringBuilder();

            // --------------------------------------------------------
            // CVs can contain both paragraphs and tables.
            // Process the document in its actual body order.
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
                return;

            var text =
                paragraph.InnerText;

            if (string.IsNullOrWhiteSpace(text))
                return;

            builder.AppendLine(
                text.Trim());
        }

        // ============================================================
        // TABLE
        // ============================================================

        private static void AppendTable(
            StringBuilder builder,
            Table table)
        {
            if (table == null)
                return;

            foreach (var row in
                     table.Elements<TableRow>())
            {
                foreach (var cell in
                         row.Elements<TableCell>())
                {
                    var text =
                        cell.InnerText;

                    if (string.IsNullOrWhiteSpace(text))
                        continue;

                    builder.AppendLine(
                        text.Trim());
                }
            }
        }

        // ============================================================
        // CLEAN EXTRACTED TEXT
        // ============================================================

        private static string CleanExtractedText(
            string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

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
                    continue;

                var line =
                    rawLine.Trim();

                // Normalize whitespace.
                line =
                    System.Text.RegularExpressions.Regex
                        .Replace(
                            line,
                            @"\s+",
                            " ");

                // Normalize common bullet characters.
                line = line
                    .Replace("▪", "•")
                    .Replace("●", "•")
                    .Replace("◦", "•");

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                builder.AppendLine(line);
            }

            return builder
                .ToString()
                .Trim();
        }
    }
}