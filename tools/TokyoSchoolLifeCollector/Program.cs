using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

const string SiteHost = "www.metro.ed.jp";
string[] pilotIds = ["wk_d113299901022", "wk_d113299902012", "wk_d113299903057"];
var options = Options.Parse(args);
var root = Path.GetFullPath(options.Root ?? FindRepositoryRoot());
var today = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd");
var schoolsPath = Path.Combine(root, "Data", "Schools", "schools.json");
var lifePath = Path.Combine(root, "Data", "Schools", "school-life.json");
var privateDirectory = Path.GetFullPath(options.PrivateDirectory ?? Path.Combine(root, "..", "wakaroute-private-data"));
var contactsPath = Path.Combine(privateDirectory, "school-contact-channels.local.json");
var manifestPath = Path.Combine(privateDirectory, "school-collection-manifest.local.json");

if (options.All && !options.AcknowledgeSitePolicy)
{
    throw new InvalidOperationException("全校収集には --acknowledge-site-policy true が必要です。公式サイト管理者への確認後に使用してください。");
}

var schoolsDocument = JsonNode.Parse(await File.ReadAllTextAsync(schoolsPath))!.AsObject();
var allSchools = schoolsDocument["schools"]!.AsArray()
    .Select(node => School.From(node!.AsObject()))
    .Where(s => Uri.TryCreate(s.OfficialUrl, UriKind.Absolute, out var uri) && uri.Host == SiteHost)
    .ToList();
var requestedIds = options.SchoolIds.Count > 0 ? options.SchoolIds : pilotIds.ToList();
var targets = options.All ? allSchools : allSchools.Where(s => requestedIds.Contains(s.Id, StringComparer.Ordinal)).ToList();
if (targets.Count == 0) throw new InvalidOperationException("対象校が見つかりませんでした。");

Directory.CreateDirectory(privateDirectory);
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
http.DefaultRequestHeaders.UserAgent.ParseAdd("WakaRouteSchoolDataCollector/1.0 (+https://wakaroute.com/company)");
var robots = await http.GetStringAsync("https://www.metro.ed.jp/robots.txt");
if (Regex.IsMatch(robots, @"(?ims)^User-agent:\s*\*.*?^Disallow:\s*/\s*$"))
    throw new InvalidOperationException("robots.txt がサイト全体の収集を許可していません。");

var lifeDocument = JsonNode.Parse(await File.ReadAllTextAsync(lifePath))!.AsObject();
var lifeSchools = lifeDocument["schools"]!.AsArray();
var contactsDocument = await ReadOrCreateDocument(contactsPath, "contacts", today);
var contacts = contactsDocument["contacts"]!.AsArray();
var manifestDocument = await ReadOrCreateDocument(manifestPath, "pages", today);
var manifest = manifestDocument["pages"]!.AsArray();

foreach (var school in targets)
{
    Console.WriteLine($"収集中: {school.Name}");
    var baseUri = new Uri(school.OfficialUrl);
    var pages = new Dictionary<string, Page?>
    {
        ["top"] = await Fetch(http, baseUri, school, "top", options.DelayMilliseconds, manifest, today),
        ["activities"] = await Fetch(http, new Uri(baseUri, "school_life/activities.html"), school, "activities", options.DelayMilliseconds, manifest, today),
        ["events"] = await Fetch(http, new Uri(baseUri, "school_life/event.html"), school, "events", options.DelayMilliseconds, manifest, today),
        ["symbols"] = await Fetch(http, new Uri(baseUri, "school_life/symbols.html"), school, "symbols", options.DelayMilliseconds, manifest, today)
    };

    var clubs = ParseClubs(pages["activities"]?.Html ?? string.Empty);
    var events = ParseEvents(pages["events"]?.Html ?? string.Empty);
    var uniform = ParseUniform(pages["symbols"]?.Html ?? string.Empty);
    var sources = new JsonArray();
    AddSource(sources, pages["activities"], $"{school.Name} 部活動", today);
    AddSource(sources, pages["events"], $"{school.Name} 学校行事", today);
    AddSource(sources, pages["symbols"], $"{school.Name} 制服・校章・校歌", today);

    var entry = new JsonObject
    {
        ["schoolId"] = school.Id,
        ["verifiedAt"] = today,
        ["uniform"] = uniform,
        ["rulesSummary"] = "学校生活のきまりは公式サイトの掲載状況を確認中です。服装や登下校などは、学校説明会や最新の公式資料でも確認してください。",
        ["lunch"] = new JsonObject { ["status"] = "unknown", ["summary"] = "昼食・食堂・購買の利用方法は自動確認の対象外です。学校見学で確認したい項目です。" },
        ["events"] = events,
        ["clubs"] = clubs,
        ["sources"] = sources
    };
    ReplaceByProperty(lifeSchools, "schoolId", school.Id, entry);
    CollectContacts(school, pages["top"], contacts, today);
    Console.WriteLine($"  部活動 {clubs.Count}件 / 行事 {events.Count}件 / 制服 {uniform["status"]}");
}

