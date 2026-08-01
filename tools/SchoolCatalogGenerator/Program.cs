using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.VisualBasic.FileIO;

const string DefaultEastUrl = "https://www.mext.go.jp/content/20251226-mxt_chousa01-000011635_2.csv";
const string DefaultWestUrl = "https://www.mext.go.jp/content/20251226-mxt_chousa01-000011635_4.csv";
const string DefaultAsOf = "2025-05-01";

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var arguments = ParseArguments(args);
var outputDirectory = Path.GetFullPath(arguments.GetValueOrDefault("output") ?? Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Data", "Schools"));
var eastUrl = arguments.GetValueOrDefault("east-url") ?? DefaultEastUrl;
var westUrl = arguments.GetValueOrDefault("west-url") ?? DefaultWestUrl;
var asOf = arguments.GetValueOrDefault("as-of") ?? DefaultAsOf;

Directory.CreateDirectory(outputDirectory);

using var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("WakaRoute-SchoolCatalogGenerator/1.0 (+https://wakaroute.com)");

Console.WriteLine("Downloading the official MEXT school-code CSV files...");
var eastBytesTask = httpClient.GetByteArrayAsync(eastUrl);
var westBytesTask = httpClient.GetByteArrayAsync(westUrl);
await Task.WhenAll(eastBytesTask, westBytesTask);

var sourceRows = ParseMextCsv(await eastBytesTask)
    .Concat(ParseMextCsv(await westBytesTask))
    .Where(row => row.SchoolType.StartsWith("D1", StringComparison.Ordinal))
    .OrderBy(row => row.MextSchoolCode, StringComparer.Ordinal)
    .ToArray();

var identityPath = Path.Combine(outputDirectory, "school-id.json");
var schoolsPath = Path.Combine(outputDirectory, "schools.json");
var existingIdentities = ReadExisting<SchoolIdentityDocument>(identityPath)?.Identities
    .ToDictionary(identity => identity.MextSchoolCode, StringComparer.Ordinal) ?? [];
var existingSchools = ReadExisting<SchoolCatalogDocument>(schoolsPath)?.Schools
    .ToDictionary(school => school.Id, StringComparer.Ordinal) ?? [];
var sourceCodeSet = sourceRows.Select(row => row.MextSchoolCode).ToHashSet(StringComparer.Ordinal);

var identities = sourceRows.Select(row =>
{
    existingIdentities.TryGetValue(row.MextSchoolCode, out var existing);
    var id = existing?.Id ?? $"wk_{row.MextSchoolCode.ToLowerInvariant()}";
    var replacementId = !string.IsNullOrWhiteSpace(row.ReplacementMextSchoolCode) && sourceCodeSet.Contains(row.ReplacementMextSchoolCode)
        ? $"wk_{row.ReplacementMextSchoolCode.ToLowerInvariant()}"
        : existing?.ReplacedById;

    return new SchoolIdentity(
        id,
        row.MextSchoolCode,
        string.IsNullOrWhiteSpace(row.LegacySchoolSurveyCode) ? null : row.LegacySchoolSurveyCode,
        string.IsNullOrWhiteSpace(row.ClosedOn) ? "active" : "closed",
        existing?.Aliases ?? [],
        replacementId,
        string.IsNullOrWhiteSpace(row.ReplacementMextSchoolCode) ? null : row.ReplacementMextSchoolCode);
}).ToArray();

var identityByCode = identities.ToDictionary(identity => identity.MextSchoolCode, StringComparer.Ordinal);
var schools = sourceRows
    .Where(row => string.IsNullOrWhiteSpace(row.ClosedOn))
    .Select(row =>
    {
        var identity = identityByCode[row.MextSchoolCode];
        existingSchools.TryGetValue(identity.Id, out var existing);
        var (prefectureCode, prefectureLabel) = ParseCodeAndLabel(row.Prefecture);
        var prefecture = NormalizePrefectureName(prefectureCode, prefectureLabel);

        return new School(
            identity.Id,
            row.SchoolName,
            existing?.NameKana,
            prefectureCode,
            prefecture,
            row.Address,
            NormalizePostalCode(row.PostalCode),
            ParseOwnership(row.Ownership),
            row.MainOrBranch.StartsWith("2", StringComparison.Ordinal) ? "branch" : "main",
            row.OpenedOn,
            asOf,
            existing?.Latitude,
            existing?.Longitude,
            existing?.OfficialUrl,
            existing?.Tags ?? []);
    })
    .OrderBy(school => school.PrefectureCode, StringComparer.Ordinal)
    .ThenBy(school => school.Name, StringComparer.Ordinal)
    .ToArray();

Validate(identities, schools);

var identityDocument = new SchoolIdentityDocument(1, asOf, [eastUrl, westUrl], identities);
var schoolDocument = new SchoolCatalogDocument(1, asOf, [eastUrl, westUrl], schools);
var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true,
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};

await File.WriteAllTextAsync(identityPath, JsonSerializer.Serialize(identityDocument, jsonOptions) + Environment.NewLine, new UTF8Encoding(false));
await File.WriteAllTextAsync(schoolsPath, JsonSerializer.Serialize(schoolDocument, jsonOptions) + Environment.NewLine, new UTF8Encoding(false));

Console.WriteLine($"Generated {Path.GetRelativePath(Environment.CurrentDirectory, identityPath)} ({identities.Length:N0} identities; {identities.Count(x => x.Status == "closed"):N0} closed)." );
Console.WriteLine($"Generated {Path.GetRelativePath(Environment.CurrentDirectory, schoolsPath)} ({schools.Length:N0} current school codes; {schools.Count(x => x.CampusType == "branch"):N0} branches)." );

