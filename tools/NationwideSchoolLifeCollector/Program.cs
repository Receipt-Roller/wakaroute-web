using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

var options = Options.Parse(args);
var root = Path.GetFullPath(options.Root ?? FindRepositoryRoot());
var today = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd");
var privateDirectory = Path.GetFullPath(options.PrivateDirectory ?? Path.Combine(root, "..", "wakaroute-private-data"));
var schoolsPath = Path.Combine(root, "Data", "Schools", "schools.json");
var lifePath = Path.Combine(root, "Data", "Schools", "school-life.json");
var contactsPath = Path.Combine(privateDirectory, "school-contact-channels.local.json");
var manifestPath = Path.Combine(privateDirectory, "school-collection-manifest.local.json");

if (options.SanitizeOnly)
{
    var document = JsonNode.Parse(await File.ReadAllTextAsync(lifePath))!.AsObject();
    var removed = 0;
    foreach (var schoolNode in document["schools"]!.AsArray())
    {
        var clubs = schoolNode!["clubs"]!.AsArray();
        for (var i = clubs.Count - 1; i >= 0; i--)
        {
            if (IsPlausibleClubName(clubs[i]!["name"]!.GetValue<string>())) continue;
            clubs.RemoveAt(i);
            removed++;
        }
    }
    await WriteJson(lifePath, document);
    Console.WriteLine($"部活名の異常候補を {removed}件除去しました。");
    return;
}

if (options.All && !options.AcknowledgeFactsOnly)
    throw new InvalidOperationException("全国実行には --acknowledge-facts-only true が必要です。文章・画像を保存せず事実だけを抽出します。");

var catalog = JsonNode.Parse(await File.ReadAllTextAsync(schoolsPath))!.AsObject();
var existingLifeDocument = JsonNode.Parse(await File.ReadAllTextAsync(lifePath))!.AsObject();
var existingLifeIds = existingLifeDocument["schools"]!.AsArray().Select(node => node!["schoolId"]!.GetValue<string>()).ToHashSet(StringComparer.Ordinal);
var checkedIds = File.Exists(manifestPath)
    ? JsonNode.Parse(await File.ReadAllTextAsync(manifestPath))!.AsObject()["pages"]!.AsArray().Select(node => node!["schoolId"]!.GetValue<string>()).ToHashSet(StringComparer.Ordinal)
    : new HashSet<string>(StringComparer.Ordinal);
var schools = catalog["schools"]!.AsArray().Select(node => School.From(node!.AsObject())).Where(s => Uri.TryCreate(s.OfficialUrl, UriKind.Absolute, out _)).ToList();
var duplicateUrls = schools.GroupBy(s => NormalizeUrl(s.OfficialUrl), StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1).Select(g => g.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
var targets = schools.Where(s =>
    (options.All || options.SchoolIds.Contains(s.Id, StringComparer.Ordinal)) &&
    (options.Prefectures.Count == 0 || options.Prefectures.Contains(s.Prefecture, StringComparer.Ordinal)) &&
    (options.RefreshExisting || (!existingLifeIds.Contains(s.Id) && !checkedIds.Contains(s.Id)))).Take(options.MaxSchools ?? int.MaxValue).ToList();
if (targets.Count == 0)
{
    Console.WriteLine("未確認の対象校はありません。");
    return;
}

Directory.CreateDirectory(privateDirectory);
using var http = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All }) { Timeout = TimeSpan.FromSeconds(25) };
http.DefaultRequestHeaders.UserAgent.ParseAdd("WakaRouteSchoolDataCollector/1.0 (+https://wakaroute.com/company)");
var hostGates = new ConcurrentDictionary<string, HostGate>(StringComparer.OrdinalIgnoreCase);
var robotsCache = new ConcurrentDictionary<string, Task<RobotsPolicy>>(StringComparer.OrdinalIgnoreCase);
var results = new ConcurrentBag<CollectionResult>();
var completed = 0;

