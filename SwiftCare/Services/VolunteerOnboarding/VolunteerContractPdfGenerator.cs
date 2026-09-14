using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SwiftCare.Data;
using SwiftCare.Models.VolunteerOnboarding;

namespace SwiftCare.Services.VolunteerOnboarding
{
    public interface IVolunteerContractPdfGenerator
    {
        Task<VolunteerContract> GeneratePdfAsync(
            VolunteerContract contract,
            CancellationToken cancellationToken = default);
    }

    public sealed class VolunteerContractPdfGenerator
        : IVolunteerContractPdfGenerator
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IVolunteerContractContentBuilder _contentBuilder;
        private readonly SwiftCareDbContext _context;
        private readonly ILogger<VolunteerContractPdfGenerator> _logger;

        public VolunteerContractPdfGenerator(
            IWebHostEnvironment environment,
            IVolunteerContractContentBuilder contentBuilder,
            SwiftCareDbContext context,
            ILogger<VolunteerContractPdfGenerator> logger)
        {
            _environment = environment;
            _contentBuilder = contentBuilder;
            _context = context;
            _logger = logger;

            QuestPDF.Settings.License = LicenseType.Community;
        }


        // ================================================================
        // GENERATE PDF
        // ================================================================

        public async Task<VolunteerContract> GeneratePdfAsync(
            VolunteerContract contract,
            CancellationToken cancellationToken = default)
        {
            if (contract == null)
                throw new ArgumentNullException(nameof(contract));

            cancellationToken.ThrowIfCancellationRequested();


            // ============================================================
            // BUILD DATA
            // ============================================================

            var documentModel =
                await _contentBuilder.BuildAsync(
                    contract,
                    cancellationToken);

            if (documentModel == null)
            {
                throw new InvalidOperationException(
                    "The volunteer contract content could not be built.");
            }


            // ============================================================
            // LOAD HTML TEMPLATE
            // ============================================================

            var templatePath = Path.Combine(
                _environment.WebRootPath,
                "Templates",
                "VolunteerContracts",
                "VolunteerServiceAgreement.html");

            if (!System.IO.File.Exists(templatePath))
            {
                throw new FileNotFoundException(
                    "The SwiftCare volunteer contract HTML template could not be found.",
                    templatePath);
            }

            var htmlTemplate =
                await System.IO.File.ReadAllTextAsync(
                    templatePath,
                    Encoding.UTF8,
                    cancellationToken);


            // ============================================================
            // POPULATE HTML
            // ============================================================

            var populatedHtml =
                PopulateTemplate(
                    htmlTemplate,
                    documentModel);


            // ============================================================
            // PARSE HTML
            // ============================================================

            var htmlDocument =
                new HtmlDocument();

            htmlDocument.LoadHtml(populatedHtml);


            // ============================================================
            // LOG
            // ============================================================

            _logger.LogInformation(
                "[SWIFTCARE CONTRACT] HTML contract parsed successfully. " +
                "ContractReference={ContractReference}",
                contract.ContractReference);


            // ============================================================
            // GENERATED DIRECTORY
            // ============================================================

            var generatedDirectory =
                Path.Combine(
                    _environment.WebRootPath,
                    "UploadedFiles",
                    "VolunteerContracts",
                    "Generated");

            Directory.CreateDirectory(generatedDirectory);


            // ============================================================
            // FILE NAME
            // ============================================================

            var safeReference =
                MakeSafeFileName(
                    contract.ContractReference);

            var fileName =
                $"{safeReference}.pdf";

            var physicalPath =
                Path.Combine(
                    generatedDirectory,
                    fileName);


            // ============================================================
            // BUILD QUESTPDF DOCUMENT
            // ============================================================

            var questDocument =
                BuildQuestPdfDocument(
                    htmlDocument,
                    documentModel);


            // ============================================================
            // GENERATE
            // ============================================================

            await Task.Run(
                () =>
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    questDocument.GeneratePdf(
                        physicalPath);
                },
                cancellationToken);


            // ============================================================
            // VERIFY
            // ============================================================

