using System.Text;
using System.Text.Json;
using System.Globalization;
using wakaroute_web.Models;

namespace wakaroute_web.Services.Schools;

public sealed class JsonSchoolCatalog : ISchoolCatalog
{
    private readonly SchoolCatalogItem[] _schools;
    private readonly Dictionary<string, string[]> _aliasesById;
    private readonly Dictionary<string, SchoolProfile> _profilesById;
    private readonly Dictionary<string, SchoolDecisionGuide> _decisionGuidesById;
    private readonly Dictionary<string, SchoolExamSchedule[]> _examSchedulesById;
    private readonly Dictionary<string, SchoolAdmissionResult[]> _admissionsById;
    private readonly Dictionary<string, SchoolDeviationScore[]> _deviationScoresById;

    public JsonSchoolCatalog(IHostEnvironment environment)
    {
        var dataDirectory = Path.Combine(environment.ContentRootPath, "Data", "Schools");
        var schoolDocument = Read<SchoolDocument>(Path.Combine(dataDirectory, "schools.json"));
        var identityDocument = Read<IdentityDocument>(Path.Combine(dataDirectory, "school-id.json"));
        var profileDocument = Read<ProfileDocument>(Path.Combine(dataDirectory, "school-profiles.json"));
        var guideDocument = Read<GuideDocument>(Path.Combine(dataDirectory, "school-guides.json"));
        var difficultyDocument = Read<DifficultyDocument>(Path.Combine(dataDirectory, "school-difficulty.json"));
        var admissionDocuments = Directory
            .EnumerateFiles(Path.Combine(dataDirectory, "school-admissions"), "*.json")
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(Read<AdmissionDocument>)
            .ToArray();
        var examDocuments = Directory
            .EnumerateFiles(Path.Combine(dataDirectory, "school-exams"), "*.json")
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(Read<ExamDocument>)
            .ToArray();

        Validate(schoolDocument, identityDocument, profileDocument, guideDocument, admissionDocuments, examDocuments, difficultyDocument);

        var japaneseNameComparer = StringComparer.Create(CultureInfo.GetCultureInfo("ja-JP"), false);
        _schools = schoolDocument.Schools
            .OrderBy(school => school.PrefectureCode, StringComparer.Ordinal)
            .ThenBy(school => school.Name, japaneseNameComparer)
            .Select(school => new SchoolCatalogItem(
                school.Id, school.Name, school.NameKana, school.PrefectureCode, school.Prefecture,
                school.Address, school.PostalCode, school.Ownership, school.CampusType, school.OpenedOn,
                school.LastVerifiedAt, school.Latitude, school.Longitude, school.OfficialUrl, school.Tags))
            .ToArray();
        _aliasesById = identityDocument.Identities.ToDictionary(identity => identity.Id, identity => identity.Aliases, StringComparer.Ordinal);
        _profilesById = profileDocument.Profiles.ToDictionary(
            profile => profile.SchoolId,
            profile => new SchoolProfile(
                profile.SchoolId,
                profile.Gender,
                profile.Programs.Select(program => new SchoolProgram(program.AttendanceType, program.Department, program.Course)).ToArray(),
                profile.Sources.Select(ToSource).ToArray()),
            StringComparer.Ordinal);
        _decisionGuidesById = guideDocument.Guides.ToDictionary(
            guide => guide.SchoolId,
            guide => new SchoolDecisionGuide(
                guide.SchoolId,
                guide.Summary,
                guide.VerifiedAt,
                guide.Highlights.Select(highlight => new SchoolHighlight(highlight.Title, highlight.Description)).ToArray(),
                guide.GoodFit,
                guide.QuestionsToConsider,
                new SchoolAccessGuide(guide.Access.Summary, guide.Access.Routes),
                guide.VisitEvents.Select(visitEvent => new SchoolVisitEvent(
                    visitEvent.Title, visitEvent.AcademicYear, visitEvent.Status, visitEvent.StatusLabel,
                    visitEvent.EventDates, visitEvent.Time, visitEvent.ApplicationPeriod, visitEvent.ApplicationMethod,
                    visitEvent.Note, visitEvent.Sources.Select(ToSource).ToArray())).ToArray(),
                guide.Sources.Select(ToSource).ToArray()),
            StringComparer.Ordinal);
        var examEntries = examDocuments
            .SelectMany(document => document.Schedules)
            .Concat(examDocuments.SelectMany(document => document.SharedSchedules.SelectMany(
                schedule => schedule.SchoolIds.Select(schoolId => schedule.ForSchool(schoolId)))));
        _examSchedulesById = examEntries
            .Select(schedule => new SchoolExamSchedule(
                schedule.SchoolId, schedule.AcademicYear, schedule.Selection, schedule.SelectionLabel,
                schedule.Status, schedule.StatusLabel, schedule.ApplicationPeriod, schedule.TestDates,
                schedule.ResultDate, schedule.DetailsStatus, schedule.DetailsStatusLabel,
                schedule.OfficialPublishedAt, schedule.VerifiedAt, schedule.Notes,
                schedule.Sources.Select(ToSource).ToArray()))
            .GroupBy(schedule => schedule.SchoolId, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(schedule => schedule.AcademicYear).ThenBy(schedule => schedule.Selection, StringComparer.Ordinal).ToArray(),
                StringComparer.Ordinal);
        _admissionsById = admissionDocuments
            .SelectMany(document => document.Results)
            .Select(result => new SchoolAdmissionResult(
                result.SchoolId, result.AcademicYear, result.Selection, result.SelectionLabel,
                result.AttendanceType, result.Department, result.Capacity, result.Applicants,
                result.Examinees, result.Admitted, result.Enrolled, result.Note,
                result.Sources.Select(ToSource).ToArray()))
            .GroupBy(result => result.SchoolId, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(result => result.AcademicYear).ThenBy(result => result.Selection, StringComparer.Ordinal).ToArray(),
                StringComparer.Ordinal);
        _deviationScoresById = difficultyDocument.ExternalDeviationScores
            .Select(score => new SchoolDeviationScore(
                score.SchoolId, score.AcademicYear, score.Provider, score.Value, score.ValueLow,
                score.ValueHigh, score.Population, score.SourceUrl, score.License, score.VerifiedAt))
            .GroupBy(score => score.SchoolId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(score => score.AcademicYear).ToArray(), StringComparer.Ordinal);

        Metadata = new SchoolCatalogMetadata(
            schoolDocument.SchemaVersion,
            schoolDocument.AsOf,
            _schools.Length,
            _schools.Count(school => school.CampusType == "branch"),
            identityDocument.Identities.Length,
            identityDocument.Identities.Count(identity => identity.Status == "closed"),
            schoolDocument.SourceUrls);
        Prefectures = _schools
            .GroupBy(school => new { school.PrefectureCode, school.Prefecture })
            .OrderBy(group => group.Key.PrefectureCode, StringComparer.Ordinal)
            .Select(group => new SchoolFilterOption(group.Key.PrefectureCode, group.Key.Prefecture, group.Count()))
            .ToArray();
    }

