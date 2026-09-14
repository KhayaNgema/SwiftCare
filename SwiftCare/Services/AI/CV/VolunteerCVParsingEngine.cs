using SwiftCare.Models.VolunteerCV;
using System.Text;
using System.Text.RegularExpressions;

namespace SwiftCare.Services.VolunteerATS.CV
{
    public class VolunteerCVParsingEngine : IVolunteerCVParsingEngine
    {
        // ============================================================
        // PUBLIC API
        // ============================================================

        public Task<ParsedCv> ParseAsync(
            string rawText,
            int volunteerApplicationId)
        {
            var result = new ParsedCv
            {
                VolunteerApplicationId = volunteerApplicationId,
                RawText = rawText ?? string.Empty,
                ParsedDate = DateTime.Now,
                ExtractionSuccessful = !string.IsNullOrWhiteSpace(rawText),
                ExtractionMessage = string.IsNullOrWhiteSpace(rawText)
                    ? "No CV text was available."
                    : "CV parsed successfully from raw text."
            };

            ParseRawText(result);

            CleanAndDeduplicate(result);

            return Task.FromResult(result);
        }

        public Task<ParsedCv> ParseAsync(
            AzureCvExtractionResult azureResult,
            int volunteerApplicationId = 0)
        {
            var result = new ParsedCv
            {
                VolunteerApplicationId = volunteerApplicationId,
                RawText = azureResult?.RawText ?? string.Empty,
                ParsedDate = DateTime.Now,
                ExtractionSuccessful = azureResult?.Success ?? false,
                ExtractionMessage =
                    azureResult?.ExtractionMessage ??
                    (
                        azureResult?.Success == true
                            ? "CV processing completed successfully."
                            : "CV processing failed."
                    )
            };

            if (azureResult == null)
            {
                result.ExtractionSuccessful = false;
                result.ExtractionMessage =
                    "Azure extraction result was null.";

                return Task.FromResult(result);
            }

            // ========================================================
            // 1. STRUCTURED AZURE FIELDS
            // ========================================================

            ParseStructuredFields(
                result,
                azureResult);

            // ========================================================
            // 2. RAW TEXT AUGMENTATION
            // ========================================================

            if (!string.IsNullOrWhiteSpace(result.RawText))
            {
                ParseRawText(result);
            }

            // ========================================================
            // 3. CLEANUP
            // ========================================================

            CleanAndDeduplicate(result);

            return Task.FromResult(result);
        }

        // ============================================================
        // STRUCTURED AZURE PARSING
        // ============================================================

        private void ParseStructuredFields(
            ParsedCv result,
            AzureCvExtractionResult azureResult)
        {
            if (azureResult.Fields == null ||
                azureResult.Fields.Count == 0)
            {
                return;
            }

            // ========================================================
            // SCALAR FIELDS
            // ========================================================

            result.FullName =
                GetFieldText(
                    azureResult.Fields,
                    "FullName");

            result.Email =
                GetFieldText(
                    azureResult.Fields,
                    "Email");

            result.PhoneNumber =
                GetFieldText(
                    azureResult.Fields,
                    "PhoneNumber");

            result.ProfessionalTitle =
                GetFieldText(
                    azureResult.Fields,
                    "ProfessionalTitle");

            result.ProfessionalSummary =
                GetFieldText(
                    azureResult.Fields,
                    "ProfessionalSummary");

            result.CareerObjective =
                GetFieldText(
                    azureResult.Fields,
                    "CareerObjective");

            result.Address =
                GetFieldText(
                    azureResult.Fields,
                    "Address");

            result.City =
                GetFieldText(
                    azureResult.Fields,
                    "City");

            result.Province =
                GetFieldText(
                    azureResult.Fields,
                    "Province");

            result.Country =
                GetFieldText(
                    azureResult.Fields,
                    "Country");

            result.LinkedInUrl =
                GetFieldText(
                    azureResult.Fields,
                    "LinkedInUrl");

            result.PortfolioUrl =
                GetFieldText(
                    azureResult.Fields,
                    "PortfolioUrl");

            result.GitHubUrl =
                GetFieldText(
                    azureResult.Fields,
                    "GitHubUrl");

            // ========================================================
            // REPEATING FIELDS
            // ========================================================

            ParseStructuredSkills(
                result,
                azureResult.Fields);

            ParseStructuredLanguages(
                result,
                azureResult.Fields);

            ParseStructuredExperience(
                result,
                azureResult.Fields);

            ParseStructuredEducation(
                result,
                azureResult.Fields);

            ParseStructuredCertifications(
                result,
                azureResult.Fields);
        }

        // ============================================================
        // STRUCTURED SKILLS
        // ============================================================

        private void ParseStructuredSkills(
            ParsedCv result,
            Dictionary<string, AzureCvField> fields)
        {
            if (!TryGetField(
                    fields,
                    "Skills",
                    out var field))
            {
                return;
            }

            // --------------------------------------------------------
            // Preferred structure:
            //
            // Skills
            //   ArrayValues
            //      Skill
            //      Skill
            //      Skill
            // --------------------------------------------------------

            if (field.ArrayValues != null &&
                field.ArrayValues.Count > 0)
            {
                foreach (var item in field.ArrayValues)
                {
                    var skill =
                        GetNestedText(
                            item,
                            "Skill");

                    if (string.IsNullOrWhiteSpace(skill))
                    {
                        skill =
                            GetFieldTextRecursive(item);
                    }

                    AddSkill(
                        result,
                        skill);
                }

                return;
            }

            // --------------------------------------------------------
            // Fallback if Azure produced an object instead of array.
            // --------------------------------------------------------

            if (field.ObjectValues != null &&
                field.ObjectValues.Count > 0)
            {
                foreach (var item in field.ObjectValues.Values)
                {
                    var skill =
                        GetNestedText(
                            item,
                            "Skill");

                    if (string.IsNullOrWhiteSpace(skill))
                    {
                        skill =
                            GetFieldTextRecursive(item);
                    }

                    AddSkill(
                        result,
                        skill);
                }
            }
        }

        // ============================================================
        // STRUCTURED LANGUAGES
        // ============================================================

