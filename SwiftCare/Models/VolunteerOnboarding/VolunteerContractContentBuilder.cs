using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SwiftCare.Data;
using SwiftCare.Models;
using SwiftCare.Models.VolunteerOnboarding;

namespace SwiftCare.Services.VolunteerOnboarding
{
    public interface IVolunteerContractContentBuilder
    {
        Task<VolunteerContractDocument> BuildAsync(
            VolunteerContract contract,
            CancellationToken cancellationToken = default);
    }

    public sealed class VolunteerContractContentBuilder
        : IVolunteerContractContentBuilder
    {
        private readonly SwiftCareDbContext _context;
        private readonly ILogger<VolunteerContractContentBuilder> _logger;

        public VolunteerContractContentBuilder(
            SwiftCareDbContext context,
            ILogger<VolunteerContractContentBuilder> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<VolunteerContractDocument> BuildAsync(
            VolunteerContract contract,
            CancellationToken cancellationToken = default)
        {
            if (contract == null)
                throw new ArgumentNullException(nameof(contract));

            cancellationToken.ThrowIfCancellationRequested();

            /*
             * We intentionally use the historical snapshots stored on the
             * VolunteerContract.
             *
             * This means that if the opportunity is changed later, the
             * already-generated contract does NOT change.
             */

            var applicant =
                await _context.Applicants
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x => x.Id == contract.ApplicantId,
                        cancellationToken);

            var volunteerEmail =
                applicant?.Email ??
                contract.VolunteerApplication?.Applicant?.Email ??
                string.Empty;

            var volunteerName =
                BuildApplicantName(
                    applicant,
                    contract.VolunteerApplication?.Applicant);

            var document =
                new VolunteerContractDocument
                {
                    OrganisationName = "SwiftCare",
                    ContractReference =
                        contract.ContractReference,
                    ContractVersion =
                        contract.ContractVersion,
                    Styles =
                        BuildStyles()
                };

            // ============================================================
            // 1. PARTIES
            // ============================================================

            document.Sections.Add(
                new VolunteerContractSection
                {
                    Title = "1. PARTIES",

                    Paragraphs =
                    {
                        $"This Volunteer Service Agreement is entered into " +
                        $"between SwiftCare and {volunteerName}."
                    },

                    Fields =
                    {
                        new VolunteerContractField
                        {
                            Label = "Volunteer",
                            Value = volunteerName
                        },

                        new VolunteerContractField
                        {
                            Label = "Email",
                            Value = volunteerEmail
                        },

                        new VolunteerContractField
                        {
                            Label = "Contract Reference",
                            Value = contract.ContractReference
                        }
                    }
                });

            // ============================================================
            // 2. VOLUNTEER POSITION
            // ============================================================

            document.Sections.Add(
                new VolunteerContractSection
                {
                    Title = "2. VOLUNTEER POSITION",

                    Fields =
                    {
                        new VolunteerContractField
                        {
                            Label = "Position",
                            Value = contract.PositionTitle
                        },

                        new VolunteerContractField
                        {
                            Label = "Category",
                            Value = FormatEnum(contract.Category)
                        },

                        new VolunteerContractField
                        {
                            Label = "Area",
                            Value = FormatEnum(contract.Area)
                        },

                        new VolunteerContractField
                        {
                            Label = "Location",
                            Value = BuildLocation(
                                contract.CityName,
                                contract.Province)
                        }
                    },

                    Paragraphs =
                    {
                        CleanText(contract.RoleDescription)
                    }
                });

            // ============================================================
            // 3. TERM AND SCHEDULE
            // ============================================================

            document.Sections.Add(
                new VolunteerContractSection
                {
                    Title = "3. VOLUNTEER TERM AND SCHEDULE",

                    Fields =
                    {
                        new VolunteerContractField
                        {
                            Label = "Start Date",
                            Value = FormatDate(contract.StartDate)
                        },

                        new VolunteerContractField
                        {
                            Label = "End Date",
                            Value = FormatDate(contract.EndDate)
                        },

                        new VolunteerContractField
                        {
                            Label = "Start Time",
                            Value = FormatTime(contract.StartTime)
                        },

                        new VolunteerContractField
                        {
                            Label = "End Time",
                            Value = FormatTime(contract.EndTime)
                        },

                        new VolunteerContractField
                        {
                            Label = "Minimum Age",
                            Value = contract.MinimumAge.HasValue
                                ? $"{contract.MinimumAge.Value} years"
                                : string.Empty
                        }
                    }
                });

            // ============================================================
            // 4. PURPOSE
            // ============================================================

            document.Sections.Add(
                new VolunteerContractSection
                {
                    Title = "4. PURPOSE OF THE VOLUNTEER ROLE",

                    Paragraphs =
                    {
                        "The purpose of this volunteer role is to support " +
                        "SwiftCare's services and activities while contributing " +
                        "positively to the wellbeing, dignity and experience " +
                        "of residents, staff and the wider SwiftCare community."
                    }
                });

            // ============================================================
            // 5. RESPONSIBILITIES
            // ============================================================

            var responsibilities =
                ParseSnapshot(
                    contract.ResponsibilitiesSnapshot);

            _logger.LogInformation(
                "[SWIFTCARE CONTRACT] Responsibilities snapshot contains {Count} items.",
                responsibilities.Count);

            foreach (var responsibility in responsibilities)
            {
                _logger.LogInformation(
                    "[SWIFTCARE CONTRACT] RESPONSIBILITY: {Responsibility}",
                    responsibility);
            }

            document.Sections.Add(
                new VolunteerContractSection
                {
                    Title = "5. VOLUNTEER RESPONSIBILITIES",

                    Paragraphs =
                    {
                        "The volunteer agrees to perform the responsibilities " +
                        "associated with the approved volunteer position and " +
                        "to follow reasonable instructions issued by the " +
                        "designated SwiftCare supervisor."
                    },

                    Bullets = responsibilities
                });

            // ============================================================
            // 6. REQUIREMENTS
            // ============================================================

            var requirements =
                ParseSnapshot(
                    contract.RequirementsSnapshot);

            _logger.LogInformation(
                "[SWIFTCARE CONTRACT] Requirements snapshot contains {Count} items.",
                requirements.Count);

            foreach (var requirement in requirements)
            {
                _logger.LogInformation(
                    "[SWIFTCARE CONTRACT] REQUIREMENT: {Requirement}",
                    requirement);
            }

            document.Sections.Add(
                new VolunteerContractSection
                {
                    Title = "6. ROLE REQUIREMENTS",

                    Paragraphs =
                    {
                        "The volunteer must comply with all requirements " +
                        "applicable to the approved volunteer opportunity."
                    },

                    Bullets = requirements
                });

            // ============================================================
            // 7. REQUIRED SKILLS
            // ============================================================

            var requiredSkills =
                ParseSnapshot(
                    contract.RequiredSkillsSnapshot);

            _logger.LogInformation(
                "[SWIFTCARE CONTRACT] Required skills snapshot contains {Count} items.",
                requiredSkills.Count);

            foreach (var skill in requiredSkills)
            {
                _logger.LogInformation(
                    "[SWIFTCARE CONTRACT] REQUIRED SKILL: {Skill}",
                    skill);
            }

            document.Sections.Add(
                new VolunteerContractSection
                {
                    Title = "7. REQUIRED SKILLS",

                    Bullets = requiredSkills
                });

            // ============================================================
            // 8. SUPERVISION
            // ============================================================

            document.Sections.Add(
                new VolunteerContractSection
                {
                    Title = "8. SUPERVISION",

                    Paragraphs =
                    {
                        "The volunteer will work under the direction and " +
                        "supervision of the person designated by SwiftCare. " +
                        "The volunteer must follow reasonable instructions " +
                        "relating to their approved role and responsibilities."
                    }
                });

            // ============================================================
            // 9. SAFEGUARDING
            // ============================================================

            document.Sections.Add(
                new VolunteerContractSection
                {
                    Title =
                        "9. SAFEGUARDING AND RESIDENT PROTECTION",

                    Paragraphs =
                    {
                        "SwiftCare is committed to protecting residents " +
                        "and vulnerable persons. The volunteer must maintain " +
                        "appropriate boundaries and must immediately report " +
                        "suspected abuse, neglect, exploitation, unsafe conduct " +
                        "or other safeguarding concerns."
                    }
                });

            // ============================================================
            // 10. CONFIDENTIALITY
            // ============================================================

            document.Sections.Add(
                new VolunteerContractSection
                {
                    Title =
                        "10. CONFIDENTIALITY AND PRIVACY",

                    Paragraphs =
                    {
                        "The volunteer must keep confidential all personal, " +
                        "medical, operational, financial and other information " +
                        "obtained through their involvement with SwiftCare. " +
                        "Information must only be accessed, used or disclosed " +
                        "for authorised purposes."
                    }
                });

            // ============================================================
            // 11. HEALTH AND SAFETY
            // ============================================================

            document.Sections.Add(
                new VolunteerContractSection
                {
                    Title =
                        "11. HEALTH AND SAFETY",

                    Paragraphs =
                    {
                        "The volunteer must comply with SwiftCare health and " +
                        "safety requirements, use equipment responsibly, attend " +
                        "required training and immediately report hazards, " +
                        "accidents, injuries or unsafe conditions."
                    }
                });

            // ============================================================
            // 12. CONDUCT
            // ============================================================

            document.Sections.Add(
                new VolunteerContractSection
                {
                    Title = "12. CONDUCT",

                    Paragraphs =
                    {
                        "The volunteer must act respectfully, honestly and " +
                        "professionally and must not engage in harassment, " +
                        "discrimination, intimidation, violence, abuse, theft, " +
                        "fraud or other inappropriate conduct."
                    }
                });

            // ============================================================
            // 13. ADDITIONAL ROLE CONDITIONS
            // ============================================================

            document.Sections.Add(
                new VolunteerContractSection
                {
                    Title = "13. ADDITIONAL ROLE CONDITIONS",

                    Bullets = new List<string>()
                });

            // ============================================================
            // 14. TERMINATION
            // ============================================================

            document.Sections.Add(
                new VolunteerContractSection
                {
                    Title =
                        "14. TERMINATION OF VOLUNTEER ARRANGEMENT",

                    Paragraphs =
                    {
                        "SwiftCare may terminate the volunteer arrangement " +
                        "where the volunteer breaches this agreement, fails " +
                        "to comply with applicable policies, presents a " +
                        "safeguarding or safety concern, engages in serious " +
                        "misconduct, or where operational circumstances " +
                        "require the arrangement to end."
                    }
                });

            // ============================================================
            // 15. DECLARATION
            // ============================================================

            document.Sections.Add(
                new VolunteerContractSection
                {
                    Title = "15. VOLUNTEER DECLARATION",

                    Paragraphs =
                    {
                        "By signing this agreement, the volunteer confirms " +
                        "that they have read and understood the terms of this " +
                        "Volunteer Service Agreement and agree to comply with " +
                        "SwiftCare policies, procedures and reasonable instructions.",

                        "The volunteer confirms that the information supplied " +
                        "in connection with their volunteer application is " +
                        "accurate to the best of their knowledge."
                    }
                });

            // ============================================================
            // 16. SIGNATURES
            // ============================================================

            document.Sections.Add(
                new VolunteerContractSection
                {
                    Title = "16. SIGNATURES",

                    Paragraphs =
                    {
                        "By signing below, the parties acknowledge their " +
                        "understanding and acceptance of this Volunteer " +
                        "Service Agreement."
                    }
                });

            document.Signatures.Add(
                new VolunteerContractSignature
                {
                    Name = volunteerName,
                    Party = "Volunteer"
                });

            document.Signatures.Add(
                new VolunteerContractSignature
                {
                    Name = "Authorised Representative",
                    Party = "SwiftCare Representative"
                });

            _logger.LogInformation(
                "[SWIFTCARE CONTRACT] Contract document built. " +
                "Reference={Reference}, Sections={Sections}, " +
                "Responsibilities={Responsibilities}, " +
                "Requirements={Requirements}, Skills={Skills}",
                contract.ContractReference,
                document.Sections.Count,
                responsibilities.Count,
                requirements.Count,
                requiredSkills.Count);

            return document;
        }

        // ================================================================
        // SNAPSHOT PARSER
        // ================================================================

        private static List<string> ParseSnapshot(
            string? snapshot)
        {
            if (string.IsNullOrWhiteSpace(snapshot))
                return new List<string>();

            var normalized =
                snapshot
                    .Replace("\r\n", "\n")
                    .Replace("\r", "\n")
                    .Trim();

            if (string.IsNullOrWhiteSpace(normalized))
                return new List<string>();

            var lines =
                normalized.Split(
                    '\n',
                    StringSplitOptions.RemoveEmptyEntries);

            var result =
                new List<string>();

            foreach (var rawLine in lines)
            {
                var line =
                    rawLine.Trim();

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                line =
                    Regex.Replace(
                        line,
                        @"^[\-\*\•\▪\◦\·\d]+[\.\):\-]?\s*",
                        string.Empty);

                line =
                    Regex.Replace(
                        line,
                        @"^\s*[\[\(]?\s*\d+\s*[\]\)]?\s*",
                        string.Empty);

                line = CleanText(line);

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (!result.Any(
                        x => string.Equals(
                            x,
                            line,
                            StringComparison.OrdinalIgnoreCase)))
                {
                    result.Add(line);
                }
            }

            return result;
        }

        // ================================================================
        // STYLES
        // ================================================================

        private static VolunteerContractStyles BuildStyles()
        {
            return new VolunteerContractStyles
            {
                PrimaryColor = "#1B5E20",
                PrimaryDarkColor = "#124116",
                PrimaryLightColor = "#2E7D32",
                AccentColor = "#8BC34A",
                LightAccentColor = "#DCEDC8",
                PageBackgroundColor = "#F7FBF7",
                SectionBackgroundColor = "#F0FDF4",
                BorderColor = "#D5E7D6",
                TextColor = "#263238",
                MutedTextColor = "#607D63",
                WhiteColor = "#FFFFFF",
                TitleColor = "#1B5E20",
                SectionTitleColor = "#1B5E20",
                ParagraphColor = "#263238",
                FontFamily = "Arial",
                FontSize = 9.5f,
                LineHeight = 1.45f,
                PagePadding = 42f,
                TitleFontSize = 18f,
                SectionTitleFontSize = 12f,
                ParagraphFontSize = 9.5f,
                FieldFontSize = 9f,
                PageSize = "A4"
            };
        }

        // ================================================================
        // APPLICANT NAME
        // ================================================================

        private static string BuildApplicantName(
            Applicant? applicant,
            Applicant? fallbackApplicant)
        {
            var source =
                applicant ?? fallbackApplicant;

            if (source == null)
                return "Applicant";

            var firstName =
                source.FirstName?.Trim() ?? string.Empty;

            var lastName =
                source.LastName?.Trim() ?? string.Empty;

            var name =
                $"{firstName} {lastName}".Trim();

            return string.IsNullOrWhiteSpace(name)
                ? "Applicant"
                : name;
        }

        // ================================================================
        // LOCATION
        // ================================================================

        private static string BuildLocation(
            string? city,
            string? province)
        {
            var parts =
                new[]
                {
                    city?.Trim(),
                    province?.Trim()
                }
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToArray();

            return string.Join(", ", parts);
        }

        // ================================================================
        // DATE
        // ================================================================

        private static string FormatDate(
            DateTime? date)
        {
            return date.HasValue
                ? date.Value.ToString(
                    "dd MMMM yyyy",
                    CultureInfo.InvariantCulture)
                : string.Empty;
        }

        // ================================================================
        // TIME
        // ================================================================

        private static string FormatTime(
            TimeSpan? time)
        {
            if (!time.HasValue)
                return string.Empty;

            return DateTime.Today
                .Add(time.Value)
                .ToString(
                    "HH:mm",
                    CultureInfo.InvariantCulture);
        }

        // ================================================================
        // ENUM
        // ================================================================

        private static string FormatEnum<T>(
            T? value)
            where T : struct, Enum
        {
            if (!value.HasValue)
                return string.Empty;

            var text =
                value.Value.ToString();

            return Regex.Replace(
                text,
                "(?<=[a-z])(?=[A-Z])",
                " ");
        }

        // ================================================================
        // CLEAN TEXT
        // ================================================================

        private static string CleanText(
            string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var text =
                value.Trim();

            text =
                Regex.Replace(
                    text,
                    @"\s+",
                    " ");

            return text.Trim();
        }
    }
}