await Parallel.ForEachAsync(targets, new ParallelOptions { MaxDegreeOfParallelism = options.Concurrency }, async (school, cancellationToken) =>
{
    var result = await CollectSchool(school, duplicateUrls, http, hostGates, robotsCache, options.DelayMilliseconds, today, cancellationToken);
    results.Add(result);
    var count = Interlocked.Increment(ref completed);
    if (count % 10 == 0 || count == targets.Count)
        Console.WriteLine($"進捗 {count}/{targets.Count}（収録 {results.Count(r => r.Life is not null)}校）");
});

var lifeDocument = JsonNode.Parse(await File.ReadAllTextAsync(lifePath))!.AsObject();
var lifeSchools = lifeDocument["schools"]!.AsArray();
var contactsDocument = await ReadOrCreateDocument(contactsPath, "contacts", today);
var contacts = contactsDocument["contacts"]!.AsArray();
var manifestDocument = await ReadOrCreateDocument(manifestPath, "pages", today);
var manifest = manifestDocument["pages"]!.AsArray();

foreach (var result in results.OrderBy(r => r.School.Id, StringComparer.Ordinal))
{
    if (result.Life is not null) ReplaceByProperty(lifeSchools, "schoolId", result.School.Id, result.Life);
    foreach (var contact in result.Contacts) ReplaceByProperty(contacts, "id", contact["id"]!.GetValue<string>(), contact);
    foreach (var page in result.Pages) ReplaceByProperty(manifest, "id", page["id"]!.GetValue<string>(), page);
}

lifeDocument["asOf"] = today;
contactsDocument["asOf"] = today;
manifestDocument["asOf"] = today;
await WriteJson(lifePath, lifeDocument);
await WriteJson(contactsPath, contactsDocument);
await WriteJson(manifestPath, manifestDocument);
Console.WriteLine($"完了: 対象 {targets.Count}校 / 学校生活を収録 {results.Count(r => r.Life is not null)}校 / 連絡先候補 {results.Sum(r => r.Contacts.Count)}件");

static async Task<CollectionResult> CollectSchool(
    School school,
    HashSet<string> duplicateUrls,
    HttpClient http,
    ConcurrentDictionary<string, HostGate> hostGates,
    ConcurrentDictionary<string, Task<RobotsPolicy>> robotsCache,
    int delayMilliseconds,
    string today,
    CancellationToken cancellationToken)
{
    var pages = new List<JsonObject>();
    var contacts = new List<JsonObject>();
    var homeUri = new Uri(school.OfficialUrl);
    if (string.Equals(homeUri.Host, "www.metro.ed.jp", StringComparison.OrdinalIgnoreCase))
    {
        pages.Add(Manifest(school, "top", homeUri, "policy-review-required", null, today, "個別ページ利用の事前確認が必要なため自動収集を保留"));
        return new CollectionResult(school, null, contacts, pages);
    }
    if (duplicateUrls.Contains(NormalizeUrl(school.OfficialUrl)))
    {
        pages.Add(Manifest(school, "top", homeUri, "shared-url", null, today, "複数校が同一URLを共有しているため自動抽出を保留"));
        return new CollectionResult(school, null, contacts, pages);
    }

    var home = await Fetch(http, homeUri, school, "top", hostGates, robotsCache, delayMilliseconds, today, pages, cancellationToken);
    if (home is null || !LooksLikeSchoolPage(home.Html, school)) return new CollectionResult(school, null, contacts, pages);
    contacts.AddRange(ParseContacts(school, home, today));

    var links = DiscoverLinks(homeUri, home.Html);
    var selected = new Dictionary<string, LinkCandidate>();
    SelectBest(selected, "clubs", links, ["部活動", "クラブ活動", "クラブ紹介", "部活", "club"]);
    SelectBest(selected, "events", links, ["学校行事", "年間行事", "行事予定", "スクールライフ", "school life", "event"]);
    SelectBest(selected, "uniform", links, ["制服", "標準服", "校則", "学校生活", "uniform"]);

    var fetched = new Dictionary<string, Page>(StringComparer.Ordinal);
    foreach (var pair in selected)
    {
        var page = await Fetch(http, pair.Value.Uri, school, pair.Key, hostGates, robotsCache, delayMilliseconds, today, pages, cancellationToken);
        if (page is null) continue;
        fetched[pair.Key] = page;
        contacts.AddRange(ParseContacts(school, page, today));
    }

    var clubs = fetched.TryGetValue("clubs", out var clubPage) ? ParseClubs(clubPage.Html) : new JsonArray();
    var events = fetched.TryGetValue("events", out var eventPage) ? ParseEvents(eventPage.Html) : new JsonArray();
    var uniform = fetched.TryGetValue("uniform", out var uniformPage) ? ParseUniform(uniformPage.Html) : UnknownUniform();
    var knownUniform = uniform["status"]!.GetValue<string>() != "unknown";
    if (clubs.Count < 3 && events.Count < 3 && !knownUniform) return new CollectionResult(school, null, contacts, pages);

    var sources = new JsonArray();
    if (clubs.Count >= 3 && clubPage is not null) AddSource(sources, school.Name + " 部活動", clubPage.Url, today);
    if (events.Count >= 3 && eventPage is not null) AddSource(sources, school.Name + " 学校行事", eventPage.Url, today);
    if (knownUniform && uniformPage is not null) AddSource(sources, school.Name + " 制服・学校生活", uniformPage.Url, today);
    var entry = new JsonObject
    {
        ["schoolId"] = school.Id, ["verifiedAt"] = today, ["uniform"] = uniform,
        ["rulesSummary"] = "学校生活のきまりは自動確認の対象外です。最新の公式資料や学校説明会で確認してください。",
        ["lunch"] = new JsonObject { ["status"] = "unknown", ["summary"] = "昼食・食堂・購買の利用方法は自動確認の対象外です。学校見学で確認したい項目です。" },
        ["events"] = events.Count >= 3 ? events : new JsonArray(), ["clubs"] = clubs.Count >= 3 ? clubs : new JsonArray(), ["sources"] = sources
    };
    return new CollectionResult(school, entry, DeduplicateContacts(contacts), pages);
}