lifeDocument["asOf"] = today;
contactsDocument["asOf"] = today;
manifestDocument["asOf"] = today;
await WriteJson(lifePath, lifeDocument);
await WriteJson(contactsPath, contactsDocument);
await WriteJson(manifestPath, manifestDocument);
Console.WriteLine($"完了: {targets.Count}校");

static async Task<Page?> Fetch(HttpClient http, Uri uri, School school, string pageType, int delayMilliseconds, JsonArray manifest, string today)
{
    if (delayMilliseconds > 0) await Task.Delay(delayMilliseconds);
    try
    {
        using var response = await http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            UpdateManifest(manifest, school, pageType, uri, "not-found", null, today);
            return null;
        }
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(html))).ToLowerInvariant();
        UpdateManifest(manifest, school, pageType, uri, "ok", hash, today);
        return new Page(uri.ToString(), html);
    }
    catch (HttpRequestException ex)
    {
        UpdateManifest(manifest, school, pageType, uri, "error", null, today, ex.Message);
        Console.WriteLine($"  取得失敗: {uri} ({ex.Message})");
        return null;
    }
}

static JsonArray ParseClubs(string html)
{
    var categories = new Dictionary<string, string>(StringComparer.Ordinal);
    foreach (Match match in Regex.Matches(html, @"<h3[^>]*class=[""'][^""']*ttl_color[^""']*[""'][^>]*>\s*(?<category>運動部|文化部|運動系|文化系)[^<]*</h3>\s*<ul[^>]*class=[""'][^""']*club_ul[^""']*[""'][^>]*>(?<items>.*?)</ul>", RegexOptions.Singleline | RegexOptions.IgnoreCase))
    {
        var category = match.Groups["category"].Value.StartsWith("運動", StringComparison.Ordinal) ? "sports" : "culture";
        foreach (Match item in Regex.Matches(match.Groups["items"].Value, @"<li[^>]*>(?<name>.*?)</li>", RegexOptions.Singleline | RegexOptions.IgnoreCase))
        {
            var name = CleanText(item.Groups["name"].Value);
            if (!string.IsNullOrWhiteSpace(name)) categories[name] = category;
        }
    }

    var found = new Dictionary<string, JsonObject>(StringComparer.Ordinal);
    foreach (Match match in Regex.Matches(html, @"<a\s+[^>]*href=[""'](?<href>[^""']*/activities/club[^""']*)[""'][^>]*>(?<body>.*?)</a>", RegexOptions.Singleline | RegexOptions.IgnoreCase))
    {
        var heading = Regex.Match(match.Groups["body"].Value, @"<h3[^>]*>(?<name>.*?)</h3>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        var name = CleanText(heading.Groups["name"].Value);
        if (string.IsNullOrWhiteSpace(name) || name.Contains("生徒会", StringComparison.Ordinal) || name.Contains("委員会", StringComparison.Ordinal)) continue;
        var days = ExtractLabeledParagraph(match.Groups["body"].Value, "活動日") ?? "未確認";
        var place = ExtractLabeledParagraph(match.Groups["body"].Value, "活動場所");
        found[name] = Club(name, categories.GetValueOrDefault(name) ?? ClassifyClub(name), days, place);
    }
    foreach (var pair in categories)
    {
        var representedByDetailedClub = found.Keys.Any(name =>
            name.StartsWith(pair.Key + "（", StringComparison.Ordinal) ||
            (pair.Key.EndsWith("部", StringComparison.Ordinal) && name.StartsWith(pair.Key[..^1], StringComparison.Ordinal)));
        if (!found.ContainsKey(pair.Key) && !representedByDetailedClub && !pair.Key.Contains("生徒会", StringComparison.Ordinal))
            found[pair.Key] = Club(pair.Key, pair.Value, "未確認", null);
    }

    var result = new JsonArray();
    foreach (var club in found.Values.OrderBy(c => c["category"]!.GetValue<string>()).ThenBy(c => c["name"]!.GetValue<string>(), StringComparer.Ordinal)) result.Add(club);
    return result;
}

static JsonObject Club(string name, string category, string days, string? place) => new()
{
    ["name"] = name, ["category"] = category,
    ["gender"] = name.Contains("男子", StringComparison.Ordinal) ? "boys" : name.Contains("女子", StringComparison.Ordinal) ? "girls" : "unknown",
    ["activityDays"] = days, ["activityPlace"] = place
};

static string ClassifyClub(string name)
{
    string[] sports = ["野球", "サッカー", "テニス", "バスケット", "バレー", "陸上", "水泳", "剣道", "柔道", "空手", "弓道", "卓球", "バドミントン", "ダンス", "ラグビー", "ハンドボール", "体操", "山岳", "ワンダーフォーゲル", "ソフトボール", "フットサル", "ホッケー", "ボート", "ヨット", "レスリング", "フェンシング", "チア", "バトン"];
    return sports.Any(name.Contains) ? "sports" : "culture";
}

static JsonArray ParseEvents(string html)
{
    var result = new JsonArray();
    var seen = new HashSet<string>(StringComparer.Ordinal);
    foreach (Match match in Regex.Matches(html, @"<li[^>]*>\s*<h2[^>]*>\s*(?<season>[0-9０-９]+月)\s*</h2>\s*<p[^>]*>(?<items>.*?)</p>", RegexOptions.Singleline | RegexOptions.IgnoreCase))
    {
        var season = CleanText(match.Groups["season"].Value);
        foreach (var name in CleanText(match.Groups["items"].Value).Split('、', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            if (name.Length is >= 2 and <= 60 && seen.Add($"{season}|{name}")) result.Add(new JsonObject { ["name"] = name, ["season"] = season });
    }
    foreach (Match match in Regex.Matches(html, @"<dt[^>]*>\s*(?<season>[0-9０-９]+月)\s*</dt>\s*<dd[^>]*>(?<items>.*?)</dd>", RegexOptions.Singleline | RegexOptions.IgnoreCase))
    {
        var season = CleanText(match.Groups["season"].Value);
        var withoutImages = Regex.Replace(match.Groups["items"].Value, @"<img[^>]*>", string.Empty, RegexOptions.IgnoreCase);
        foreach (var name in CleanText(withoutImages).Split(['、', '／'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            if (name.Length is >= 2 and <= 60 && seen.Add($"{season}|{name}")) result.Add(new JsonObject { ["name"] = name, ["season"] = season });
    }
    return result;
}

static JsonObject ParseUniform(string html)
{
    var heading = Regex.Match(html, @"<h[23][^>]*>\s*制服\s*</h[23]>", RegexOptions.IgnoreCase);
    if (!heading.Success) return new JsonObject { ["status"] = "unknown", ["summary"] = "制服に関する公式情報を自動確認できていません。学校説明会や公式資料で確認してください。" };
    var rest = html[(heading.Index + heading.Length)..];
    var nextHeading = Regex.Match(rest, @"<h[23][^>]*>", RegexOptions.IgnoreCase);
    var section = CleanText(nextHeading.Success ? rest[..nextHeading.Index] : rest);
    if (Regex.IsMatch(section, @"制服(が|は)(ありません|ございません)|制服を定めていません"))
        return new JsonObject { ["status"] = "none", ["summary"] = "制服の指定はありません。服装に関する最新の決まりは公式情報を確認してください。" };
    if (section.Contains("標準服", StringComparison.Ordinal))
        return new JsonObject { ["status"] = "optional", ["summary"] = "標準服の案内があります。着用条件などの詳細は公式情報を確認してください。" };
    if (section.Contains("制服", StringComparison.Ordinal) || section.Length > 20)
        return new JsonObject { ["status"] = "required", ["summary"] = "制服の案内があります。着用方法や季節ごとの扱いは公式情報を確認してください。" };
    return new JsonObject { ["status"] = "unknown", ["summary"] = "制服に関するページはありますが、指定の扱いは自動確認できていません。" };
}

static void CollectContacts(School school, Page? page, JsonArray contacts, string today)
{
    if (page is null) return;
    foreach (Match match in Regex.Matches(page.Html, @"<a\s+[^>]*href=[""']mailto:(?<email>[^?""']+)[^""']*[""'][^>]*>", RegexOptions.IgnoreCase))
    {
        var email = WebUtility.UrlDecode(match.Groups["email"].Value).Trim().ToLowerInvariant();
        if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$") || HasContact(contacts, school.Id, email)) continue;
        contacts.Add(new JsonObject
        {
            ["id"] = $"{school.Id}-email-{ShortHash(email)}", ["schoolId"] = school.Id, ["channelType"] = "email", ["value"] = email,
            ["purpose"] = "unknown", ["organizationManaged"] = email.EndsWith(".lg.jp", StringComparison.OrdinalIgnoreCase) || email.EndsWith(".ed.jp", StringComparison.OrdinalIgnoreCase),
            ["sourceUrl"] = page.Url, ["verifiedAt"] = today, ["publicOnOfficialSite"] = true, ["doNotContact"] = false,
            ["notes"] = "公式サイト上の公開mailtoリンクから確認。送信前に用途と宛先を人が再確認すること。"
        });
    }
}

static string? ExtractLabeledParagraph(string html, string label)
{
    var match = Regex.Match(html, $@"<h4[^>]*>\s*{Regex.Escape(label)}\s*</h4>\s*<p[^>]*>(?<value>.*?)</p>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
    var value = CleanText(match.Groups["value"].Value).Replace("\n", "、", StringComparison.Ordinal);
    return string.IsNullOrWhiteSpace(value) ? null : value;
}

static string CleanText(string html)
{
    var text = Regex.Replace(html, @"<(script|style)[^>]*>.*?</\1>", string.Empty, RegexOptions.Singleline | RegexOptions.IgnoreCase);
    text = Regex.Replace(text, @"<br\s*/?>|</p>|</li>", "\n", RegexOptions.IgnoreCase);
    text = Regex.Replace(text, @"<[^>]+>", string.Empty);
    text = WebUtility.HtmlDecode(text).Replace('\u3000', ' ');
    return string.Join("\n", text.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Select(line => Regex.Replace(line, @"\s+", " "))).Trim();
}

static void AddSource(JsonArray sources, Page? page, string title, string today)
{
    if (page is not null) sources.Add(new JsonObject { ["title"] = title, ["url"] = page.Url, ["publishedAt"] = null, ["verifiedAt"] = today });
}

static void UpdateManifest(JsonArray manifest, School school, string pageType, Uri uri, string status, string? hash, string today, string? error = null)
{
    var entry = new JsonObject { ["id"] = $"{school.Id}-{pageType}", ["schoolId"] = school.Id, ["pageType"] = pageType, ["url"] = uri.ToString(), ["status"] = status, ["contentHash"] = hash, ["fetchedAt"] = DateTimeOffset.Now.ToString("O"), ["verifiedAt"] = today, ["error"] = error };
    ReplaceByProperty(manifest, "id", $"{school.Id}-{pageType}", entry);
}

static void ReplaceByProperty(JsonArray array, string property, string value, JsonObject replacement)
{
    for (var i = 0; i < array.Count; i++) if (array[i]?[property]?.GetValue<string>() == value) { array[i] = replacement; return; }
    array.Add(replacement);
}

static bool HasContact(JsonArray contacts, string schoolId, string value) => contacts.Any(node => node?["schoolId"]?.GetValue<string>() == schoolId && node?["value"]?.GetValue<string>() == value);
static string ShortHash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..10].ToLowerInvariant();

static async Task<JsonObject> ReadOrCreateDocument(string path, string arrayName, string today)
{
    if (File.Exists(path)) return JsonNode.Parse(await File.ReadAllTextAsync(path))!.AsObject();
    return new JsonObject { ["schemaVersion"] = 1, ["asOf"] = today, [arrayName] = new JsonArray() };
}

static async Task WriteJson(string path, JsonObject document)
{
    await File.WriteAllTextAsync(path, document.ToJsonString(new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }) + Environment.NewLine, new UTF8Encoding(false));
}

static string FindRepositoryRoot()
{
    var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (directory is not null)
    {
        if (File.Exists(Path.Combine(directory.FullName, "wakaroute-web.csproj"))) return directory.FullName;
        directory = directory.Parent;
    }
    throw new DirectoryNotFoundException("wakaroute-web.csproj があるリポジトリを見つけられませんでした。");
}

sealed record School(string Id, string Name, string OfficialUrl)
{
    public static School From(JsonObject node) => new(node["id"]!.GetValue<string>(), node["name"]!.GetValue<string>(), node["officialUrl"]?.GetValue<string>() ?? string.Empty);
}
sealed record Page(string Url, string Html);

sealed class Options
{
    public string? Root { get; private set; }
    public string? PrivateDirectory { get; private set; }
    public List<string> SchoolIds { get; } = [];
    public bool All { get; private set; }
    public bool AcknowledgeSitePolicy { get; private set; }
    public int DelayMilliseconds { get; private set; } = 1500;
    public static Options Parse(string[] args)
    {
        var result = new Options();
        for (var i = 0; i < args.Length; i++)
        {
            var value = i + 1 < args.Length ? args[i + 1] : string.Empty;
            switch (args[i])
            {
                case "--root": result.Root = value; i++; break;
                case "--private-directory": result.PrivateDirectory = value; i++; break;
                case "--school-ids": result.SchoolIds.AddRange(value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)); i++; break;
                case "--delay-ms": result.DelayMilliseconds = int.Parse(value); i++; break;
                case "--all": result.All = bool.Parse(value); i++; break;
                case "--acknowledge-site-policy": result.AcknowledgeSitePolicy = bool.Parse(value); i++; break;
            }
        }
        if (result.DelayMilliseconds < 1000) throw new ArgumentOutOfRangeException(nameof(DelayMilliseconds), "取得間隔は1000ms以上にしてください。");
        return result;
    }
}
