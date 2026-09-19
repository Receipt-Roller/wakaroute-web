using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

const string NgslUrl = "https://static1.squarespace.com/static/64336926d7c6bb38965fdf3b/t/644e0be4ad7bae3d45b9e62a/1682836452194/NGSL_1.2_stats.csv";
const string EJDictArchiveUrl = "https://codeload.github.com/kujirahand/EJDict/zip/refs/tags/v2.0.0";
const string DatasetVersion = "2026.1";
const string DatasetAsOf = "2026-09-19";
const int ElementaryCount = 650;
const int JuniorHighCount = 1800;
const int TotalCount = ElementaryCount + JuniorHighCount;

var arguments = ParseArguments(args);
var outputPath = Path.GetFullPath(arguments.GetValueOrDefault("output") ??
    Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Data", "EnglishVocabulary", "english-words.json"));

using var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("WakaRoute-EnglishVocabularyGenerator/1.0 (+https://wakaroute.com)");

var ngslPath = arguments.GetValueOrDefault("ngsl-csv");
var ejDictDirectory = arguments.GetValueOrDefault("ejdict-directory");
var temporaryDirectory = Path.Combine(Path.GetTempPath(), $"wakaroute-english-{Guid.NewGuid():N}");

try
{
    Directory.CreateDirectory(temporaryDirectory);
    if (string.IsNullOrWhiteSpace(ngslPath))
    {
        ngslPath = Path.Combine(temporaryDirectory, "NGSL_1.2_stats.csv");
        Console.WriteLine("Downloading NGSL 1.2...");
        await DownloadAsync(httpClient, NgslUrl, ngslPath);
    }

    if (string.IsNullOrWhiteSpace(ejDictDirectory))
    {
        var archivePath = Path.Combine(temporaryDirectory, "ejdict-v2.0.0.zip");
        Console.WriteLine("Downloading EJDict v2.0.0...");
        await DownloadAsync(httpClient, EJDictArchiveUrl, archivePath);
        ZipFile.ExtractToDirectory(archivePath, temporaryDirectory);
        ejDictDirectory = Directory.EnumerateDirectories(temporaryDirectory, "src", SearchOption.AllDirectories).SingleOrDefault()
            ?? throw new InvalidDataException("The EJDict src directory was not found in the archive.");
    }

    var ngslEntries = ReadNgsl(ngslPath).Take(TotalCount).ToArray();
    if (ngslEntries.Length != TotalCount)
        throw new InvalidDataException($"Expected at least {TotalCount:N0} NGSL entries, but found {ngslEntries.Length:N0}.");

    Console.WriteLine("Reading EJDict translations...");
    var dictionary = ReadEJDict(ejDictDirectory);
    var items = ngslEntries.Select((entry, index) =>
    {
        dictionary.TryGetValue(entry.Lemma, out var details);
        var recommendedGrade = index < ElementaryCount ? (int?)null : ((index - ElementaryCount) / 600) + 1;
        return new EnglishWordItem(
            Id: $"en-{Slugify(entry.Lemma)}",
            Lemma: entry.Lemma,
            Stage: index < ElementaryCount ? "elementary-review" : "junior-high",
            RecommendedGrade: recommendedGrade,
            Sequence: index + 1,
            FrequencyRank: entry.Rank,
            PartsOfSpeech: [],
            MeaningsJa: details?.MeaningsJa ?? [],
            Pronunciations: []);
    }).ToArray();

    Validate(items);
    var missingMeaningCount = items.Count(item => item.MeaningsJa.Length == 0);

    var document = new EnglishVocabularyDocument(
        SchemaVersion: 1,
        DatasetVersion: arguments.GetValueOrDefault("dataset-version") ?? DatasetVersion,
        AsOf: arguments.GetValueOrDefault("as-of") ?? DatasetAsOf,
        Language: "ja-JP",
        Licenses:
        [
            new DatasetLicense("NGSL 1.2", "CC-BY-SA-4.0", "https://creativecommons.org/licenses/by-sa/4.0/", "New General Service List by Browne, C., Culligan, B., and Phillips, J."),
            new DatasetLicense("EJDict-hand", "CC0-1.0", "https://creativecommons.org/publicdomain/zero/1.0/", "English-Japanese Dictionary data EJDict-hand by kujirahand and contributors.")
        ],
        Sources:
        [
            new DatasetSource("mext-junior-high-foreign-language-2017", "中学校学習指導要領（平成29年告示）解説 外国語編", "https://www.mext.go.jp/component/a_menu/education/micro_detail/__icsFiles/afieldfile/2019/03/18/1387018_010.pdf", "小学校600～700語程度、中学校で新たに1,600～1,800語程度という語数目標"),
            new DatasetSource("ngsl-1.2", "New General Service List 1.2", "https://www.newgeneralservicelist.com/new-general-service-list", "見出し語と一般英語での使用頻度順位", "1.2"),
            new DatasetSource("ejdict-hand", "EJDict-hand", "https://github.com/kujirahand/EJDict", "日本語の語義", "v2.0.0")
        ],
        Assignment: new GradeAssignment(
            Classification: "wakaroute-editorial",
            OfficialGradeAssignment: false,
            ElementaryBaselineCount: ElementaryCount,
            JuniorHighNewWordCount: JuniorHighCount,
            Method: "NGSL 1.2の使用頻度順位上位650語を小学校既習想定の復習語、その後の1,800語を中学校新出推奨語とし、中1・中2・中3へ600語ずつ割り振った初版の学習順です。国や教科書の公式配当ではありません。",
            GradeCounts: new Dictionary<string, int> { ["1"] = 600, ["2"] = 600, ["3"] = 600 }),
        OfficialLearningGoals: new OfficialLearningGoals(
            ElementaryWordRange: "600～700語程度",
            JuniorHighNewWordRange: "1,600～1,800語程度",
            CumulativeWordRange: "2,200～2,500語程度"),
        LearningSkills:
        [
            new LearningSkill("listening", "聞く", "音を聞いて語を認識する"),
            new LearningSkill("reading", "読む", "文字を見て発音と意味を結び付ける"),
            new LearningSkill("meaning", "意味", "文脈に合う意味を選ぶ"),
            new LearningSkill("spelling", "書く", "つづりを正確に再現する"),
            new LearningSkill("usage", "使う", "文や会話の中で使う")
        ],
        Quality: new DatasetQuality(
            MeaningCoverageCount: items.Length - missingMeaningCount,
            MissingMeaningCount: missingMeaningCount,
            Note: "日本語の語義はEJDict-handから機械的に対応付けた初版です。語の意味は文脈により変わるため、例文や問題へ利用する際は編集確認します。"),
        Items: items);

    Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
    var jsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    await File.WriteAllTextAsync(outputPath, JsonSerializer.Serialize(document, jsonOptions) + Environment.NewLine, new UTF8Encoding(false));

    Console.WriteLine($"Generated {Path.GetRelativePath(Environment.CurrentDirectory, outputPath)} with {items.Length:N0} words.");
    Console.WriteLine($"  Elementary review: {items.Count(item => item.Stage == "elementary-review"):N0}");
    foreach (var group in items.Where(item => item.RecommendedGrade is not null).GroupBy(item => item.RecommendedGrade).OrderBy(group => group.Key))
        Console.WriteLine($"  Grade {group.Key}: {group.Count():N0}");
    Console.WriteLine($"  Japanese meaning coverage: {items.Length - missingMeaningCount:N0}/{items.Length:N0}");
}
finally
{
    if (Directory.Exists(temporaryDirectory))
        Directory.Delete(temporaryDirectory, recursive: true);
}

static async Task DownloadAsync(HttpClient httpClient, string url, string path)
{
    await using var source = await httpClient.GetStreamAsync(url);
    await using var destination = File.Create(path);
    await source.CopyToAsync(destination);
}

static IEnumerable<NgslEntry> ReadNgsl(string path)
{
    foreach (var line in File.ReadLines(path).Skip(1))
    {
        if (string.IsNullOrWhiteSpace(line)) continue;
        var parts = line.Split(',');
        if (parts.Length < 2 || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var rank)) continue;
        yield return new NgslEntry(parts[0].Trim().ToLowerInvariant(), rank);
    }
}