    public SchoolCatalogMetadata Metadata { get; }
    public IReadOnlyList<SchoolFilterOption> Prefectures { get; }

    public SchoolSearchPage Search(string? query, string? prefecture, string? ownership, int page, int pageSize)
    {
        var normalizedQuery = Normalize(query);
        var normalizedPrefecture = prefecture?.Trim() ?? string.Empty;
        var normalizedOwnership = ownership?.Trim().ToLowerInvariant() ?? string.Empty;
        var safePageSize = Math.Clamp(pageSize, 1, 48);

        IEnumerable<SchoolCatalogItem> filtered = _schools;
        if (!string.IsNullOrEmpty(normalizedPrefecture))
            filtered = filtered.Where(school => school.PrefectureCode == normalizedPrefecture);
        if (normalizedOwnership is "national" or "public" or "private")
            filtered = filtered.Where(school => school.Ownership == normalizedOwnership);
        if (!string.IsNullOrEmpty(normalizedQuery))
            filtered = filtered.Where(school => Matches(school, normalizedQuery));

        var results = filtered.ToArray();
        var totalPages = results.Length == 0 ? 0 : (int)Math.Ceiling(results.Length / (double)safePageSize);
        var safePage = totalPages == 0 ? 1 : Math.Clamp(page, 1, totalPages);
        var items = results.Skip((safePage - 1) * safePageSize).Take(safePageSize).ToArray();

        return new SchoolSearchPage(items, results.Length, safePage, safePageSize, totalPages);
    }

