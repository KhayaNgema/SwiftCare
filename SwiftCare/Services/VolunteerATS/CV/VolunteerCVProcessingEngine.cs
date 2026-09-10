using Microsoft.AspNetCore.Http;
using SwiftCare.Models.VolunteerCV;

namespace SwiftCare.Services.VolunteerATS.CV
{
    public class VolunteerCVProcessingEngine
        : IVolunteerCVProcessingEngine
    {
        private readonly IVolunteerCVDocumentEngine
            _documentEngine;

        private readonly IAzureDocumentIntelligenceCvService
            _azureCvService;

        private readonly IVolunteerCVParsingEngine
            _parsingEngine;

        public VolunteerCVProcessingEngine(
            IVolunteerCVDocumentEngine documentEngine,
            IAzureDocumentIntelligenceCvService azureCvService,
            IVolunteerCVParsingEngine parsingEngine)
        {
            _documentEngine =
                documentEngine;

            _azureCvService =
                azureCvService;

            _parsingEngine =
                parsingEngine;
        }

        public async Task<ParsedCv> ProcessAsync(
            Stream documentStream,
            string fileName,
            int volunteerApplicationId = 0)
        {
            // ============================================================
            // 1. CHECK FILE FORMAT
            // ============================================================

            if (!_documentEngine.IsSupported(fileName))
            {
                return new ParsedCv
                {
                    VolunteerApplicationId =
                        volunteerApplicationId,

                    ExtractionSuccessful =
                        false,

                    ExtractionMessage =
                        "Unsupported CV format."
                };
            }

            // ============================================================
            // 2. COPY STREAM TO IFORMFILE
            // ============================================================

            if (documentStream == null)
            {
                return new ParsedCv
                {
                    VolunteerApplicationId =
                        volunteerApplicationId,

                    ExtractionSuccessful =
                        false,

                    ExtractionMessage =
                        "CV document stream is null."
                };
            }

            if (documentStream.CanSeek)
            {
                documentStream.Position = 0;
            }

            using var memoryStream =
                new MemoryStream();

            await documentStream.CopyToAsync(
                memoryStream);

            memoryStream.Position = 0;

            // ============================================================
            // 3. CREATE IFORMFILE
            // ============================================================

            var formFile =
                new FormFile(
                    memoryStream,
                    0,
                    memoryStream.Length,
                    "file",
                    fileName)
                {
                    Headers =
                        new HeaderDictionary(),

                    ContentType =
                        GetContentType(fileName)
                };

            // ============================================================
            // 4. SEND DOCUMENT TO AZURE DOCUMENT INTELLIGENCE
            // ============================================================

            var azureResult =
                await _azureCvService.AnalyzeAsync(
                    formFile);

            // ============================================================
            // 5. AZURE EXTRACTION FAILED
            // ============================================================

            if (azureResult == null)
            {
                return new ParsedCv
                {
                    VolunteerApplicationId =
                        volunteerApplicationId,

                    ExtractionSuccessful =
                        false,

                    ExtractionMessage =
                        "Azure Document Intelligence returned no result."
                };
            }

            if (!azureResult.Success)
            {
                return new ParsedCv
                {
                    VolunteerApplicationId =
                        volunteerApplicationId,

                    RawText =
                        azureResult.RawText ?? string.Empty,

                    ExtractionSuccessful =
                        false,

                    ExtractionMessage =
                        azureResult.ExtractionMessage
                        ?? "Azure CV extraction failed."
                };
            }

            // ============================================================
            // 6. LOG AZURE EXTRACTION
            // ============================================================

            Console.WriteLine(
                "================================================================");

            Console.WriteLine(
                "[SWIFTCARE ATS] AZURE STRUCTURED EXTRACTION RESULT");

            Console.WriteLine(
                "================================================================");

            Console.WriteLine(
                $"[SWIFTCARE ATS] Success: {azureResult.Success}");

            Console.WriteLine(
                $"[SWIFTCARE ATS] RawText Length: {azureResult.RawText?.Length ?? 0}");

            Console.WriteLine(
                $"[SWIFTCARE ATS] Field Count: {azureResult.Fields?.Count ?? 0}");

            if (azureResult.Fields != null)
            {
                foreach (var field in azureResult.Fields)
                {
                    Console.WriteLine(
                        $"[SWIFTCARE ATS] Azure Field: {field.Key}");
                }
            }

            Console.WriteLine(
                "================================================================");

            // ============================================================
            // 7. IMPORTANT:
            //    USE STRUCTURED AZURE RESULT
            //    DO NOT CALL ParseAsync(rawText, ...)
            // ============================================================

            var parsedCv =
                await _parsingEngine.ParseAsync(
                    azureResult,
                    volunteerApplicationId);

            // ============================================================
            // 8. FINAL SAFETY CHECK
            // ============================================================

            if (parsedCv == null)
            {
                return new ParsedCv
                {
                    VolunteerApplicationId =
                        volunteerApplicationId,

                    RawText =
                        azureResult.RawText ?? string.Empty,

                    ExtractionSuccessful =
                        false,

                    ExtractionMessage =
                        "CV parsing returned no ParsedCv result."
                };
            }

            // ============================================================
            // 9. LOG FINAL PARSED RESULT
            // ============================================================

            Console.WriteLine(
                "================================================================");

            Console.WriteLine(
                "[SWIFTCARE ATS] FINAL STRUCTURED PARSED CV");

            Console.WriteLine(
                "================================================================");

            Console.WriteLine(
                $"[SWIFTCARE ATS] Full Name: {parsedCv.FullName}");

            Console.WriteLine(
                $"[SWIFTCARE ATS] Email: {parsedCv.Email}");

            Console.WriteLine(
                $"[SWIFTCARE ATS] Phone: {parsedCv.PhoneNumber}");

            Console.WriteLine(
                $"[SWIFTCARE ATS] Professional Title: {parsedCv.ProfessionalTitle}");

            Console.WriteLine(
                $"[SWIFTCARE ATS] Professional Summary: {parsedCv.ProfessionalSummary}");

            Console.WriteLine(
                $"[SWIFTCARE ATS] Career Objective: {parsedCv.CareerObjective}");

            Console.WriteLine(
                $"[SWIFTCARE ATS] Address: {parsedCv.Address}");

            Console.WriteLine(
                $"[SWIFTCARE ATS] City: {parsedCv.City}");

            Console.WriteLine(
                $"[SWIFTCARE ATS] Province: {parsedCv.Province}");

            Console.WriteLine(
                $"[SWIFTCARE ATS] Country: {parsedCv.Country}");

            Console.WriteLine(
                $"[SWIFTCARE ATS] LinkedIn: {parsedCv.LinkedInUrl}");

            Console.WriteLine(
                $"[SWIFTCARE ATS] Portfolio: {parsedCv.PortfolioUrl}");

            Console.WriteLine(
                $"[SWIFTCARE ATS] GitHub: {parsedCv.GitHubUrl}");

            Console.WriteLine(
                $"[SWIFTCARE ATS] Skills: {parsedCv.Skills.Count}");

            Console.WriteLine(
                $"[SWIFTCARE ATS] Education: {parsedCv.Education.Count}");

            Console.WriteLine(
                $"[SWIFTCARE ATS] Experience: {parsedCv.Experience.Count}");

            Console.WriteLine(
                $"[SWIFTCARE ATS] Certifications: {parsedCv.Certifications.Count}");

            Console.WriteLine(
                $"[SWIFTCARE ATS] Languages: {parsedCv.Languages.Count}");

            Console.WriteLine(
                $"[SWIFTCARE ATS] Extraction Successful: {parsedCv.ExtractionSuccessful}");

            Console.WriteLine(
                $"[SWIFTCARE ATS] Extraction Message: {parsedCv.ExtractionMessage}");

            Console.WriteLine(
                "================================================================");

            return parsedCv;
        }

        // ================================================================
        // CONTENT TYPE
        // ================================================================

        private static string GetContentType(
            string fileName)
        {
            var extension =
                Path.GetExtension(fileName)
                    .ToLowerInvariant();

            return extension switch
            {
                ".pdf" =>
                    "application/pdf",

                ".docx" =>
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",

                ".doc" =>
                    "application/msword",

                ".txt" =>
                    "text/plain",

                _ =>
                    "application/octet-stream"
            };
        }
    }
}