static async Task<Page?> Fetch(
    HttpClient http, Uri uri, School school, string pageType,
    ConcurrentDictionary<string, HostGate> hostGates,
    ConcurrentDictionary<string, Task<RobotsPolicy>> robotsCache,
    int delayMilliseconds, string today, List<JsonObject> manifest, CancellationToken cancellationToken)
{
    if (uri.Scheme is not ("http" or "https")) return null;
    var origin = uri.GetLeftPart(UriPartial.Authority);
    var robots = await robotsCache.GetOrAdd(origin, _ => LoadRobots(http, new Uri(origin + "/robots.txt"), cancellationToken));
    if (!robots.Allows(uri.AbsolutePath))
    {
        manifest.Add(Manifest(school, pageType, uri, "robots-disallowed", null, today));
        return null;
    }

    var gate = hostGates.GetOrAdd(uri.Host, _ => new HostGate());
    await gate.Semaphore.WaitAsync(cancellationToken);
    try
    {
        var wait = TimeSpan.FromMilliseconds(delayMilliseconds) - (DateTimeOffset.UtcNow - gate.LastRequestAt);
        if (wait > TimeSpan.Zero) await Task.Delay(wait, cancellationToken);
        gate.LastRequestAt = DateTimeOffset.UtcNow;
        using var response = await http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            manifest.Add(Manifest(school, pageType, uri, $"http-{(int)response.StatusCode}", null, today));
            return null;
        }
        var mediaType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
        if (!mediaType.Contains("html", StringComparison.OrdinalIgnoreCase))
        {
            manifest.Add(Manifest(school, pageType, uri, "non-html", null, today));
            return null;
        }
        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        if (bytes.Length > 4_000_000)
        {
            manifest.Add(Manifest(school, pageType, uri, "too-large", null, today));
            return null;
        }
        var charset = response.Content.Headers.ContentType?.CharSet?.Trim('"');
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Encoding encoding;
        try { encoding = string.IsNullOrWhiteSpace(charset) ? Encoding.UTF8 : Encoding.GetEncoding(charset); }
        catch { encoding = Encoding.UTF8; }
        var html = encoding.GetString(bytes);
        var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        manifest.Add(Manifest(school, pageType, uri, "ok", hash, today));
        return new Page(uri.ToString(), html);
    }
    catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or UriFormatException)
    {
        manifest.Add(Manifest(school, pageType, uri, "error", null, today, ex.Message));
        return null;
    }
    finally { gate.Semaphore.Release(); }
}

