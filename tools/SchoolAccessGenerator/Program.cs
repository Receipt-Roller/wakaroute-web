using System.IO.Compression;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

const string schoolDataUrl = "https://nlftp.mlit.go.jp/ksj/gml/data/P29/P29-21/P29-21_GML.zip";
const string stationDataUrl = "https://nlftp.mlit.go.jp/ksj/gml/data/N02/N02-25/N02-25_GML.zip";
const string schoolDataPage = "https://nlftp.mlit.go.jp/ksj/gml/datalist/KsjTmplt-P29-v2_0.html";
const string stationDataPage = "https://nlftp.mlit.go.jp/ksj/gml/datalist/KsjTmplt-N02-2025.html";

var options = ParseOptions(args);
var repositoryRoot = FindRepositoryRoot(options.GetValueOrDefault("root"));
var catalogPath = Path.GetFullPath(options.GetValueOrDefault("catalog") ?? Path.Combine(repositoryRoot, "Data", "Schools", "schools.json"));
var outputPath = Path.GetFullPath(options.GetValueOrDefault("output") ?? Path.Combine(repositoryRoot, "Data", "Schools", "school-access.json"));
var sourceDirectory = Path.GetFullPath(options.GetValueOrDefault("source-directory") ?? Path.Combine(repositoryRoot, "tmp", "access-data"));
Directory.CreateDirectory(sourceDirectory);

var schoolGeoJson = options.GetValueOrDefault("school-geojson") ?? await DownloadAndExtract(
    schoolDataUrl, Path.Combine(sourceDirectory, "P29-21_GML.zip"), Path.Combine(sourceDirectory, "school"), "P29-21.geojson");
var stationGeoJson = options.GetValueOrDefault("station-geojson") ?? await DownloadAndExtract(
    stationDataUrl, Path.Combine(sourceDirectory, "N02-25_GML.zip"), Path.Combine(sourceDirectory, "rail"), "N02-25_Station.geojson");

Console.WriteLine("Reading official school locations...");
var officialSchools = ReadSchoolLocations(schoolGeoJson);
Console.WriteLine("Reading official railway stations...");
var stations = ReadStations(stationGeoJson);
Console.WriteLine($"Loaded {officialSchools.Count:N0} school locations and {stations.Count:N0} station groups.");

var catalog = JsonNode.Parse(await File.ReadAllTextAsync(catalogPath))?.AsObject()
    ?? throw new InvalidDataException("schools.json is empty or invalid.");
var schools = catalog["schools"]?.AsArray() ?? throw new InvalidDataException("schools.json does not contain schools.");
var catalogSources = catalog["sourceUrls"]?.AsArray() ?? throw new InvalidDataException("schools.json does not contain sourceUrls.");
if (!catalogSources.Any(node => string.Equals(node?.GetValue<string>(), schoolDataPage, StringComparison.Ordinal))) catalogSources.Add(schoolDataPage);
var accessEntries = new JsonArray();
var unmatchedIds = new JsonArray();
var matched = 0;

