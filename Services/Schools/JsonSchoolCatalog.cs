using System.Text;
using System.Text.Json;
using System.Globalization;
using wakaroute_web.Models;

namespace wakaroute_web.Services.Schools;

public sealed class JsonSchoolCatalog : ISchoolCatalog
{
    private static readonly DepartmentCategoryDefinition[] DepartmentCategoryDefinitions =
    [
        new("general", "普通・文理", ["普通", "文理"]),
        new("integrated", "総合学科", ["総合学科"]),
        new("commerce", "商業・ビジネス", ["商業", "ビジネス", "会計", "流通", "経営"]),
        new("industry", "工業・技術", ["工業", "機械", "電気", "電子", "建築", "土木", "化学技術", "ロボット"]),
        new("agriculture", "農業・食品", ["農業", "園芸", "畜産", "食品", "生物生産", "森林"]),
        new("marine", "水産・海洋", ["水産", "海洋", "航海", "機関"]),
        new("home", "家庭・生活", ["家庭", "家政", "生活", "服飾", "調理"]),
        new("care", "看護・福祉", ["看護", "福祉"]),
        new("information", "情報", ["情報", "デジタル", "コンピュータ"]),
        new("science", "理数・科学", ["理数", "科学"]),
        new("international", "国際・外国語", ["国際", "外国語", "英語"]),
        new("arts", "芸術・デザイン", ["芸術", "音楽", "美術", "デザイン", "工芸"]),
        new("sports", "体育・スポーツ", ["体育", "スポーツ"])
    ];

    private readonly SchoolCatalogItem[] _schools;
    private readonly Dictionary<string, SchoolCatalogItem> _schoolsById;
    private readonly Dictionary<string, string[]> _aliasesById;
    private readonly Dictionary<string, SchoolProfile> _profilesById;
    private readonly Dictionary<string, SchoolDecisionGuide> _decisionGuidesById;
    private readonly Dictionary<string, SchoolExamSchedule[]> _examSchedulesById;
    private readonly Dictionary<string, SchoolAdmissionResult[]> _admissionsById;
    private readonly Dictionary<string, SchoolDeviationScore[]> _deviationScoresById;
    private readonly Dictionary<string, SchoolAccessInfo> _accessById;
    private readonly Dictionary<string, SchoolLifeInfo> _schoolLifeById;