    public SchoolDetailsViewModel? GetById(string id)
    {
        var school = _schools.FirstOrDefault(item => string.Equals(item.Id, id, StringComparison.Ordinal));
        if (school is null) return null;

        _profilesById.TryGetValue(id, out var profile);
        _decisionGuidesById.TryGetValue(id, out var decisionGuide);
        _examSchedulesById.TryGetValue(id, out var examSchedules);
        _admissionsById.TryGetValue(id, out var admissions);
        _deviationScoresById.TryGetValue(id, out var deviationScores);
        return new SchoolDetailsViewModel(school, profile, decisionGuide, examSchedules ?? [], admissions ?? [], deviationScores ?? []);
    }

    private bool Matches(SchoolCatalogItem school, string query)
    {
        if (Normalize(school.Name).Contains(query, StringComparison.Ordinal) ||
            Normalize(school.NameKana).Contains(query, StringComparison.Ordinal) ||
            Normalize(school.Address).Contains(query, StringComparison.Ordinal) ||
            Normalize(school.Prefecture).Contains(query, StringComparison.Ordinal) ||
            school.Tags.Any(tag => Normalize(tag).Contains(query, StringComparison.Ordinal)))
            return true;

        return _aliasesById.TryGetValue(school.Id, out var aliases) &&
               aliases.Any(alias => Normalize(alias).Contains(query, StringComparison.Ordinal));
    }

    private static string Normalize(string? value) => (value ?? string.Empty).Trim().Normalize(NormalizationForm.FormKC).ToLowerInvariant();

