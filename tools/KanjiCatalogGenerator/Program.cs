using System.IO.Compression;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;

const string DefaultSourceUrl = "https://www.edrdg.org/pub/Nihongo/kanjidic2.xml.gz";
const string DefaultDatasetVersion = "2026.1";
const string DefaultAsOf = "2026-09-19";

var arguments = ParseArguments(args);
var outputPath = Path.GetFullPath(arguments.GetValueOrDefault("output") ??
    Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Data", "Kanji", "kanji.json"));
var sourceUrl = arguments.GetValueOrDefault("source-url") ?? DefaultSourceUrl;
var datasetVersion = arguments.GetValueOrDefault("dataset-version") ?? DefaultDatasetVersion;
var asOf = arguments.GetValueOrDefault("as-of") ?? DefaultAsOf;

using var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("WakaRoute-KanjiCatalogGenerator/1.0 (+https://wakaroute.com)");

Console.WriteLine("Downloading KANJIDIC2...");
await using var compressed = await httpClient.GetStreamAsync(sourceUrl);
await using var gzip = new GZipStream(compressed, CompressionMode.Decompress);
var source = await XDocument.LoadAsync(gzip, LoadOptions.None, CancellationToken.None);

var sourceHeader = source.Root?.Element("header")
    ?? throw new InvalidDataException("KANJIDIC2 header was not found.");

var entries = source.Root?.Elements("character")
    .Where(character => (string?)character.Element("misc")?.Element("grade") == "8")
    .Select(ParseEntry)
    .OrderBy(entry => entry.FrequencyRank is null ? 1 : 0)
    .ThenBy(entry => entry.FrequencyRank ?? int.MaxValue)
    .ThenBy(entry => entry.StrokeCount)
    .ThenBy(entry => entry.Character, StringComparer.Ordinal)
    .ToArray()
    ?? [];

if (entries.Length != 1110)
    throw new InvalidDataException($"Expected 1,110 junior-high-stage kanji, but found {entries.Length:N0}.");

var items = entries.Select((entry, index) => new KanjiItem(
    Id: $"jhs-u{char.ConvertToUtf32(entry.Character, 0):x}",
    Character: entry.Character,
    OfficialStage: "junior-high",
    RecommendedGrade: index < 350 ? 1 : index < 750 ? 2 : 3,
    Sequence: index + 1,
    StrokeCount: entry.StrokeCount,
    FrequencyRank: entry.FrequencyRank,
    OnReadings: entry.OnReadings,
    KunReadings: entry.KunReadings)).ToArray();

var document = new KanjiDocument(
    SchemaVersion: 1,
    DatasetVersion: datasetVersion,
    AsOf: asOf,
    Language: "ja-JP",
    License: new DatasetLicense(
        Name: "Creative Commons Attribution-ShareAlike 4.0 International",
        SpdxId: "CC-BY-SA-4.0",
        Url: "https://creativecommons.org/licenses/by-sa/4.0/",
        Attribution: "KANJIDIC2 is copyright the Electronic Dictionary Research and Development Group and is used under CC BY-SA 4.0."),
    Sources:
    [
        new DatasetSource(
            Id: "mext-junior-high-curriculum-2017",
            Name: "中学校学習指導要領（平成29年告示）解説 国語編",
            Url: "https://www.mext.go.jp/component/a_menu/education/micro_detail/__icsFiles/afieldfile/2019/03/18/1387018_002.pdf",
            Role: "中学校で読む対象字数と、読み・書きの到達目標の根拠"),
        new DatasetSource(
            Id: "edrdg-kanjidic2",
            Name: "KANJIDIC2",
            Url: sourceUrl,
            Role: "中学校段階1,110字、画数、読み、使用頻度順位",
            Version: (string?)sourceHeader.Element("database_version"),
            CreatedAt: (string?)sourceHeader.Element("date_of_creation"))
    ],
    Assignment: new GradeAssignment(
        Classification: "wakaroute-editorial",
        OfficialGradeAssignment: false,
        Method: "KANJIDIC2の使用頻度順位を優先し、同順位・順位なしは画数と文字コード順で並べ、350字・400字・360字に分割した初版の学習順です。",
        GradeCounts: new Dictionary<string, int> { ["1"] = 350, ["2"] = 400, ["3"] = 360 }),
    OfficialLearningGoals: new OfficialLearningGoals(
        Reading: new Dictionary<string, string>
        {
            ["1"] = "小学校配当1,026字に加え、その他の常用漢字300字程度から400字程度までを読む。",
            ["2"] = "第1学年までに学習した常用漢字に加え、その他の常用漢字350字程度から450字程度までを読む。",
            ["3"] = "第2学年までに学習した常用漢字に加え、その他の常用漢字の大体を読む。"
        },
        Writing: new Dictionary<string, string>
        {
            ["1"] = "小学校配当漢字のうち900字程度を書き、文や文章の中で使う。",
            ["2"] = "小学校配当1,026字を書き、文や文章の中で使う。",
            ["3"] = "小学校配当漢字について、文や文章の中で使い慣れる。"
        }),
    Items: items);

Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true,
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};
await File.WriteAllTextAsync(
    outputPath,
    JsonSerializer.Serialize(document, jsonOptions) + Environment.NewLine,
    new UTF8Encoding(false));

