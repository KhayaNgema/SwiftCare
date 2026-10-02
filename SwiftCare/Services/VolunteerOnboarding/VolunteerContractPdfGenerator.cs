using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using SwiftCare.Data;
using SwiftCare.Models.VolunteerOnboarding;
using SwiftCare.Services;
using System.Text;
using System.Text.RegularExpressions;

namespace SwiftCare.Services.VolunteerOnboarding
{
    public interface IVolunteerContractPdfGenerator
    {
        Task<VolunteerContract> GeneratePdfAsync(
            VolunteerContract contract,
            CancellationToken cancellationToken = default);
    }

    public class VolunteerContractPdfGenerator
        : IVolunteerContractPdfGenerator
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IVolunteerContractContentBuilder _contentBuilder;
        private readonly SwiftCareDbContext _context;
        private readonly FileUploadService _fileUploadService;
        private readonly ILogger<VolunteerContractPdfGenerator> _logger;

        public VolunteerContractPdfGenerator(
            IWebHostEnvironment environment,
            IVolunteerContractContentBuilder contentBuilder,
            SwiftCareDbContext context,
            FileUploadService fileUploadService,
            ILogger<VolunteerContractPdfGenerator> logger)
        {
            _environment = environment;
            _contentBuilder = contentBuilder;
            _context = context;
            _fileUploadService = fileUploadService;
            _logger = logger;
        }

        // ============================================================
        // GENERATE PDF
        // ============================================================