        private void ParseStructuredLanguages(
            ParsedCv result,
            Dictionary<string, AzureCvField> fields)
        {
            if (!TryGetField(
                    fields,
                    "Languages",
                    out var field))
            {
                return;
            }

            if (field.ArrayValues != null &&
                field.ArrayValues.Count > 0)
            {
                foreach (var item in field.ArrayValues)
                {
                    var language =
                        GetNestedText(
                            item,
                            "Language");

                    if (string.IsNullOrWhiteSpace(language))
                    {
                        language =
                            GetFieldTextRecursive(item);
                    }

                    AddLanguage(
                        result,
                        language);
                }

                return;
            }

            if (field.ObjectValues != null &&
                field.ObjectValues.Count > 0)
            {
                foreach (var item in field.ObjectValues.Values)
                {
                    var language =
                        GetNestedText(
                            item,
                            "Language");

                    if (string.IsNullOrWhiteSpace(language))
                    {
                        language =
                            GetFieldTextRecursive(item);
                    }

                    AddLanguage(
                        result,
                        language);
                }
            }
        }

        // ============================================================
        // STRUCTURED EXPERIENCE
        // ============================================================

        private void ParseStructuredExperience(
            ParsedCv result,
            Dictionary<string, AzureCvField> fields)
        {
            if (!TryGetField(
                    fields,
                    "Experience",
                    out var field))
            {
                return;
            }

            // ========================================================
            // ARRAY STRUCTURE
            // ========================================================

            if (field.ArrayValues != null &&
                field.ArrayValues.Count > 0)
            {
                foreach (var item in field.ArrayValues)
                {
                    ParseExperienceItem(
                        result,
                        item);
                }

                return;
            }

            // ========================================================
            // OBJECT STRUCTURE FALLBACK
            // ========================================================

            if (field.ObjectValues != null &&
                field.ObjectValues.Count > 0)
            {
                // If the object itself contains Employer/Position,
                // it is one experience record.
                if (ContainsAnyKey(
                        field.ObjectValues,
                        "Employer",
                        "Position",
                        "Description",
                        "StartDate",
                        "EndDate"))
                {
                    ParseExperienceItem(
                        result,
                        field);

                    return;
                }

                // Otherwise each child may represent one item.
                foreach (var item in field.ObjectValues.Values)
                {
                    ParseExperienceItem(
                        result,
                        item);
                }
            }
        }

        private void ParseExperienceItem(
            ParsedCv result,
            AzureCvField item)
        {
            if (item == null)
                return;

            var employer =
                GetNestedText(
                    item,
                    "Employer");

            var position =
                GetNestedText(
                    item,
                    "Position");

            var startDate =
                GetNestedText(
                    item,
                    "StartDate");

            var endDate =
                GetNestedText(
                    item,
                    "EndDate");

            var description =
                GetNestedText(
                    item,
                    "Description");

            employer =
                CleanValue(employer);

            position =
                CleanValue(position);

            startDate =
                CleanValue(startDate);

            endDate =
                CleanValue(endDate);

            description =
                CleanValue(description);

            if (string.IsNullOrWhiteSpace(employer) &&
                string.IsNullOrWhiteSpace(position) &&
                string.IsNullOrWhiteSpace(description))
            {
                return;
            }

            result.Experience.Add(
                new CvExperience
                {
                    Employer = employer,
                    Position = position,
                    StartDate = startDate,
                    EndDate = endDate,
                    Description = description,
                    RawText =
                        BuildExperienceRawText(
                            employer,
                            position,
                            startDate,
                            endDate,
                            description)
                });
        }

        // ============================================================
        // STRUCTURED EDUCATION
        // ============================================================

        private void ParseStructuredEducation(
            ParsedCv result,
            Dictionary<string, AzureCvField> fields)
        {
            if (!TryGetField(
                    fields,
                    "Education",
                    out var field))
            {
                return;
            }

            if (field.ArrayValues != null &&
                field.ArrayValues.Count > 0)
            {
                foreach (var item in field.ArrayValues)
                {
                    ParseEducationItem(
                        result,
                        item);
                }

                return;
            }

            if (field.ObjectValues != null &&
                field.ObjectValues.Count > 0)
            {
                if (ContainsAnyKey(
                        field.ObjectValues,
                        "Institution",
                        "Qualification",
                        "FieldOfStudy",
                        "StartDate",
                        "EndDate"))
                {
                    ParseEducationItem(
                        result,
                        field);

                    return;
                }

                foreach (var item in field.ObjectValues.Values)
                {
                    ParseEducationItem(
                        result,
                        item);
                }
            }
        }

        private void ParseEducationItem(
            ParsedCv result,
            AzureCvField item)
        {
            if (item == null)
                return;

            var institution =
                GetNestedText(
                    item,
                    "Institution");

            var qualification =
                GetNestedText(
                    item,
                    "Qualification");

            var fieldOfStudy =
                GetNestedText(
                    item,
                    "FieldOfStudy");

            var startDate =
                GetNestedText(
                    item,
                    "StartDate");

            var endDate =
                GetNestedText(
                    item,
                    "EndDate");

            institution =
                CleanValue(institution);

            qualification =
                CleanValue(qualification);

            fieldOfStudy =
                CleanValue(fieldOfStudy);

            startDate =
                CleanValue(startDate);

            endDate =
                CleanValue(endDate);

            if (string.IsNullOrWhiteSpace(institution) &&
                string.IsNullOrWhiteSpace(qualification) &&
                string.IsNullOrWhiteSpace(fieldOfStudy))
            {
                return;
            }

            result.Education.Add(
                new CvEducation
                {
                    Institution = institution,
                    Qualification = qualification,
                    FieldOfStudy = fieldOfStudy,
                    StartDate = startDate,
                    EndDate = endDate,
                    RawText =
                        $"{qualification} | {institution} | {fieldOfStudy} | {startDate} | {endDate}"
                });
        }

        // ============================================================
        // STRUCTURED CERTIFICATIONS
        // ============================================================

        private void ParseStructuredCertifications(
            ParsedCv result,
            Dictionary<string, AzureCvField> fields)
        {
            if (!TryGetField(
                    fields,
                    "Certifications",
                    out var field))
            {
                return;
            }

            if (field.ArrayValues != null &&
                field.ArrayValues.Count > 0)
            {
                foreach (var item in field.ArrayValues)
                {
                    ParseCertificationItem(
                        result,
                        item);
                }

                return;
            }

            if (field.ObjectValues != null &&
                field.ObjectValues.Count > 0)
            {
                if (ContainsAnyKey(
                        field.ObjectValues,
                        "Name",
                        "IssuingOrganisation",
                        "Date"))
                {
                    ParseCertificationItem(
                        result,
                        field);

                    return;
                }

                foreach (var item in field.ObjectValues.Values)
                {
                    ParseCertificationItem(
                        result,
                        item);
                }
            }
        }