static Dictionary<string, DictionaryEntry> ReadEJDict(string directory)
{
    var entries = new Dictionary<string, DictionaryEntry>(StringComparer.Ordinal);
    foreach (var path in Directory.EnumerateFiles(directory, "*.txt", SearchOption.TopDirectoryOnly).OrderBy(value => value, StringComparer.Ordinal))
    {
        foreach (var line in File.ReadLines(path))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var separatorIndex = line.IndexOf('\t');
            if (separatorIndex <= 0 || separatorIndex == line.Length - 1) continue;

            var meanings = line[(separatorIndex + 1)..]
                .Split(" / ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(TrimMeaning)
                .Where(value => value.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .Take(3)
                .ToArray();
            if (meanings.Length == 0) continue;

            var headwords = line[..separatorIndex].Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (headwords.Length == 0 || !string.Equals(headwords[0], headwords[0].ToLowerInvariant(), StringComparison.Ordinal)) continue;
            foreach (var headword in headwords)
            {
                // NGSL見出し語は小文字。大文字1文字の略語が a / i と誤対応しないよう、
                // 先頭見出し語が大文字の略語グループは学習語へ対応付けない。
                if (!string.Equals(headword, headword.ToLowerInvariant(), StringComparison.Ordinal)) continue;
                entries.TryAdd(headword, new DictionaryEntry(meanings));
            }
        }
    }

    return entries;
}

static string TrimMeaning(string value)
{
    const int maximumLength = 180;
    return value.Length <= maximumLength ? value : value[..maximumLength].TrimEnd() + "…";
}

static string Slugify(string value)
{
    var slug = Regex.Replace(value.ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
    return slug.Length > 0 ? slug : Convert.ToHexString(Encoding.UTF8.GetBytes(value)).ToLowerInvariant();
}

static void Validate(EnglishWordItem[] items)
{
    if (items.Length != TotalCount) throw new InvalidDataException($"Dataset must contain exactly {TotalCount:N0} words.");
    if (items.Count(item => item.Stage == "elementary-review") != ElementaryCount) throw new InvalidDataException("Elementary review must contain exactly 650 words.");
    foreach (var grade in Enumerable.Range(1, 3))
        if (items.Count(item => item.RecommendedGrade == grade) != 600) throw new InvalidDataException($"Grade {grade} must contain exactly 600 words.");
    if (items.Select(item => item.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() != items.Length) throw new InvalidDataException("Word IDs must be unique.");
    if (!items.Select(item => item.Sequence).SequenceEqual(Enumerable.Range(1, TotalCount))) throw new InvalidDataException("Sequence must be contiguous.");
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

internal sealed record NgslEntry(string Lemma, int Rank);
internal sealed record DictionaryEntry(string[] MeaningsJa);
internal sealed record EnglishVocabularyDocument(int SchemaVersion, string DatasetVersion, string AsOf, string Language, DatasetLicense[] Licenses, DatasetSource[] Sources, GradeAssignment Assignment, OfficialLearningGoals OfficialLearningGoals, LearningSkill[] LearningSkills, DatasetQuality Quality, EnglishWordItem[] Items);
internal sealed record DatasetLicense(string Name, string SpdxId, string Url, string Attribution);
internal sealed record DatasetSource(string Id, string Name, string Url, string Role, string? Version = null);
internal sealed record GradeAssignment(string Classification, bool OfficialGradeAssignment, int ElementaryBaselineCount, int JuniorHighNewWordCount, string Method, IReadOnlyDictionary<string, int> GradeCounts);
internal sealed record OfficialLearningGoals(string ElementaryWordRange, string JuniorHighNewWordRange, string CumulativeWordRange);
internal sealed record LearningSkill(string Id, string Name, string Description);
internal sealed record DatasetQuality(int MeaningCoverageCount, int MissingMeaningCount, string Note);
internal sealed record EnglishWordItem(string Id, string Lemma, string Stage, [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? RecommendedGrade, int Sequence, int FrequencyRank, string[] PartsOfSpeech, string[] MeaningsJa, string[] Pronunciations);