    private static T Read<T>(string path)
    {
        if (!File.Exists(path)) throw new InvalidOperationException($"School catalog file was not found: {path}");
        return JsonSerializer.Deserialize<T>(File.ReadAllText(path), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException($"School catalog file is empty or invalid: {path}");
    }

    private static SchoolDataSource ToSource(SourceEntry source) =>
        new(source.Title, source.Url, source.PublishedAt, source.VerifiedAt);

    private static void Validate(
        SchoolDocument schools,
        IdentityDocument identities,
        ProfileDocument profiles,
        GuideDocument guides,
        IReadOnlyList<AdmissionDocument> admissions,
        IReadOnlyList<ExamDocument> exams,
        DifficultyDocument difficulty)
    {
        if (schools.SchemaVersion != 1 || identities.SchemaVersion != 1 || profiles.SchemaVersion != 1 || guides.SchemaVersion != 1 ||
            admissions.Any(document => document.SchemaVersion != 1) || exams.Any(document => document.SchemaVersion != 1) || difficulty.SchemaVersion != 1)
            throw new InvalidOperationException("Unsupported school catalog schema version.");
        if (!string.Equals(schools.AsOf, identities.AsOf, StringComparison.Ordinal))
            throw new InvalidOperationException("School catalog files use different snapshot dates.");

        static void EnsureUnique(IEnumerable<string> values, string label)
        {
            var duplicate = values.GroupBy(value => value, StringComparer.Ordinal).FirstOrDefault(group => group.Count() > 1);
            if (duplicate is not null) throw new InvalidOperationException($"Duplicate {label}: {duplicate.Key}");
        }

        EnsureUnique(schools.Schools.Select(school => school.Id), "school ID");
        EnsureUnique(identities.Identities.Select(identity => identity.Id), "identity ID");
        EnsureUnique(identities.Identities.Select(identity => identity.MextSchoolCode), "MEXT school code");

        var currentIds = schools.Schools.Select(school => school.Id).ToHashSet(StringComparer.Ordinal);
        var activeIds = identities.Identities.Where(identity => identity.Status == "active").Select(identity => identity.Id).ToHashSet(StringComparer.Ordinal);
        if (!currentIds.SetEquals(activeIds)) throw new InvalidOperationException("Active school identities and current schools are inconsistent.");

        var referencedIds = profiles.Profiles.Select(profile => profile.SchoolId)
            .Concat(guides.Guides.Select(guide => guide.SchoolId))
            .Concat(admissions.SelectMany(document => document.Results).Select(result => result.SchoolId))
            .Concat(exams.SelectMany(document => document.Schedules).Select(schedule => schedule.SchoolId))
            .Concat(exams.SelectMany(document => document.SharedSchedules).SelectMany(schedule => schedule.SchoolIds))
            .Concat(difficulty.ExternalDeviationScores.Select(score => score.SchoolId));
        var unknownId = referencedIds.FirstOrDefault(id => !currentIds.Contains(id));
        if (unknownId is not null) throw new InvalidOperationException($"School detail data references an unknown school ID: {unknownId}");

        EnsureUnique(profiles.Profiles.Select(profile => profile.SchoolId), "school profile ID");
        EnsureUnique(guides.Guides.Select(guide => guide.SchoolId), "school guide ID");

        var admissionResults = admissions.SelectMany(document => document.Results).ToArray();
        if (admissions.Any(document => document.Results.Any(result => result.AcademicYear != document.AcademicYear)))
            throw new InvalidOperationException("Admission result year does not match its annual document.");
        EnsureUnique(
            admissionResults.Select(result => $"{result.SchoolId}|{result.AcademicYear}|{result.Selection}|{result.AttendanceType}|{result.Department}"),
            "admission result key");

        if (exams.Any(document => document.Schedules.Any(schedule => schedule.AcademicYear != document.AcademicYear) ||
                                  document.SharedSchedules.Any(schedule => schedule.AcademicYear != document.AcademicYear)))
            throw new InvalidOperationException("Exam schedule year does not match its annual document.");
        EnsureUnique(
            exams.SelectMany(document => document.Schedules)
                .Concat(exams.SelectMany(document => document.SharedSchedules.SelectMany(schedule => schedule.SchoolIds.Select(schedule.ForSchool))))
                .Select(schedule => $"{schedule.SchoolId}|{schedule.AcademicYear}|{schedule.Selection}"),
            "exam schedule key");
    }

    private sealed class SchoolDocument
    {
        public int SchemaVersion { get; init; }
        public string AsOf { get; init; } = string.Empty;
        public string[] SourceUrls { get; init; } = [];
        public SchoolEntry[] Schools { get; init; } = [];
    }

    private sealed class IdentityDocument
    {
        public int SchemaVersion { get; init; }
        public string AsOf { get; init; } = string.Empty;
        public IdentityEntry[] Identities { get; init; } = [];
    }

    private sealed class ProfileDocument
    {
        public int SchemaVersion { get; init; }
        public ProfileEntry[] Profiles { get; init; } = [];
    }

    private sealed class AdmissionDocument
    {
        public int SchemaVersion { get; init; }
        public int AcademicYear { get; init; }
        public AdmissionEntry[] Results { get; init; } = [];
    }

    private sealed class GuideDocument
    {
        public int SchemaVersion { get; init; }
        public GuideEntry[] Guides { get; init; } = [];
    }

    private sealed class ExamDocument
    {
        public int SchemaVersion { get; init; }
        public int AcademicYear { get; init; }
        public ExamEntry[] Schedules { get; init; } = [];
        public SharedExamEntry[] SharedSchedules { get; init; } = [];
    }

    private sealed class DifficultyDocument
    {
        public int SchemaVersion { get; init; }
        public DeviationScoreEntry[] ExternalDeviationScores { get; init; } = [];
    }

    private sealed class SchoolEntry
    {
        public string Id { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string? NameKana { get; init; }
        public string PrefectureCode { get; init; } = string.Empty;
        public string Prefecture { get; init; } = string.Empty;
        public string Address { get; init; } = string.Empty;
        public string PostalCode { get; init; } = string.Empty;
        public string Ownership { get; init; } = string.Empty;
        public string CampusType { get; init; } = string.Empty;
        public string OpenedOn { get; init; } = string.Empty;
        public string LastVerifiedAt { get; init; } = string.Empty;
        public decimal? Latitude { get; init; }
        public decimal? Longitude { get; init; }
        public string? OfficialUrl { get; init; }
        public string[] Tags { get; init; } = [];
    }

    private sealed class IdentityEntry
    {
        public string Id { get; init; } = string.Empty;
        public string MextSchoolCode { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public string[] Aliases { get; init; } = [];
    }

    private sealed class ProfileEntry
    {
        public string SchoolId { get; init; } = string.Empty;
        public string Gender { get; init; } = "unknown";
        public ProgramEntry[] Programs { get; init; } = [];
        public SourceEntry[] Sources { get; init; } = [];
    }

    private sealed class ProgramEntry
    {
        public string AttendanceType { get; init; } = string.Empty;
        public string Department { get; init; } = string.Empty;
        public string? Course { get; init; }
    }

    private sealed class AdmissionEntry
    {
        public string SchoolId { get; init; } = string.Empty;
        public int AcademicYear { get; init; }
        public string Selection { get; init; } = string.Empty;
        public string SelectionLabel { get; init; } = string.Empty;
        public string AttendanceType { get; init; } = string.Empty;
        public string Department { get; init; } = string.Empty;
        public int? Capacity { get; init; }
        public int? Applicants { get; init; }
        public int? Examinees { get; init; }
        public int? Admitted { get; init; }
        public int? Enrolled { get; init; }
        public string? Note { get; init; }
        public SourceEntry[] Sources { get; init; } = [];
    }

    private sealed class GuideEntry
    {
        public string SchoolId { get; init; } = string.Empty;
        public string Summary { get; init; } = string.Empty;
        public string VerifiedAt { get; init; } = string.Empty;
        public HighlightEntry[] Highlights { get; init; } = [];
        public string[] GoodFit { get; init; } = [];
        public string[] QuestionsToConsider { get; init; } = [];
        public AccessEntry Access { get; init; } = new();
        public VisitEventEntry[] VisitEvents { get; init; } = [];
        public SourceEntry[] Sources { get; init; } = [];
    }

    private sealed class HighlightEntry
    {
        public string Title { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
    }

    private sealed class AccessEntry
    {
        public string Summary { get; init; } = string.Empty;
        public string[] Routes { get; init; } = [];
    }

    private sealed class VisitEventEntry
    {
        public string Title { get; init; } = string.Empty;
        public int AcademicYear { get; init; }
        public string Status { get; init; } = string.Empty;
        public string StatusLabel { get; init; } = string.Empty;
        public string[] EventDates { get; init; } = [];
        public string Time { get; init; } = string.Empty;
        public string ApplicationPeriod { get; init; } = string.Empty;
        public string ApplicationMethod { get; init; } = string.Empty;
        public string? Note { get; init; }
        public SourceEntry[] Sources { get; init; } = [];
    }

    private sealed class ExamEntry
    {
        public string SchoolId { get; init; } = string.Empty;
        public int AcademicYear { get; init; }
        public string Selection { get; init; } = string.Empty;
        public string SelectionLabel { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public string StatusLabel { get; init; } = string.Empty;
        public string ApplicationPeriod { get; init; } = string.Empty;
        public string[] TestDates { get; init; } = [];
        public string ResultDate { get; init; } = string.Empty;
        public string DetailsStatus { get; init; } = string.Empty;
        public string DetailsStatusLabel { get; init; } = string.Empty;
        public string? OfficialPublishedAt { get; init; }
        public string VerifiedAt { get; init; } = string.Empty;
        public string[] Notes { get; init; } = [];
        public SourceEntry[] Sources { get; init; } = [];
    }

    private sealed class SharedExamEntry
    {
        public string[] SchoolIds { get; init; } = [];
        public int AcademicYear { get; init; }
        public string Selection { get; init; } = string.Empty;
        public string SelectionLabel { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public string StatusLabel { get; init; } = string.Empty;
        public string ApplicationPeriod { get; init; } = string.Empty;
        public string[] TestDates { get; init; } = [];
        public string ResultDate { get; init; } = string.Empty;
        public string DetailsStatus { get; init; } = string.Empty;
        public string DetailsStatusLabel { get; init; } = string.Empty;
        public string? OfficialPublishedAt { get; init; }
        public string VerifiedAt { get; init; } = string.Empty;
        public string[] Notes { get; init; } = [];
        public SourceEntry[] Sources { get; init; } = [];

        public ExamEntry ForSchool(string schoolId) => new()
        {
            SchoolId = schoolId,
            AcademicYear = AcademicYear,
            Selection = Selection,
            SelectionLabel = SelectionLabel,
            Status = Status,
            StatusLabel = StatusLabel,
            ApplicationPeriod = ApplicationPeriod,
            TestDates = TestDates,
            ResultDate = ResultDate,
            DetailsStatus = DetailsStatus,
            DetailsStatusLabel = DetailsStatusLabel,
            OfficialPublishedAt = OfficialPublishedAt,
            VerifiedAt = VerifiedAt,
            Notes = Notes,
            Sources = Sources
        };
    }

    private sealed class DeviationScoreEntry
    {
        public string SchoolId { get; init; } = string.Empty;
        public int AcademicYear { get; init; }
        public string Provider { get; init; } = string.Empty;
        public decimal? Value { get; init; }
        public decimal? ValueLow { get; init; }
        public decimal? ValueHigh { get; init; }
        public string Population { get; init; } = string.Empty;
        public string SourceUrl { get; init; } = string.Empty;
        public string License { get; init; } = string.Empty;
        public string VerifiedAt { get; init; } = string.Empty;
    }

    private sealed class SourceEntry
    {
        public string Title { get; init; } = string.Empty;
        public string Url { get; init; } = string.Empty;
        public string? PublishedAt { get; init; }
        public string VerifiedAt { get; init; } = string.Empty;
    }
}