            if (!System.IO.File.Exists(physicalPath))
            {
                throw new InvalidOperationException(
                    "The volunteer contract PDF was not created.");
            }

            var fileInfo =
                new FileInfo(physicalPath);

            if (fileInfo.Length <= 0)
            {
                throw new InvalidOperationException(
                    "The volunteer contract PDF was created but is empty.");
            }


            // ============================================================
            // SAVE PATH
            // ============================================================

            var relativePath =
                $"/UploadedFiles/VolunteerContracts/Generated/{fileName}";


            // ============================================================
            // UPDATE CONTRACT
            // ============================================================

            contract.GeneratedDate =
                DateTime.Now;

            contract.GeneratedFileName =
                fileName;

            contract.GeneratedFilePath =
                relativePath;

            contract.Status =
                VolunteerContractStatus.AwaitingSignature;

            contract.ModifiedDate =
                DateTime.Now;


            // ============================================================
            // SAVE
            // ============================================================

            await _context.SaveChangesAsync(
                cancellationToken);


            _logger.LogInformation(
                "[SWIFTCARE CONTRACT] PDF generated successfully. " +
                "ContractId={ContractId}, " +
                "ContractReference={ContractReference}, " +
                "FileSize={FileSize}, " +
                "GeneratedFilePath={GeneratedFilePath}",
                contract.VolunteerContractId,
                contract.ContractReference,
                fileInfo.Length,
                contract.GeneratedFilePath);