        private void ParseCertificationItem(
            ParsedCv result,
            AzureCvField item)
        {
            if (item == null)
                return;

            var name =
                GetNestedText(
                    item,
                    "Name");

            var issuingOrganisation =
                GetNestedText(
                    item,
                    "IssuingOrganisation");

            var date =
                GetNestedText(
                    item,
                    "Date");

            name =
                CleanValue(name);

            issuingOrganisation =
                CleanValue(issuingOrganisation);

            date =
                CleanValue(date);

            if (string.IsNullOrWhiteSpace(name) &&
                string.IsNullOrWhiteSpace(issuingOrganisation))
            {
                return;
            }

            result.Certifications.Add(
                new CvCertification
                {
                    Name = name,
                    IssuingOrganisation =
                        issuingOrganisation,
                    Date = date,
                    RawText =
                        $"{name} | {issuingOrganisation} | {date}"
                });
        }

        // ============================================================
        // RAW TEXT PARSING
        // ============================================================

        private void ParseRawText(
            ParsedCv result)
        {
            if (string.IsNullOrWhiteSpace(
                    result.RawText))
            {
                return;
            }

            var normalized =
                NormalizeLineBreaks(
                    result.RawText);

            // ========================================================
            // CONTACT INFORMATION
            // ========================================================

            ParseRawContactInformation(
                result,
                normalized);

            // ========================================================
            // SECTIONS
            // ========================================================

            var skillsSection =
                ExtractSection(
                    normalized,
                    "CORE SKILLS",
                    "SKILLS",
                    "KEY SKILLS",
                    "TECHNICAL SKILLS",
                    "COMPETENCIES",
                    "AREAS OF EXPERTISE");

            var languagesSection =
                ExtractSection(
                    normalized,
                    "LANGUAGES",
                    "LANGUAGE SKILLS");

            var experienceSection =
                ExtractSection(
                    normalized,
                    "EXPERIENCE",
                    "WORK EXPERIENCE",
                    "PROFESSIONAL EXPERIENCE",
                    "EMPLOYMENT HISTORY",
                    "VOLUNTEER EXPERIENCE",
                    "VOLUNTEERING");

            var educationSection =
                ExtractSection(
                    normalized,
                    "EDUCATION",
                    "EDUCATIONAL BACKGROUND",
                    "ACADEMIC QUALIFICATIONS",
                    "QUALIFICATIONS");

            var certificationSection =
                ExtractSection(
                    normalized,
                    "CERTIFICATIONS",
                    "CERTIFICATES",
                    "TRAINING",
                    "PROFESSIONAL CERTIFICATIONS");

            // ========================================================
            // RAW SKILLS
            // ========================================================

            ParseRawSkills(
                result,
                skillsSection);

            // ========================================================
            // RAW LANGUAGES
            // ========================================================

            ParseRawLanguages(
                result,
                languagesSection);

            // ========================================================
            // RAW EXPERIENCE
            // ========================================================

            ParseRawExperience(
                result,
                experienceSection);

            // ========================================================
            // RAW EDUCATION
            // ========================================================

            ParseRawEducation(
                result,
                educationSection);

            // ========================================================
            // RAW CERTIFICATIONS
            // ========================================================

            ParseRawCertifications(
                result,
                certificationSection);

            // ========================================================
            // FALLBACK EXPERIENCE
            // ========================================================

            if (result.Experience.Count == 0)
            {
                ParsePossibleExperienceFromWholeCv(
                    result,
                    normalized);
            }
        }

        // ============================================================
        // RAW CONTACT INFORMATION
        // ============================================================