foreach (var schoolNode in schools)
{
    var school = schoolNode?.AsObject() ?? throw new InvalidDataException("schools.json contains an invalid school.");
    var schoolId = school["id"]?.GetValue<string>() ?? throw new InvalidDataException("A school is missing id.");
    var schoolCode = schoolId.StartsWith("wk_", StringComparison.Ordinal) ? schoolId[3..].ToUpperInvariant() : string.Empty;
    SchoolLocation? officialLocation = officialSchools.GetValueOrDefault(schoolCode);
    var latitude = school["latitude"]?.GetValue<decimal?>();
    var longitude = school["longitude"]?.GetValue<decimal?>();
    var matchedBy = "existing-coordinate";

    if ((!latitude.HasValue || !longitude.HasValue) && officialLocation is not null)
    {
        latitude = decimal.Round((decimal)officialLocation.Latitude, 6);
        longitude = decimal.Round((decimal)officialLocation.Longitude, 6);
        school["latitude"] = latitude.Value;
        school["longitude"] = longitude.Value;
        matchedBy = "mext-school-code";
    }
    else if (latitude.HasValue && longitude.HasValue && officialLocation is not null &&
             Math.Abs((double)latitude.Value - officialLocation.Latitude) < 0.000001 &&
             Math.Abs((double)longitude.Value - officialLocation.Longitude) < 0.000001)
    {
        matchedBy = "mext-school-code";
    }

    if (!latitude.HasValue || !longitude.HasValue)
    {
        unmatchedIds.Add(schoolId);
        continue;
    }

    matched += 1;
    var nearest = stations
        .Select(station => new { Station = station, Distance = HaversineMeters((double)latitude.Value, (double)longitude.Value, station.Latitude, station.Longitude) })
        .OrderBy(item => item.Distance)
        .ThenBy(item => item.Station.Name, StringComparer.Ordinal)
        .Take(3)
        .ToArray();

    accessEntries.Add(new JsonObject
    {
        ["schoolId"] = schoolId,
        ["latitude"] = latitude.Value,
        ["longitude"] = longitude.Value,
        ["coordinateMatchedBy"] = matchedBy,
        ["coordinateMatchedName"] = officialLocation?.Name,
        ["nearestStations"] = new JsonArray(nearest.Select(item => (JsonNode)new JsonObject
        {
            ["groupCode"] = item.Station.GroupCode,
            ["name"] = item.Station.Name,
            ["latitude"] = decimal.Round((decimal)item.Station.Latitude, 6),
            ["longitude"] = decimal.Round((decimal)item.Station.Longitude, 6),
            ["distanceMeters"] = (int)Math.Round(item.Distance),
            ["distanceKind"] = "straight-line",
            ["lines"] = new JsonArray(item.Station.Lines.Select(value => (JsonNode)value).ToArray()),
            ["operators"] = new JsonArray(item.Station.Operators.Select(value => (JsonNode)value).ToArray())
        }).ToArray())
    });
}

var output = new JsonObject
{
    ["schemaVersion"] = 1,
    ["asOf"] = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"),
    ["coordinateSource"] = new JsonObject
    {
        ["title"] = "国土交通省 国土数値情報（学校）2021年度",
        ["url"] = schoolDataPage,
        ["downloadUrl"] = schoolDataUrl,
        ["dataYear"] = 2021,
        ["license"] = "国土数値情報ダウンロードサイトの適用利用規約（オープンデータ）"
    },
    ["stationSource"] = new JsonObject
    {
        ["title"] = "国土交通省 国土数値情報（鉄道）2025年度",
        ["url"] = stationDataPage,
        ["downloadUrl"] = stationDataUrl,
        ["dataYear"] = 2025,
        ["license"] = "CC BY 4.0"
    },
    ["method"] = "2021年度学校データを文部科学省学校コードで結合し、2025年度駅データの駅グループ重心までの直線距離を算出。徒歩距離・所要時間ではありません。",
    ["statistics"] = new JsonObject
    {
        ["currentSchoolCount"] = schools.Count,
        ["matchedSchoolCount"] = matched,
        ["unmatchedSchoolCount"] = schools.Count - matched,
        ["stationGroupCount"] = stations.Count
    },
    ["schools"] = accessEntries,
    ["unmatchedSchoolIds"] = unmatchedIds
};

var jsonOptions = new JsonSerializerOptions
{
    WriteIndented = true,
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
};
await File.WriteAllTextAsync(catalogPath, catalog.ToJsonString(jsonOptions) + Environment.NewLine);
await File.WriteAllTextAsync(outputPath, output.ToJsonString(jsonOptions) + Environment.NewLine);
Console.WriteLine($"Updated coordinates for {matched:N0}/{schools.Count:N0} current schools.");
Console.WriteLine($"Wrote {outputPath}");

static Dictionary<string, string> ParseOptions(string[] arguments)
{
    var result = new Dictionary<string, string>(StringComparer.Ordinal);
    for (var index = 0; index < arguments.Length; index += 2)
    {
        if (!arguments[index].StartsWith("--", StringComparison.Ordinal) || index + 1 >= arguments.Length)
            throw new ArgumentException("Options must be provided as --name value pairs.");
        result[arguments[index][2..]] = arguments[index + 1];
    }
    return result;
}

static string FindRepositoryRoot(string? requestedRoot)
{
    if (!string.IsNullOrWhiteSpace(requestedRoot)) return Path.GetFullPath(requestedRoot);
    var current = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (current is not null)
    {
        if (File.Exists(Path.Combine(current.FullName, "Data", "Schools", "schools.json"))) return current.FullName;
        current = current.Parent;
    }
    throw new DirectoryNotFoundException("Could not find the WakaRoute repository root.");
}

