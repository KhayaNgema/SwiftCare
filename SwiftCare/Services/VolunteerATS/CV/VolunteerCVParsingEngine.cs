using SwiftCare.Models.VolunteerCV;
using System.Text.RegularExpressions;

namespace SwiftCare.Services.VolunteerATS.CV
{
    public class VolunteerCVParsingEngine
        : IVolunteerCVParsingEngine
    {
        // ============================================================
        // SECTION DEFINITIONS
        // ============================================================

        private static readonly Dictionary<string, string[]>
            SectionAliases =
                new(StringComparer.OrdinalIgnoreCase)
                {
                    ["Summary"] =
                    [
                        "summary",
                        "professional summary",
                        "profile",
                        "professional profile",
                        "about me",
                        "career profile"
                    ],

                    ["Skills"] =
                    [
                        "skills",
                        "technical skills",
                        "core skills",
                        "key skills",
                        "competencies",
                        "core competencies",
                        "areas of expertise"
                    ],

                    ["Experience"] =
                    [
                        "experience",
                        "work experience",
                        "employment history",
                        "professional experience",
                        "career history",
                        "work history"
                    ],

                    ["Education"] =
                    [
                        "education",
                        "academic background",
                        "qualifications",
                        "academic qualifications"
                    ],

                    ["Certifications"] =
                    [
                        "certifications",
                        "certificates",
                        "licenses",
                        "licences"
                    ],

                    ["Languages"] =
                    [
                        "languages",
                        "language skills"
                    ],

                    ["References"] =
                    [
                        "references",
                        "referees"
                    ],

                    ["Projects"] =
                    [
                        "projects",
                        "key projects",
                        "personal projects"
                    ],

                    ["Awards"] =
                    [
                        "awards",
                        "achievements",
                        "honors",
                        "honours"
                    ]
                };

        // ============================================================
        // AZURE DOCUMENT INTELLIGENCE PARSING
        // ============================================================

        public async Task<ParsedCv> ParseAsync(
            AzureCvExtractionResult azureResult,
            int volunteerApplicationId = 0)
        {
            if (azureResult == null)
            {
                throw new ArgumentNullException(
                    nameof(azureResult));
            }

            var parsedCv =
                new ParsedCv
                {
                    VolunteerApplicationId =
                        volunteerApplicationId > 0
                            ? volunteerApplicationId
                            : null,

                    RawText =
                        azureResult.RawText,

                    ParsedDate =
                        DateTime.UtcNow
                };

            if (!azureResult.Success)
            {
                parsedCv.ExtractionSuccessful = false;

                parsedCv.ExtractionMessage =
                    azureResult.ErrorMessage
                    ?? "Azure CV extraction failed.";

                return parsedCv;
            }

            var rawText =
                NormalizeLines(
                    azureResult.RawText ?? string.Empty);

            // ========================================================
            // SCALAR FIELDS FROM AZURE
            // ========================================================

            parsedCv.FullName =
                CleanValue(
                    GetFieldText(
                        azureResult,
                        "FullName"));

            parsedCv.Email =
                CleanValue(
                    GetFieldText(
                        azureResult,
                        "Email"));

            parsedCv.PhoneNumber =
                CleanValue(
                    GetFieldText(
                        azureResult,
                        "PhoneNumber"));

            parsedCv.ProfessionalTitle =
                CleanValue(
                    GetFieldText(
                        azureResult,
                        "ProfessionalTitle"));

            parsedCv.ProfessionalSummary =
                CleanValue(
                    GetFieldText(
                        azureResult,
                        "ProfessionalSummary"));

            parsedCv.CareerObjective =
                CleanValue(
                    GetFieldText(
                        azureResult,
                        "CareerObjective"));

            parsedCv.Address =
                CleanValue(
                    GetFieldText(
                        azureResult,
                        "Address"));

            parsedCv.City =
                CleanValue(
                    GetFieldText(
                        azureResult,
                        "City"));

            parsedCv.Province =
                CleanValue(
                    GetFieldText(
                        azureResult,
                        "Province"));

            parsedCv.Country =
                CleanValue(
                    GetFieldText(
                        azureResult,
                        "Country"));

            parsedCv.LinkedInUrl =
                CleanValue(
                    GetFieldText(
                        azureResult,
                        "LinkedInUrl"));

            parsedCv.PortfolioUrl =
                CleanValue(
                    GetFieldText(
                        azureResult,
                        "PortfolioUrl"));

            parsedCv.GitHubUrl =
                CleanValue(
                    GetFieldText(
                        azureResult,
                        "GitHubUrl"));

            // ========================================================
            // NORMALIZE / SECTION PARSING
            // ========================================================

            var sections =
                ParseSections(rawText);

            // ========================================================
            // FALLBACK: FULL NAME
            // ========================================================

            if (string.IsNullOrWhiteSpace(
                    parsedCv.FullName))
            {
                parsedCv.FullName =
                    ExtractFullName(rawText);
            }

            // ========================================================
            // FALLBACK: EMAIL
            // ========================================================

            if (string.IsNullOrWhiteSpace(
                    parsedCv.Email))
            {
                parsedCv.Email =
                    ExtractEmail(rawText);
            }

            // ========================================================
            // FALLBACK: PHONE
            // ========================================================

            if (string.IsNullOrWhiteSpace(
                    parsedCv.PhoneNumber))
            {
                parsedCv.PhoneNumber =
                    ExtractPhoneNumber(rawText);
            }

            // ========================================================
            // FALLBACK: PROFESSIONAL SUMMARY
            // ========================================================

            if (string.IsNullOrWhiteSpace(
                    parsedCv.ProfessionalSummary))
            {
                parsedCv.ProfessionalSummary =
                    ExtractSectionText(
                        sections,
                        "Summary");
            }

            // ========================================================
            // SKILLS
            // ========================================================

            ParseStructuredSkills(
                azureResult,
                parsedCv);

            if (parsedCv.Skills.Count == 0)
            {
                var skills =
                    ExtractSkills(
                        sections,
                        rawText);

                foreach (var skill in skills)
                {
                    AddSkill(
                        parsedCv,
                        skill);
                }
            }

            // ========================================================
            // LANGUAGES
            // ========================================================

            ParseStructuredLanguages(
                azureResult,
                parsedCv);

            if (parsedCv.Languages.Count == 0)
            {
                var languages =
                    ExtractLanguages(
                        sections,
                        rawText);

                foreach (var language in languages)
                {
                    AddLanguage(
                        parsedCv,
                        language);
                }
            }

            // ========================================================
            // EXPERIENCE
            // ========================================================

            ParseStructuredExperience(
                azureResult,
                parsedCv);

            if (parsedCv.Experience.Count == 0)
            {
                var experience =
                    ExtractExperience(
                        sections);

                foreach (var item in experience)
                {
                    parsedCv.Experience.Add(item);
                }
            }

            // ========================================================
            // EDUCATION
            // ========================================================

            ParseStructuredEducation(
                azureResult,
                parsedCv);

            if (parsedCv.Education.Count == 0)
            {
                var education =
                    ExtractEducation(
                        sections);

                foreach (var item in education)
                {
                    parsedCv.Education.Add(item);
                }
            }

            // ========================================================
            // CERTIFICATIONS
            // ========================================================

            ParseStructuredCertifications(
                azureResult,
                parsedCv);

            if (parsedCv.Certifications.Count == 0)
            {
                var certifications =
                    ExtractCertifications(
                        sections);

                foreach (var item in certifications)
                {
                    parsedCv.Certifications.Add(item);
                }
            }

            // ========================================================
            // EXTRACTION RESULT
            // ========================================================

            parsedCv.ExtractionSuccessful =
                HasUsefulData(parsedCv);

            parsedCv.ExtractionMessage =
                BuildExtractionMessage(
                    parsedCv,
                    azureResult);

            await Task.CompletedTask;

            return parsedCv;
        }

        // ============================================================
        // RAW TEXT PARSING
        // ============================================================

        public async Task<ParsedCv> ParseAsync(
            string rawText,
            int volunteerApplicationId = 0)
        {
            rawText =
                NormalizeLines(
                    rawText ?? string.Empty);

            var parsedCv =
                new ParsedCv
                {
                    VolunteerApplicationId =
                        volunteerApplicationId > 0
                            ? volunteerApplicationId
                            : null,

                    RawText =
                        rawText,

                    ParsedDate =
                        DateTime.UtcNow
                };

            if (string.IsNullOrWhiteSpace(rawText))
            {
                parsedCv.ExtractionSuccessful = false;

                parsedCv.ExtractionMessage =
                    "No CV text was supplied.";

                return parsedCv;
            }

            var sections =
                ParseSections(rawText);

            // ========================================================
            // BASIC INFORMATION
            // ========================================================

            parsedCv.FullName =
                ExtractFullName(rawText);

            parsedCv.Email =
                ExtractEmail(rawText);

            parsedCv.PhoneNumber =
                ExtractPhoneNumber(rawText);

            // ========================================================
            // SUMMARY
            // ========================================================

            parsedCv.ProfessionalSummary =
                ExtractSectionText(
                    sections,
                    "Summary");

            // ========================================================
            // SKILLS
            // ========================================================

            var skills =
                ExtractSkills(
                    sections,
                    rawText);

            foreach (var skill in skills)
            {
                AddSkill(
                    parsedCv,
                    skill);
            }

            // ========================================================
            // LANGUAGES
            // ========================================================

            var languages =
                ExtractLanguages(
                    sections,
                    rawText);

            foreach (var language in languages)
            {
                AddLanguage(
                    parsedCv,
                    language);
            }

            // ========================================================
            // EXPERIENCE
            // ========================================================

            var experience =
                ExtractExperience(
                    sections);

            foreach (var item in experience)
            {
                parsedCv.Experience.Add(item);
            }

            // ========================================================
            // EDUCATION
            // ========================================================

            var education =
                ExtractEducation(
                    sections);

            foreach (var item in education)
            {
                parsedCv.Education.Add(item);
            }

            // ========================================================
            // CERTIFICATIONS
            // ========================================================

            var certifications =
                ExtractCertifications(
                    sections);

            foreach (var item in certifications)
            {
                parsedCv.Certifications.Add(item);
            }

            // ========================================================
            // RESULT
            // ========================================================

            parsedCv.ExtractionSuccessful =
                HasUsefulData(parsedCv);

            parsedCv.ExtractionMessage =
                BuildExtractionMessage(
                    parsedCv,
                    null);

            await Task.CompletedTask;

            return parsedCv;
        }

        // ============================================================
        // AZURE FIELD HELPERS
        // ============================================================

        private static string? GetFieldText(
            AzureCvExtractionResult result,
            string fieldName)
        {
            if (result.Fields == null)
                return null;

            if (!result.Fields.TryGetValue(
                    fieldName,
                    out var field))
            {
                return null;
            }

            if (field == null)
                return null;

            return field.Content;
        }

        private static string? GetNestedText(
            AzureCvField? parent,
            string fieldName)
        {
            if (parent == null)
                return null;

            if (parent.ObjectValues == null)
                return null;

            if (!parent.ObjectValues.TryGetValue(
                    fieldName,
                    out var child))
            {
                return null;
            }

            if (child == null)
                return null;

            return child.Content;
        }

        // ============================================================
        // STRUCTURED SKILLS
        // ============================================================

        private static void ParseStructuredSkills(
            AzureCvExtractionResult result,
            ParsedCv parsedCv)
        {
            if (!result.Fields.TryGetValue(
                    "Skills",
                    out var field))
            {
                return;
            }

            if (field == null)
                return;

            // --------------------------------------------------------
            // Array of objects
            // --------------------------------------------------------

            if (field.ArrayValues != null &&
                field.ArrayValues.Count > 0)
            {
                foreach (var item in field.ArrayValues)
                {
                    if (item == null)
                        continue;

                    var skill =
                        GetNestedText(
                            item,
                            "Skill");

                    if (string.IsNullOrWhiteSpace(skill))
                    {
                        skill =
                            item.Content;
                    }

                    AddSkill(
                        parsedCv,
                        skill);
                }
            }

            // --------------------------------------------------------
            // Object dictionary
            // --------------------------------------------------------

            if (field.ObjectValues != null &&
                field.ObjectValues.Count > 0)
            {
                foreach (var item in field.ObjectValues)
                {
                    var skill =
                        item.Value?.Content;

                    AddSkill(
                        parsedCv,
                        skill);
                }
            }

            // --------------------------------------------------------
            // Direct value
            // --------------------------------------------------------

            if (parsedCv.Skills.Count == 0 &&
                !string.IsNullOrWhiteSpace(
                    field.Content))
            {
                var values =
                    SplitValues(
                        field.Content);

                foreach (var value in values)
                {
                    AddSkill(
                        parsedCv,
                        value);
                }
            }
        }

        // ============================================================
        // STRUCTURED LANGUAGES
        // ============================================================

        private static void ParseStructuredLanguages(
            AzureCvExtractionResult result,
            ParsedCv parsedCv)
        {
            if (!result.Fields.TryGetValue(
                    "Languages",
                    out var field))
            {
                return;
            }

            if (field == null)
                return;

            // --------------------------------------------------------
            // Array
            // --------------------------------------------------------

            if (field.ArrayValues != null &&
                field.ArrayValues.Count > 0)
            {
                foreach (var item in field.ArrayValues)
                {
                    if (item == null)
                        continue;

                    var language =
                        GetNestedText(
                            item,
                            "Language");

                    if (string.IsNullOrWhiteSpace(language))
                    {
                        language =
                            item.Content;
                    }

                    AddLanguage(
                        parsedCv,
                        language);
                }
            }

            // --------------------------------------------------------
            // Dictionary
            // --------------------------------------------------------

            if (field.ObjectValues != null &&
                field.ObjectValues.Count > 0)
            {
                foreach (var item in field.ObjectValues)
                {
                    var language =
                        item.Value?.Content;

                    AddLanguage(
                        parsedCv,
                        language);
                }
            }

            // --------------------------------------------------------
            // Direct value
            // --------------------------------------------------------

            if (parsedCv.Languages.Count == 0 &&
                !string.IsNullOrWhiteSpace(
                    field.Content))
            {
                var values =
                    SplitValues(
                        field.Content);

                foreach (var value in values)
                {
                    AddLanguage(
                        parsedCv,
                        value);
                }
            }
        }

        // ============================================================
        // STRUCTURED EXPERIENCE
        // ============================================================

        private static void ParseStructuredExperience(
            AzureCvExtractionResult result,
            ParsedCv parsedCv)
        {
            if (!result.Fields.TryGetValue(
                    "Experience",
                    out var field))
            {
                return;
            }

            if (field == null)
                return;

            if (field.ArrayValues == null)
                return;

            foreach (var item in field.ArrayValues)
            {
                if (item == null)
                    continue;

                var experience =
                    new CvExperience
                    {
                        Employer =
                            CleanValue(
                                GetNestedText(
                                    item,
                                    "Employer")),

                        Position =
                            CleanValue(
                                GetNestedText(
                                    item,
                                    "Position")),

                        StartDate =
                            CleanValue(
                                GetNestedText(
                                    item,
                                    "StartDate")),

                        EndDate =
                            CleanValue(
                                GetNestedText(
                                    item,
                                    "EndDate")),

                        Description =
                            CleanValue(
                                GetNestedText(
                                    item,
                                    "Description")),

                        RawText =
                            CleanValue(
                                item.Content)
                    };

                if (HasExperienceData(
                        experience))
                {
                    parsedCv.Experience.Add(
                        experience);
                }
            }
        }

        // ============================================================
        // STRUCTURED EDUCATION
        // ============================================================

        private static void ParseStructuredEducation(
            AzureCvExtractionResult result,
            ParsedCv parsedCv)
        {
            if (!result.Fields.TryGetValue(
                    "Education",
                    out var field))
            {
                return;
            }

            if (field == null)
                return;

            if (field.ArrayValues == null)
                return;

            foreach (var item in field.ArrayValues)
            {
                if (item == null)
                    continue;

                var education =
                    new CvEducation
                    {
                        Institution =
                            CleanValue(
                                GetNestedText(
                                    item,
                                    "Institution")),

                        Qualification =
                            CleanValue(
                                GetNestedText(
                                    item,
                                    "Qualification")),

                        FieldOfStudy =
                            CleanValue(
                                GetNestedText(
                                    item,
                                    "FieldOfStudy")),

                        StartDate =
                            CleanValue(
                                GetNestedText(
                                    item,
                                    "StartDate")),

                        EndDate =
                            CleanValue(
                                GetNestedText(
                                    item,
                                    "EndDate")),

                        RawText =
                            CleanValue(
                                item.Content)
                    };

                if (HasEducationData(
                        education))
                {
                    parsedCv.Education.Add(
                        education);
                }
            }
        }

        // ============================================================
        // STRUCTURED CERTIFICATIONS
        // ============================================================

        private static void ParseStructuredCertifications(
            AzureCvExtractionResult result,
            ParsedCv parsedCv)
        {
            if (!result.Fields.TryGetValue(
                    "Certifications",
                    out var field))
            {
                return;
            }

            if (field == null)
                return;

            if (field.ArrayValues == null)
                return;

            foreach (var item in field.ArrayValues)
            {
                if (item == null)
                    continue;

                var certification =
                    new CvCertification
                    {
                        Name =
                            CleanValue(
                                GetNestedText(
                                    item,
                                    "Name")),

                        IssuingOrganisation =
                            CleanValue(
                                GetNestedText(
                                    item,
                                    "IssuingOrganisation")),

                        Date =
                            CleanValue(
                                GetNestedText(
                                    item,
                                    "Date")),

                        RawText =
                            CleanValue(
                                item.Content)
                    };

                if (HasCertificationData(
                        certification))
                {
                    parsedCv.Certifications.Add(
                        certification);
                }
            }
        }

        // ============================================================
        // COLLECTION HELPERS
        // ============================================================

        private static void AddSkill(
            ParsedCv parsedCv,
            string? skill)
        {
            if (parsedCv == null)
                return;

            if (string.IsNullOrWhiteSpace(skill))
                return;

            var cleaned =
                CleanValue(skill);

            if (string.IsNullOrWhiteSpace(cleaned))
                return;

            var existing =
                parsedCv.Skills.Any(
                    x =>
                        string.Equals(
                            x.Skill,
                            cleaned,
                            StringComparison.OrdinalIgnoreCase));

            if (existing)
                return;

            parsedCv.Skills.Add(
                new CvSkill
                {
                    Skill = cleaned,
                    RawText = cleaned
                });
        }

        private static void AddLanguage(
            ParsedCv parsedCv,
            string? language)
        {
            if (parsedCv == null)
                return;

            if (string.IsNullOrWhiteSpace(language))
                return;

            var cleaned =
                CleanValue(language);

            if (string.IsNullOrWhiteSpace(cleaned))
                return;

            var existing =
                parsedCv.Languages.Any(
                    x =>
                        string.Equals(
                            x.Language,
                            cleaned,
                            StringComparison.OrdinalIgnoreCase));

            if (existing)
                return;

            parsedCv.Languages.Add(
                new CvLanguage
                {
                    Language = cleaned,
                    RawText = cleaned
                });
        }

        // ============================================================
        // RAW TEXT SECTIONS
        // ============================================================

        private static Dictionary<string, string>
            ParseSections(string rawText)
        {
            var sections =
                new Dictionary<string, string>(
                    StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(rawText))
                return sections;

            var lines =
                rawText
                    .Split(
                        '\n',
                        StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .ToList();

            string? currentSection = null;

            var buffer =
                new List<string>();

            void SaveCurrentSection()
            {
                if (string.IsNullOrWhiteSpace(
                        currentSection))
                {
                    return;
                }

                var text =
                    string.Join(
                        Environment.NewLine,
                        buffer).Trim();

                if (!string.IsNullOrWhiteSpace(text))
                {
                    sections[currentSection] =
                        text;
                }

                buffer.Clear();
            }

            foreach (var line in lines)
            {
                var sectionType =
                    GetSectionType(line);

                if (sectionType != null)
                {
                    SaveCurrentSection();

                    currentSection =
                        sectionType;

                    continue;
                }

                if (currentSection != null)
                {
                    buffer.Add(line);
                }
            }

            SaveCurrentSection();

            return sections;
        }

        private static string? GetSectionType(
            string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return null;

            var normalized =
                NormalizeHeading(line);

            foreach (var section in SectionAliases)
            {
                foreach (var alias in section.Value)
                {
                    if (string.Equals(
                            normalized,
                            NormalizeHeading(alias),
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return section.Key;
                    }
                }
            }

            return null;
        }

        private static string NormalizeHeading(
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            value =
                value
                    .Trim()
                    .Trim(
                        ':',
                        '-',
                        '=',
                        '*',
                        '#',
                        ' ');

            value =
                Regex.Replace(
                    value,
                    @"\s+",
                    " ");

            return value.ToLowerInvariant();
        }

        private static string? ExtractSectionText(
            Dictionary<string, string> sections,
            string sectionName)
        {
            if (sections.TryGetValue(
                    sectionName,
                    out var value))
            {
                return CleanValue(value);
            }

            return null;
        }

        // ============================================================
        // SKILL EXTRACTION
        // ============================================================

        private static List<string> ExtractSkills(
            Dictionary<string, string> sections,
            string rawText)
        {
            var skills =
                new List<string>();

            if (sections.TryGetValue(
                    "Skills",
                    out var section))
            {
                AddSkillValues(
                    skills,
                    section);
            }

            return skills;
        }

        private static void AddSkillValues(
            List<string> skills,
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            var values =
                SplitValues(value);

            foreach (var item in values)
            {
                var cleaned =
                    CleanBullet(item);

                if (string.IsNullOrWhiteSpace(cleaned))
                    continue;

                if (cleaned.Length > 150)
                    continue;

                if (!skills.Any(
                        x =>
                            string.Equals(
                                x,
                                cleaned,
                                StringComparison.OrdinalIgnoreCase)))
                {
                    skills.Add(cleaned);
                }
            }
        }

        // ============================================================
        // LANGUAGE EXTRACTION
        // ============================================================

        private static List<string> ExtractLanguages(
            Dictionary<string, string> sections,
            string rawText)
        {
            var languages =
                new List<string>();

            if (sections.TryGetValue(
                    "Languages",
                    out var section))
            {
                AddDelimitedValues(
                    languages,
                    section);
            }

            return languages;
        }

        private static void AddDelimitedValues(
            List<string> values,
            string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            foreach (var value in
                     SplitValues(text))
            {
                var cleaned =
                    CleanBullet(value);

                if (string.IsNullOrWhiteSpace(cleaned))
                    continue;

                if (!values.Any(
                        x =>
                            string.Equals(
                                x,
                                cleaned,
                                StringComparison.OrdinalIgnoreCase)))
                {
                    values.Add(cleaned);
                }
            }
        }

        private static List<string> SplitValues(
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return new List<string>();

            var result =
                new List<string>();

            var lines =
                value.Split(
                    '\n',
                    StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var parts =
                    line.Split(
                        new[]
                        {
                            ',',
                            ';',
                            '|',
                            '•'
                        },
                        StringSplitOptions.RemoveEmptyEntries);

                foreach (var part in parts)
                {
                    var cleaned =
                        CleanBullet(part);

                    if (!string.IsNullOrWhiteSpace(
                            cleaned))
                    {
                        result.Add(cleaned);
                    }
                }
            }

            return result;
        }

        // ============================================================
        // CERTIFICATIONS
        // ============================================================

        private static List<CvCertification>
            ExtractCertifications(
                Dictionary<string, string> sections)
        {
            var certifications =
                new List<CvCertification>();

            if (!sections.TryGetValue(
                    "Certifications",
                    out var section))
            {
                return certifications;
            }

            var lines =
                section
                    .Split(
                        '\n',
                        StringSplitOptions.RemoveEmptyEntries)
                    .Select(
                        CleanBullet)
                    .Where(
                        x =>
                            !string.IsNullOrWhiteSpace(x))
                    .ToList();

            foreach (var line in lines)
            {
                var certification =
                    new CvCertification
                    {
                        Name = line,
                        RawText = line
                    };

                if (line.Contains(
                        "|",
                        StringComparison.Ordinal))
                {
                    var parts =
                        line.Split(
                            '|',
                            StringSplitOptions.RemoveEmptyEntries);

                    certification.Name =
                        CleanValue(
                            parts.ElementAtOrDefault(0));

                    certification.IssuingOrganisation =
                        CleanValue(
                            parts.ElementAtOrDefault(1));

                    certification.Date =
                        CleanValue(
                            parts.ElementAtOrDefault(2));
                }

                certifications.Add(
                    certification);
            }

            return certifications;
        }

        // ============================================================
        // EDUCATION
        // ============================================================

        private static List<CvEducation>
            ExtractEducation(
                Dictionary<string, string> sections)
        {
            var education =
                new List<CvEducation>();

            if (!sections.TryGetValue(
                    "Education",
                    out var section))
            {
                return education;
            }

            var lines =
                section
                    .Split(
                        '\n',
                        StringSplitOptions.RemoveEmptyEntries)
                    .Select(
                        CleanBullet)
                    .Where(
                        x =>
                            !string.IsNullOrWhiteSpace(x))
                    .ToList();

            CvEducation? current = null;

            foreach (var line in lines)
            {
                if (LooksLikeDateRange(line))
                {
                    if (current != null)
                    {
                        SetEducationDates(
                            current,
                            line);
                    }

                    continue;
                }

                if (current == null)
                {
                    current =
                        new CvEducation
                        {
                            RawText = line,
                            Institution = line
                        };

                    continue;
                }

                if (string.IsNullOrWhiteSpace(
                        current.Qualification))
                {
                    current.Qualification =
                        line;
                }
                else if (string.IsNullOrWhiteSpace(
                             current.FieldOfStudy))
                {
                    current.FieldOfStudy =
                        line;
                }
                else
                {
                    education.Add(current);

                    current =
                        new CvEducation
                        {
                            Institution = line,
                            RawText = line
                        };
                }
            }

            if (current != null)
            {
                education.Add(current);
            }

            return education;
        }

        private static void SetEducationDates(
            CvEducation education,
            string value)
        {
            var match =
                Regex.Match(
                    value,
                    @"(?<start>\d{4}(?:-\d{2})?)\s*[-–—]\s*(?<end>\d{4}(?:-\d{2})?|Present|Current)",
                    RegexOptions.IgnoreCase);

            if (!match.Success)
                return;

            education.StartDate =
                match.Groups["start"].Value;

            education.EndDate =
                match.Groups["end"].Value;
        }

        // ============================================================
        // EXPERIENCE
        // ============================================================

        private static List<CvExperience>
            ExtractExperience(
                Dictionary<string, string> sections)
        {
            var experience =
                new List<CvExperience>();

            if (!sections.TryGetValue(
                    "Experience",
                    out var section))
            {
                return experience;
            }

            var lines =
                section
                    .Split(
                        '\n',
                        StringSplitOptions.RemoveEmptyEntries)
                    .Select(
                        CleanBullet)
                    .Where(
                        x =>
                            !string.IsNullOrWhiteSpace(x))
                    .ToList();

            CvExperience? current = null;

            foreach (var line in lines)
            {
                if (LooksLikeDateRange(line))
                {
                    if (current != null)
                    {
                        SetExperienceDates(
                            current,
                            line);
                    }

                    continue;
                }

                if (current == null)
                {
                    current =
                        new CvExperience
                        {
                            Position = line,
                            RawText = line
                        };

                    continue;
                }

                if (string.IsNullOrWhiteSpace(
                        current.Employer))
                {
                    current.Employer =
                        line;
                }
                else if (string.IsNullOrWhiteSpace(
                             current.Description))
                {
                    current.Description =
                        line;
                }
                else
                {
                    current.Description +=
                        Environment.NewLine +
                        line;
                }
            }

            if (current != null)
            {
                experience.Add(current);
            }

            return experience;
        }

        private static void SetExperienceDates(
            CvExperience experience,
            string value)
        {
            var match =
                Regex.Match(
                    value,
                    @"(?<start>\d{4}(?:-\d{2})?)\s*[-–—]\s*(?<end>\d{4}(?:-\d{2})?|Present|Current)",
                    RegexOptions.IgnoreCase);

            if (!match.Success)
                return;

            experience.StartDate =
                match.Groups["start"].Value;

            experience.EndDate =
                match.Groups["end"].Value;
        }

        private static bool LooksLikeDateRange(
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            return Regex.IsMatch(
                value,
                @"\d{4}.*(?:[-–—]|to).*(?:\d{4}|present|current)",
                RegexOptions.IgnoreCase);
        }

        // ============================================================
        // BASIC FIELD EXTRACTION
        // ============================================================

        private static string? ExtractEmail(
            string rawText)
        {
            var match =
                Regex.Match(
                    rawText,
                    @"[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}",
                    RegexOptions.IgnoreCase);

            return match.Success
                ? match.Value.Trim()
                : null;
        }

        private static string? ExtractPhoneNumber(
            string rawText)
        {
            var matches =
                Regex.Matches(
                    rawText,
                    @"(?<!\d)(?:\+?\d[\d\s().-]{7,}\d)(?!\d)");

            foreach (Match match in matches)
            {
                var cleaned =
                    CleanPhoneNumber(
                        match.Value);

                if (cleaned.Length >= 8)
                    return cleaned;
            }

            return null;
        }

        private static string CleanPhoneNumber(
            string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return string.Empty;

            return Regex.Replace(
                phone.Trim(),
                @"\s+",
                " ");
        }

        private static string? ExtractFullName(
            string rawText)
        {
            var lines =
                rawText
                    .Split(
                        '\n',
                        StringSplitOptions.RemoveEmptyEntries)
                    .Select(
                        x => x.Trim())
                    .Where(
                        x =>
                            !string.IsNullOrWhiteSpace(x))
                    .Take(15)
                    .ToList();

            foreach (var line in lines)
            {
                if (line.Contains("@"))
                    continue;

                if (Regex.IsMatch(
                        line,
                        @"\+?\d[\d\s().-]{7,}"))
                {
                    continue;
                }

                if (GetSectionType(line) != null)
                    continue;

                if (line.Length < 3 ||
                    line.Length > 100)
                {
                    continue;
                }

                if (line.Contains(
                        ":",
                        StringComparison.Ordinal))
                {
                    continue;
                }

                var words =
                    line.Split(
                        ' ',
                        StringSplitOptions.RemoveEmptyEntries);

                if (words.Length >= 2 &&
                    words.Length <= 6)
                {
                    return line;
                }
            }

            return null;
        }

        // ============================================================
        // CLEANING
        // ============================================================

        private static string NormalizeLines(
            string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            text =
                text.Replace(
                    "\r\n",
                    "\n");

            text =
                text.Replace(
                    "\r",
                    "\n");

            text =
                Regex.Replace(
                    text,
                    @"[ \t]+",
                    " ");

            text =
                Regex.Replace(
                    text,
                    @"\n{3,}",
                    "\n\n");

            return text.Trim();
        }

        private static string CleanBullet(
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return value
                .Trim()
                .TrimStart(
                    '-',
                    '*',
                    '•',
                    '▪',
                    '◦',
                    '→',
                    '>');
        }

        private static string? CleanValue(
            string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var cleaned =
                value.Trim();

            cleaned =
                Regex.Replace(
                    cleaned,
                    @"\s+",
                    " ");

            return string.IsNullOrWhiteSpace(cleaned)
                ? null
                : cleaned;
        }

        // ============================================================
        // VALIDATION
        // ============================================================

        private static bool HasUsefulData(
            ParsedCv parsedCv)
        {
            return
                !string.IsNullOrWhiteSpace(
                    parsedCv.FullName)
                ||
                !string.IsNullOrWhiteSpace(
                    parsedCv.Email)
                ||
                !string.IsNullOrWhiteSpace(
                    parsedCv.PhoneNumber)
                ||
                !string.IsNullOrWhiteSpace(
                    parsedCv.ProfessionalTitle)
                ||
                !string.IsNullOrWhiteSpace(
                    parsedCv.ProfessionalSummary)
                ||
                parsedCv.Skills.Count > 0
                ||
                parsedCv.Languages.Count > 0
                ||
                parsedCv.Experience.Count > 0
                ||
                parsedCv.Education.Count > 0
                ||
                parsedCv.Certifications.Count > 0;
        }

        private static bool HasExperienceData(
            CvExperience experience)
        {
            return
                !string.IsNullOrWhiteSpace(
                    experience.Employer)
                ||
                !string.IsNullOrWhiteSpace(
                    experience.Position)
                ||
                !string.IsNullOrWhiteSpace(
                    experience.Description)
                ||
                !string.IsNullOrWhiteSpace(
                    experience.StartDate)
                ||
                !string.IsNullOrWhiteSpace(
                    experience.EndDate);
        }

        private static bool HasEducationData(
            CvEducation education)
        {
            return
                !string.IsNullOrWhiteSpace(
                    education.Institution)
                ||
                !string.IsNullOrWhiteSpace(
                    education.Qualification)
                ||
                !string.IsNullOrWhiteSpace(
                    education.FieldOfStudy)
                ||
                !string.IsNullOrWhiteSpace(
                    education.StartDate)
                ||
                !string.IsNullOrWhiteSpace(
                    education.EndDate);
        }

        private static bool HasCertificationData(
            CvCertification certification)
        {
            return
                !string.IsNullOrWhiteSpace(
                    certification.Name)
                ||
                !string.IsNullOrWhiteSpace(
                    certification.IssuingOrganisation)
                ||
                !string.IsNullOrWhiteSpace(
                    certification.Date);
        }

        // ============================================================
        // EXTRACTION MESSAGE
        // ============================================================

        private static string BuildExtractionMessage(
            ParsedCv parsedCv,
            AzureCvExtractionResult? azureResult)
        {
            if (!parsedCv.ExtractionSuccessful)
            {
                return
                    "The CV was processed, but no useful " +
                    "candidate information could be extracted.";
            }

            var parts =
                new List<string>();

            if (!string.IsNullOrWhiteSpace(
                    parsedCv.FullName))
            {
                parts.Add("personal details");
            }

            if (parsedCv.Skills.Count > 0)
            {
                parts.Add(
                    $"{parsedCv.Skills.Count} skill(s)");
            }

            if (parsedCv.Languages.Count > 0)
            {
                parts.Add(
                    $"{parsedCv.Languages.Count} language(s)");
            }

            if (parsedCv.Experience.Count > 0)
            {
                parts.Add(
                    $"{parsedCv.Experience.Count} experience record(s)");
            }

            if (parsedCv.Education.Count > 0)
            {
                parts.Add(
                    $"{parsedCv.Education.Count} education record(s)");
            }

            if (parsedCv.Certifications.Count > 0)
            {
                parts.Add(
                    $"{parsedCv.Certifications.Count} certification(s)");
            }

            var message =
                parts.Count == 0
                    ? "CV processing completed successfully."
                    : "CV processed successfully. Extracted "
                      + string.Join(", ", parts)
                      + ".";

            if (azureResult?.Warnings != null &&
                azureResult.Warnings.Count > 0)
            {
                message +=
                    " Azure reported " +
                    azureResult.Warnings.Count +
                    " warning(s).";
            }

            return message;
        }
    }
}