        private void ParseRawContactInformation(
            ParsedCv result,
            string text)
        {
            var lines =
                text.Split(
                    '\n',
                    StringSplitOptions.RemoveEmptyEntries);

            foreach (var rawLine in lines)
            {
                var line =
                    CleanLine(rawLine);

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // ----------------------------------------------------
                // EMAIL
                // ----------------------------------------------------

                if (string.IsNullOrWhiteSpace(
                        result.Email))
                {
                    var emailMatch =
                        Regex.Match(
                            line,
                            @"[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}",
                            RegexOptions.IgnoreCase);

                    if (emailMatch.Success)
                    {
                        result.Email =
                            emailMatch.Value.Trim();
                    }
                }

                // ----------------------------------------------------
                // PHONE
                // ----------------------------------------------------

                if (string.IsNullOrWhiteSpace(
                        result.PhoneNumber))
                {
                    var phoneMatch =
                        Regex.Match(
                            line,
                            @"(?<!\d)(?:\+27|0)\s*\d{2}\s*\d{3}\s*\d{4}(?!\d)");

                    if (phoneMatch.Success)
                    {
                        result.PhoneNumber =
                            Regex.Replace(
                                phoneMatch.Value,
                                @"\s+",
                                " ").Trim();
                    }
                }

                // ----------------------------------------------------
                // LINKEDIN
                // ----------------------------------------------------

                if (string.IsNullOrWhiteSpace(
                        result.LinkedInUrl) &&
                    line.Contains(
                        "linkedin.com",
                        StringComparison.OrdinalIgnoreCase))
                {
                    result.LinkedInUrl =
                        ExtractUrl(line);
                }

                // ----------------------------------------------------
                // GITHUB
                // ----------------------------------------------------

                if (string.IsNullOrWhiteSpace(
                        result.GitHubUrl) &&
                    line.Contains(
                        "github.com",
                        StringComparison.OrdinalIgnoreCase))
                {
                    result.GitHubUrl =
                        ExtractUrl(line);
                }

                // ----------------------------------------------------
                // PORTFOLIO
                // ----------------------------------------------------

                if (string.IsNullOrWhiteSpace(
                        result.PortfolioUrl))
                {
                    if (line.Contains(
                            "portfolio",
                            StringComparison.OrdinalIgnoreCase) ||
                        line.Contains(
                            "www.",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        var url =
                            ExtractUrl(line);

                        if (!string.IsNullOrWhiteSpace(url))
                        {
                            result.PortfolioUrl =
                                url;
                        }
                    }
                }
            }

            // ========================================================
            // NAME FALLBACK
            // ========================================================

            if (string.IsNullOrWhiteSpace(
                    result.FullName))
            {
                var firstLines =
                    lines
                        .Select(CleanLine)
                        .Where(
                            x =>
                                !string.IsNullOrWhiteSpace(x))
                        .Take(8)
                        .ToList();

                foreach (var line in firstLines)
                {
                    if (LooksLikeHeader(line))
                        continue;

                    if (line.Contains("@"))
                        continue;

                    if (Regex.IsMatch(
                            line,
                            @"\d"))
                    {
                        continue;
                    }

                    if (line.Length < 3 ||
                        line.Length > 80)
                    {
                        continue;
                    }

                    result.FullName =
                        line;

                    break;
                }
            }
        }

        // ============================================================
        // RAW SKILLS
        // ============================================================

        private void ParseRawSkills(
            ParsedCv result,
            string? section)
        {
            if (string.IsNullOrWhiteSpace(section))
                return;

            Console.WriteLine(
                "[SWIFTCARE ATS] RAW SKILLS SECTION:");

            Console.WriteLine(section);

            var items =
                ExtractListItems(
                    section);

            foreach (var item in items)
            {
                var skill =
                    CleanSkillItem(item);

                if (string.IsNullOrWhiteSpace(skill))
                    continue;

                AddSkill(
                    result,
                    skill);
            }
        }

        // ============================================================
        // RAW LANGUAGES
        // ============================================================

        private void ParseRawLanguages(
            ParsedCv result,
            string? section)
        {
            if (string.IsNullOrWhiteSpace(section))
                return;

            var items =
                ExtractListItems(
                    section);

            foreach (var item in items)
            {
                var language =
                    CleanLine(item);

                if (string.IsNullOrWhiteSpace(language))
                    continue;

                if (language.Length > 60)
                    continue;

                AddLanguage(
                    result,
                    language);
            }
        }

        // ============================================================
        // RAW EXPERIENCE
        // ============================================================

        private void ParseRawExperience(
            ParsedCv result,
            string? section)
        {
            if (string.IsNullOrWhiteSpace(section))
                return;

            var blocks =
                BuildExperienceBlocks(
                    section);

            foreach (var block in blocks)
            {
                if (block.Count == 0)
                    continue;

                var experience =
                    ParseExperienceBlock(
                        block);

                if (experience == null)
                    continue;

                if (string.IsNullOrWhiteSpace(
                        experience.Position) &&
                    string.IsNullOrWhiteSpace(
                        experience.Employer) &&
                    string.IsNullOrWhiteSpace(
                        experience.Description))
                {
                    continue;
                }

                MergeRawExperience(
                    result,
                    experience);
            }
        }

        // ============================================================
        // EXPERIENCE BLOCK BUILDER
        // ============================================================

        private List<List<string>> BuildExperienceBlocks(
            string section)
        {
            var lines =
                section
                    .Split(
                        '\n',
                        StringSplitOptions.RemoveEmptyEntries)
                    .Select(CleanLine)
                    .Where(
                        x =>
                            !string.IsNullOrWhiteSpace(x))
                    .ToList();

            var blocks =
                new List<List<string>>();

            List<string>? currentBlock = null;

            foreach (var line in lines)
            {
                if (LooksLikeExperienceHeader(
                        line))
                {
                    if (currentBlock != null &&
                        currentBlock.Count > 0)
                    {
                        blocks.Add(
                            currentBlock);
                    }

                    currentBlock =
                        new List<string>();

                    currentBlock.Add(
                        line);
                }
                else
                {
                    if (currentBlock == null)
                    {
                        currentBlock =
                            new List<string>();
                    }

                    currentBlock.Add(
                        line);
                }
            }

            if (currentBlock != null &&
                currentBlock.Count > 0)
            {
                blocks.Add(
                    currentBlock);
            }

            if (blocks.Count == 0 &&
                lines.Count > 0)
            {
                blocks.Add(
                    lines);
            }

            return blocks;
        }

        // ============================================================
        // EXPERIENCE HEADER DETECTION
        // ============================================================

        private bool LooksLikeExperienceHeader(
            string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return false;

            var clean =
                CleanLine(line);

            if (clean.Length < 4 ||
                clean.Length > 140)
            {
                return false;
            }

            // --------------------------------------------------------
            // Bullet points are descriptions.
            // --------------------------------------------------------

            if (line.StartsWith("•") ||
                line.StartsWith("·") ||
                line.StartsWith("-") ||
                line.StartsWith("*"))
            {
                return false;
            }

            // --------------------------------------------------------
            // Dates.
            // --------------------------------------------------------

            bool hasDate =
                Regex.IsMatch(
                    clean,
                    @"\b(?:Jan(?:uary)?|Feb(?:ruary)?|Mar(?:ch)?|Apr(?:il)?|May|Jun(?:e)?|Jul(?:y)?|Aug(?:ust)?|Sep(?:tember)?|Oct(?:ober)?|Nov(?:ember)?|Dec(?:ember)?)\b",
                    RegexOptions.IgnoreCase)
                ||
                Regex.IsMatch(
                    clean,
                    @"\b20\d{2}\b");

            // --------------------------------------------------------
            // Separators.
            // --------------------------------------------------------

            bool hasSeparator =
                clean.Contains("|") ||
                clean.Contains(" - ") ||
                clean.Contains(" — ") ||
                clean.Contains(" – ");

            // --------------------------------------------------------
            // Role words.
            // --------------------------------------------------------

            bool hasRoleWord =
                Regex.IsMatch(
                    clean,
                    @"\b(volunteer|assistant|worker|support|intern|internship|coordinator|officer|manager|developer|nurse|caregiver|companion|facilitator|teacher|tutor|administrator|sales|retail|driver|representative|specialist|technician|student)\b",
                    RegexOptions.IgnoreCase);

            bool looksLikeSentence =
                clean.EndsWith(".") ||
                clean.EndsWith("!") ||
                clean.EndsWith("?");

            int wordCount =
                Regex.Matches(
                    clean,
                    @"\b[\w'-]+\b").Count;

            // --------------------------------------------------------
            // This is what prevents normal description sentences
            // becoming positions.
            // --------------------------------------------------------

            if (looksLikeSentence &&
                wordCount >= 8 &&
                !hasDate)
            {
                return false;
            }

            if (hasDate &&
                (hasRoleWord || hasSeparator))
            {
                return true;
            }

            if (hasSeparator &&
                hasRoleWord)
            {
                return true;
            }

            if (!looksLikeSentence &&
                hasRoleWord &&
                wordCount <= 12)
            {
                return true;
            }

            return false;
        }

        // ============================================================
        // EXPERIENCE BLOCK PARSER
        // ============================================================

        private CvExperience? ParseExperienceBlock(
            List<string> block)
        {
            if (block.Count == 0)
                return null;

            var experience =
                new CvExperience();

            var header =
                block[0];

            // --------------------------------------------------------
            // Dates
            // --------------------------------------------------------

            ExtractExperienceDates(
                header,
                out var startDate,
                out var endDate);

            experience.StartDate =
                startDate;

            experience.EndDate =
                endDate;

            // --------------------------------------------------------
            // Remove dates from header.
            // --------------------------------------------------------

            var headerWithoutDates =
                RemoveExperienceDates(
                    header);

            // --------------------------------------------------------
            // POSITION | EMPLOYER
            // --------------------------------------------------------

            if (headerWithoutDates.Contains("|"))
            {
                var parts =
                    headerWithoutDates
                        .Split(
                            '|',
                            StringSplitOptions.RemoveEmptyEntries)
                        .Select(CleanLine)
                        .Where(
                            x =>
                                !string.IsNullOrWhiteSpace(x))
                        .ToList();

                if (parts.Count >= 2)
                {
                    experience.Position =
                        parts[0];

                    experience.Employer =
                        parts[1];
                }
                else if (parts.Count == 1)
                {
                    experience.Position =
                        parts[0];
                }
            }
            else
            {
                var parts =
                    Regex.Split(
                            headerWithoutDates,
                            @"\s+[—–-]\s+")
                        .Select(CleanLine)
                        .Where(
                            x =>
                                !string.IsNullOrWhiteSpace(x))
                        .ToList();

                if (parts.Count >= 2)
                {
                    experience.Position =
                        parts[0];

                    experience.Employer =
                        parts[1];
                }
                else
                {
                    experience.Position =
                        headerWithoutDates;
                }
            }

            // ========================================================
            // DESCRIPTION
            // ========================================================

            var descriptions =
                block
                    .Skip(1)
                    .Select(CleanLine)
                    .Where(
                        x =>
                            !string.IsNullOrWhiteSpace(x))
                    .Where(
                        x =>
                            !IsStandaloneDateLine(x))
                    .ToList();

            if (descriptions.Count > 0)
            {
                experience.Description =
                    string.Join(
                        " ",
                        descriptions);
            }

            experience.Position =
                CleanValue(
                    experience.Position);

            experience.Employer =
                CleanValue(
                    experience.Employer);

            experience.Description =
                CleanValue(
                    experience.Description);

            experience.StartDate =
                CleanValue(
                    experience.StartDate);

            experience.EndDate =
                CleanValue(
                    experience.EndDate);

            experience.RawText =
                BuildExperienceRawText(
                    experience.Employer,
                    experience.Position,
                    experience.StartDate,
                    experience.EndDate,
                    experience.Description);

            return experience;
        }

        // ============================================================
        // EXPERIENCE DATES
        // ============================================================

        private void ExtractExperienceDates(
            string text,
            out string? startDate,
            out string? endDate)
        {
            startDate = null;
            endDate = null;

            var datePattern =
                @"(?:Jan(?:uary)?|Feb(?:ruary)?|Mar(?:ch)?|Apr(?:il)?|May|Jun(?:e)?|Jul(?:y)?|Aug(?:ust)?|Sep(?:tember)?|Oct(?:ober)?|Nov(?:ember)?|Dec(?:ember)?)\s+20\d{2}|20\d{2}";

            var matches =
                Regex.Matches(
                    text,
                    datePattern,
                    RegexOptions.IgnoreCase);

            if (matches.Count >= 1)
            {
                startDate =
                    matches[0].Value.Trim();
            }

            if (matches.Count >= 2)
            {
                endDate =
                    matches[1].Value.Trim();
            }
        }

        private string RemoveExperienceDates(
            string text)
        {
            var result =
                Regex.Replace(
                    text,
                    @"(?:Jan(?:uary)?|Feb(?:ruary)?|Mar(?:ch)?|Apr(?:il)?|May|Jun(?:e)?|Jul(?:y)?|Aug(?:ust)?|Sep(?:tember)?|Oct(?:ober)?|Nov(?:ember)?|Dec(?:ember)?)\s+20\d{2}",
                    "",
                    RegexOptions.IgnoreCase);

            result =
                Regex.Replace(
                    result,
                    @"\b20\d{2}\b",
                    "");

            result =
                Regex.Replace(
                    result,
                    @"\s{2,}",
                    " ");

            return result.Trim(
                ' ',
                '|',
                '-',
                '—',
                '–',
                ':');
        }

        private bool IsStandaloneDateLine(
            string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return false;

            return Regex.IsMatch(
                line,
                @"^(?:Jan(?:uary)?|Feb(?:ruary)?|Mar(?:ch)?|Apr(?:il)?|May|Jun(?:e)?|Jul(?:y)?|Aug(?:ust)?|Sep(?:tember)?|Oct(?:ober)?|Nov(?:ember)?|Dec(?:ember)?|\d{4})",
                RegexOptions.IgnoreCase);
        }

        // ============================================================
        // RAW EDUCATION
        // ============================================================

        private void ParseRawEducation(
            ParsedCv result,
            string? section)
        {
            if (string.IsNullOrWhiteSpace(section))
                return;

            var lines =
                ExtractListItems(
                    section);

            foreach (var line in lines)
            {
                var clean =
                    CleanLine(line);

                if (string.IsNullOrWhiteSpace(clean))
                    continue;

                if (!LooksLikeEducationLine(clean))
                    continue;

                var education =
                    new CvEducation
                    {
                        Qualification =
                            clean,
                        RawText =
                            clean
                    };

                var yearMatch =
                    Regex.Match(
                        clean,
                        @"\b20\d{2}\b");

                if (yearMatch.Success)
                {
                    education.EndDate =
                        yearMatch.Value;
                }

                result.Education.Add(
                    education);
            }
        }

        private bool LooksLikeEducationLine(
            string line)
        {
            return Regex.IsMatch(
                line,
                @"\b(matric|national senior certificate|certificate|diploma|degree|bachelor|bsc|ba|bcom|honours|masters|master|phd|doctorate|qualification|school|college|university|academy)\b",
                RegexOptions.IgnoreCase);
        }

        // ============================================================
        // RAW CERTIFICATIONS
        // ============================================================

        private void ParseRawCertifications(
            ParsedCv result,
            string? section)
        {
            if (string.IsNullOrWhiteSpace(section))
                return;

            var items =
                ExtractListItems(
                    section);

            foreach (var item in items)
            {
                var clean =
                    CleanLine(item);

                if (string.IsNullOrWhiteSpace(clean))
                    continue;

                if (clean.Length > 150)
                    continue;

                result.Certifications.Add(
                    new CvCertification
                    {
                        Name = clean,
                        RawText = clean
                    });
            }
        }

        // ============================================================
        // FALLBACK EXPERIENCE
        // ============================================================

        private void ParsePossibleExperienceFromWholeCv(
            ParsedCv result,
            string text)
        {
            var lines =
                text
                    .Split(
                        '\n',
                        StringSplitOptions.RemoveEmptyEntries)
                    .Select(CleanLine)
                    .Where(
                        x =>
                            !string.IsNullOrWhiteSpace(x))
                    .ToList();

            foreach (var line in lines)
            {
                if (!LooksLikeExperienceHeader(
                        line))
                {
                    continue;
                }

                var experience =
                    ParseExperienceBlock(
                        new List<string>
                        {
                            line
                        });

                if (experience == null)
                    continue;

                if (string.IsNullOrWhiteSpace(
                        experience.Position))
                {
                    continue;
                }

                MergeRawExperience(
                    result,
                    experience);
            }
        }

        // ============================================================
        // MERGE EXPERIENCE
        // ============================================================

        private void MergeRawExperience(
            ParsedCv result,
            CvExperience incoming)
        {
            var incomingPosition =
                NormalizeForComparison(
                    incoming.Position);

            var incomingEmployer =
                NormalizeForComparison(
                    incoming.Employer);

            var existing =
                result.Experience.FirstOrDefault(
                    x =>
                    (
                        !string.IsNullOrWhiteSpace(
                            incomingPosition)
                        &&
                        NormalizeForComparison(
                            x.Position) ==
                        incomingPosition
                    )
                    ||
                    (
                        !string.IsNullOrWhiteSpace(
                            incomingEmployer)
                        &&
                        NormalizeForComparison(
                            x.Employer) ==
                        incomingEmployer
                    ));

            if (existing == null)
            {
                result.Experience.Add(
                    incoming);

                return;
            }

            // --------------------------------------------------------
            // Employer
            // --------------------------------------------------------

            if (string.IsNullOrWhiteSpace(
                    existing.Employer) &&
                !string.IsNullOrWhiteSpace(
                    incoming.Employer))
            {
                existing.Employer =
                    incoming.Employer;
            }

            // --------------------------------------------------------
            // Position
            // --------------------------------------------------------

            if (string.IsNullOrWhiteSpace(
                    existing.Position) &&
                !string.IsNullOrWhiteSpace(
                    incoming.Position))
            {
                existing.Position =
                    incoming.Position;
            }

            // --------------------------------------------------------
            // Dates
            // --------------------------------------------------------

            if (string.IsNullOrWhiteSpace(
                    existing.StartDate) &&
                !string.IsNullOrWhiteSpace(
                    incoming.StartDate))
            {
                existing.StartDate =
                    incoming.StartDate;
            }

            if (string.IsNullOrWhiteSpace(
                    existing.EndDate) &&
                !string.IsNullOrWhiteSpace(
                    incoming.EndDate))
            {
                existing.EndDate =
                    incoming.EndDate;
            }

            // --------------------------------------------------------
            // Description
            // --------------------------------------------------------

            if (!string.IsNullOrWhiteSpace(
                    incoming.Description))
            {
                if (string.IsNullOrWhiteSpace(
                        existing.Description))
                {
                    existing.Description =
                        incoming.Description;
                }
                else if (
                    !existing.Description.Contains(
                        incoming.Description,
                        StringComparison.OrdinalIgnoreCase))
                {
                    existing.Description +=
                        " " +
                        incoming.Description;
                }
            }

            existing.RawText =
                BuildExperienceRawText(
                    existing.Employer,
                    existing.Position,
                    existing.StartDate,
                    existing.EndDate,
                    existing.Description);
        }

        // ============================================================
        // SECTION EXTRACTION
        // ============================================================

        private string? ExtractSection(
            string text,
            params string[] headings)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            var lines =
                text
                    .Split(
                        '\n',
                        StringSplitOptions.None)
                    .Select(CleanLine)
                    .ToList();

            for (int i = 0;
                 i < lines.Count;
                 i++)
            {
                if (!IsHeadingMatch(
                        lines[i],
                        headings))
                {
                    continue;
                }

                var builder =
                    new StringBuilder();

                for (int j = i + 1;
                     j < lines.Count;
                     j++)
                {
                    if (LooksLikeMajorSectionHeading(
                            lines[j]))
                    {
                        break;
                    }

                    builder.AppendLine(
                        lines[j]);
                }

                return builder
                    .ToString()
                    .Trim();
            }

            return null;
        }