    public JsonSchoolCatalog(IHostEnvironment environment)
    {
        var dataDirectory = Path.Combine(environment.ContentRootPath, "Data", "Schools");
        var schoolDocument = Read<SchoolDocument>(Path.Combine(dataDirectory, "schools.json"));
        var identityDocument = Read<IdentityDocument>(Path.Combine(dataDirectory, "school-id.json"));
        var profileDocument = Read<ProfileDocument>(Path.Combine(dataDirectory, "school-profiles.json"));
        var guideDocument = Read<GuideDocument>(Path.Combine(dataDirectory, "school-guides.json"));
        var difficultyDocument = Read<DifficultyDocument>(Path.Combine(dataDirectory, "school-difficulty.json"));
        var accessDocument = Read<AccessDocument>(Path.Combine(dataDirectory, "school-access.json"));
        var schoolLifeDocument = Read<SchoolLifeDocument>(Path.Combine(dataDirectory, "school-life.json"));
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

        Validate(schoolDocument, identityDocument, profileDocument, guideDocument, admissionDocuments, examDocuments, difficultyDocument, accessDocument, schoolLifeDocument);

        _schoolLifeById = schoolLifeDocument.Schools.ToDictionary(
            item => item.SchoolId,
            item => new SchoolLifeInfo(
                item.SchoolId,
                item.VerifiedAt,
                new SchoolLifeStatus(item.Uniform.Status, item.Uniform.Summary),
                item.RulesSummary,
                new SchoolLifeStatus(item.Lunch.Status, item.Lunch.Summary),
                item.Events.Select(schoolEvent => new SchoolLifeEvent(schoolEvent.Name, schoolEvent.Season)).ToArray(),
                item.Clubs.Select(club => new SchoolClub(club.Name, club.Category, club.Gender, club.ActivityDays, club.ActivityPlace)).ToArray(),
                item.Sources.Select(ToSource).ToArray()),
            StringComparer.Ordinal);

        _accessById = accessDocument.Schools.ToDictionary(
            access => access.SchoolId,
            access => new SchoolAccessInfo(
                accessDocument.CoordinateSource.DataYear,
                accessDocument.StationSource.DataYear,
                accessDocument.CoordinateSource.Url,
                accessDocument.StationSource.Url,
                accessDocument.Method,
                access.NearestStations.Select(station => new SchoolStation(
                    station.GroupCode, station.Name, station.Latitude, station.Longitude,
                    station.DistanceMeters, station.Lines, station.Operators)).ToArray()),
            StringComparer.Ordinal);

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

        var japaneseNameComparer = StringComparer.Create(CultureInfo.GetCultureInfo("ja-JP"), false);
        _schools = schoolDocument.Schools
            .OrderBy(school => school.PrefectureCode, StringComparer.Ordinal)
            .ThenBy(school => school.Name, japaneseNameComparer)
            .Select(school =>
            {
                _profilesById.TryGetValue(school.Id, out var profile);
                _admissionsById.TryGetValue(school.Id, out var admissions);
                _examSchedulesById.TryGetValue(school.Id, out var examSchedules);
                _decisionGuidesById.TryGetValue(school.Id, out var guide);
                _accessById.TryGetValue(school.Id, out var accessInfo);
                _schoolLifeById.TryGetValue(school.Id, out var schoolLife);
                var latestYear = admissions?.MaxBy(result => result.AcademicYear)?.AcademicYear;
                var latestRatio = latestYear.HasValue
                    ? admissions!
                        .Where(result => result.AcademicYear == latestYear.Value)
                        .Select(result => result.ApplicationRatio)
                        .Where(ratio => ratio.HasValue)
                        .Select(ratio => ratio!.Value)
                        .DefaultIfEmpty()
                        .Max()
                    : 0m;

                return new SchoolCatalogItem(
                    school.Id, school.Name, school.NameKana, school.PrefectureCode, school.Prefecture,
                    school.Address, school.PostalCode, school.Ownership, school.CampusType, school.OpenedOn,
                    school.LastVerifiedAt, school.Latitude, school.Longitude, school.OfficialUrl, school.Tags)
                {
                    Gender = profile?.Gender ?? "unknown",
                    Programs = profile?.Programs ?? [],
                    HasAdmissionResults = admissions?.Length > 0,
                    HasExamSchedules = examSchedules?.Length > 0,
                    HasVisitEvents = guide?.VisitEvents.Count > 0,
                    HasSchoolLife = schoolLife is not null,
                    LatestApplicationRatio = latestRatio > 0 ? latestRatio : null,
                    AccessInfo = accessInfo,
                    SchoolLife = schoolLife
                };
            })
            .ToArray();
        _schoolsById = _schools.ToDictionary(school => school.Id, StringComparer.Ordinal);

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
        Genders = new[]
        {
            new SchoolFilterOption("coeducational", "共学", _schools.Count(school => school.Gender == "coeducational")),
            new SchoolFilterOption("boys", "男子校", _schools.Count(school => school.Gender == "boys")),
            new SchoolFilterOption("girls", "女子校", _schools.Count(school => school.Gender == "girls")),
            new SchoolFilterOption("unknown", "未確認", _schools.Count(school => school.Gender == "unknown"))
        };
        AttendanceTypes = new[]
        {
            new SchoolFilterOption("full-time", "全日制", _schools.Count(school => school.Programs.Any(program => program.AttendanceType == "full-time"))),
            new SchoolFilterOption("part-time", "定時制", _schools.Count(school => school.Programs.Any(program => program.AttendanceType == "part-time"))),
            new SchoolFilterOption("correspondence", "通信制", _schools.Count(school => school.Programs.Any(program => program.AttendanceType == "correspondence")))
        };
        DepartmentCategories = DepartmentCategoryDefinitions
            .Select(category => new SchoolFilterOption(category.Value, category.Label, _schools.Count(school => MatchesDepartment(school, category))))
            .Where(option => option.Count > 0)
            .ToArray();
    }

    public SchoolCatalogMetadata Metadata { get; }
    public IReadOnlyList<SchoolCatalogItem> Schools => _schools;
    public IReadOnlyList<SchoolFilterOption> Prefectures { get; }
    public IReadOnlyList<SchoolFilterOption> Genders { get; }
    public IReadOnlyList<SchoolFilterOption> AttendanceTypes { get; }
    public IReadOnlyList<SchoolFilterOption> DepartmentCategories { get; }

