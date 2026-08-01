using System.Text;
using System.Text.Json;
using System.Globalization;
using wakaroute_web.Models;

namespace wakaroute_web.Services.Schools;

public sealed class JsonSchoolCatalog : ISchoolCatalog
{
    private readonly SchoolCatalogItem[] _schools;
    private readonly Dictionary<string, string[]> _aliasesById;

    public JsonSchoolCatalog(IHostEnvironment environment)
    {
        var dataDirectory = Path.Combine(environment.ContentRootPath, "Data", "Schools");
        var schoolDocument = Read<SchoolDocument>(Path.Combine(dataDirectory, "schools.json"));
        var identityDocument = Read<IdentityDocument>(Path.Combine(dataDirectory, "school-id.json"));

        Validate(schoolDocument, identityDocument);

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

    private static void Validate(SchoolDocument schools, IdentityDocument identities)
    {
        if (schools.SchemaVersion != 1 || identities.SchemaVersion != 1)
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
}
