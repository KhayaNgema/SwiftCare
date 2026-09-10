using Azure;
using Azure.AI.DocumentIntelligence;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SwiftCare.Services.VolunteerATS.CV
{
    public class AzureDocumentIntelligenceCvService
        : IAzureDocumentIntelligenceCvService
    {
        private readonly DocumentIntelligenceClient _client;
        private readonly IConfiguration _configuration;

        public AzureDocumentIntelligenceCvService(
            DocumentIntelligenceClient client,
            IConfiguration configuration)
        {
            _client = client
                ?? throw new ArgumentNullException(nameof(client));

            _configuration = configuration
                ?? throw new ArgumentNullException(nameof(configuration));
        }

        // ============================================================
        // MAIN CV ANALYSIS METHOD
        // ============================================================

        public async Task<AzureCvExtractionResult> AnalyzeAsync(
            IFormFile file,
            CancellationToken cancellationToken = default)
        {
            var result = new AzureCvExtractionResult();

            try
            {
                // ----------------------------------------------------
                // VALIDATE FILE
                // ----------------------------------------------------

                if (file == null)
                    throw new ArgumentNullException(nameof(file));

                if (file.Length <= 0)
                    throw new InvalidOperationException(
                        "The uploaded CV file is empty.");

                if (string.IsNullOrWhiteSpace(file.FileName))
                    throw new InvalidOperationException(
                        "The CV file name is missing.");

                var extension =
                    Path.GetExtension(file.FileName)
                        .ToLowerInvariant();

                if (extension != ".pdf" &&
                    extension != ".docx")
                {
                    throw new InvalidOperationException(
                        $"Unsupported CV file type '{extension}'. " +
                        "Only PDF and DOCX files are supported.");
                }

                // ----------------------------------------------------
                // GET MODEL ID
                // ----------------------------------------------------

                var modelId =
                    _configuration[
                        "AzureDocumentIntelligence:ModelId"];

                if (string.IsNullOrWhiteSpace(modelId))
                {
                    throw new InvalidOperationException(
                        "AzureDocumentIntelligence:ModelId is not configured.");
                }

                // ----------------------------------------------------
                // COPY FILE TO MEMORY
                // ----------------------------------------------------

                byte[] documentBytes;

                await using (var inputStream = file.OpenReadStream())
                await using (var memoryStream = new MemoryStream())
                {
                    await inputStream.CopyToAsync(
                        memoryStream,
                        cancellationToken);

                    documentBytes =
                        memoryStream.ToArray();
                }

                if (documentBytes.Length == 0)
                {
                    throw new InvalidOperationException(
                        "The CV file contains no readable data.");
                }

                // ----------------------------------------------------
                // LOG START
                // ----------------------------------------------------

                Console.WriteLine(
                    "============================================================");

                Console.WriteLine(
                    "[SWIFTCARE CV] STARTING AZURE CV EXTRACTION");

                Console.WriteLine(
                    $"[SWIFTCARE CV] File: {file.FileName}");

                Console.WriteLine(
                    $"[SWIFTCARE CV] Extension: {extension}");

                Console.WriteLine(
                    $"[SWIFTCARE CV] Bytes: {documentBytes.Length}");

                Console.WriteLine(
                    $"[SWIFTCARE CV] Model ID: {modelId}");

                // ----------------------------------------------------
                // CREATE AZURE ANALYSIS OPTIONS
                // ----------------------------------------------------

                var options =
                    new AnalyzeDocumentOptions(
                        modelId,
                        BinaryData.FromBytes(documentBytes))
                    {
                        OutputContentFormat =
                            DocumentContentFormat.Markdown
                    };

                Console.WriteLine(
                    "[SWIFTCARE CV] Sending CV to Azure...");

                // ----------------------------------------------------
                // SEND TO AZURE DOCUMENT INTELLIGENCE
                // ----------------------------------------------------

                var operation =
                    await _client.AnalyzeDocumentAsync(
                        WaitUntil.Completed,
                        options,
                        cancellationToken);

                var analyzeResult =
                    operation.Value;

                Console.WriteLine(
                    "[SWIFTCARE CV] Azure analysis completed.");

                // ----------------------------------------------------
                // BASIC RESULT INFORMATION
                // ----------------------------------------------------

                result.Success = true;

                result.ContentFormat =
                    analyzeResult.ContentFormat?.ToString()
                    ?? string.Empty;

                result.RawText =
                    analyzeResult.Content
                    ?? string.Empty;

                result.ExtractionMessage =
                    "CV processing completed successfully.";

                Console.WriteLine(
                    $"[SWIFTCARE CV] Azure Model: {analyzeResult.ModelId}");

                Console.WriteLine(
                    $"[SWIFTCARE CV] Documents: {analyzeResult.Documents.Count}");

                Console.WriteLine(
                    $"[SWIFTCARE CV] Paragraphs: {analyzeResult.Paragraphs.Count}");

                Console.WriteLine(
                    $"[SWIFTCARE CV] Sections: {analyzeResult.Sections.Count}");

                Console.WriteLine(
                    $"[SWIFTCARE CV] Tables: {analyzeResult.Tables.Count}");

                Console.WriteLine(
                    $"[SWIFTCARE CV] Raw Text Length: {result.RawText.Length}");

                // ====================================================
                // CUSTOM MODEL FIELDS
                // ====================================================

                foreach (var document in analyzeResult.Documents)
                {
                    Console.WriteLine(
                        "------------------------------------------------------------");

                    Console.WriteLine(
                        $"[SWIFTCARE CV] DOCUMENT TYPE: {document.DocumentType}");

                    Console.WriteLine(
                        $"[SWIFTCARE CV] FIELD COUNT: {document.Fields.Count}");

                    foreach (var fieldPair in document.Fields)
                    {
                        var fieldName =
                            fieldPair.Key;

                        var field =
                            fieldPair.Value;

                        Console.WriteLine(
                            $"[SWIFTCARE CV] FIELD: {fieldName}");

                        Console.WriteLine(
                            $"[SWIFTCARE CV]   Type: {field.FieldType}");

                        Console.WriteLine(
                            $"[SWIFTCARE CV]   Confidence: {field.Confidence}");

                        Console.WriteLine(
                            $"[SWIFTCARE CV]   Content: {field.Content}");

                        var converted =
                            ConvertField(field);

                        result.Fields[fieldName] =
                            converted;

                        LogNestedField(
                            fieldName,
                            converted,
                            1);
                    }
                }

                // ====================================================
                // WARNINGS
                // ====================================================

                foreach (var warning in analyzeResult.Warnings)
                {
                    if (!string.IsNullOrWhiteSpace(
                        warning.Message))
                    {
                        result.Warnings.Add(
                            warning.Message);

                        Console.WriteLine(
                            $"[SWIFTCARE CV] WARNING: {warning.Message}");
                    }
                }

                // ====================================================
                // FINAL SUMMARY
                // ====================================================

                Console.WriteLine(
                    "============================================================");

                Console.WriteLine(
                    "[SWIFTCARE CV] FINAL AZURE EXTRACTION SUMMARY");

                Console.WriteLine(
                    $"[SWIFTCARE CV] Success: {result.Success}");

                Console.WriteLine(
                    $"[SWIFTCARE CV] Fields: {result.Fields.Count}");

                Console.WriteLine(
                    $"[SWIFTCARE CV] Paragraphs: {result.Paragraphs.Count}");

                Console.WriteLine(
                    $"[SWIFTCARE CV] Sections: {result.Sections.Count}");

                Console.WriteLine(
                    $"[SWIFTCARE CV] Tables: {result.Tables.Count}");

                Console.WriteLine(
                    $"[SWIFTCARE CV] Raw text: {result.RawText.Length}");

                Console.WriteLine(
                    $"[SWIFTCARE CV] Message: {result.ExtractionMessage}");

                Console.WriteLine(
                    "[SWIFTCARE CV] FIELD NAMES RETURNED BY AZURE:");

                foreach (var fieldName in result.Fields.Keys)
                {
                    Console.WriteLine(
                        $"[SWIFTCARE CV]   -> {fieldName}");
                }

                Console.WriteLine(
                    "============================================================");

                return result;
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine(
                    "[SWIFTCARE CV] CV extraction was cancelled.");

                result.Success = false;

                result.ExtractionMessage =
                    "CV processing was cancelled.";

                result.ErrorMessage =
                    "The CV extraction operation was cancelled.";

                return result;
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine(
                    "============================================================");

                Console.WriteLine(
                    "[SWIFTCARE CV] AZURE REQUEST FAILED");

                Console.WriteLine(
                    $"[SWIFTCARE CV] Status: {ex.Status}");

                Console.WriteLine(
                    $"[SWIFTCARE CV] Error Code: {ex.ErrorCode}");

                Console.WriteLine(
                    $"[SWIFTCARE CV] Message: {ex.Message}");

                Console.WriteLine(
                    "============================================================");

                result.Success = false;

                result.ExtractionMessage =
                    $"Azure Document Intelligence error: {ex.Message}";

                result.ErrorMessage =
                    ex.ToString();

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "============================================================");

                Console.WriteLine(
                    "[SWIFTCARE CV] CV EXTRACTION FAILED");

                Console.WriteLine(
                    $"[SWIFTCARE CV] Exception: {ex.GetType().FullName}");

                Console.WriteLine(
                    $"[SWIFTCARE CV] Message: {ex.Message}");

                Console.WriteLine(
                    $"[SWIFTCARE CV] Stack: {ex.StackTrace}");

                Console.WriteLine(
                    "============================================================");

                result.Success = false;

                result.ExtractionMessage =
                    $"CV processing failed: {ex.Message}";

                result.ErrorMessage =
                    ex.ToString();

                return result;
            }
        }

        // ============================================================
        // CONVERT AZURE FIELD
        // ============================================================

        private AzureCvField ConvertField(
            DocumentField field)
        {
            var converted =
                new AzureCvField
                {
                    Content =
                        field.Content
                        ?? string.Empty,

                    Confidence =
                        field.Confidence,

                    FieldType =
                        field.FieldType.ToString()
                };

            // --------------------------------------------------------
            // OBJECT
            // --------------------------------------------------------

            if (field.ValueDictionary != null)
            {
                foreach (var item in field.ValueDictionary)
                {
                    converted.ObjectValues[item.Key] =
                        ConvertField(item.Value);
                }
            }

            // --------------------------------------------------------
            // ARRAY
            // --------------------------------------------------------

            if (field.ValueList != null)
            {
                foreach (var item in field.ValueList)
                {
                    converted.ArrayValues.Add(
                        ConvertField(item));
                }
            }

            return converted;
        }

        // ============================================================
        // LOG NESTED FIELD
        // ============================================================

        private void LogNestedField(
            string fieldName,
            AzureCvField field,
            int level)
        {
            var indentation =
                new string(' ', level * 2);

            // --------------------------------------------------------
            // CONTENT
            // --------------------------------------------------------

            if (!string.IsNullOrWhiteSpace(
                field.Content))
            {
                Console.WriteLine(
                    $"[SWIFTCARE CV] " +
                    $"{indentation}{fieldName} = {field.Content}");
            }

            // --------------------------------------------------------
            // OBJECT VALUES
            // --------------------------------------------------------

            if (field.ObjectValues != null &&
                field.ObjectValues.Count > 0)
            {
                foreach (var item in field.ObjectValues)
                {
                    Console.WriteLine(
                        $"[SWIFTCARE CV] " +
                        $"{indentation}{fieldName}.{item.Key}");

                    LogNestedField(
                        item.Key,
                        item.Value,
                        level + 1);
                }
            }

            // --------------------------------------------------------
            // ARRAY VALUES
            // --------------------------------------------------------

            if (field.ArrayValues != null &&
                field.ArrayValues.Count > 0)
            {
                for (var i = 0;
                     i < field.ArrayValues.Count;
                     i++)
                {
                    Console.WriteLine(
                        $"[SWIFTCARE CV] " +
                        $"{indentation}{fieldName}[{i}]");

                    LogNestedField(
                        $"{fieldName}[{i}]",
                        field.ArrayValues[i],
                        level + 1);
                }
            }
        }
    }
}