static async Task<string> DownloadAndExtract(string url, string archivePath, string extractDirectory, string expectedFileName)
{
    if (!File.Exists(archivePath))
    {
        Console.WriteLine($"Downloading {url}");
        using var client = new HttpClient();
        await using var source = await client.GetStreamAsync(url);
        await using var destination = File.Create(archivePath);
        await source.CopyToAsync(destination);
    }
    if (!Directory.Exists(extractDirectory) || !Directory.EnumerateFiles(extractDirectory, expectedFileName, SearchOption.AllDirectories).Any())
    {
        Directory.CreateDirectory(extractDirectory);
        ZipFile.ExtractToDirectory(archivePath, extractDirectory, overwriteFiles: true);
    }
    return Directory.EnumerateFiles(extractDirectory, expectedFileName, SearchOption.AllDirectories).Single();
}

static Dictionary<string, SchoolLocation> ReadSchoolLocations(string path)
{
    using var document = JsonDocument.Parse(File.ReadAllText(path));
    var result = new Dictionary<string, SchoolLocation>(StringComparer.Ordinal);
    foreach (var feature in document.RootElement.GetProperty("features").EnumerateArray())
    {
        var properties = feature.GetProperty("properties");
        var code = properties.GetProperty("P29_002").GetString();
        if (string.IsNullOrWhiteSpace(code)) continue;
        var coordinates = feature.GetProperty("geometry").GetProperty("coordinates");
        result[code] = new SchoolLocation(
            properties.GetProperty("P29_004").GetString() ?? string.Empty,
            coordinates[1].GetDouble(),
            coordinates[0].GetDouble());
    }
    return result;
}

static IReadOnlyList<StationGroup> ReadStations(string path)
{
    using var document = JsonDocument.Parse(File.ReadAllText(path));
    var builders = new Dictionary<string, StationBuilder>(StringComparer.Ordinal);
    foreach (var feature in document.RootElement.GetProperty("features").EnumerateArray())
    {
        var properties = feature.GetProperty("properties");
        var groupCode = properties.GetProperty("N02_005g").GetString();
        if (string.IsNullOrWhiteSpace(groupCode)) continue;
        if (!builders.TryGetValue(groupCode, out var builder))
        {
            builder = new StationBuilder(groupCode, properties.GetProperty("N02_005").GetString() ?? string.Empty);
            builders.Add(groupCode, builder);
        }
        builder.Lines.Add(properties.GetProperty("N02_003").GetString() ?? string.Empty);
        builder.Operators.Add(properties.GetProperty("N02_004").GetString() ?? string.Empty);
        var coordinates = feature.GetProperty("geometry").GetProperty("coordinates");
        foreach (var coordinate in coordinates.EnumerateArray()) builder.AddPoint(coordinate[1].GetDouble(), coordinate[0].GetDouble());
    }
    return builders.Values.Select(builder => builder.Build()).ToArray();
}

static double HaversineMeters(double latitude1, double longitude1, double latitude2, double longitude2)
{
    const double earthRadius = 6_371_000;
    var lat1 = Math.PI * latitude1 / 180;
    var lat2 = Math.PI * latitude2 / 180;
    var deltaLatitude = Math.PI * (latitude2 - latitude1) / 180;
    var deltaLongitude = Math.PI * (longitude2 - longitude1) / 180;
    var a = Math.Sin(deltaLatitude / 2) * Math.Sin(deltaLatitude / 2) +
            Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(deltaLongitude / 2) * Math.Sin(deltaLongitude / 2);
    return earthRadius * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
}

sealed record SchoolLocation(string Name, double Latitude, double Longitude);
sealed record StationGroup(string GroupCode, string Name, double Latitude, double Longitude, string[] Lines, string[] Operators);

sealed class StationBuilder(string groupCode, string name)
{
    private double _latitudeTotal;
    private double _longitudeTotal;
    private int _pointCount;
    public HashSet<string> Lines { get; } = new(StringComparer.Ordinal);
    public HashSet<string> Operators { get; } = new(StringComparer.Ordinal);

    public void AddPoint(double latitude, double longitude)
    {
        _latitudeTotal += latitude;
        _longitudeTotal += longitude;
        _pointCount += 1;
    }

    public StationGroup Build() => new(
        groupCode,
        name,
        _latitudeTotal / _pointCount,
        _longitudeTotal / _pointCount,
        Lines.Where(value => !string.IsNullOrWhiteSpace(value)).Order(StringComparer.Ordinal).ToArray(),
        Operators.Where(value => !string.IsNullOrWhiteSpace(value)).Order(StringComparer.Ordinal).ToArray());
}