            return contract;
        }


        // ================================================================
        // TEMPLATE POPULATION
        // ================================================================

        private static string PopulateTemplate(
            string html,
            VolunteerContractDocument document)
        {
            var values =
                new Dictionary<string, string>(
                    StringComparer.OrdinalIgnoreCase)
                {
                    ["ContractReference"] =
                        document.ContractReference,

                    ["ContractVersion"] =
                        document.ContractVersion,

                    ["OrganisationName"] =
                        document.OrganisationName
                };


            // ============================================================
            // GET FIELDS FROM CONTENT MODEL
            // ============================================================

            foreach (var section in document.Sections)
            {
                if (section.Fields == null)
                    continue;

                foreach (var field in section.Fields)
                {
                    if (string.IsNullOrWhiteSpace(field.Label))
                        continue;

                    var key =
                        NormalizePlaceholderKey(
                            field.Label);

                    if (!string.IsNullOrWhiteSpace(key))
                    {
                        values[key] =
                            field.Value ?? string.Empty;
                    }
                }
            }


            // ============================================================
            // STANDARD ALIASES
            // ============================================================

            var volunteerName =
                GetField(
                    document,
                    "Volunteer");

            var volunteerEmail =
                GetField(
                    document,
                    "Email");

            var positionTitle =
                GetField(
                    document,
                    "Position");

            var category =
                GetField(
                    document,
                    "Category");

            var area =
                GetField(
                    document,
                    "Area");

            var location =
                GetField(
                    document,
                    "Location");

            var startDate =
                GetField(
                    document,
                    "Start Date");

            var endDate =
                GetField(
                    document,
                    "End Date");

            var startTime =
                GetField(
                    document,
                    "Start Time");

            var endTime =
                GetField(
                    document,
                    "End Time");

            var minimumAge =
                GetField(
                    document,
                    "Minimum Age");


            // ============================================================
            // LOCATION
            // ============================================================

            var locationParts =
                SplitLocation(location);

            values["VolunteerName"] =
                volunteerName;

            values["VolunteerEmail"] =
                volunteerEmail;

            values["PositionTitle"] =
                positionTitle;

            values["Category"] =
                category;

            values["Area"] =
                area;

            values["CityName"] =
                locationParts.City;

            values["Province"] =
                locationParts.Province;

            values["StartDate"] =
                startDate;

            values["EndDate"] =
                endDate;

            values["StartTime"] =
                startTime;

            values["EndTime"] =
                endTime;

            values["MinimumAge"] =
                minimumAge;


            // ============================================================
            // ROLE DESCRIPTION
            // ============================================================

            var roleDescription =
                document.Sections
                    .FirstOrDefault(
                        x =>
                            string.Equals(
                                x.Title,
                                "2. VOLUNTEER POSITION",
                                StringComparison.OrdinalIgnoreCase))
                    ?.Paragraphs?
                    .FirstOrDefault();

            values["RoleDescription"] =
                roleDescription ?? string.Empty;


            // ============================================================
            // DYNAMIC LISTS
            // ============================================================

            values["Responsibilities"] =
                BuildHtmlListItems(
                    FindSectionBullets(
                        document,
                        "5. VOLUNTEER RESPONSIBILITIES"));

            values["Requirements"] =
                BuildHtmlListItems(
                    FindSectionBullets(
                        document,
                        "6. ROLE REQUIREMENTS"));

            values["RequiredSkills"] =
                BuildHtmlListItems(
                    FindSectionBullets(
                        document,
                        "7. REQUIRED SKILLS"));

            values["AdditionalClauses"] =
                BuildHtmlListItems(
                    FindSectionBullets(
                        document,
                        "13. ADDITIONAL ROLE CONDITIONS"));


            // ============================================================
            // BASIC PLACEHOLDER REPLACEMENT
            // ============================================================

            foreach (var pair in values)
            {
                var placeholder =
                    "{{" + pair.Key + "}}";

                html =
                    html.Replace(
                        placeholder,
                        HtmlEncode(pair.Value),
                        StringComparison.OrdinalIgnoreCase);
            }


            // ============================================================
            // REMOVE UNKNOWN PLACEHOLDERS
            // ============================================================

            html =
                Regex.Replace(
                    html,
                    @"\{\{[A-Za-z0-9_]+\}\}",
                    string.Empty);


            return html;
        }


        // ================================================================
        // QUESTPDF DOCUMENT
        // ================================================================

        private static Document BuildQuestPdfDocument(
            HtmlDocument htmlDocument,
            VolunteerContractDocument document)
        {
            var styles =
                document.Styles;

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    ConfigurePage(
                        page,
                        styles);

                    page.Header()
                        .Element(
                            header =>
                                BuildHeader(
                                    header,
                                    htmlDocument,
                                    document,
                                    styles));

                    page.Content()
                        .Element(
                            content =>
                                BuildHtmlContent(
                                    content,
                                    htmlDocument,
                                    styles));

                    page.Footer()
                        .Element(
                            footer =>
                                BuildFooter(
                                    footer,
                                    document,
                                    styles));
                });
            });
        }


        // ================================================================
        // PAGE CONFIGURATION
        // ================================================================

        private static void ConfigurePage(
            PageDescriptor page,
            VolunteerContractStyles styles)
        {
            page.Size(
                ParsePageSize(
                    styles.PageSize));

            page.MarginTop(
                42);

            page.MarginBottom(
                45);

            page.MarginLeft(
                42);

            page.MarginRight(
                42);

            page.DefaultTextStyle(
                text =>
                    text
                        .FontFamily(
                            "Arial")
                        .FontSize(
                            9.5f)
                        .FontColor(
                            "#263238"));
        }


        // ================================================================
        // HEADER
        // ================================================================

        private static void BuildHeader(
            IContainer container,
            HtmlDocument htmlDocument,
            VolunteerContractDocument document,
            VolunteerContractStyles styles)
        {
            container
                .PaddingBottom(10)
                .BorderBottom(2)
                .BorderColor(
                    "#1B5E20")
                .Row(row =>
                {
                    row.ConstantItem(34)
                        .Height(34)
                        .AlignMiddle()
                        .Element(
                            logo =>
                                BuildHeartPulseLogo(
                                    logo,
                                    styles));

                    row.ConstantItem(10);

                    row.RelativeItem()
                        .Column(column =>
                        {
                            column.Item()
                                .Text("SwiftCare")
                                .FontFamily("Arial")
                                .FontSize(13)
                                .Bold()
                                .FontColor("#1B5E20");

                            column.Item()
                                .PaddingTop(2)
                                .Text(
                                    "VOLUNTEER SERVICE AGREEMENT")
                                .FontSize(7)
                                .Bold()
                                .LetterSpacing(1)
                                .FontColor("#607D63");
                        });

                    row.ConstantItem(170)
                        .AlignRight()
                        .Column(column =>
                        {
                            column.Item()
                                .Text(
                                    document.ContractReference)
                                .FontSize(8)
                                .Bold()
                                .FontColor("#1B5E20");

                            column.Item()
                                .PaddingTop(2)
                                .Text(
                                    $"Version {document.ContractVersion}")
                                .FontSize(7)
                                .FontColor("#607D63");
                        });
                });
        }


        // ================================================================
        // HEART PULSE LOGO
        // ================================================================

        private static void BuildHeartPulseLogo(
            IContainer container,
            VolunteerContractStyles styles)
        {
            const string svg = """
<svg xmlns="http://www.w3.org/2000/svg"
     width="34"
     height="34"
     viewBox="0 0 100 100">

    <circle
        cx="50"
        cy="50"
        r="47"
        fill="#F0FDF4"
        stroke="#1B5E20"
        stroke-width="5"/>

    <path
        d="M50 76
           C45 70 23 57 23 40
           C23 29 31 23 40 23
           C46 23 50 27 50 32
           C50 27 54 23 60 23
           C69 23 77 29 77 40
           C77 57 55 70 50 76Z"
        fill="#8BC34A"
        stroke="#1B5E20"
        stroke-width="4"
        stroke-linejoin="round"/>

    <path
        d="M16 51
           H31
           L37 39
           L44 63
           L51 45
           L57 54
           H84"
        fill="none"
        stroke="#1B5E20"
        stroke-width="4"
        stroke-linecap="round"
        stroke-linejoin="round"/>

</svg>
""";

            container.Svg(svg);
        }


        // ================================================================
        // HTML CONTENT
        // ================================================================

        private static void BuildHtmlContent(
            IContainer container,
            HtmlDocument htmlDocument,
            VolunteerContractStyles styles)
        {
            var body =
                htmlDocument.DocumentNode
                    .SelectSingleNode("//body");

            if (body == null)
                return;


            container
                .Column(column =>
                {
                    foreach (var node in body.ChildNodes)
                    {
                        if (node.NodeType !=
                            HtmlNodeType.Element)
                            continue;

                        var tag =
                            node.Name.ToLowerInvariant();


                        // ------------------------------------------------
                        // BRAND
                        // ------------------------------------------------

                        if (tag == "company-brand")
                        {
                            continue;
                        }


                        // ------------------------------------------------
                        // METADATA
                        // ------------------------------------------------

                        if (tag == "contract-metadata")
                        {
                            continue;
                        }


                        // ------------------------------------------------
                        // DOCUMENT HEADER
                        // ------------------------------------------------

                        if (tag == "document-header")
                        {
                            RenderDocumentHeader(
                                column,
                                node,
                                styles);

                            continue;
                        }


                        // ------------------------------------------------
                        // SECTIONS
                        // ------------------------------------------------

                        if (tag == "section")
                        {
                            column.Item()
                                .PaddingTop(14)
                                .Element(
                                    section =>
                                        RenderSection(
                                            section,
                                            node,
                                            styles));

                            continue;
                        }


                        // ------------------------------------------------
                        // FOOTER
                        // ------------------------------------------------

                        if (tag == "footer")
                        {
                            continue;
                        }
                    }
                });
        }


        // ================================================================
        // DOCUMENT HEADER
        // ================================================================

        private static void RenderDocumentHeader(
            ColumnDescriptor column,
            HtmlNode node,
            VolunteerContractStyles styles)
        {
            var title =
                node.SelectSingleNode(
                    "./document-title");

            var subtitle =
                node.SelectSingleNode(
                    "./document-subtitle");


            if (title != null)
            {
                column.Item()
                    .PaddingTop(18)
                    .Text(
                        CleanNodeText(
                            title))
                    .FontSize(21)
                    .Bold()
                    .FontColor("#1B5E20");
            }


            if (subtitle != null)
            {
                column.Item()
                    .PaddingTop(5)
                    .Text(
                        CleanNodeText(
                            subtitle))
                    .FontSize(8.5f)
                    .FontColor("#607D63");
            }


            column.Item()
                .PaddingTop(11)
                .Height(4)
                .Width(55)
                .Background("#8BC34A");
        }


        // ================================================================
        // SECTION
        // ================================================================

        private static void RenderSection(
            IContainer container,
            HtmlNode section,
            VolunteerContractStyles styles)
        {
            container.Column(column =>
            {
                var sectionTitle =
                    section.SelectSingleNode(
                        "./section-title");


                // --------------------------------------------------------
                // SECTION HEADING
                // --------------------------------------------------------

                if (sectionTitle != null)
                {
                    column.Item()
                        .Background("#F0FDF4")
                        .BorderBottom(1)
                        .BorderColor("#D5E7D6")
                        .PaddingHorizontal(9)
                        .PaddingVertical(7)
                        .Row(row =>
                        {
                            row.ConstantItem(5)
                                .Height(15)
                                .Background("#8BC34A");

                            row.RelativeItem()
                                .PaddingLeft(8)
                                .Text(
                                    CleanNodeText(
                                        sectionTitle))
                                .FontSize(12)
                                .Bold()
                                .FontColor("#1B5E20");
                        });
                }


                // --------------------------------------------------------
                // SECTION CHILDREN
                // --------------------------------------------------------

                foreach (var node in section.ChildNodes)
                {
                    if (node.NodeType !=
                        HtmlNodeType.Element)
                        continue;

                    var tag =
                        node.Name.ToLowerInvariant();


                    switch (tag)
                    {
                        case "section-title":
                            break;

                        case "field":
                            RenderField(
                                column,
                                node,
                                styles);
                            break;

                        case "paragraph":
                            RenderParagraph(
                                column,
                                node,
                                styles);
                            break;

                        case "list":
                            RenderList(
                                column,
                                node,
                                styles);
                            break;

                        case "signatures":
                            RenderSignatures(
                                column,
                                node,
                                styles);
                            break;
                    }
                }
            });
        }


        // ================================================================
        // FIELD
        // ================================================================

        private static void RenderField(
            ColumnDescriptor column,
            HtmlNode node,
            VolunteerContractStyles styles)
        {
            var label =
                node.SelectSingleNode(
                    "./field-label");

            var value =
                node.SelectSingleNode(
                    "./field-value");


            var labelText =
                CleanNodeText(
                    label);

            var valueText =
                CleanNodeText(
                    value);


            if (string.IsNullOrWhiteSpace(valueText))
                return;


            column.Item()
                .PaddingTop(6)
                .Background("#F7FBF7")
                .Border(1)
                .BorderColor("#D5E7D6")
                .PaddingHorizontal(9)
                .PaddingVertical(6)
                .Row(row =>
                {
                    row.ConstantItem(120)
                        .Text(labelText)
                        .FontSize(8.5f)
                        .Bold()
                        .FontColor("#1B5E20");

                    row.RelativeItem()
                        .Text(valueText)
                        .FontSize(9)
                        .FontColor("#263238");
                });
        }


        // ================================================================
        // PARAGRAPH
        // ================================================================

        private static void RenderParagraph(
            ColumnDescriptor column,
            HtmlNode node,
            VolunteerContractStyles styles)
        {
            var text =
                CleanNodeText(
                    node);

            if (string.IsNullOrWhiteSpace(text))
                return;


            var isMuted =
                node.GetAttributeValue(
                    "class",
                    string.Empty)
                    .Contains(
                        "muted",
                        StringComparison.OrdinalIgnoreCase);


            column.Item()
                .PaddingTop(7)
                .Text(text)
                .FontSize(9.5f)
                .LineHeight(1.45f)
                .FontColor(
                    isMuted
                        ? "#607D63"
                        : "#263238");
        }


        // ================================================================
        // LIST
        // ================================================================

        private static void RenderList(
            ColumnDescriptor column,
            HtmlNode node,
            VolunteerContractStyles styles)
        {
            var items =
                node.SelectNodes("./item");


            if (items == null ||
                items.Count == 0)
            {
                return;
            }


            column.Item()
                .PaddingTop(7)
                .Background("#F7FBF7")
                .BorderLeft(3)
                .BorderColor("#8BC34A")
                .PaddingLeft(11)
                .PaddingRight(9)
                .PaddingTop(8)
                .PaddingBottom(8)
                .Column(listColumn =>
                {
                    foreach (var item in items)
                    {
                        var text =
                            CleanNodeText(
                                item);

                        if (string.IsNullOrWhiteSpace(text))
                            continue;


                        listColumn.Item()
                            .PaddingBottom(5)
                            .Row(row =>
                            {
                                row.ConstantItem(16)
                                    .AlignTop()
                                    .Text("•")
                                    .FontSize(11)
                                    .Bold()
                                    .FontColor("#1B5E20");

                                row.RelativeItem()
                                    .Text(text)
                                    .FontSize(9.5f)
                                    .LineHeight(1.45f)
                                    .FontColor("#263238");
                            });
                    }
                });
        }


        // ================================================================
        // SIGNATURES
        // ================================================================

        private static void RenderSignatures(
            ColumnDescriptor column,
            HtmlNode node,
            VolunteerContractStyles styles)
        {
            var signatures =
                node.SelectNodes("./signature");


            if (signatures == null ||
                signatures.Count == 0)
            {
                return;
            }


            column.Item()
                .PaddingTop(18)
                .Background("#F0FDF4")
                .BorderBottom(1)
                .BorderColor("#D5E7D6")
                .PaddingHorizontal(9)
                .PaddingVertical(7)
                .Text("SIGNATURES")
                .FontSize(12)
                .Bold()
                .FontColor("#1B5E20");


            column.Item()
                .PaddingTop(18)
                .Row(row =>
                {
                    foreach (var signature in signatures)
                    {
                        var name =
                            CleanNodeText(
                                signature.SelectSingleNode(
                                    "./signature-name"));

                        var party =
                            CleanNodeText(
                                signature.SelectSingleNode(
                                    "./signature-party"));

                        var date =
                            CleanNodeText(
                                signature.SelectSingleNode(
                                    "./signature-date"));


                        row.RelativeItem()
                            .PaddingRight(18)
                            .Column(signatureColumn =>
                            {
                                signatureColumn.Item()
                                    .Height(35)
                                    .BorderBottom(1)
                                    .BorderColor("#1B5E20");

                                signatureColumn.Item()
                                    .PaddingTop(7)
                                    .Text(name)
                                    .FontSize(9)
                                    .Bold()
                                    .FontColor("#263238");

                                signatureColumn.Item()
                                    .PaddingTop(2)
                                    .Text(party)
                                    .FontSize(8)
                                    .FontColor("#607D63");

                                signatureColumn.Item()
                                    .PaddingTop(9)
                                    .Text(date)
                                    .FontSize(8)
                                    .FontColor("#263238");
                            });
                    }
                });
        }


        // ================================================================
        // FOOTER
        // ================================================================

        private static void BuildFooter(
            IContainer container,
            VolunteerContractDocument document,
            VolunteerContractStyles styles)
        {
            container
                .BorderTop(1)
                .BorderColor("#D5E7D6")
                .PaddingTop(6)
                .Row(row =>
                {
                    row.RelativeItem()
                        .Text(
                            $"SwiftCare Volunteer Contract | " +
                            $"{document.ContractReference} | " +
                            $"Version {document.ContractVersion}")
                        .FontSize(7.5f)
                        .FontColor("#607D63");

                    row.ConstantItem(75)
                        .AlignRight()
                        .Text(text =>
                        {
                            text.DefaultTextStyle(
                                style =>
                                    style
                                        .FontSize(7.5f)
                                        .FontColor("#607D63"));

                            text.Span("Page ");
                            text.CurrentPageNumber();

                            text.Span(" of ");
                            text.TotalPages();
                        });
                });
        }


        // ================================================================
        // FIELD LOOKUP
        // ================================================================

        private static string GetField(
            VolunteerContractDocument document,
            string label)
        {
            foreach (var section in document.Sections)
            {
                if (section.Fields == null)
                    continue;

                foreach (var field in section.Fields)
                {
                    if (string.Equals(
                            field.Label?.Trim(),
                            label,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return field.Value?.Trim()
                            ?? string.Empty;
                    }
                }
            }

            return string.Empty;
        }


        // ================================================================
        // SECTION BULLETS
        // ================================================================

        private static IEnumerable<string> FindSectionBullets(
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
                ?? Enumerable.Empty<string>();
        }


        // ================================================================
        // HTML LIST ITEMS
        // ================================================================

        private static string BuildHtmlListItems(
            IEnumerable<string> values)
        {
            var builder =
                new StringBuilder();


            foreach (var value in values)
            {
                if (string.IsNullOrWhiteSpace(value))
                    continue;

                builder.Append(
                    "<item>");

                builder.Append(
                    HtmlEncode(
                        value.Trim()));

                builder.Append(
                    "</item>");
            }


            return builder.ToString();
        }


        // ================================================================
        // PLACEHOLDER KEY
        // ================================================================

        private static string NormalizePlaceholderKey(
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;


            return value.Trim() switch
            {
                "Volunteer" =>
                    "VolunteerName",

                "Email" =>
                    "VolunteerEmail",

                "Position" =>
                    "PositionTitle",

                "Start Date" =>
                    "StartDate",

                "End Date" =>
                    "EndDate",

                "Start Time" =>
                    "StartTime",

                "End Time" =>
                    "EndTime",

                "Minimum Age" =>
                    "MinimumAge",

                _ =>
                    Regex.Replace(
                        value.Trim(),
                        @"[^A-Za-z0-9]",
                        string.Empty)
            };
        }


        // ================================================================
        // LOCATION
        // ================================================================

        private static (
            string City,
            string Province)
            SplitLocation(
                string? location)
        {
            if (string.IsNullOrWhiteSpace(location))
                return (
                    string.Empty,
                    string.Empty);


            var parts =
                location
                    .Split(
                        ',',
                        StringSplitOptions.RemoveEmptyEntries)
                    .Select(
                        x => x.Trim())
                    .ToArray();


            if (parts.Length == 1)
                return (
                    parts[0],
                    string.Empty);


            return (
                parts[0],
                string.Join(
                    ", ",
                    parts.Skip(1)));
        }


        // ================================================================
        // HTML ENCODING
        // ================================================================

        private static string HtmlEncode(
            string? value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;


            return System.Net.WebUtility.HtmlEncode(
                value);
        }


        // ================================================================
        // NODE TEXT
        // ================================================================

        private static string CleanNodeText(
            HtmlNode? node)
        {
            if (node == null)
                return string.Empty;


            var text =
                HtmlEntity.DeEntitize(
                    node.InnerText ?? string.Empty);


            return Regex.Replace(
                    text,
                    @"\s+",
                    " ")
                .Trim();
        }


        // ================================================================
        // PAGE SIZE
        // ================================================================

        private static PageSize ParsePageSize(
            string? pageSize)
        {
            return pageSize?
                .Trim()
                .ToUpperInvariant() switch
            {
                "A3" =>
                    PageSizes.A3,

                "A5" =>
                    PageSizes.A5,

                "LETTER" =>
                    PageSizes.Letter,

                "LEGAL" =>
                    PageSizes.Legal,

                _ =>
                    PageSizes.A4
            };
        }


        // ================================================================
        // SAFE FILE NAME
        // ================================================================

        private static string MakeSafeFileName(
            string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return
                    $"SwiftCare-Volunteer-Contract-{DateTime.Now:yyyyMMddHHmmss}";
            }


            var invalid =
                Path.GetInvalidFileNameChars();


            var builder =
                new StringBuilder(
                    value.Length);


            foreach (var character in value)
            {
                if (invalid.Contains(character))
                {
                    builder.Append('_');
                }
                else
                {
                    builder.Append(character);
                }
            }


            return builder
                .ToString()
                .Trim();
        }
    }
}