        // ============================================================
        // LIST EXTRACTION
        // ============================================================

        private List<string> ExtractListItems(
            string section)
        {
            var results =
                new List<string>();

            if (string.IsNullOrWhiteSpace(section))
                return results;

            var lines =
                section
                    .Split(
                        '\n',
                        StringSplitOptions.RemoveEmptyEntries)
                    .Select(CleanLine)
                    .Where(
                        x =>
                            !string.IsNullOrWhiteSpace(x))
                    .ToList();

            foreach (var line in lines)
            {
                var split =
                    Regex.Split(
                        line,
                        @"(?=[•·])");

                if (split.Length > 1)
                {
                    foreach (var part in split)
                    {
                        var clean =
                            CleanSkillItem(
                                part);

                        if (!string.IsNullOrWhiteSpace(
                                clean))
                        {
                            results.Add(
                                clean);
                        }
                    }

                    continue;
                }

                var cleanLine =
                    CleanSkillItem(
                        line);

                if (!string.IsNullOrWhiteSpace(
                        cleanLine))
                {
                    results.Add(
                        cleanLine);
                }
            }

            return results
                .GroupBy(
                    NormalizeForComparison)
                .Select(
                    g => g.First())
                .ToList();
        }

        // ============================================================
        // HEADING HELPERS
        // ============================================================