static async Task<RobotsPolicy> LoadRobots(HttpClient http, Uri uri, CancellationToken cancellationToken)
{
    try
    {
        using var response = await http.GetAsync(uri, cancellationToken);
        if (!response.IsSuccessStatusCode) return RobotsPolicy.AllowAll;
        return RobotsPolicy.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
    }
    catch { return RobotsPolicy.AllowAll; }
}

static bool LooksLikeSchoolPage(string html, School school)
{
    var text = CleanText(Regex.Match(html, @"<title[^>]*>(?<value>.*?)</title>", RegexOptions.Singleline | RegexOptions.IgnoreCase).Groups["value"].Value);
    var core = school.Name.Replace("高等学校", string.Empty, StringComparison.Ordinal).Replace("中等教育学校", string.Empty, StringComparison.Ordinal);
    foreach (var prefix in new[] { school.Prefecture, "県立", "府立", "都立", "道立", "市立", "町立", "村立", "私立", "学校法人" }) core = core.Replace(prefix, string.Empty, StringComparison.Ordinal);
    core = Regex.Replace(core, @"\s+", string.Empty);
    return text.Contains(school.Name, StringComparison.OrdinalIgnoreCase) || (core.Length >= 2 && text.Replace(" ", string.Empty, StringComparison.Ordinal).Contains(core, StringComparison.OrdinalIgnoreCase));
}

static List<LinkCandidate> DiscoverLinks(Uri baseUri, string html)
{
    var result = new List<LinkCandidate>();
    foreach (Match match in Regex.Matches(html, @"<a\s+[^>]*href=[""'](?<href>[^""'#]+)[""'][^>]*>(?<text>.*?)</a>", RegexOptions.Singleline | RegexOptions.IgnoreCase))
    {
        var text = CleanText(match.Groups["text"].Value);
        if (text.Length is < 2 or > 80 || !Uri.TryCreate(baseUri, WebUtility.HtmlDecode(match.Groups["href"].Value), out var uri)) continue;
        if (!string.Equals(uri.Host, baseUri.Host, StringComparison.OrdinalIgnoreCase) || uri.AbsolutePath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)) continue;
        result.Add(new LinkCandidate(uri, text));
    }
    return result.GroupBy(x => x.Uri.ToString(), StringComparer.OrdinalIgnoreCase).Select(g => g.First()).ToList();
}

static void SelectBest(Dictionary<string, LinkCandidate> selected, string category, List<LinkCandidate> links, string[] keywords)
{
    var best = links.Select(link => new
        {
            Link = link,
            Score = keywords.Sum(keyword => link.Text.Contains(keyword, StringComparison.OrdinalIgnoreCase) ? keyword.Length : 0)
                + (keywords.Any(keyword => string.Equals(link.Text.Trim(), keyword, StringComparison.OrdinalIgnoreCase)) ? 30 : 0)
                - (Regex.IsMatch(link.Uri.AbsolutePath, @"/(topics?|news|blog|diary)/|/20[0-9]{2}/", RegexOptions.IgnoreCase) ? 25 : 0)
        })
        .Where(x => x.Score > 0).OrderByDescending(x => x.Score).ThenBy(x => x.Link.Uri.AbsolutePath.Length).FirstOrDefault();
    if (best is not null) selected[category] = best.Link;
}

static JsonArray ParseClubs(string html)
{
    var names = new HashSet<string>(StringComparer.Ordinal);
    string[] clubBlacklist = ["部活動", "クラブ活動", "運動部", "文化部", "体育系クラブ", "文化系クラブ", "各部", "本部", "学部", "全部", "外部", "内部", "一部", "入部", "退部", "部員", "部門", "部屋", "倶楽部"];
    foreach (Match element in Regex.Matches(html, @"<(?:a|li|h[2-6]|td|th|span|option)[^>]*>(?<body>.*?)</(?:a|li|h[2-6]|td|th|span|option)>", RegexOptions.Singleline | RegexOptions.IgnoreCase))
    {
        var value = CleanText(element.Groups["body"].Value);
        if (value.Length is < 2 or > 120) continue;
        foreach (var token in Regex.Split(value, @"[\s、，,／/｜|・]+"))
        {
            var name = token.Trim('・', '●', '○', '■', '□', '【', '】', '「', '」', '『', '』');
            if (!Regex.IsMatch(name, @"^[一-龠々ぁ-んァ-ヶA-Za-z0-9＆&ー（）()]{1,28}(?:部|同好会|クラブ)(?:）|\))?$")) continue;
            if (name.Length is < 3 or > 32 || clubBlacklist.Any(blocked => string.Equals(name, blocked, StringComparison.Ordinal)) || !IsPlausibleClubName(name)) continue;
            names.Add(name);
        }
    }
    var result = new JsonArray();
    foreach (var name in names.Order(StringComparer.Ordinal).Take(100)) result.Add(Club(name));
    return result;
}