Console.WriteLine($"Generated {Path.GetRelativePath(Environment.CurrentDirectory, outputPath)} with {items.Length:N0} kanji.");
foreach (var group in items.GroupBy(item => item.RecommendedGrade).OrderBy(group => group.Key))
    Console.WriteLine($"  Grade {group.Key}: {group.Count():N0}");

static SourceEntry ParseEntry(XElement character)
{
    var literal = (string?)character.Element("literal")
        ?? throw new InvalidDataException("KANJIDIC2 entry has no literal.");
    var misc = character.Element("misc")
        ?? throw new InvalidDataException($"KANJIDIC2 entry has no misc element: {literal}");
    var strokeCount = (int?)misc.Elements("stroke_count").FirstOrDefault()
        ?? throw new InvalidDataException($"KANJIDIC2 entry has no stroke count: {literal}");
    var frequencyRank = (int?)misc.Element("freq");
    var readings = character.Element("reading_meaning")?
        .Elements("rmgroup")
        .SelectMany(group => group.Elements("reading"))
        .ToArray() ?? [];

    string[] ReadingValues(string type) => readings
        .Where(reading => string.Equals((string?)reading.Attribute("r_type"), type, StringComparison.Ordinal))
        .Select(reading => reading.Value)
        .Distinct(StringComparer.Ordinal)
        .ToArray();

    return new SourceEntry(literal, strokeCount, frequencyRank, ReadingValues("ja_on"), ReadingValues("ja_kun"));
}

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

internal sealed record SourceEntry(string Character, int StrokeCount, int? FrequencyRank, string[] OnReadings, string[] KunReadings);
internal sealed record KanjiDocument(int SchemaVersion, string DatasetVersion, string AsOf, string Language, DatasetLicense License, DatasetSource[] Sources, GradeAssignment Assignment, OfficialLearningGoals OfficialLearningGoals, KanjiItem[] Items);
internal sealed record DatasetLicense(string Name, string SpdxId, string Url, string Attribution);
internal sealed record DatasetSource(string Id, string Name, string Url, string Role, string? Version = null, string? CreatedAt = null);
internal sealed record GradeAssignment(string Classification, bool OfficialGradeAssignment, string Method, IReadOnlyDictionary<string, int> GradeCounts);
internal sealed record OfficialLearningGoals(IReadOnlyDictionary<string, string> Reading, IReadOnlyDictionary<string, string> Writing);
internal sealed record KanjiItem(string Id, string Character, string OfficialStage, int RecommendedGrade, int Sequence, int StrokeCount, int? FrequencyRank, string[] OnReadings, string[] KunReadings);