        private bool IsHeadingMatch(
            string line,
            params string[] headings)
        {
            var clean =
                CleanSectionHeading(
                    line);

            foreach (var heading in headings)
            {
                if (string.Equals(
                        clean,
                        CleanSectionHeading(heading),
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private bool LooksLikeMajorSectionHeading(
            string line)
        {
            var clean =
                CleanSectionHeading(
                    line);

            if (string.IsNullOrWhiteSpace(clean))
                return false;

            var headings =
                new[]
                {
                    "CORE SKILLS",
                    "SKILLS",
                    "KEY SKILLS",
                    "TECHNICAL SKILLS",
                    "COMPETENCIES",
                    "AREAS OF EXPERTISE",

                    "EXPERIENCE",
                    "WORK EXPERIENCE",
                    "PROFESSIONAL EXPERIENCE",
                    "EMPLOYMENT HISTORY",
                    "VOLUNTEER EXPERIENCE",
                    "VOLUNTEERING",

                    "EDUCATION",
                    "EDUCATIONAL BACKGROUND",
                    "ACADEMIC QUALIFICATIONS",
                    "QUALIFICATIONS",

                    "CERTIFICATIONS",
                    "CERTIFICATES",
                    "TRAINING",
                    "PROFESSIONAL CERTIFICATIONS",

                    "LANGUAGES",
                    "LANGUAGE SKILLS",

                    "PROFESSIONAL SUMMARY",
                    "SUMMARY",
                    "PROFILE",

                    "CAREER OBJECTIVE",
                    "OBJECTIVE",

                    "CONTACT",
                    "REFERENCES",
                    "PERSONAL DETAILS"
                };

            return headings.Any(
                x =>
                    string.Equals(
                        clean,
                        x,
                        StringComparison.OrdinalIgnoreCase));
        }

        // ============================================================
        // AZURE FIELD HELPERS
        // ============================================================

        private bool TryGetField(
            Dictionary<string, AzureCvField> fields,
            string name,
            out AzureCvField field)
        {
            if (fields.TryGetValue(
                    name,
                    out field!))
            {
                return true;
            }

            var match =
                fields.FirstOrDefault(
                    x =>
                        string.Equals(
                            x.Key,
                            name,
                            StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(
                    match.Key))
            {
                field =
                    match.Value;

                return true;
            }

            field = null!;

            return false;
        }

        private string? GetFieldText(
            Dictionary<string, AzureCvField> fields,
            string name)
        {
            if (!TryGetField(
                    fields,
                    name,
                    out var field))
            {
                return null;
            }

            return GetFieldTextRecursive(
                field);
        }

        private string? GetNestedText(
            AzureCvField? parent,
            string name)
        {
            if (parent == null)
                return null;

            // --------------------------------------------------------
            // ObjectValues is the correct property in your DTO.
            // --------------------------------------------------------

            if (parent.ObjectValues != null &&
                parent.ObjectValues.Count > 0)
            {
                if (parent.ObjectValues.TryGetValue(
                        name,
                        out var direct))
                {
                    return GetFieldTextRecursive(
                        direct);
                }

                var match =
                    parent.ObjectValues.FirstOrDefault(
                        x =>
                            string.Equals(
                                x.Key,
                                name,
                                StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrWhiteSpace(
                        match.Key))
                {
                    return GetFieldTextRecursive(
                        match.Value);
                }
            }

            return null;
        }

        private string? GetFieldTextRecursive(
            AzureCvField? field)
        {
            if (field == null)
                return null;

            var values =
                new List<string>();

            // --------------------------------------------------------
            // Content
            // --------------------------------------------------------

            if (!string.IsNullOrWhiteSpace(
                    field.Content))
            {
                values.Add(
                    field.Content.Trim());
            }

            // --------------------------------------------------------
            // Nested object values
            // --------------------------------------------------------

            if (field.ObjectValues != null)
            {
                foreach (var child in
                    field.ObjectValues.Values)
                {
                    var childText =
                        GetFieldTextRecursive(
                            child);

                    if (string.IsNullOrWhiteSpace(
                            childText))
                    {
                        continue;
                    }

                    if (!values.Any(
                            x =>
                                string.Equals(
                                    x,
                                    childText,
                                    StringComparison.OrdinalIgnoreCase)))
                    {
                        values.Add(
                            childText);
                    }
                }
            }

            // --------------------------------------------------------
            // Array values
            // --------------------------------------------------------

            if (field.ArrayValues != null)
            {
                foreach (var child in
                    field.ArrayValues)
                {
                    var childText =
                        GetFieldTextRecursive(
                            child);

                    if (string.IsNullOrWhiteSpace(
                            childText))
                    {
                        continue;
                    }

                    if (!values.Any(
                            x =>
                                string.Equals(
                                    x,
                                    childText,
                                    StringComparison.OrdinalIgnoreCase)))
                    {
                        values.Add(
                            childText);
                    }
                }
            }

            if (values.Count == 0)
                return null;

            return string.Join(
                " ",
                values);
        }

        // ============================================================
        // OBJECT KEY CHECK
        // ============================================================

        private bool ContainsAnyKey(
            Dictionary<string, AzureCvField> values,
            params string[] keys)
        {
            foreach (var key in keys)
            {
                if (values.Keys.Any(
                        x =>
                            string.Equals(
                                x,
                                key,
                                StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }

            return false;
        }

        // ============================================================
        // ADD SKILL
        // ============================================================

        private void AddSkill(
            ParsedCv result,
            string? skill)
        {
            skill =
                CleanSkillItem(
                    skill);

            if (string.IsNullOrWhiteSpace(skill))
                return;

            var normalized =
                NormalizeForComparison(
                    skill);

            if (string.IsNullOrWhiteSpace(
                    normalized))
            {
                return;
            }

            bool exists =
                result.Skills.Any(
                    x =>
                        NormalizeForComparison(
                            x.Skill) ==
                        normalized);

            if (exists)
                return;

            result.Skills.Add(
                new CvSkill
                {
                    Skill = skill,
                    RawText = skill
                });
        }

        // ============================================================
        // ADD LANGUAGE
        // ============================================================

        private void AddLanguage(
            ParsedCv result,
            string? language)
        {
            language =
                CleanValue(
                    language);

            if (string.IsNullOrWhiteSpace(
                    language))
            {
                return;
            }

            var normalized =
                NormalizeForComparison(
                    language);

            bool exists =
                result.Languages.Any(
                    x =>
                        NormalizeForComparison(
                            x.Language) ==
                        normalized);

            if (exists)
                return;

            result.Languages.Add(
                new CvLanguage
                {
                    Language = language,
                    RawText = language
                });
        }

        // ============================================================
        // CLEANUP
        // ============================================================

        private void CleanAndDeduplicate(
            ParsedCv result)
        {
            // ========================================================
            // SKILLS
            // ========================================================

            result.Skills =
                result.Skills
                    .Where(
                        x =>
                            !string.IsNullOrWhiteSpace(
                                x.Skill))
                    .GroupBy(
                        x =>
                            NormalizeForComparison(
                                x.Skill))
                    .Select(
                        g => g.First())
                    .ToList();

            // ========================================================
            // LANGUAGES
            // ========================================================

            result.Languages =
                result.Languages
                    .Where(
                        x =>
                            !string.IsNullOrWhiteSpace(
                                x.Language))
                    .GroupBy(
                        x =>
                            NormalizeForComparison(
                                x.Language))
                    .Select(
                        g => g.First())
                    .ToList();

            // ========================================================
            // EDUCATION
            // ========================================================

            result.Education =
                result.Education
                    .Where(
                        x =>
                            !string.IsNullOrWhiteSpace(
                                x.Qualification)
                            ||
                            !string.IsNullOrWhiteSpace(
                                x.Institution)
                            ||
                            !string.IsNullOrWhiteSpace(
                                x.FieldOfStudy))
                    .GroupBy(
                        x =>
                            NormalizeForComparison(
                                $"{x.Qualification}|{x.Institution}|{x.FieldOfStudy}"))
                    .Select(
                        g => g.First())
                    .ToList();

            // ========================================================
            // CERTIFICATIONS
            // ========================================================

            result.Certifications =
                result.Certifications
                    .Where(
                        x =>
                            !string.IsNullOrWhiteSpace(
                                x.Name))
                    .GroupBy(
                        x =>
                            NormalizeForComparison(
                                $"{x.Name}|{x.IssuingOrganisation}"))
                    .Select(
                        g => g.First())
                    .ToList();

            // ========================================================
            // EXPERIENCE
            // ========================================================

            result.Experience =
                result.Experience
                    .Where(
                        x =>
                            !string.IsNullOrWhiteSpace(
                                x.Position)
                            ||
                            !string.IsNullOrWhiteSpace(
                                x.Employer)
                            ||
                            !string.IsNullOrWhiteSpace(
                                x.Description))
                    .GroupBy(
                        x =>
                            NormalizeForComparison(
                                $"{x.Position}|{x.Employer}|{x.StartDate}|{x.EndDate}"))
                    .Select(
                        g => g.First())
                    .ToList();

            // ========================================================
            // SCALAR VALUES
            // ========================================================

            result.FullName =
                CleanValue(
                    result.FullName);

            result.Email =
                CleanValue(
                    result.Email);

            result.PhoneNumber =
                CleanValue(
                    result.PhoneNumber);

            result.ProfessionalTitle =
                CleanValue(
                    result.ProfessionalTitle);

            result.ProfessionalSummary =
                CleanValue(
                    result.ProfessionalSummary);

            result.CareerObjective =
                CleanValue(
                    result.CareerObjective);

            result.Address =
                CleanValue(
                    result.Address);

            result.City =
                CleanValue(
                    result.City);

            result.Province =
                CleanValue(
                    result.Province);

            result.Country =
                CleanValue(
                    result.Country);

            result.LinkedInUrl =
                CleanValue(
                    result.LinkedInUrl);

            result.PortfolioUrl =
                CleanValue(
                    result.PortfolioUrl);

            result.GitHubUrl =
                CleanValue(
                    result.GitHubUrl);
        }

        // ============================================================
        // GENERAL HELPERS
        // ============================================================

        private string CleanValue(
            string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var result =
                value
                    .Replace("\r", " ")
                    .Replace("\n", " ")
                    .Trim();

            result =
                Regex.Replace(
                    result,
                    @"\s{2,}",
                    " ");

            return result.Trim();
        }

        private string CleanLine(
            string? line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return string.Empty;

            var result =
                line
                    .Replace("\r", "")
                    .Trim();

            result =
                Regex.Replace(
                    result,
                    @"^#{1,6}\s*",
                    "");

            result =
                result
                    .Replace("**", "")
                    .Replace("__", "")
                    .Replace("*", "");

            return result.Trim();
        }

        private string CleanSectionHeading(
            string? line)
        {
            var result =
                CleanLine(
                    line);

            result =
                result.Trim(
                    ':',
                    '-',
                    '—',
                    '–',
                    ' ');

            return result.ToUpperInvariant();
        }

        private string CleanSkillItem(
            string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var result =
                CleanLine(
                    value);

            result =
                result.Trim(
                    '•',
                    '·',
                    '●',
                    '▪',
                    '◦',
                    '-',
                    '*',
                    '.',
                    ' ',
                    ':');

            result =
                Regex.Replace(
                    result,
                    @"^\d+[\.\)]\s*",
                    "");

            result =
                Regex.Replace(
                    result,
                    @"\s{2,}",
                    " ");

            return result.Trim();
        }

        private string NormalizeForComparison(
            string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return Regex.Replace(
                value
                    .Trim()
                    .ToLowerInvariant(),
                @"[^a-z0-9]+",
                "");
        }

        private string ExtractUrl(
            string line)
        {
            var match =
                Regex.Match(
                    line,
                    @"https?://[^\s]+",
                    RegexOptions.IgnoreCase);

            if (!match.Success)
                return string.Empty;

            return match.Value.Trim(
                '.',
                ',',
                ';',
                ')',
                ']');
        }

        private bool LooksLikeHeader(
            string line)
        {
            return LooksLikeMajorSectionHeading(
                line);
        }

        private string NormalizeLineBreaks(
            string text)
        {
            return text
                .Replace("\r\n", "\n")
                .Replace("\r", "\n");
        }

        private string BuildExperienceRawText(
            string? employer,
            string? position,
            string? startDate,
            string? endDate,
            string? description)
        {
            var parts =
                new List<string>();

            if (!string.IsNullOrWhiteSpace(
                    position))
            {
                parts.Add(
                    position);
            }

            if (!string.IsNullOrWhiteSpace(
                    employer))
            {
                parts.Add(
                    employer);
            }

            if (!string.IsNullOrWhiteSpace(
                    startDate))
            {
                parts.Add(
                    startDate);
            }

            if (!string.IsNullOrWhiteSpace(
                    endDate))
            {
                parts.Add(
                    endDate);
            }

            if (!string.IsNullOrWhiteSpace(
                    description))
            {
                parts.Add(
                    description);
            }

            return string.Join(
                " | ",
                parts);
        }
    }
}