static Dictionary<string, string> ParseArguments(string[] values)
{
    var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    for (var index = 0; index < values.Length; index++)
    {
        if (!values[index].StartsWith("--", StringComparison.Ordinal)) continue;
        var key = values[index][2..];
        if (index + 1 >= values.Length || values[index + 1].StartsWith("--", StringComparison.Ordinal))
            throw new ArgumentException($"Missing value for --{key}.");
        result[key] = values[++index];
    }
    return result;
}

static IEnumerable<MextSchoolRow> ParseMextCsv(byte[] bytes)
{
    var text = Encoding.GetEncoding(932).GetString(bytes);
    using var reader = new StringReader(text);
    using var parser = new TextFieldParser(reader)
    {
        TextFieldType = FieldType.Delimited,
        HasFieldsEnclosedInQuotes = true,
        TrimWhiteSpace = false
    };
    parser.SetDelimiters(",");

    var headers = parser.ReadFields() ?? throw new InvalidDataException("The MEXT CSV does not contain a header row.");
    var headerIndexes = headers.Select((header, index) => (header, index))
        .ToDictionary(item => item.header, item => item.index, StringComparer.Ordinal);
    var requiredHeaders = new[] { "学校コード", "学校種", "都道府県番号", "設置区分", "本分校", "学校名", "学校所在地", "郵便番号", "属性情報設定年月日", "属性情報廃止年月日", "旧学校調査番号", "移行後の学校コード" };
    foreach (var header in requiredHeaders)
        if (!headerIndexes.ContainsKey(header)) throw new InvalidDataException($"Required MEXT column '{header}' was not found.");

    while (!parser.EndOfData)
    {
        var fields = parser.ReadFields();
        if (fields is null || fields.Length == 0) continue;
        string Value(string header) => headerIndexes[header] < fields.Length ? fields[headerIndexes[header]].Trim() : string.Empty;

        yield return new MextSchoolRow(
            Value("学校コード"), Value("学校種"), Value("都道府県番号"), Value("設置区分"), Value("本分校"),
            Value("学校名"), Value("学校所在地"), Value("郵便番号"), Value("属性情報設定年月日"),
            Value("属性情報廃止年月日"), Value("旧学校調査番号"), Value("移行後の学校コード"));
    }
}

static T? ReadExisting<T>(string path)
{
    if (!File.Exists(path)) return default;
    return JsonSerializer.Deserialize<T>(File.ReadAllText(path), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
}

static (string Code, string Label) ParseCodeAndLabel(string value)
{
    var opening = value.IndexOf('(');
    var closing = value.LastIndexOf(')');
    return opening > 0 && closing > opening
        ? (value[..opening], value[(opening + 1)..closing])
        : (value, value);
}

static string ParseOwnership(string value) => value.Length > 0 ? value[0] switch
{
    '1' => "national",
    '2' => "public",
    '3' => "private",
    _ => "unknown"
} : "unknown";

static string NormalizePrefectureName(string code, string label) => code switch
{
    "01" => "北海道",
    "13" => "東京都",
    "26" => "京都府",
    "27" => "大阪府",
    _ => label.EndsWith('県') ? label : label + "県"
};

static string NormalizePostalCode(string value)
{
    var digits = new string(value.Where(char.IsDigit).ToArray());
    return digits.Length == 7 ? $"{digits[..3]}-{digits[3..]}" : value;
}

static void Validate(IReadOnlyCollection<SchoolIdentity> identities, IReadOnlyCollection<School> schools)
{
    static void EnsureUnique(IEnumerable<string> values, string label)
    {
        var duplicate = values.GroupBy(value => value, StringComparer.Ordinal).FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null) throw new InvalidDataException($"Duplicate {label}: {duplicate.Key}");
    }

    EnsureUnique(identities.Select(identity => identity.Id), "WakaRoute school ID");
    EnsureUnique(identities.Select(identity => identity.MextSchoolCode), "MEXT school code");
    EnsureUnique(schools.Select(school => school.Id), "current school ID");

    var identityIds = identities.Select(identity => identity.Id).ToHashSet(StringComparer.Ordinal);
    var activeIdentityIds = identities.Where(identity => identity.Status == "active").Select(identity => identity.Id).ToHashSet(StringComparer.Ordinal);
    var schoolIds = schools.Select(school => school.Id).ToHashSet(StringComparer.Ordinal);
    if (!activeIdentityIds.SetEquals(schoolIds)) throw new InvalidDataException("Active identities and current schools are inconsistent.");

    var missingReplacement = identities.FirstOrDefault(identity => identity.ReplacedById is not null && !identityIds.Contains(identity.ReplacedById));
    if (missingReplacement is not null) throw new InvalidDataException($"Replacement ID '{missingReplacement.ReplacedById}' does not exist.");
}

internal sealed record MextSchoolRow(string MextSchoolCode, string SchoolType, string Prefecture, string Ownership, string MainOrBranch, string SchoolName, string Address, string PostalCode, string OpenedOn, string ClosedOn, string LegacySchoolSurveyCode, string ReplacementMextSchoolCode);
internal sealed record SchoolIdentityDocument(int SchemaVersion, string AsOf, string[] SourceUrls, SchoolIdentity[] Identities);
internal sealed record SchoolIdentity(string Id, string MextSchoolCode, string? LegacySchoolSurveyCode, string Status, string[] Aliases, string? ReplacedById, string? ReplacementMextSchoolCode);
internal sealed record SchoolCatalogDocument(int SchemaVersion, string AsOf, string[] SourceUrls, School[] Schools);
internal sealed record School(string Id, string Name, string? NameKana, string PrefectureCode, string Prefecture, string Address, string PostalCode, string Ownership, string CampusType, string OpenedOn, string LastVerifiedAt, decimal? Latitude, decimal? Longitude, string? OfficialUrl, string[] Tags);