        public async Task<VolunteerContract> GeneratePdfAsync(
            VolunteerContract contract,
            CancellationToken cancellationToken = default)
        {
            if (contract == null)
            {
                _logger.LogError(
                    "[CONTRACT PDF] GeneratePdfAsync received a null contract.");

                throw new ArgumentNullException(nameof(contract));
            }

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                _logger.LogInformation(
                    "[CONTRACT PDF] ===== GENERATION STARTED ===== ContractId={ContractId}, Reference={ContractReference}",
                    contract.VolunteerContractId,
                    contract.ContractReference);

                // ====================================================
                // 1. VALIDATE WEB ROOT
                // ====================================================

                if (string.IsNullOrWhiteSpace(_environment.WebRootPath))
                {
                    _logger.LogError(
                        "[CONTRACT PDF] WebRootPath is empty.");

                    throw new InvalidOperationException(
                        "WebRootPath is not configured.");
                }

                _logger.LogInformation(
                    "[CONTRACT PDF] WebRootPath={WebRootPath}",
                    _environment.WebRootPath);

                // ====================================================
                // 2. BUILD DOCUMENT MODEL
                // ====================================================

                _logger.LogInformation(
                    "[CONTRACT PDF] Building contract document model. ContractId={ContractId}",
                    contract.VolunteerContractId);

                var documentModel =
                    await _contentBuilder.BuildAsync(
                        contract,
                        cancellationToken);

                if (documentModel == null)
                {
                    _logger.LogError(
                        "[CONTRACT PDF] Content builder returned NULL. ContractId={ContractId}",
                        contract.VolunteerContractId);

                    throw new InvalidOperationException(
                        "Volunteer contract document model could not be built.");
                }

                _logger.LogInformation(
                    "[CONTRACT PDF] Document model built successfully. " +
                    "ContractReference={ContractReference}, Sections={SectionCount}",
                    documentModel.ContractReference,
                    documentModel.Sections?.Count ?? 0);

                // ====================================================
                // 3. TEMPLATE PATH
                // ====================================================

                var templatePath =
                    Path.Combine(
                        _environment.WebRootPath,
                        "Templates",
                        "VolunteerContracts",
                        "VolunteerServiceAgreement.html");

                _logger.LogInformation(
                    "[CONTRACT PDF] Looking for HTML template at {TemplatePath}",
                    templatePath);

                if (!File.Exists(templatePath))
                {
                    _logger.LogError(
                        "[CONTRACT PDF] HTML template NOT FOUND at {TemplatePath}",
                        templatePath);

                    throw new FileNotFoundException(
                        "Volunteer contract HTML template was not found.",
                        templatePath);
                }

                _logger.LogInformation(
                    "[CONTRACT PDF] HTML template found.");

                // ====================================================
                // 4. READ TEMPLATE
                // ====================================================

                var htmlTemplate =
                    await File.ReadAllTextAsync(
                        templatePath,
                        cancellationToken);

                if (string.IsNullOrWhiteSpace(htmlTemplate))
                {
                    _logger.LogError(
                        "[CONTRACT PDF] HTML template is empty. TemplatePath={TemplatePath}",
                        templatePath);

                    throw new InvalidOperationException(
                        "Volunteer contract HTML template is empty.");
                }

                _logger.LogInformation(
                    "[CONTRACT PDF] HTML template loaded. Characters={CharacterCount}",
                    htmlTemplate.Length);

                // ====================================================
                // 5. POPULATE TEMPLATE
                // ====================================================

                _logger.LogInformation(
                    "[CONTRACT PDF] Populating HTML template.");

                var populatedHtml =
                    PopulateTemplate(
                        htmlTemplate,
                        documentModel);

                if (string.IsNullOrWhiteSpace(populatedHtml))
                {
                    _logger.LogError(
                        "[CONTRACT PDF] Populated HTML is empty.");

                    throw new InvalidOperationException(
                        "Volunteer contract populated HTML is empty.");
                }

                _logger.LogInformation(
                    "[CONTRACT PDF] HTML template populated successfully. Characters={CharacterCount}",
                    populatedHtml.Length);

                // ====================================================
                // 6. VALIDATE HTML
                // ====================================================

                ValidatePopulatedHtml(
                    populatedHtml);

                // ====================================================
                // 7. SAFE FILE NAME
                // ====================================================

                var safeReference =
                    MakeSafeFileName(
                        contract.ContractReference);

                var fileName =
                    $"{safeReference}.pdf";

                _logger.LogInformation(
                    "[CONTRACT PDF] PDF file name resolved. FileName={FileName}",
                    fileName);

                // ====================================================
                // 8. RENDER HTML/CSS USING PLAYWRIGHT
                // ====================================================

                _logger.LogInformation(
                    "[CONTRACT PDF] Rendering HTML/CSS using Chromium.");

                var pdfBytes =
                    await RenderHtmlToPdfAsync(
                        populatedHtml,
                        templatePath,
                        cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();

                _logger.LogInformation(
                    "[CONTRACT PDF] Chromium PDF generation completed. Size={Size} bytes",
                    pdfBytes.Length);

                if (pdfBytes.Length == 0)
                {
                    _logger.LogError(
                        "[CONTRACT PDF] Generated PDF contains ZERO bytes.");

                    throw new InvalidOperationException(
                        "The volunteer contract PDF could not be generated.");
                }

                // ====================================================
                // 9. UPLOAD TO AZURE BLOB
                // ====================================================

                _logger.LogInformation(
                    "[CONTRACT PDF] Uploading generated contract to Azure Blob Storage. " +
                    "FileName={FileName}, Size={Size}",
                    fileName,
                    pdfBytes.Length);

                var blobPath =
                    await _fileUploadService.SaveFileAsync(
                        pdfBytes,
                        fileName,
                        cancellationToken);

                if (string.IsNullOrWhiteSpace(blobPath))
                {
                    _logger.LogError(
                        "[CONTRACT PDF] Azure Blob Storage returned an EMPTY path.");

                    throw new InvalidOperationException(
                        "Azure Blob Storage returned an empty blob path for the generated contract.");
                }

                _logger.LogInformation(
                    "[CONTRACT PDF] Azure upload successful. BlobPath={BlobPath}",
                    blobPath);

                // ====================================================
                // 10. VERIFY BLOB
                // ====================================================

                _logger.LogInformation(
                    "[CONTRACT PDF] Verifying uploaded blob. BlobPath={BlobPath}",
                    blobPath);

                var blobExists =
                    await _fileUploadService.FileExistsAsync(
                        blobPath,
                        cancellationToken);

                if (!blobExists)
                {
                    _logger.LogError(
                        "[CONTRACT PDF] Blob verification FAILED. BlobPath={BlobPath}",
                        blobPath);

                    throw new InvalidOperationException(
                        $"Generated contract was uploaded but could not be verified in Azure Blob Storage. BlobPath={blobPath}");
                }

                _logger.LogInformation(
                    "[CONTRACT PDF] Blob verification successful. BlobPath={BlobPath}",
                    blobPath);

                // ====================================================
                // 11. UPDATE CONTRACT ENTITY
                // ====================================================

                contract.GeneratedFileName =
                    fileName;

                contract.GeneratedFilePath =
                    blobPath;

                contract.Status =
                    VolunteerContractStatus.AwaitingApplicant;

                contract.ModifiedDate =
                    DateTime.Now;

                _logger.LogInformation(
                    "[CONTRACT PDF] Updating contract database record. " +
                    "ContractId={ContractId}, FileName={FileName}, BlobPath={BlobPath}, Status={Status}",
                    contract.VolunteerContractId,
                    contract.GeneratedFileName,
                    contract.GeneratedFilePath,
                    contract.Status);

                _context.VolunteerContracts.Update(
                    contract);

                await _context.SaveChangesAsync(
                    cancellationToken);

                // ====================================================
                // 12. SUCCESS
                // ====================================================

                _logger.LogInformation(
                    "[CONTRACT PDF] ===== GENERATION SUCCESSFUL ===== " +
                    "ContractId={ContractId}, Reference={ContractReference}, BlobPath={BlobPath}",
                    contract.VolunteerContractId,
                    contract.ContractReference,
                    blobPath);

                return contract;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning(
                    "[CONTRACT PDF] Generation was cancelled. " +
                    "ContractId={ContractId}, Reference={ContractReference}",
                    contract.VolunteerContractId,
                    contract.ContractReference);

                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[CONTRACT PDF] ===== GENERATION FAILED ===== " +
                    "ContractId={ContractId}, Reference={ContractReference}, Message={Message}",
                    contract.VolunteerContractId,
                    contract.ContractReference,
                    ex.Message);

                throw;
            }
        }

        // ============================================================
        // RENDER HTML/CSS TO PDF USING PLAYWRIGHT
        // ============================================================

        private async Task<byte[]> RenderHtmlToPdfAsync(
            string html,
            string templatePath,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                throw new ArgumentException(
                    "HTML content cannot be empty.",
                    nameof(html));
            }

            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogInformation(
                "[CONTRACT PDF] Starting Playwright.");

            using var playwright =
                await Playwright.CreateAsync();

            _logger.LogInformation(
                "[CONTRACT PDF] Launching Chromium.");

            await using var browser =
                await playwright.Chromium.LaunchAsync(
                    new BrowserTypeLaunchOptions
                    {
                        Headless = true
                    });

            _logger.LogInformation(
                "[CONTRACT PDF] Chromium launched successfully.");

            var page =
                await browser.NewPageAsync(
                    new BrowserNewPageOptions
                    {
                        ViewportSize =
                            new ViewportSize
                            {
                                Width = 1240,
                                Height = 1754
                            },

                        DeviceScaleFactor = 1
                    });

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // ====================================================
                // TEMPLATE DIRECTORY
                // ====================================================

                var templateDirectory =
                    Path.GetDirectoryName(
                        templatePath);

                if (string.IsNullOrWhiteSpace(
                    templateDirectory))
                {
                    throw new InvalidOperationException(
                        "Could not determine the HTML template directory.");
                }

                // ====================================================
                // BASE URI
                // ====================================================

                var normalizedDirectory =
                    templateDirectory.EndsWith(
                        Path.DirectorySeparatorChar.ToString(),
                        StringComparison.Ordinal)
                        ? templateDirectory
                        : templateDirectory +
                          Path.DirectorySeparatorChar;

                var baseUri =
                    new Uri(
                        normalizedDirectory);

                // ====================================================
                // INJECT BASE URL
                // ====================================================

                var finalHtml =
                    InjectBaseHref(
                        html,
                        baseUri.AbsoluteUri);

                _logger.LogInformation(
                    "[CONTRACT PDF] Loading populated HTML into Chromium.");

                // ====================================================
                // LOAD HTML
                // ====================================================

                await page.SetContentAsync(
                    finalHtml,
                    new PageSetContentOptions
                    {
                        WaitUntil =
                            WaitUntilState.NetworkIdle
                    });

                cancellationToken.ThrowIfCancellationRequested();

                _logger.LogInformation(
                    "[CONTRACT PDF] HTML loaded into Chromium.");

                // ====================================================
                // WAIT FOR FONTS
                // ====================================================

                await page.EvaluateAsync(
                    """
                    async () => {
                        if (document.fonts) {
                            await document.fonts.ready;
                        }
                    }
                    """);

                cancellationToken.ThrowIfCancellationRequested();

                _logger.LogInformation(
                    "[CONTRACT PDF] Document fonts loaded.");

                // ====================================================
                // USE PRINT MEDIA
                // ====================================================

                await page.EmulateMediaAsync(
                    new PageEmulateMediaOptions
                    {
                        Media = Media.Print
                    });

                // ====================================================
                // ENSURE PRINT COLORS
                // ====================================================

                await page.AddStyleTagAsync(
                    new PageAddStyleTagOptions
                    {
                        Content =
                            """
                            @media print {
                                *,
                                *::before,
                                *::after {
                                    -webkit-print-color-adjust: exact !important;
                                    print-color-adjust: exact !important;
                                }
                            }
                            """
                    });

                // ====================================================
                // WAIT FOR IMAGES
                // ====================================================

                await page.EvaluateAsync(
                    """
                    async () => {
                        const images = Array.from(document.images);

                        await Promise.all(
                            images.map(img => {
                                if (img.complete) {
                                    return Promise.resolve();
                                }

                                return new Promise(resolve => {
                                    img.addEventListener(
                                        'load',
                                        resolve,
                                        { once: true });

                                    img.addEventListener(
                                        'error',
                                        resolve,
                                        { once: true });
                                });
                            })
                        );
                    }
                    """);

                cancellationToken.ThrowIfCancellationRequested();

                _logger.LogInformation(
                    "[CONTRACT PDF] Images finished loading.");

                // ====================================================
                // GENERATE A4 PDF
                // ====================================================

                _logger.LogInformation(
                    "[CONTRACT PDF] Rendering A4 PDF from HTML/CSS.");

                var pdfBytes =
                    await page.PdfAsync(
                        new PagePdfOptions
                        {
                            Format = "A4",

                            PrintBackground = true,

                            PreferCSSPageSize = true,

                            Margin =
                                new Margin
                                {
                                    Top = "0",
                                    Right = "0",
                                    Bottom = "0",
                                    Left = "0"
                                }
                        });

                _logger.LogInformation(
                    "[CONTRACT PDF] Chromium rendered PDF successfully. Size={Size} bytes",
                    pdfBytes.Length);

                return pdfBytes;
            }
            finally
            {
                _logger.LogInformation(
                    "[CONTRACT PDF] Closing Chromium page.");

                await page.CloseAsync();

                _logger.LogInformation(
                    "[CONTRACT PDF] Closing Chromium browser.");

                await browser.CloseAsync();

                _logger.LogInformation(
                    "[CONTRACT PDF] Playwright browser closed.");
            }
        }

        // ============================================================
        // INJECT BASE HREF
        // ============================================================

        private static string InjectBaseHref(
            string html,
            string baseUrl)
        {
            var encodedBaseUrl =
                System.Net.WebUtility.HtmlEncode(
                    baseUrl);

            var baseTag =
                $"<base href=\"{encodedBaseUrl}\">";

            // --------------------------------------------------------
            // Existing <head>
            // --------------------------------------------------------

            if (Regex.IsMatch(
                html,
                @"<head\b[^>]*>",
                RegexOptions.IgnoreCase))
            {
                return Regex.Replace(
                    html,
                    @"(<head\b[^>]*>)",
                    $"$1{baseTag}",
                    RegexOptions.IgnoreCase);
            }

            // --------------------------------------------------------
            // No <head>
            // --------------------------------------------------------

            return
                $"""
                <!DOCTYPE html>
                <html>
                <head>
                    {baseTag}
                </head>
                <body>
                    {html}
                </body>
                </html>
                """;
        }

        // ============================================================
        // VALIDATE POPULATED HTML
        // ============================================================

        private void ValidatePopulatedHtml(
            string html)
        {
            if (!Regex.IsMatch(
                html,
                @"<html\b",
                RegexOptions.IgnoreCase))
            {
                _logger.LogWarning(
                    "[CONTRACT PDF] Populated HTML does not contain an <html> element.");
            }

            if (!Regex.IsMatch(
                html,
                @"<body\b",
                RegexOptions.IgnoreCase))
            {
                _logger.LogWarning(
                    "[CONTRACT PDF] Populated HTML does not contain a <body> element.");
            }

            // --------------------------------------------------------
            // Detect unresolved placeholders.
            // --------------------------------------------------------

            var unresolved =
                Regex.Matches(
                    html,
                    @"\{\{\s*([^{}]+?)\s*\}\}");

            if (unresolved.Count > 0)
            {
                var names =
                    unresolved
                        .Select(x =>
                            x.Groups[1]
                                .Value
                                .Trim())
                        .Distinct(
                            StringComparer.OrdinalIgnoreCase)
                        .ToList();

                _logger.LogWarning(
                    "[CONTRACT PDF] Unresolved HTML placeholders detected: {Placeholders}",
                    string.Join(", ", names));
            }
            else
            {
                _logger.LogInformation(
                    "[CONTRACT PDF] No unresolved HTML placeholders detected.");
            }
        }

        // ============================================================
        // POPULATE HTML TEMPLATE
        // ============================================================

        private string PopulateTemplate(
            string htmlTemplate,
            VolunteerContractDocument document)
        {
            var values =
                new Dictionary<string, string>(
                    StringComparer.OrdinalIgnoreCase);

            // ========================================================
            // BASIC CONTRACT VALUES
            // ========================================================

            values["ContractReference"] =
                document.ContractReference
                ?? string.Empty;

            values["ContractVersion"] =
                document.ContractVersion.ToString();

            values["OrganisationName"] =
                document.OrganisationName
                ?? "SwiftCare";

            // ========================================================
            // SECTION FIELDS
            // ========================================================

            if (document.Sections != null)
            {
                foreach (var section in document.Sections)
                {
                    if (section.Fields == null)
                    {
                        continue;
                    }

                    foreach (var field in section.Fields)
                    {
                        if (string.IsNullOrWhiteSpace(
                            field.Label))
                        {
                            continue;
                        }

                        var key =
                            field.Label.Trim();

                        values[key] =
                            field.Value?.Trim()
                            ?? string.Empty;
                    }
                }
            }

            // ========================================================
            // COMMON ALIASES
            // ========================================================

            SetAlias(
                values,
                "Volunteer",
                "VolunteerName");

            SetAlias(
                values,
                "Email",
                "VolunteerEmail");

            SetAlias(
                values,
                "Position",
                "PositionTitle");

            SetAlias(
                values,
                "Category",
                "Category");

            SetAlias(
                values,
                "Category",
                "OpportunityCategory");

            SetAlias(
                values,
                "Area",
                "Area");

            SetAlias(
                values,
                "Area",
                "OpportunityArea");

            SetAlias(
                values,
                "Location",
                "OpportunityLocation");

            SetAlias(
                values,
                "Start Date",
                "StartDate");

            SetAlias(
                values,
                "End Date",
                "EndDate");

            SetAlias(
                values,
                "Start Time",
                "StartTime");

            SetAlias(
                values,
                "End Time",
                "EndTime");

            SetAlias(
                values,
                "Minimum Age",
                "MinimumAge");

            // ========================================================
            // ADDITIONAL COMMON ALIASES
            // ========================================================

            SetAlias(
                values,
                "City",
                "CityName");

            SetAlias(
                values,
                "Province",
                "Province");

            SetAlias(
                values,
                "Role Description",
                "RoleDescription");

            SetAlias(
                values,
                "Description",
                "RoleDescription");

            // ========================================================
            // RESPONSIBILITIES
            // ========================================================

            values["Responsibilities"] =
                BuildHtmlListItems(
                    FindSectionBullets(
                        document,
                        "5. VOLUNTEER RESPONSIBILITIES"));

            // ========================================================
            // REQUIREMENTS
            // ========================================================

            values["Requirements"] =
                BuildHtmlListItems(
                    FindSectionBullets(
                        document,
                        "6. ROLE REQUIREMENTS"));

            // ========================================================
            // REQUIRED SKILLS
            // ========================================================

            values["RequiredSkills"] =
                BuildHtmlListItems(
                    FindSectionBullets(
                        document,
                        "7. REQUIRED SKILLS"));

            // ========================================================
            // ADDITIONAL CLAUSES
            // ========================================================

            values["AdditionalClauses"] =
                BuildHtmlListItems(
                    FindSectionBullets(
                        document,
                        "8. ADDITIONAL CLAUSES"));

            // ========================================================
            // REPLACE PLACEHOLDERS
            // ========================================================

            var result =
                Regex.Replace(
                    htmlTemplate,
                    @"\{\{\s*([^{}]+?)\s*\}\}",
                    match =>
                    {
                        var key =
                            match.Groups[1]
                                .Value
                                .Trim();

                        return values.TryGetValue(
                            key,
                            out var value)
                            ? value
                            : string.Empty;
                    });

            return result;
        }

        // ============================================================
        // FIND SECTION BULLETS
        // ============================================================

        private static List<string> FindSectionBullets(
            VolunteerContractDocument document,
            string title)
        {
            if (document.Sections == null)
            {
                return new List<string>();
            }

            return document.Sections
                .FirstOrDefault(
                    x =>
                        string.Equals(
                            x.Title?.Trim(),
                            title,
                            StringComparison.OrdinalIgnoreCase))
                ?.Bullets
                ?.Where(
                    x =>
                        !string.IsNullOrWhiteSpace(x))
                .Select(
                    x =>
                        x.Trim())
                .ToList()
                ?? new List<string>();
        }

        // ============================================================
        // BUILD HTML LIST ITEMS
        // ============================================================

        private static string BuildHtmlListItems(
            IEnumerable<string>? items)
        {
            if (items == null)
            {
                return string.Empty;
            }

            var builder =
                new StringBuilder();

            foreach (var item in items)
            {
                if (string.IsNullOrWhiteSpace(item))
                {
                    continue;
                }

                // ----------------------------------------------------
                // Custom <item> element intentionally matches the
                // HTML template.
                // ----------------------------------------------------

                builder.Append(
                    "<item>");

                builder.Append(
                    System.Net.WebUtility.HtmlEncode(
                        item.Trim()));

                builder.Append(
                    "</item>");
            }

            return builder.ToString();
        }

        // ============================================================
        // SET ALIAS
        // ============================================================

        private static void SetAlias(
            IDictionary<string, string> values,
            string source,
            string alias)
        {
            if (values.TryGetValue(
                source,
                out var value))
            {
                values[alias] =
                    value ?? string.Empty;
            }
            else if (!values.ContainsKey(alias))
            {
                values[alias] =
                    string.Empty;
            }
        }

        // ============================================================
        // SAFE FILE NAME
        // ============================================================

        private static string MakeSafeFileName(
            string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return
                    $"VolunteerContract_{DateTime.Now:yyyyMMddHHmmss}";
            }

            var invalidCharacters =
                Path.GetInvalidFileNameChars();

            var builder =
                new StringBuilder();

            foreach (var character in value)
            {
                if (invalidCharacters.Contains(
                    character))
                {
                    builder.Append('_');
                }
                else
                {
                    builder.Append(character);
                }
            }

            var result =
                builder
                    .ToString()
                    .Trim();

            return string.IsNullOrWhiteSpace(result)
                ? $"VolunteerContract_{DateTime.Now:yyyyMMddHHmmss}"
                : result;
        }
    }
}