    public SchoolSearchPage Search(SchoolSearchCriteria criteria, int page, int pageSize)
    {
        var normalizedQuery = Normalize(criteria.Query);
        var normalizedPrefecture = criteria.Prefecture.Trim();
        var normalizedOwnership = criteria.Ownership.Trim().ToLowerInvariant();
        var safePageSize = Math.Clamp(pageSize, 1, 48);

        IEnumerable<SchoolCatalogItem> filtered = _schools;
        if (!string.IsNullOrEmpty(normalizedPrefecture))
            filtered = filtered.Where(school => school.PrefectureCode == normalizedPrefecture);
        if (normalizedOwnership is "national" or "public" or "private")
            filtered = filtered.Where(school => school.Ownership == normalizedOwnership);
        if (!string.IsNullOrEmpty(normalizedQuery))
            filtered = filtered.Where(school => Matches(school, normalizedQuery));
        if (criteria.Gender is "coeducational" or "boys" or "girls" or "unknown")
            filtered = filtered.Where(school => school.Gender == criteria.Gender);
        if (criteria.AttendanceType is "full-time" or "part-time" or "correspondence")
            filtered = filtered.Where(school => school.Programs.Any(program => program.AttendanceType == criteria.AttendanceType));
        var departmentCategory = DepartmentCategoryDefinitions.FirstOrDefault(category => category.Value == criteria.DepartmentCategory);
        if (departmentCategory is not null)
            filtered = filtered.Where(school => MatchesDepartment(school, departmentCategory));
        if (criteria.Recruitment == "excluding-stopped")
            filtered = filtered.Where(school => !school.Tags.Contains("募集停止中"));
        else if (criteria.Recruitment == "stopped")
            filtered = filtered.Where(school => school.Tags.Contains("募集停止中"));
        if (criteria.HasAdmissions)
            filtered = filtered.Where(school => school.HasAdmissionResults);
        if (criteria.HasExamSchedule)
            filtered = filtered.Where(school => school.HasExamSchedules);
        if (criteria.HasVisitEvents)
            filtered = filtered.Where(school => school.HasVisitEvents);
        if (criteria.HasSchoolLife)
            filtered = filtered.Where(school => school.HasSchoolLife);

        filtered = criteria.Sort switch
        {
            "name" => filtered.OrderBy(school => school.Name, StringComparer.Create(CultureInfo.GetCultureInfo("ja-JP"), false)),
            "updated" => filtered.OrderByDescending(school => school.LastVerifiedAt, StringComparer.Ordinal).ThenBy(school => school.Name, StringComparer.Ordinal),
            "ratio" => filtered.OrderByDescending(school => school.LatestApplicationRatio ?? -1m).ThenBy(school => school.Name, StringComparer.Ordinal),
            _ when !string.IsNullOrEmpty(normalizedQuery) => filtered.OrderBy(school => MatchRank(school, normalizedQuery)).ThenBy(school => school.PrefectureCode, StringComparer.Ordinal).ThenBy(school => school.Name, StringComparer.Ordinal),
            _ => filtered
        };

        var results = filtered.ToArray();
        var totalPages = results.Length == 0 ? 0 : (int)Math.Ceiling(results.Length / (double)safePageSize);
        var safePage = totalPages == 0 ? 1 : Math.Clamp(page, 1, totalPages);
        var items = results.Skip((safePage - 1) * safePageSize).Take(safePageSize).ToArray();

        return new SchoolSearchPage(items, results.Length, safePage, safePageSize, totalPages);
    }

    public SchoolDetailsViewModel? GetById(string id)
    {
        if (!_schoolsById.TryGetValue(id, out var school)) return null;

        _profilesById.TryGetValue(id, out var profile);
        _decisionGuidesById.TryGetValue(id, out var decisionGuide);
        _schoolLifeById.TryGetValue(id, out var schoolLife);
        _examSchedulesById.TryGetValue(id, out var examSchedules);
        _admissionsById.TryGetValue(id, out var admissions);
        _deviationScoresById.TryGetValue(id, out var deviationScores);
        return new SchoolDetailsViewModel(school, profile, decisionGuide, schoolLife, examSchedules ?? [], admissions ?? [], deviationScores ?? []);
    }