static JsonObject Club(string name)
{
    string[] sportWords = ["野球", "サッカー", "テニス", "バスケット", "バレー", "陸上", "水泳", "剣道", "柔道", "空手", "弓道", "卓球", "バドミントン", "ダンス", "ラグビー", "ハンドボール", "体操", "山岳", "ワンダーフォーゲル", "ソフトボール", "フットサル", "ホッケー", "ボート", "ヨット", "レスリング", "フェンシング", "チア", "バトン", "スキー", "スケート"];
    return new JsonObject
    {
        ["name"] = name, ["category"] = sportWords.Any(name.Contains) ? "sports" : "culture",
        ["gender"] = name.Contains("男子", StringComparison.Ordinal) ? "boys" : name.Contains("女子", StringComparison.Ordinal) ? "girls" : "unknown",
        ["activityDays"] = "未確認", ["activityPlace"] = null
    };
}

static bool IsPlausibleClubName(string name)
{
    string[] articlePhrases = ["大会", "予選", "結果", "出場", "総合体育", "選手権", "展覧会", "壮行会", "施設", "見守り", "連休", "推薦対象", "常連", "について", "向けて", "まつり", "国スポ", "高校総体", "報告", "開催", "活かして"];
    return name.Length is >= 2 and <= 32
        && Regex.IsMatch(name, @"(?:部|同好会|クラブ)(?:）|\))?$")
        && !Regex.IsMatch(name, @"(?:個人|団体|女子|男子|定通|漢字仮名交じり書|15人制)の部(?:）|\))?$")
        && !articlePhrases.Any(name.Contains);
}

