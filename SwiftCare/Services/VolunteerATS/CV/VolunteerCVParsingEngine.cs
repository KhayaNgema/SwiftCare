using SwiftCare.Models.VolunteerCV;
using SwiftCare.Services.VolunteerATS.CV;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SwiftCare.Services.VolunteerATS
{
    public class VolunteerCVParsingEngine
        : IVolunteerCVParsingEngine
    {
        // =============================================================
        // SECTION DEFINITIONS
        // =============================================================

        private static readonly Dictionary<string, string[]>
            SectionAliases =
                new(StringComparer.OrdinalIgnoreCase)
                {
                    ["summary"] = new[]
                    {
                        "professional summary",
                        "summary",
                        "profile",
                        "professional profile",
                        "career profile",
                        "career summary",
                        "objective",
                        "career objective"
                    },

                    ["skills"] = new[]
                    {
                        "skills",
                        "core skills",
                        "key skills",
                        "technical skills",
                        "professional skills",
                        "competencies",
                        "core competencies",
                        "key competencies",
                        "areas of expertise",
                        "expertise",
                        "skills and competencies"
                    },

                    ["experience"] = new[]
                    {
                        "experience",
                        "work experience",
                        "professional experience",
                        "employment history",
                        "employment experience",
                        "relevant experience",
                        "career experience",
                        "work history",
                        "volunteer experience",
                        "volunteering experience",
                        "administrative experience",
                        "internship",
                        "internships",
                        "relevant work experience"
                    },

                    ["education"] = new[]
                    {
                        "education",
                        "educational background",
                        "academic background",
                        "academic history",
                        "qualifications",
                        "academic qualifications",
                        "education and qualifications",
                        "education qualifications"
                    },

                    ["certifications"] = new[]
                    {
                        "certifications",
                        "certificates",
                        "professional certifications",
                        "professional certificates",
                        "licenses",
                        "licences",
                        "training",
                        "courses",
                        "certification and training"
                    },

                    ["languages"] = new[]
                    {
                        "languages",
                        "language skills",
                        "languages spoken"
                    },

                    ["references"] = new[]
                    {
                        "references",
                        "referees"
                    },

                    ["projects"] = new[]
                    {
                        "projects",
                        "academic projects",
                        "personal projects"
                    },

                    ["awards"] = new[]
                    {
                        "awards",
                        "achievements",
                        "honours",
                        "honors"
                    }
                };

        // =============================================================
        // PUBLIC PARSER
        // =============================================================

        public Task<ParsedCv> ParseAsync(
            string rawText,
            int volunteerApplicationId = 0)
        {
            var result = new ParsedCv
            {
                VolunteerApplicationId =
                    volunteerApplicationId,

                RawText =
                    rawText ?? string.Empty,

                ExtractionSuccessful =
                    !string.IsNullOrWhiteSpace(rawText),

                ExtractionMessage =
                    string.IsNullOrWhiteSpace(rawText)
                        ? "No CV text was available for parsing."
                        : "CV text successfully extracted and parsed."
            };

            if (string.IsNullOrWhiteSpace(rawText))
            {
                return Task.FromResult(result);
            }

            // ---------------------------------------------------------
            // NORMALIZE
            // ---------------------------------------------------------

            var lines =
                NormalizeLines(rawText);

            // ---------------------------------------------------------
            // PARSE SECTIONS
            // ---------------------------------------------------------

            var sections =
                ParseSections(lines);

            // ---------------------------------------------------------
            // BASIC INFORMATION
            // ---------------------------------------------------------

            result.FullName =
                ExtractFullName(lines);

            result.Email =
                ExtractEmail(rawText);

            result.PhoneNumber =
                ExtractPhoneNumber(rawText);

            // ---------------------------------------------------------
            // SKILLS
            // ---------------------------------------------------------

            result.Skills =
                ExtractSkills(
                    sections,
                    rawText);

            // ---------------------------------------------------------
            // LANGUAGES
            // ---------------------------------------------------------

            result.Languages =
                ExtractLanguages(
                    sections,
                    rawText);

            // ---------------------------------------------------------
            // CERTIFICATIONS
            // ---------------------------------------------------------

            result.Certifications =
                ExtractCertifications(
                    sections);

            // ---------------------------------------------------------
            // EDUCATION
            // ---------------------------------------------------------

            result.Education =
                ExtractEducation(
                    sections);

            // ---------------------------------------------------------
            // EXPERIENCE
            // ---------------------------------------------------------

            result.Experience =
                ExtractExperience(
                    sections);

            // ---------------------------------------------------------
            // FINAL PARSE STATUS
            // ---------------------------------------------------------

            result.ExtractionSuccessful =
                !string.IsNullOrWhiteSpace(rawText);

            result.ExtractionMessage =
                BuildExtractionMessage(result);

            return Task.FromResult(result);
        }

        // =============================================================
        // EXTRACTION MESSAGE
        // =============================================================

        private static string BuildExtractionMessage(
            ParsedCv result)
        {
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(result.FullName))
                parts.Add("name");

            if (!string.IsNullOrWhiteSpace(result.Email))
                parts.Add("email");

            if (!string.IsNullOrWhiteSpace(result.PhoneNumber))
                parts.Add("phone");

            if (result.Skills.Any())
                parts.Add($"{result.Skills.Count} skills");

            if (result.Education.Any())
                parts.Add($"{result.Education.Count} education record(s)");

            if (result.Experience.Any())
                parts.Add($"{result.Experience.Count} experience record(s)");

            if (result.Certifications.Any())
                parts.Add($"{result.Certifications.Count} certification(s)");

            if (result.Languages.Any())
                parts.Add($"{result.Languages.Count} language(s)");

            if (parts.Count == 0)
            {
                return "CV text was extracted, but no structured candidate information could be identified.";
            }

            return
                "CV successfully parsed. Identified: " +
                string.Join(", ", parts) +
                ".";
        }

        // =============================================================
        // NORMALIZE LINES
        // =============================================================

        private static List<string> NormalizeLines(
            string rawText)
        {
            rawText = rawText
                .Replace("\r\n", "\n")
                .Replace("\r", "\n")
                .Replace("\u00A0", " ");

            var result =
                new List<string>();

            foreach (var rawLine in rawText.Split('\n'))
            {
                if (string.IsNullOrWhiteSpace(rawLine))
                    continue;

                var line =
                    rawLine.Trim();

                line =
                    Regex.Replace(
                        line,
                        @"\s+",
                        " ");

                line = line
                    .Replace("–", "-")
                    .Replace("—", "-")
                    .Replace("▪", "•")
                    .Replace("●", "•")
                    .Replace("◦", "•");

                if (!string.IsNullOrWhiteSpace(line))
                {
                    result.Add(
                        line.Trim());
                }
            }

            return result;
        }

        // =============================================================
        // PARSE ALL SECTIONS
        // =============================================================

        private static Dictionary<string, List<string>>
            ParseSections(
                List<string> lines)
        {
            var sections =
                new Dictionary<string, List<string>>(
                    StringComparer.OrdinalIgnoreCase);

            string? currentSection = null;

            foreach (var line in lines)
            {
                var sectionType =
                    GetSectionType(line);

                if (sectionType != null)
                {
                    currentSection =
                        sectionType;

                    if (!sections.ContainsKey(
                        currentSection))
                    {
                        sections[currentSection] =
                            new List<string>();
                    }

                    continue;
                }

                if (currentSection != null)
                {
                    sections[currentSection]
                        .Add(line);
                }
            }

            return sections;
        }

        // =============================================================
        // SECTION TYPE
        // =============================================================

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
                    if (normalized ==
                        NormalizeHeading(alias))
                    {
                        return section.Key;
                    }
                }
            }

            var withoutSymbols =
                normalized
                    .Replace("&", " and ")
                    .Replace("/", " ")
                    .Replace("|", " ");

            withoutSymbols =
                Regex.Replace(
                    withoutSymbols,
                    @"\s+",
                    " ")
                .Trim();

            foreach (var section in SectionAliases)
            {
                foreach (var alias in section.Value)
                {
                    var normalizedAlias =
                        NormalizeHeading(alias)
                            .Replace("&", " and ");

                    if (withoutSymbols ==
                        normalizedAlias)
                    {
                        return section.Key;
                    }
                }
            }

            return null;
        }

        // =============================================================
        // HEADING NORMALIZATION
        // =============================================================

        private static string NormalizeHeading(
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            value =
                value.Trim();

            value =
                Regex.Replace(
                    value,
                    @"[:\-–—]+$",
                    "");

            value =
                Regex.Replace(
                    value,
                    @"[^\p{L}\p{N}&/ ]",
                    " ");

            value =
                Regex.Replace(
                    value,
                    @"\s+",
                    " ");

            return value
                .Trim()
                .ToLowerInvariant();
        }

        // =============================================================
        // FULL NAME
        // =============================================================

        private static string? ExtractFullName(
            List<string> lines)
        {
            foreach (var line in lines.Take(12))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (line.Contains("@"))
                    continue;

                if (Regex.IsMatch(
                    line,
                    @"\d"))
                    continue;

                if (IsSectionHeading(line))
                    continue;

                var normalized =
                    NormalizeHeading(line);

                if (normalized.Contains(
                        "curriculum vitae") ||
                    normalized == "resume" ||
                    normalized == "cv")
                {
                    continue;
                }

                var words =
                    line.Split(
                        ' ',
                        StringSplitOptions.RemoveEmptyEntries);

                if (words.Length < 2 ||
                    words.Length > 5)
                {
                    continue;
                }

                if (line.Length < 3 ||
                    line.Length > 80)
                {
                    continue;
                }

                return line;
            }

            return null;
        }

        // =============================================================
        // EMAIL
        // =============================================================

        private static string? ExtractEmail(
            string rawText)
        {
            var match =
                Regex.Match(
                    rawText,
                    @"[A-Z0-9._%+\-]+@[A-Z0-9.\-]+\.[A-Z]{2,}",
                    RegexOptions.IgnoreCase);

            return match.Success
                ? match.Value.Trim()
                : null;
        }

        // =============================================================
        // PHONE
        // =============================================================

        private static string? ExtractPhoneNumber(
            string rawText)
        {
            var patterns =
                new[]
                {
                    @"(?:\+27|0027)\s?\d{2}\s?\d{3}\s?\d{4}",
                    @"0\d{2}[\s\-]?\d{3}[\s\-]?\d{4}",
                    @"\(\d{3}\)[\s\-]?\d{3}[\s\-]?\d{4}"
                };

            foreach (var pattern in patterns)
            {
                var match =
                    Regex.Match(
                        rawText,
                        pattern);

                if (match.Success)
                    return match.Value.Trim();
            }

            return null;
        }

        // =============================================================
        // SKILLS
        // =============================================================

        private static List<string> ExtractSkills(
            Dictionary<string, List<string>> sections,
            string rawText)
        {
            var skills =
                new List<string>();

            if (sections.TryGetValue(
                "skills",
                out var skillLines))
            {
                foreach (var line in skillLines)
                {
                    AddSkillValues(
                        skills,
                        line);
                }
            }

            if (skills.Count == 0)
            {
                var knownSkills =
                    new[]
                    {
                        "communication",
                        "communication skills",
                        "interpersonal skills",
                        "teamwork",
                        "leadership",
                        "problem solving",
                        "attention to detail",
                        "administrative support",
                        "data capturing",
                        "data capture",
                        "record keeping",
                        "community outreach",
                        "community engagement",
                        "youth engagement",
                        "youth programme support",
                        "event coordination",
                        "programme administration",
                        "microsoft word",
                        "microsoft excel",
                        "microsoft powerpoint",
                        "microsoft office",
                        "customer service",
                        "project management",
                        "time management",
                        "confidentiality",
                        "participant registration",
                        "programme materials",
                        "activity reporting"
                    };

                foreach (var skill in knownSkills)
                {
                    if (rawText.Contains(
                        skill,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        skills.Add(skill);
                    }
                }
            }

            return skills
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        // =============================================================
        // ADD SKILL VALUES
        // =============================================================

        private static void AddSkillValues(
            List<string> skills,
            string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return;

            line =
                CleanBullet(line);

            var values =
                Regex.Split(
                    line,
                    @"[,;|]")
                .Select(x => x.Trim())
                .Where(x =>
                    !string.IsNullOrWhiteSpace(x));

            foreach (var value in values)
            {
                var clean =
                    CleanBullet(value);

                if (clean.Length >= 2 &&
                    clean.Length <= 120)
                {
                    skills.Add(clean);
                }
            }
        }

        // =============================================================
        // LANGUAGES
        // =============================================================

        private static List<string> ExtractLanguages(
            Dictionary<string, List<string>> sections,
            string rawText)
        {
            var languages =
                new List<string>();

            if (sections.TryGetValue(
                "languages",
                out var languageLines))
            {
                foreach (var line in languageLines)
                {
                    var values =
                        Regex.Split(
                            CleanBullet(line),
                            @"[,;|]")
                        .Select(x => x.Trim())
                        .Where(x =>
                            !string.IsNullOrWhiteSpace(x));

                    languages.AddRange(values);
                }
            }

            var knownLanguages =
                new[]
                {
                    "English",
                    "isiZulu",
                    "Zulu",
                    "isiXhosa",
                    "Xhosa",
                    "Afrikaans",
                    "Sesotho",
                    "Setswana",
                    "Sepedi",
                    "siSwati",
                    "Tshivenda",
                    "itsonga",
                    "Ndebele"
                };

            foreach (var language in knownLanguages)
            {
                if (rawText.Contains(
                    language,
                    StringComparison.OrdinalIgnoreCase))
                {
                    languages.Add(language);
                }
            }

            return languages
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        // =============================================================
        // CERTIFICATIONS
        // =============================================================

        private static List<CvCertification>
            ExtractCertifications(
                Dictionary<string, List<string>> sections)
        {
            var certifications =
                new List<CvCertification>();

            if (!sections.TryGetValue(
                "certifications",
                out var lines))
            {
                return certifications;
            }

            foreach (var line in lines)
            {
                var clean =
                    CleanBullet(line);

                if (string.IsNullOrWhiteSpace(clean))
                    continue;

                var date =
                    ExtractYearOrDate(clean);

                certifications.Add(
                    new CvCertification
                    {
                        Name = clean,
                        IssuingOrganisation = null,
                        Date = date,
                        RawText = clean
                    });
            }

            return certifications;
        }

        // =============================================================
        // EDUCATION
        // =============================================================

        private static List<CvEducation>
            ExtractEducation(
                Dictionary<string, List<string>> sections)
        {
            var education =
                new List<CvEducation>();

            if (!sections.TryGetValue(
                "education",
                out var lines))
            {
                return education;
            }

            var cleaned =
                lines
                    .Select(CleanBullet)
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x))
                    .ToList();

            if (cleaned.Count == 0)
                return education;

            var records =
                GroupEducationRecords(cleaned);

            foreach (var record in records)
            {
                var parsed =
                    BuildEducationRecord(record);

                if (parsed != null)
                    education.Add(parsed);
            }

            return education;
        }

        // =============================================================
        // GROUP EDUCATION RECORDS
        // =============================================================

        private static List<List<string>>
            GroupEducationRecords(
                List<string> lines)
        {
            var records =
                new List<List<string>>();

            var current =
                new List<string>();

            foreach (var line in lines)
            {
                if (current.Count > 0 &&
                    LooksLikeNewEducationRecord(line))
                {
                    records.Add(current);
                    current = new List<string>();
                }

                current.Add(line);
            }

            if (current.Count > 0)
                records.Add(current);

            return records;
        }

        // =============================================================
        // BUILD EDUCATION RECORD
        // =============================================================

        private static CvEducation?
            BuildEducationRecord(
                List<string> lines)
        {
            if (lines == null ||
                lines.Count == 0)
            {
                return null;
            }

            var raw =
                string.Join(
                    " | ",
                    lines);

            var qualification =
                lines.FirstOrDefault(
                    LooksLikeQualification);

            if (string.IsNullOrWhiteSpace(
                qualification))
            {
                qualification =
                    lines.FirstOrDefault();
            }

            var dateMatches =
                Regex.Matches(
                    raw,
                    @"\b(19|20)\d{2}\b");

            string? startDate = null;
            string? endDate = null;

            if (dateMatches.Count >= 2)
            {
                startDate =
                    dateMatches[0].Value;

                endDate =
                    dateMatches[1].Value;
            }
            else if (dateMatches.Count == 1)
            {
                endDate =
                    dateMatches[0].Value;
            }

            var institution =
                FindEducationInstitution(
                    lines,
                    qualification);

            var field =
                ExtractFieldOfStudy(
                    qualification);

            return new CvEducation
            {
                Qualification =
                    CleanValue(qualification),

                Institution =
                    CleanValue(institution),

                FieldOfStudy =
                    CleanValue(field),

                StartDate =
                    startDate,

                EndDate =
                    endDate,

                RawText =
                    raw
            };
        }

        // =============================================================
        // EDUCATION INSTITUTION
        // =============================================================

        private static string? FindEducationInstitution(
            List<string> lines,
            string? qualification)
        {
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (line == qualification)
                    continue;

                if (Regex.IsMatch(
                    line,
                    @"\b(19|20)\d{2}\b"))
                {
                    continue;
                }

                if (LooksLikeQualification(line))
                    continue;

                return line;
            }

            return null;
        }

        // =============================================================
        // FIELD OF STUDY
        // =============================================================

        private static string? ExtractFieldOfStudy(
            string? qualification)
        {
            if (string.IsNullOrWhiteSpace(
                qualification))
            {
                return null;
            }

            var match =
                Regex.Match(
                    qualification,
                    @"\b(?:in|of)\s+(.+)$",
                    RegexOptions.IgnoreCase);

            if (!match.Success)
                return null;

            return match.Groups[1]
                .Value
                .Trim();
        }

        // =============================================================
        // NEW EDUCATION RECORD
        // =============================================================

        private static bool LooksLikeNewEducationRecord(
            string line)
        {
            if (LooksLikeQualification(line))
                return true;

            return Regex.IsMatch(
                line,
                @"\b(19|20)\d{2}\b\s*[-]\s*\b(19|20)\d{2}\b");
        }

        // =============================================================
        // QUALIFICATION DETECTION
        // =============================================================

        private static bool LooksLikeQualification(
            string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return false;

            var patterns =
                new[]
                {
                    @"\bdiploma\b",
                    @"\bdegree\b",
                    @"\bbachelor\b",
                    @"\bmaster\b",
                    @"\bdoctorate\b",
                    @"\bphd\b",
                    @"\bbsc\b",
                    @"\bba\b",
                    @"\bbcom\b",
                    @"\bbtech\b",
                    @"\bhonours\b",
                    @"\bhonors\b",
                    @"\bcertificate\b",
                    @"\bcertification\b",
                    @"\bnational senior certificate\b",
                    @"\bmatric\b",
                    @"\bgrade\s*12\b"
                };

            return patterns.Any(
                pattern =>
                    Regex.IsMatch(
                        line,
                        pattern,
                        RegexOptions.IgnoreCase));
        }

        // =============================================================
        // EXPERIENCE
        // =============================================================

        private static List<CvExperience>
            ExtractExperience(
                Dictionary<string, List<string>> sections)
        {
            var experience =
                new List<CvExperience>();

            if (!sections.TryGetValue(
                "experience",
                out var lines))
            {
                return experience;
            }

            var cleaned =
                lines
                    .Select(CleanBullet)
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x))
                    .ToList();

            if (cleaned.Count == 0)
                return experience;

            var records =
                GroupExperienceRecords(cleaned);

            foreach (var record in records)
            {
                var parsed =
                    BuildExperienceRecord(record);

                if (parsed != null)
                    experience.Add(parsed);
            }

            return experience;
        }

        // =============================================================
        // GROUP EXPERIENCE RECORDS
        // =============================================================

        private static List<List<string>>
            GroupExperienceRecords(
                List<string> lines)
        {
            var records =
                new List<List<string>>();

            var current =
                new List<string>();

            foreach (var line in lines)
            {
                if (current.Count > 0 &&
                    LooksLikeNewExperienceRecord(line))
                {
                    records.Add(current);

                    current =
                        new List<string>();
                }

                current.Add(line);
            }

            if (current.Count > 0)
                records.Add(current);

            return records;
        }

        // =============================================================
        // EXPERIENCE RECORD DETECTION
        // =============================================================

        private static bool LooksLikeNewExperienceRecord(
            string line)
        {
            // Example:
            // Community Outreach Volunteer - Durban Community
            // Development Centre (2024-2025)

            if (ContainsDateRange(line))
                return true;

            // Also recognize common position headings.
            if (Regex.IsMatch(
                line,
                @"\b(volunteer|assistant|officer|coordinator|manager|administrator|intern|facilitator|worker|support)\b",
                RegexOptions.IgnoreCase))
            {
                return true;
            }

            return false;
        }

        // =============================================================
        // BUILD EXPERIENCE RECORD
        // =============================================================

        private static CvExperience?
            BuildExperienceRecord(
                List<string> lines)
        {
            if (lines == null ||
                lines.Count == 0)
            {
                return null;
            }

            var raw =
                string.Join(
                    " | ",
                    lines);

            var header =
                lines.FirstOrDefault(
                    ContainsDateRange)
                ?? lines.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(header))
                return null;

            var dates =
                ExtractDateRange(header);

            var position =
                ExtractPosition(
                    header);

            var employer =
                ExtractEmployer(
                    header,
                    position);

            var descriptionLines =
                lines
                    .Where(x =>
                        !string.Equals(
                            x,
                            header,
                            StringComparison.OrdinalIgnoreCase))
                    .ToList();

            var description =
                string.Join(
                    " ",
                    descriptionLines);

            if (string.IsNullOrWhiteSpace(
                description))
            {
                description =
                    header;
            }

            return new CvExperience
            {
                Position =
                    CleanValue(position),

                Employer =
                    CleanValue(employer),

                StartDate =
                    dates.StartDate,

                EndDate =
                    dates.EndDate,

                Description =
                    CleanValue(description),

                RawText =
                    raw
            };
        }

        // =============================================================
        // DATE RANGE
        // =============================================================

        private static bool ContainsDateRange(
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            return Regex.IsMatch(
                       value,
                       @"\b(19|20)\d{2}\b\s*[-]\s*\b(19|20)\d{2}\b")
                   ||
                   Regex.IsMatch(
                       value,
                       @"\b(19|20)\d{2}\b\s*[-]\s*(present|current)\b",
                       RegexOptions.IgnoreCase);
        }

        // =============================================================
        // EXTRACT DATE RANGE
        // =============================================================

        private static (
            string? StartDate,
            string? EndDate)
            ExtractDateRange(
                string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return (null, null);

            var range =
                Regex.Match(
                    value,
                    @"\b((?:19|20)\d{2})\b\s*[-]\s*((?:19|20)\d{2}|present|current)\b",
                    RegexOptions.IgnoreCase);

            if (range.Success)
            {
                return
                (
                    range.Groups[1].Value,
                    range.Groups[2].Value
                );
            }

            return (null, null);
        }

        // =============================================================
        // EXTRACT POSITION
        // =============================================================

        private static string? ExtractPosition(
            string header)
        {
            if (string.IsNullOrWhiteSpace(header))
                return null;

            var withoutDate =
                Regex.Replace(
                    header,
                    @"\(?\b(19|20)\d{2}\b\s*[-]\s*((19|20)\d{2}|present|current)\b\)?",
                    "",
                    RegexOptions.IgnoreCase);

            withoutDate =
                withoutDate.Trim(
                    ' ',
                    '-',
                    '|',
                    ':',
                    '(',
                    ')');

            // ---------------------------------------------------------
            // Common format:
            //
            // Position - Employer
            // ---------------------------------------------------------

            var parts =
                Regex.Split(
                    withoutDate,
                    @"\s+[|]\s+|\s+-\s+");

            if (parts.Length >= 2)
            {
                return parts[0].Trim();
            }

            // ---------------------------------------------------------
            // Fallback based on common job-title terms.
            // ---------------------------------------------------------

            var match =
                Regex.Match(
                    withoutDate,
                    @"^(.+?\b(?:volunteer|assistant|officer|coordinator|manager|administrator|intern|facilitator|worker|support)\b)",
                    RegexOptions.IgnoreCase);

            if (match.Success)
                return match.Groups[1].Value.Trim();

            return withoutDate;
        }

        // =============================================================
        // EXTRACT EMPLOYER
        // =============================================================

        private static string? ExtractEmployer(
            string header,
            string? position)
        {
            if (string.IsNullOrWhiteSpace(header))
                return null;

            var withoutDate =
                Regex.Replace(
                    header,
                    @"\(?\b(19|20)\d{2}\b\s*[-]\s*((19|20)\d{2}|present|current)\b\)?",
                    "",
                    RegexOptions.IgnoreCase);

            withoutDate =
                withoutDate.Trim(
                    ' ',
                    '-',
                    '|',
                    ':',
                    '(',
                    ')');

            if (!string.IsNullOrWhiteSpace(position))
            {
                var separators =
                    new[]
                    {
                        " - ",
                        " | ",
                        " at "
                    };

                foreach (var separator in separators)
                {
                    var index =
                        withoutDate.IndexOf(
                            separator,
                            StringComparison.OrdinalIgnoreCase);

                    if (index >= 0)
                    {
                        var employer =
                            withoutDate[
                                (index + separator.Length)..]
                            .Trim();

                        if (!string.IsNullOrWhiteSpace(
                            employer))
                        {
                            return employer;
                        }
                    }
                }
            }

            return null;
        }

        // =============================================================
        // DATE / YEAR EXTRACTION
        // =============================================================

        private static string? ExtractYearOrDate(
            string value)
        {
            var match =
                Regex.Match(
                    value,
                    @"\b(19|20)\d{2}\b");

            return match.Success
                ? match.Value
                : null;
        }

        // =============================================================
        // BULLET CLEANING
        // =============================================================

        private static string CleanBullet(
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var clean =
                value.Trim();

            clean =
                Regex.Replace(
                    clean,
                    @"^[•▪●◦*]+\s*",
                    "");

            clean =
                Regex.Replace(
                    clean,
                    @"^\-\s*",
                    "");

            clean =
                clean.Trim();

            return clean;
        }

        // =============================================================
        // CLEAN VALUE
        // =============================================================

        private static string? CleanValue(
            string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var clean =
                value.Trim();

            clean =
                Regex.Replace(
                    clean,
                    @"\s+",
                    " ");

            return string.IsNullOrWhiteSpace(clean)
                ? null
                : clean;
        }

        // =============================================================
        // SECTION CHECK
        // =============================================================

        private static bool IsSectionHeading(
            string line)
        {
            return GetSectionType(line) != null;
        }
    }
}