    public IReadOnlyList<SchoolDetailsViewModel> GetByIds(IEnumerable<string> ids, int maximumCount)
    {
        var safeMaximum = Math.Clamp(maximumCount, 1, 5);
        return ids
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.Ordinal)
            .Take(safeMaximum)
            .Select(GetById)
            .Where(school => school is not null)
            .Cast<SchoolDetailsViewModel>()
            .ToArray();
    }

    private bool Matches(SchoolCatalogItem school, string query)
    {
        if (Normalize(school.Name).Contains(query, StringComparison.Ordinal) ||
            Normalize(school.NameKana).Contains(query, StringComparison.Ordinal) ||
            Normalize(school.Address).Contains(query, StringComparison.Ordinal) ||
            Normalize(school.Prefecture).Contains(query, StringComparison.Ordinal) ||
            school.Tags.Any(tag => IsSearchableTag(tag) && Normalize(tag).Contains(query, StringComparison.Ordinal)))
            return true;

        if (school.Programs.Any(program =>
                Normalize(program.Department).Contains(query, StringComparison.Ordinal) ||
                Normalize(program.Course).Contains(query, StringComparison.Ordinal) ||
                Normalize(program.AttendanceTypeLabel).Contains(query, StringComparison.Ordinal)))
            return true;

        if (school.AccessInfo?.NearestStations.Any(station =>
                Normalize(station.Name).Contains(query, StringComparison.Ordinal) ||
                station.Lines.Any(line => Normalize(line).Contains(query, StringComparison.Ordinal)) ||
                station.Operators.Any(operatorName => Normalize(operatorName).Contains(query, StringComparison.Ordinal))) == true)
            return true;

        if (school.SchoolLife is { } schoolLife &&
            (Normalize(schoolLife.Uniform.Summary).Contains(query, StringComparison.Ordinal) ||
             Normalize(schoolLife.RulesSummary).Contains(query, StringComparison.Ordinal) ||
             Normalize(schoolLife.Lunch.Summary).Contains(query, StringComparison.Ordinal) ||
             schoolLife.Events.Any(schoolEvent => Normalize(schoolEvent.Name).Contains(query, StringComparison.Ordinal)) ||
             schoolLife.Clubs.Any(club => Normalize(club.Name).Contains(query, StringComparison.Ordinal))))
            return true;

        return _aliasesById.TryGetValue(school.Id, out var aliases) &&
               aliases.Any(alias => Normalize(alias).Contains(query, StringComparison.Ordinal));
    }

    private static bool MatchesDepartment(SchoolCatalogItem school, DepartmentCategoryDefinition category) =>
        school.Programs.Any(program => category.Keywords.Any(keyword =>
            program.Department.Contains(keyword, StringComparison.Ordinal) ||
            (program.Course?.Contains(keyword, StringComparison.Ordinal) ?? false)));

    private static int MatchRank(SchoolCatalogItem school, string query)
    {
        var name = Normalize(school.Name);
        var kana = Normalize(school.NameKana);
        if (name == query || kana == query) return 0;
        if (name.StartsWith(query, StringComparison.Ordinal) || kana.StartsWith(query, StringComparison.Ordinal)) return 1;
        if (name.Contains(query, StringComparison.Ordinal) || kana.Contains(query, StringComparison.Ordinal)) return 2;
        return 3;
    }

    private static bool IsSearchableTag(string tag) =>
        !tag.Contains("情報確認済み", StringComparison.Ordinal) &&
        !tag.Contains("未確認", StringComparison.Ordinal);

    private static string Normalize(string? value) => (value ?? string.Empty).Trim().Normalize(NormalizationForm.FormKC).ToLowerInvariant();

    private static T Read<T>(string path)
    {
        if (!File.Exists(path)) throw new InvalidOperationException($"School catalog file was not found: {path}");
        return JsonSerializer.Deserialize<T>(File.ReadAllText(path), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException($"School catalog file is empty or invalid: {path}");
    }

    private static SchoolDataSource ToSource(SourceEntry source) =>
        new(source.Title, source.Url, source.PublishedAt, source.VerifiedAt);

    private sealed record DepartmentCategoryDefinition(string Value, string Label, IReadOnlyList<string> Keywords);

    private static void Validate(
        SchoolDocument schools,
        IdentityDocument identities,
        ProfileDocument profiles,
        GuideDocument guides,
        IReadOnlyList<AdmissionDocument> admissions,
        IReadOnlyList<ExamDocument> exams,
        DifficultyDocument difficulty,
        AccessDocument access,
        SchoolLifeDocument schoolLife)
    {
        if (schools.SchemaVersion != 1 || identities.SchemaVersion != 1 || profiles.SchemaVersion != 1 || guides.SchemaVersion != 1 ||
            admissions.Any(document => document.SchemaVersion != 1) || exams.Any(document => document.SchemaVersion != 1) || difficulty.SchemaVersion != 1 || access.SchemaVersion != 1 || schoolLife.SchemaVersion != 1)
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
        EnsureUnique(
            identities.Identities
                .Select(identity => identity.MextSchoolCode)
                .Where(code => !string.IsNullOrWhiteSpace(code)),
            "MEXT school code");

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
        EnsureUnique(access.Schools.Select(item => item.SchoolId), "school access ID");
        var unknownAccessId = access.Schools.Select(item => item.SchoolId).FirstOrDefault(id => !currentIds.Contains(id));
        if (unknownAccessId is not null) throw new InvalidOperationException($"School access data references an unknown school ID: {unknownAccessId}");
        if (access.Schools.Any(item => item.NearestStations.Length > 3 || item.NearestStations.Any(station => station.DistanceMeters < 0 || station.DistanceKind != "straight-line")))
            throw new InvalidOperationException("School access data contains an invalid nearest-station entry.");

        EnsureUnique(schoolLife.Schools.Select(item => item.SchoolId), "school life ID");
        var unknownSchoolLifeId = schoolLife.Schools.Select(item => item.SchoolId).FirstOrDefault(id => !currentIds.Contains(id));
        if (unknownSchoolLifeId is not null) throw new InvalidOperationException($"School life data references an unknown school ID: {unknownSchoolLifeId}");
        if (schoolLife.Schools.Any(item => item.Clubs.Any(club => club.Category is not ("sports" or "culture") || club.Gender is not ("boys" or "girls" or "mixed" or "unknown"))))
            throw new InvalidOperationException("School life data contains an invalid club category or gender.");

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

    private sealed class AccessDocument
    {
        public int SchemaVersion { get; init; }
        public AccessSourceEntry CoordinateSource { get; init; } = new();
        public AccessSourceEntry StationSource { get; init; } = new();
        public string Method { get; init; } = string.Empty;
        public SchoolAccessEntry[] Schools { get; init; } = [];
    }

    private sealed class SchoolLifeDocument
    {
        public int SchemaVersion { get; init; }
        public string AsOf { get; init; } = string.Empty;
        public SchoolLifeEntry[] Schools { get; init; } = [];
    }

    private sealed class SchoolLifeEntry
    {
        public string SchoolId { get; init; } = string.Empty;
        public string VerifiedAt { get; init; } = string.Empty;
        public StatusSummaryEntry Uniform { get; init; } = new();
        public string RulesSummary { get; init; } = string.Empty;
        public StatusSummaryEntry Lunch { get; init; } = new();
        public SchoolLifeEventEntry[] Events { get; init; } = [];
        public SchoolClubEntry[] Clubs { get; init; } = [];
        public SourceEntry[] Sources { get; init; } = [];
    }

    private sealed class StatusSummaryEntry
    {
        public string Status { get; init; } = "unknown";
        public string Summary { get; init; } = string.Empty;
    }

    private sealed class SchoolLifeEventEntry
    {
        public string Name { get; init; } = string.Empty;
        public string Season { get; init; } = string.Empty;
    }

    private sealed class SchoolClubEntry
    {
        public string Name { get; init; } = string.Empty;
        public string Category { get; init; } = string.Empty;
        public string Gender { get; init; } = "unknown";
        public string ActivityDays { get; init; } = string.Empty;
        public string? ActivityPlace { get; init; }
    }

    private sealed class AccessSourceEntry
    {
        public string Url { get; init; } = string.Empty;
        public int DataYear { get; init; }
    }

    private sealed class SchoolAccessEntry
    {
        public string SchoolId { get; init; } = string.Empty;
        public StationEntry[] NearestStations { get; init; } = [];
    }

    private sealed class StationEntry
    {
        public string GroupCode { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public decimal Latitude { get; init; }
        public decimal Longitude { get; init; }
        public int DistanceMeters { get; init; }
        public string DistanceKind { get; init; } = string.Empty;
        public string[] Lines { get; init; } = [];
        public string[] Operators { get; init; } = [];
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