static JsonArray ParseEvents(string html)
{
    var result = new JsonArray();
    var seen = new HashSet<string>(StringComparer.Ordinal);
    foreach (Match match in Regex.Matches(html, @"<(?:dt|th|h[2-4]|strong)[^>]*>\s*(?<month>[0-9０-９]{1,2}月)\s*</(?:dt|th|h[2-4]|strong)>\s*<(?:dd|td|p|div)[^>]*>(?<items>.*?)</(?:dd|td|p|div)>", RegexOptions.Singleline | RegexOptions.IgnoreCase))
    {
        var month = CleanText(match.Groups["month"].Value);
        var body = Regex.Replace(match.Groups["items"].Value, @"<img[^>]*>", string.Empty, RegexOptions.IgnoreCase);
        foreach (var name in CleanText(body).Split(['、', '／', '/', '\n'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            if (name.Length is >= 2 and <= 50 && seen.Add(month + "|" + name)) result.Add(new JsonObject { ["name"] = name, ["season"] = month });
    }
    return result;
}

static JsonObject ParseUniform(string html)
{
    var text = CleanText(html);
    if (Regex.IsMatch(text, @"制服(が|は)(ありません|ございません|ない学校)|制服を(定めて|指定して)いません|制服なし"))
        return new JsonObject { ["status"] = "none", ["summary"] = "制服の指定はありません。服装に関する最新の決まりは公式情報を確認してください。" };
    if (text.Contains("標準服", StringComparison.Ordinal) && Regex.IsMatch(text, @"自由|任意|希望|着用できます|購入できます"))
        return new JsonObject { ["status"] = "optional", ["summary"] = "標準服の案内があります。着用条件などの詳細は公式情報を確認してください。" };
    if (Regex.IsMatch(text, @"本校の制服|制服を着用|指定制服|制服があります|制服紹介"))
        return new JsonObject { ["status"] = "required", ["summary"] = "制服の案内があります。着用方法や季節ごとの扱いは公式情報を確認してください。" };
    return UnknownUniform();
}

static JsonObject UnknownUniform() => new() { ["status"] = "unknown", ["summary"] = "制服に関する公式情報を自動確認できていません。学校説明会や公式資料で確認してください。" };

static List<JsonObject> ParseContacts(School school, Page page, string today)
{
    var result = new List<JsonObject>();
    foreach (Match match in Regex.Matches(page.Html, @"<a\s+[^>]*href=[""']mailto:(?<email>[^?""']+)[^""']*[""'][^>]*>(?<text>.*?)</a>", RegexOptions.Singleline | RegexOptions.IgnoreCase))
    {
        var email = WebUtility.UrlDecode(match.Groups["email"].Value).Trim().ToLowerInvariant();
        var linkText = CleanText(match.Groups["text"].Value);
        var local = email.Split('@')[0];
        var isGeneric = Regex.IsMatch(local, @"info|office|school|admin|contact|koho|nyushi|koukou|koko|jim[u]?|mail", RegexOptions.IgnoreCase) || Regex.IsMatch(linkText, @"問い合わせ|お問合せ|連絡|事務|学校|入試|広報");
        if (!isGeneric || !Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$")) continue;
        result.Add(new JsonObject
        {
            ["id"] = $"{school.Id}-email-{ShortHash(email)}", ["schoolId"] = school.Id, ["channelType"] = "email", ["value"] = email,
            ["purpose"] = Regex.IsMatch(linkText, @"入試|入学") ? "admissions" : "unknown", ["organizationManaged"] = true,
            ["sourceUrl"] = page.Url, ["verifiedAt"] = today, ["publicOnOfficialSite"] = true, ["doNotContact"] = false,
            ["notes"] = "公式サイトの公開mailtoリンクから抽出した組織窓口候補。CRM取込・送信前に人が再確認すること。"
        });
    }
    return result;
}

static List<JsonObject> DeduplicateContacts(List<JsonObject> contacts) => contacts.GroupBy(c => c["id"]!.GetValue<string>(), StringComparer.Ordinal).Select(g => g.First()).ToList();

static string CleanText(string html)
{
    var text = Regex.Replace(html, @"<(script|style|noscript|svg)[^>]*>.*?</\1>", string.Empty, RegexOptions.Singleline | RegexOptions.IgnoreCase);
    text = Regex.Replace(text, @"<br\s*/?>|</p>|</li>|</div>|</tr>", "\n", RegexOptions.IgnoreCase);
    text = Regex.Replace(text, @"<[^>]+>", string.Empty);
    text = WebUtility.HtmlDecode(text).Replace('\u3000', ' ');
    return string.Join("\n", text.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Select(line => Regex.Replace(line, @"[\t ]+", " "))).Trim();
}

static JsonObject Manifest(School school, string pageType, Uri uri, string status, string? hash, string today, string? error = null) => new()
{
    ["id"] = $"{school.Id}-{pageType}", ["schoolId"] = school.Id, ["pageType"] = pageType, ["url"] = uri.ToString(),
    ["status"] = status, ["contentHash"] = hash, ["fetchedAt"] = DateTimeOffset.Now.ToString("O"), ["verifiedAt"] = today, ["error"] = error
};

static void AddSource(JsonArray sources, string title, string url, string today) => sources.Add(new JsonObject { ["title"] = title, ["url"] = url, ["publishedAt"] = null, ["verifiedAt"] = today });
static string NormalizeUrl(string value) => value.Trim().TrimEnd('/').ToLowerInvariant();
static string ShortHash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..10].ToLowerInvariant();

static void ReplaceByProperty(JsonArray array, string property, string value, JsonObject replacement)
{
    for (var i = 0; i < array.Count; i++) if (array[i]?[property]?.GetValue<string>() == value) { array[i] = replacement; return; }
    array.Add(replacement);
}

static async Task<JsonObject> ReadOrCreateDocument(string path, string arrayName, string today)
{
    if (File.Exists(path)) return JsonNode.Parse(await File.ReadAllTextAsync(path))!.AsObject();
    return new JsonObject { ["schemaVersion"] = 1, ["asOf"] = today, [arrayName] = new JsonArray() };
}

static async Task WriteJson(string path, JsonObject document)
{
    var json = document.ToJsonString(new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
    await File.WriteAllTextAsync(path, json + Environment.NewLine, new UTF8Encoding(false));
}

static string FindRepositoryRoot()
{
    var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (directory is not null)
    {
        if (File.Exists(Path.Combine(directory.FullName, "wakaroute-web.csproj"))) return directory.FullName;
        directory = directory.Parent;
    }
    throw new DirectoryNotFoundException("リポジトリを見つけられませんでした。");
}

sealed record School(string Id, string Name, string Prefecture, string OfficialUrl)
{
    public static School From(JsonObject node) => new(node["id"]!.GetValue<string>(), node["name"]!.GetValue<string>(), node["prefecture"]!.GetValue<string>(), node["officialUrl"]?.GetValue<string>() ?? string.Empty);
}
sealed record Page(string Url, string Html);
sealed record LinkCandidate(Uri Uri, string Text);
sealed record CollectionResult(School School, JsonObject? Life, List<JsonObject> Contacts, List<JsonObject> Pages);

sealed class HostGate
{
    public SemaphoreSlim Semaphore { get; } = new(1, 1);
    public DateTimeOffset LastRequestAt { get; set; } = DateTimeOffset.MinValue;
}

sealed record RobotsPolicy(IReadOnlyList<string> Disallowed)
{
    public static RobotsPolicy AllowAll { get; } = new([]);
    public bool Allows(string path) => !Disallowed.Any(rule => rule == "/" || (rule.Length > 1 && path.StartsWith(rule, StringComparison.Ordinal)));
    public static RobotsPolicy Parse(string text)
    {
        var active = false;
        var rules = new List<string>();
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Split('#')[0].Trim();
            if (line.StartsWith("User-agent:", StringComparison.OrdinalIgnoreCase)) active = line[(line.IndexOf(':') + 1)..].Trim() == "*";
            else if (active && line.StartsWith("Disallow:", StringComparison.OrdinalIgnoreCase))
            {
                var value = line[(line.IndexOf(':') + 1)..].Trim();
                if (!string.IsNullOrEmpty(value)) rules.Add(value.Split('*')[0]);
            }
        }
        return new RobotsPolicy(rules);
    }
}

sealed class Options
{
    public string? Root { get; private set; }
    public string? PrivateDirectory { get; private set; }
    public List<string> SchoolIds { get; } = [];
    public List<string> Prefectures { get; } = [];
    public bool All { get; private set; }
    public bool AcknowledgeFactsOnly { get; private set; }
    public bool RefreshExisting { get; private set; }
    public bool SanitizeOnly { get; private set; }
    public int DelayMilliseconds { get; private set; } = 1200;
    public int Concurrency { get; private set; } = 8;
    public int? MaxSchools { get; private set; }
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
                case "--prefectures": result.Prefectures.AddRange(value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)); i++; break;
                case "--all": result.All = bool.Parse(value); i++; break;
                case "--acknowledge-facts-only": result.AcknowledgeFactsOnly = bool.Parse(value); i++; break;
                case "--refresh-existing": result.RefreshExisting = bool.Parse(value); i++; break;
                case "--sanitize-only": result.SanitizeOnly = bool.Parse(value); i++; break;
                case "--delay-ms": result.DelayMilliseconds = int.Parse(value); i++; break;
                case "--concurrency": result.Concurrency = int.Parse(value); i++; break;
                case "--max-schools": result.MaxSchools = int.Parse(value); i++; break;
            }
        }
        if (!result.SanitizeOnly && !result.All && result.SchoolIds.Count == 0) throw new ArgumentException("--all true または --school-ids を指定してください。");
        if (result.DelayMilliseconds < 1000) throw new ArgumentOutOfRangeException(nameof(DelayMilliseconds), "同一ホストへの取得間隔は1000ms以上にしてください。");
        if (result.Concurrency is < 1 or > 12) throw new ArgumentOutOfRangeException(nameof(Concurrency), "並列数は1〜12にしてください。");
        return result;
    }
}
