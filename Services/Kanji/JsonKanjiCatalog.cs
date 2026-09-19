using System.Globalization;
using System.Text.Json;
using wakaroute_web.Models;

namespace wakaroute_web.Services.Kanji;

public sealed class JsonKanjiCatalog : IKanjiCatalog
{
    private readonly Dictionary<string, KanjiEntry> _itemsById;

    public JsonKanjiCatalog(IHostEnvironment environment)
    {
        var path = Path.Combine(environment.ContentRootPath, "Data", "Kanji", "kanji.json");
        if (!File.Exists(path))
            throw new InvalidOperationException($"Kanji dataset file was not found: {path}");

        Dataset = JsonSerializer.Deserialize<KanjiDataset>(
            File.ReadAllText(path),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException($"Kanji dataset file is empty or invalid: {path}");

        Validate(Dataset);
        _itemsById = Dataset.Items.ToDictionary(item => item.Id, StringComparer.OrdinalIgnoreCase);
    }

    public KanjiDataset Dataset { get; }

    public KanjiEntry? GetById(string id) =>
        string.IsNullOrWhiteSpace(id) ? null : _itemsById.GetValueOrDefault(id.Trim());

    public KanjiSearchResult Search(
        int? recommendedGrade,
        string? query,
        int page,
        int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 500);
        var normalizedQuery = Normalize(query);

        IEnumerable<KanjiEntry> items = Dataset.Items;
        if (recommendedGrade is not null)
            items = items.Where(item => item.RecommendedGrade == recommendedGrade);
        if (normalizedQuery.Length > 0)
        {
            items = items.Where(item =>
                Normalize(item.Character).Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase) ||
                item.OnReadings.Any(reading => Normalize(reading).Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase)) ||
                item.KunReadings.Any(reading => Normalize(reading).Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase)));
        }

        var matches = items.OrderBy(item => item.Sequence).ToArray();
        var totalPages = Math.Max(1, (int)Math.Ceiling(matches.Length / (double)pageSize));
        page = Math.Min(page, totalPages);

        return new KanjiSearchResult(
            matches.Skip((page - 1) * pageSize).Take(pageSize).ToArray(),
            matches.Length,
            page,
            pageSize,
            totalPages);
    }

    private static string Normalize(string? value) => (value ?? string.Empty)
        .Trim()
        .Replace(".", string.Empty, StringComparison.Ordinal)
        .Replace("・", string.Empty, StringComparison.Ordinal)
        .Replace(" ", string.Empty, StringComparison.Ordinal)
        .Replace("　", string.Empty, StringComparison.Ordinal);

    private static void Validate(KanjiDataset dataset)
    {
        if (dataset.SchemaVersion != 1)
            throw new InvalidOperationException("Unsupported kanji dataset schema version.");
        if (!DateOnly.TryParse(dataset.AsOf, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            throw new InvalidOperationException("Kanji dataset asOf must be an ISO date.");
        if (dataset.Items.Count != 1110)
            throw new InvalidOperationException("Kanji dataset must contain exactly 1,110 entries.");
        if (dataset.Assignment.OfficialGradeAssignment)
            throw new InvalidOperationException("WakaRoute grade assignment must not be marked as official.");

        var expectedCounts = new Dictionary<int, int> { [1] = 350, [2] = 400, [3] = 360 };
        foreach (var expected in expectedCounts)
        {
            var actual = dataset.Items.Count(item => item.RecommendedGrade == expected.Key);
            if (actual != expected.Value)
                throw new InvalidOperationException($"Grade {expected.Key} must contain {expected.Value} kanji, but found {actual}.");
        }

        var duplicateId = dataset.Items.GroupBy(item => item.Id, StringComparer.OrdinalIgnoreCase).FirstOrDefault(group => group.Count() > 1);
        var duplicateCharacter = dataset.Items.GroupBy(item => item.Character, StringComparer.Ordinal).FirstOrDefault(group => group.Count() > 1);
        var duplicateSequence = dataset.Items.GroupBy(item => item.Sequence).FirstOrDefault(group => group.Count() > 1);
        if (duplicateId is not null || duplicateCharacter is not null || duplicateSequence is not null)
            throw new InvalidOperationException("Kanji dataset contains duplicate IDs, characters, or sequence numbers.");
        if (!dataset.Items.Select(item => item.Sequence).SequenceEqual(Enumerable.Range(1, 1110)))
            throw new InvalidOperationException("Kanji dataset sequence must be contiguous from 1 to 1,110.");
        if (dataset.Items.Any(item => item.OfficialStage != "junior-high" || item.StrokeCount <= 0))
            throw new InvalidOperationException("Kanji dataset contains an invalid stage or stroke count.");
    }
}
