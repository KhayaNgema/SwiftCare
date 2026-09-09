using Azure;
using Azure.AI.DocumentIntelligence;
using Microsoft.AspNetCore.Http;

namespace SwiftCare.Services.VolunteerATS.CV
{
    public class AzureDocumentIntelligenceCvService
        : IAzureDocumentIntelligenceCvService
    {
        private readonly DocumentIntelligenceClient _client;
        private readonly IConfiguration _configuration;

        private static readonly HashSet<string>
            SupportedExtensions =
                new(StringComparer.OrdinalIgnoreCase)
                {
                    ".pdf",
                    ".docx"
                };

        public AzureDocumentIntelligenceCvService(
            DocumentIntelligenceClient client,
            IConfiguration configuration)
        {
            _client = client
                ?? throw new ArgumentNullException(
                    nameof(client));

            _configuration = configuration
                ?? throw new ArgumentNullException(
                    nameof(configuration));
        }

        // ============================================================
        // ANALYZE CV
        // ============================================================

        public async Task<AzureCvExtractionResult> AnalyzeAsync(
            IFormFile file,
            CancellationToken cancellationToken = default)
        {
            if (file == null)
            {
                throw new ArgumentNullException(
                    nameof(file));
            }

            // --------------------------------------------------------
            // VALIDATE FILE
            // --------------------------------------------------------

            if (file.Length <= 0)
            {
                return new AzureCvExtractionResult
                {
                    Success = false,
                    ErrorMessage =
                        "The uploaded CV is empty."
                };
            }

            // --------------------------------------------------------
            // VALIDATE EXTENSION
            // --------------------------------------------------------

            var extension =
                Path.GetExtension(
                    file.FileName)
                .ToLowerInvariant();

            if (!SupportedExtensions.Contains(
                    extension))
            {
                return new AzureCvExtractionResult
                {
                    Success = false,
                    ErrorMessage =
                        $"Unsupported CV format '{extension}'. " +
                        "Only PDF and DOCX files are supported."
                };
            }

            // --------------------------------------------------------
            // GET CUSTOM MODEL ID
            // --------------------------------------------------------

            var modelId =
                _configuration[
                    "AzureDocumentIntelligence:ModelId"];

            if (string.IsNullOrWhiteSpace(modelId))
            {
                return new AzureCvExtractionResult
                {
                    Success = false,
                    ErrorMessage =
                        "Azure Document Intelligence custom " +
                        "ModelId is not configured."
                };
            }

            try
            {
                // ====================================================
                // READ FILE
                // ====================================================

                await using var inputStream =
                    file.OpenReadStream();

                using var memoryStream =
                    new MemoryStream();

                await inputStream.CopyToAsync(
                    memoryStream,
                    cancellationToken);

                var documentBytes =
                    memoryStream.ToArray();

                if (documentBytes.Length == 0)
                {
                    return new AzureCvExtractionResult
                    {
                        Success = false,
                        ErrorMessage =
                            "The uploaded CV contains no readable data."
                    };
                }

                // ====================================================
                // CREATE ANALYSIS OPTIONS
                // ====================================================

                var options =
                    new AnalyzeDocumentOptions(
                        modelId,
                        BinaryData.FromBytes(
                            documentBytes))
                    {
                        OutputContentFormat =
                            DocumentContentFormat.Markdown
                    };

                // ====================================================
                // SEND TO AZURE
                // ====================================================

                var operation =
                    await _client.AnalyzeDocumentAsync(
                        WaitUntil.Completed,
                        options,
                        cancellationToken);

                var result =
                    operation.Value;

                // ====================================================
                // CREATE RESULT
                // ====================================================

                var extractionResult =
                    new AzureCvExtractionResult
                    {
                        Success = true,

                        RawText =
                            result.Content
                            ?? string.Empty,

                        ContentFormat =
                            result.ContentFormat?
                                .ToString()
                            ?? string.Empty
                    };

                // ====================================================
                // CUSTOM MODEL FIELDS
                // ====================================================

                if (result.Documents != null &&
                    result.Documents.Count > 0)
                {
                    foreach (var document in result.Documents)
                    {
                        if (document.Fields == null ||
                            document.Fields.Count == 0)
                        {
                            continue;
                        }

                        foreach (var field in document.Fields)
                        {
                            var fieldName =
                                field.Key;

                            var documentField =
                                field.Value;

                            if (string.IsNullOrWhiteSpace(
                                    fieldName))
                            {
                                continue;
                            }

                            if (documentField == null)
                            {
                                continue;
                            }

                            extractionResult.Fields[
                                fieldName] =
                                ConvertField(
                                    documentField);
                        }
                    }
                }

                // ====================================================
                // PARAGRAPHS
                // ====================================================

                if (result.Paragraphs != null)
                {
                    foreach (var paragraph in result.Paragraphs)
                    {
                        if (string.IsNullOrWhiteSpace(
                                paragraph.Content))
                        {
                            continue;
                        }

                        extractionResult.Paragraphs.Add(
                            new AzureCvParagraph
                            {
                                Content =
                                    paragraph.Content.Trim(),

                                Role =
                                    paragraph.Role?
                                        .ToString()
                            });
                    }
                }

                // ====================================================
                // SECTIONS
                // ====================================================

                if (result.Sections != null)
                {
                    foreach (var section in result.Sections)
                    {
                        extractionResult.Sections.Add(
                            new AzureCvSection
                            {
                                Role = "section"
                            });
                    }
                }

                // ====================================================
                // TABLES
                // ====================================================

                if (result.Tables != null)
                {
                    foreach (var table in result.Tables)
                    {
                        var extractedTable =
                            new AzureCvTable
                            {
                                RowCount =
                                    table.RowCount,

                                ColumnCount =
                                    table.ColumnCount
                            };

                        if (table.Cells != null)
                        {
                            foreach (var cell in table.Cells)
                            {
                                extractedTable.Cells.Add(
                                    new AzureCvTableCell
                                    {
                                        RowIndex =
                                            cell.RowIndex,

                                        ColumnIndex =
                                            cell.ColumnIndex,

                                        Content =
                                            cell.Content?
                                                .Trim()
                                            ?? string.Empty
                                    });
                            }
                        }

                        extractionResult.Tables.Add(
                            extractedTable);
                    }
                }

                // ====================================================
                // WARNINGS
                // ====================================================

                if (result.Warnings != null)
                {
                    foreach (var warning in result.Warnings)
                    {
                        if (!string.IsNullOrWhiteSpace(
                                warning.Message))
                        {
                            extractionResult.Warnings.Add(
                                warning.Message);
                        }
                    }
                }

                // ====================================================
                // VALIDATION
                // ====================================================

                var hasRawText =
                    !string.IsNullOrWhiteSpace(
                        extractionResult.RawText);

                var hasFields =
                    extractionResult.Fields.Count > 0;

                var hasParagraphs =
                    extractionResult.Paragraphs.Count > 0;

                var hasTables =
                    extractionResult.Tables.Count > 0;

                if (!hasRawText &&
                    !hasFields &&
                    !hasParagraphs &&
                    !hasTables)
                {
                    extractionResult.Success = false;

                    extractionResult.ErrorMessage =
                        "Azure Document Intelligence completed " +
                        "successfully, but no readable CV data " +
                        "was extracted.";
                }

                return extractionResult;
            }
            catch (RequestFailedException ex)
            {
                return new AzureCvExtractionResult
                {
                    Success = false,

                    ErrorMessage =
                        "Azure Document Intelligence failed. " +
                        $"Status: {ex.Status}. " +
                        $"Message: {ex.Message}"
                };
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return new AzureCvExtractionResult
                {
                    Success = false,

                    ErrorMessage =
                        "An unexpected error occurred while " +
                        "processing the CV with Azure Document " +
                        $"Intelligence: {ex.Message}"
                };
            }
        }

        // ============================================================
        // CONVERT AZURE FIELD
        // ============================================================

        private static AzureCvField ConvertField(
            DocumentField field)
        {
            if (field == null)
            {
                return new AzureCvField();
            }

            var converted =
                new AzureCvField
                {
                    Content =
                        field.Content?.Trim()
                        ?? string.Empty,

                    FieldType =
                        field.FieldType.ToString(),

                    Confidence =
                        field.Confidence
                };

            // ========================================================
            // OBJECT / NESTED FIELDS
            // ========================================================

            if (field.ValueDictionary != null &&
                field.ValueDictionary.Count > 0)
            {
                foreach (var item in field.ValueDictionary)
                {
                    if (string.IsNullOrWhiteSpace(
                            item.Key))
                    {
                        continue;
                    }

                    if (item.Value == null)
                    {
                        continue;
                    }

                    converted.ObjectValues[
                        item.Key] =
                        ConvertField(
                            item.Value);
                }
            }

            // ========================================================
            // ARRAY / LIST FIELDS
            // ========================================================

            if (field.ValueList != null &&
                field.ValueList.Count > 0)
            {
                foreach (var item in field.ValueList)
                {
                    if (item == null)
                    {
                        continue;
                    }

                    converted.ArrayValues.Add(
                        ConvertField(item));
                }
            }

            return converted;
        }
    }
}