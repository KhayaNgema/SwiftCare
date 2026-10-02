using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
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
                throw new ArgumentNullException(
                    nameof(contract));
            }

            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogInformation(
                "Generating volunteer contract PDF for {ContractReference}",
                contract.ContractReference);

            // ========================================================
            // BUILD DOCUMENT MODEL
            // ========================================================

            var documentModel =
                await _contentBuilder.BuildAsync(
                    contract,
                    cancellationToken);

            // ========================================================
            // HTML TEMPLATE
            // ========================================================

            var templatePath =
                Path.Combine(
                    _environment.WebRootPath,
                    "Templates",
                    "VolunteerContracts",
                    "VolunteerServiceAgreement.html");

            if (!File.Exists(templatePath))
            {
                throw new FileNotFoundException(
                    "Volunteer contract HTML template was not found.",
                    templatePath);
            }

            var htmlTemplate =
                await File.ReadAllTextAsync(
                    templatePath,
                    cancellationToken);

            // ========================================================
            // POPULATE TEMPLATE
            // ========================================================

            var populatedHtml =
                PopulateTemplate(
                    htmlTemplate,
                    documentModel);

            // ========================================================
            // PARSE HTML
            // ========================================================

            var htmlDocument =
                new HtmlDocument();

            htmlDocument.LoadHtml(
                populatedHtml);

            // ========================================================
            // SAFE FILE NAME
            // ========================================================

            var safeReference =
                MakeSafeFileName(
                    contract.ContractReference);

            var fileName =
                $"{safeReference}.pdf";

            // ========================================================
            // BUILD QUEST PDF
            // ========================================================

            var pdfDocument =
                BuildQuestPdfDocument(
                    htmlDocument,
                    documentModel);

            // ========================================================
            // GENERATE PDF IN MEMORY
            // ========================================================

            await using var pdfStream =
                new MemoryStream();

            await Task.Run(
                () =>
                {
                    pdfDocument.GeneratePdf(
                        pdfStream);
                },
                cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            var pdfBytes =
                pdfStream.ToArray();

            if (pdfBytes.Length == 0)
            {
                throw new InvalidOperationException(
                    "The volunteer contract PDF could not be generated.");
            }
            // ============================================================
            // UPLOAD GENERATED PDF DIRECTLY TO AZURE BLOB
            // ============================================================

            _logger.LogInformation(
                "[CONTRACT PDF] Uploading generated contract directly to Azure Blob Storage. " +
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
                throw new InvalidOperationException(
                    "Azure Blob Storage returned an empty blob path for the generated contract.");
            }

            _logger.LogInformation(
                "[CONTRACT PDF] Generated contract uploaded successfully. " +
                "BlobPath={BlobPath}",
                blobPath);

            // ============================================================
            // VERIFY BLOB
            // ============================================================

            var blobExists =
                await _fileUploadService.FileExistsAsync(
                    blobPath,
                    cancellationToken);

            if (!blobExists)
            {
                throw new InvalidOperationException(
                    $"Generated contract was uploaded but could not be verified in Azure Blob Storage. BlobPath={blobPath}");
            }

            // ============================================================
            // STORE BLOB INFORMATION IN DATABASE ENTITY
            // ============================================================

            contract.GeneratedFileName =
                fileName;

            contract.GeneratedFilePath =
                blobPath;

            _logger.LogInformation(
                "[CONTRACT PDF] Contract blob verified. " +
                "ContractId={ContractId}, FileName={FileName}, BlobPath={BlobPath}",
                contract.VolunteerContractId,
                contract.GeneratedFileName,
                contract.GeneratedFilePath);

            contract.Status =
                VolunteerContractStatus.AwaitingApplicant;

            contract.ModifiedDate =
                DateTime.Now;

            _context.VolunteerContracts.Update(
                contract);

            await _context.SaveChangesAsync(
                cancellationToken);

            _logger.LogInformation(
                "Volunteer contract PDF generated and uploaded successfully. " +
                "ContractReference={ContractReference}, BlobPath={BlobPath}",
                contract.ContractReference,
                blobPath);

            return contract;
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

            foreach (var section in document.Sections)
            {
                foreach (var field in section.Fields)
                {
                    if (string.IsNullOrWhiteSpace(
                        field.Label))
                    {
                        continue;
                    }

                    values[field.Label.Trim()] =
                        field.Value?.Trim()
                        ?? string.Empty;
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
                "OpportunityCategory");

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
            return document.Sections
                .FirstOrDefault(
                    x =>
                        string.Equals(
                            x.Title,
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
        // BUILD HTML LIST
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

                builder.Append("<item>");

                builder.Append(
                    System.Net.WebUtility.HtmlEncode(
                        item.Trim()));

                builder.Append("</item>");
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
            else
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

        // ============================================================
        // QUEST PDF DOCUMENT
        // ============================================================

        private IDocument BuildQuestPdfDocument(
            HtmlDocument htmlDocument,
            VolunteerContractDocument documentModel)
        {
            return Document.Create(
                container =>
                {
                    container.Page(
                        page =>
                        {
                            page.Size(
                                PageSizes.A4);

                            page.Margin(
                                40);

                            page.DefaultTextStyle(
                                x =>
                                    x.FontSize(10));

                            page.Header()
                                .Element(
                                    header =>
                                        BuildHeader(
                                            header,
                                            documentModel));

                            page.Content()
                                .Element(
                                    content =>
                                        BuildHtmlContent(
                                            content,
                                            htmlDocument));

                            page.Footer()
                                .AlignCenter()
                                .Text(
                                    text =>
                                    {
                                        text.Span(
                                            "SwiftCare Volunteer Service Agreement");

                                        text.Span(
                                            "  •  ");

                                        text.Span(
                                            documentModel.ContractReference
                                            ?? string.Empty);
                                    });
                        });
                });
        }

        // ============================================================
        // HEADER
        // ============================================================

        private static void BuildHeader(
            IContainer container,
            VolunteerContractDocument document)
        {
            container
                .PaddingBottom(10)
                .Column(
                    column =>
                    {
                        column.Item()
                            .Text(
                                document.OrganisationName
                                ?? "SwiftCare")
                            .FontSize(20)
                            .Bold();

                        column.Item()
                            .Text(
                                "VOLUNTEER SERVICE AGREEMENT")
                            .FontSize(14)
                            .Bold();

                        column.Item()
                            .Text(
                                $"Contract Reference: " +
                                $"{document.ContractReference}");

                        column.Item()
                            .Text(
                                $"Version: " +
                                $"{document.ContractVersion}");
                    });
        }

        // ============================================================
        // HTML BODY
        // ============================================================

        private static void BuildHtmlContent(
            IContainer container,
            HtmlDocument htmlDocument)
        {
            var body =
                htmlDocument.DocumentNode
                    .SelectSingleNode(
                        "//body");

            if (body == null)
            {
                container.Text(
                    "Volunteer contract content could not be loaded.");

                return;
            }

            container.Column(
                column =>
                {
                    foreach (
                        var node
                        in body.ChildNodes)
                    {
                        RenderNode(
                            column,
                            node);
                    }
                });
        }

        // ============================================================
        // RENDER HTML NODE
        // ============================================================

        private static void RenderNode(
            ColumnDescriptor column,
            HtmlNode node)
        {
            if (node.NodeType ==
                HtmlNodeType.Text)
            {
                var text =
                    HtmlEntity.DeEntitize(
                        node.InnerText)
                    .Trim();

                if (!string.IsNullOrWhiteSpace(
                    text))
                {
                    column.Item()
                        .PaddingBottom(5)
                        .Text(text);
                }

                return;
            }

            if (node.NodeType !=
                HtmlNodeType.Element)
            {
                return;
            }

            switch (node.Name.ToLowerInvariant())
            {
                case "section":

                    column.Item()
                        .PaddingTop(10)
                        .Element(
                            container =>
                                RenderSection(
                                    container,
                                    node));

                    break;

                case "p":
                case "paragraph":

                    RenderParagraph(
                        column,
                        node);

                    break;

                case "h1":
                case "h2":
                case "h3":
                case "h4":

                    column.Item()
                        .PaddingTop(8)
                        .PaddingBottom(4)
                        .Text(
                            HtmlEntity.DeEntitize(
                                node.InnerText)
                                .Trim())
                        .Bold();

                    break;

                case "list":
                case "ul":
                case "ol":

                    column.Item()
                        .Element(
                            container =>
                                RenderList(
                                    container,
                                    node));

                    break;

                default:

                    if (node.ChildNodes.Count > 0)
                    {
                        foreach (
                            var child
                            in node.ChildNodes)
                        {
                            RenderNode(
                                column,
                                child);
                        }
                    }

                    break;
            }
        }

        // ============================================================
        // RENDER SECTION
        // ============================================================

        private static void RenderSection(
            IContainer container,
            HtmlNode section)
        {
            var titleNode =
                section.SelectSingleNode("./h1")
                ?? section.SelectSingleNode("./h2")
                ?? section.SelectSingleNode("./h3")
                ?? section.SelectSingleNode("./title");

            container.Column(
                column =>
                {
                    if (titleNode != null)
                    {
                        column.Item()
                            .PaddingBottom(6)
                            .Text(
                                HtmlEntity.DeEntitize(
                                    titleNode.InnerText)
                                    .Trim())
                            .Bold()
                            .FontSize(12);
                    }

                    foreach (
                        var child
                        in section.ChildNodes)
                    {
                        if (child == titleNode)
                        {
                            continue;
                        }

                        RenderSectionChild(
                            column,
                            child);
                    }
                });
        }

        // ============================================================
        // SECTION CHILD
        // ============================================================

        private static void RenderSectionChild(
            ColumnDescriptor column,
            HtmlNode node)
        {
            if (node.NodeType !=
                HtmlNodeType.Element)
            {
                return;
            }

            switch (node.Name.ToLowerInvariant())
            {
                case "field":

                    RenderField(
                        column,
                        node);

                    break;

                case "paragraph":
                case "p":

                    RenderParagraph(
                        column,
                        node);

                    break;

                case "list":
                case "ul":
                case "ol":

                    column.Item()
                        .Element(
                            container =>
                                RenderList(
                                    container,
                                    node));

                    break;

                case "signatures":

                    RenderSignatures(
                        column,
                        node);

                    break;

                default:

                    var text =
                        HtmlEntity.DeEntitize(
                            node.InnerText)
                        .Trim();

                    if (!string.IsNullOrWhiteSpace(
                        text))
                    {
                        column.Item()
                            .PaddingBottom(5)
                            .Text(text);
                    }

                    break;
            }
        }

        // ============================================================
        // FIELD
        // ============================================================

        private static void RenderField(
            ColumnDescriptor column,
            HtmlNode node)
        {
            var label =
                node.GetAttributeValue(
                    "label",
                    string.Empty);

            var value =
                node.GetAttributeValue(
                    "value",
                    string.Empty);

            if (string.IsNullOrWhiteSpace(
                value))
            {
                value =
                    HtmlEntity.DeEntitize(
                        node.InnerText)
                    .Trim();
            }

            column.Item()
                .PaddingBottom(4)
                .Row(
                    row =>
                    {
                        row.ConstantItem(130)
                            .Text(label)
                            .Bold();

                        row.RelativeItem()
                            .Text(value);
                    });
        }

        // ============================================================
        // PARAGRAPH
        // ============================================================

        private static void RenderParagraph(
            ColumnDescriptor column,
            HtmlNode node)
        {
            var text =
                HtmlEntity.DeEntitize(
                    node.InnerText)
                .Trim();

            if (string.IsNullOrWhiteSpace(
                text))
            {
                return;
            }

            column.Item()
                .PaddingBottom(6)
                .Text(text);
        }

        // ============================================================
        // LIST
        // ============================================================

        private static void RenderList(
            IContainer container,
            HtmlNode listNode)
        {
            var items =
                listNode.SelectNodes("./item")
                ?? listNode.SelectNodes("./li");

            if (items == null ||
                items.Count == 0)
            {
                return;
            }

            container.Column(
                column =>
                {
                    foreach (var item in items)
                    {
                        var text =
                            HtmlEntity.DeEntitize(
                                item.InnerText)
                            .Trim();

                        if (string.IsNullOrWhiteSpace(
                            text))
                        {
                            continue;
                        }

                        column.Item()
                            .PaddingBottom(4)
                            .Row(
                                row =>
                                {
                                    row.ConstantItem(15)
                                        .Text("•");

                                    row.RelativeItem()
                                        .Text(text);
                                });
                    }
                });
        }

        // ============================================================
        // SIGNATURES
        // ============================================================

        private static void RenderSignatures(
            ColumnDescriptor column,
            HtmlNode node)
        {
            var signatureNodes =
                node.SelectNodes(
                    "./signature");

            if (signatureNodes == null)
            {
                return;
            }

            column.Item()
                .PaddingTop(15)
                .Row(
                    row =>
                    {
                        foreach (
                            var signature
                            in signatureNodes)
                        {
                            var label =
                                signature.GetAttributeValue(
                                    "label",
                                    "Signature");

                            row.RelativeItem()
                                .PaddingRight(15)
                                .Column(
                                    signatureColumn =>
                                    {
                                        signatureColumn.Item()
                                            .Text(label)
                                            .Bold();

                                        signatureColumn.Item()
                                            .PaddingTop(20)
                                            .Text(
                                                "____________________________");

                                        signatureColumn.Item()
                                            .Text(
                                                "Signature");

                                        signatureColumn.Item()
                                            .PaddingTop(10)
                                            .Text(
                                                "Date: ________________________");
                                    });
                        }
                    });
        }
    }
}