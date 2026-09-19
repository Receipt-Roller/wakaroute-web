using System.Globalization;
using System.Text.Json;
using wakaroute_web.Models;

namespace wakaroute_web.Services.EnglishVocabulary;

public sealed class JsonEnglishVocabularyCatalog : IEnglishVocabularyCatalog
{
    private readonly Dictionary<string, EnglishWord> _itemsById;

    public JsonEnglishVocabularyCatalog(IHostEnvironment environment)
    {
        var path = Path.Combine(environment.ContentRootPath, "Data", "EnglishVocabulary", "english-words.json");
        if (!File.Exists(path)) throw new InvalidOperationException($"English vocabulary dataset was not found: {path}");
        Dataset = JsonSerializer.Deserialize<EnglishVocabularyDataset>(File.ReadAllText(path), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException($"English vocabulary dataset is invalid: {path}");
        Validate(Dataset);
        _itemsById = Dataset.Items.ToDictionary(item => item.Id, StringComparer.OrdinalIgnoreCase);
    }

    public EnglishVocabularyDataset Dataset { get; }

    public EnglishWord? GetById(string id) => string.IsNullOrWhiteSpace(id) ? null : _itemsById.GetValueOrDefault(id.Trim());

    public EnglishWordSearchResult Search(string? stage, int? recommendedGrade, string? query, int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 500);
        var normalizedQuery = query?.Trim() ?? string.Empty;
        IEnumerable<EnglishWord> items = Dataset.Items;
        if (!string.IsNullOrWhiteSpace(stage)) items = items.Where(item => item.Stage.Equals(stage.Trim(), StringComparison.OrdinalIgnoreCase));
        if (recommendedGrade is not null) items = items.Where(item => item.RecommendedGrade == recommendedGrade);
        if (normalizedQuery.Length > 0)
            items = items.Where(item => item.Lemma.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase) || item.MeaningsJa.Any(meaning => meaning.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase)));

        var matches = items.OrderBy(item => item.Sequence).ToArray();
        var totalPages = Math.Max(1, (int)Math.Ceiling(matches.Length / (double)pageSize));
        page = Math.Min(page, totalPages);
        return new EnglishWordSearchResult(matches.Skip((page - 1) * pageSize).Take(pageSize).ToArray(), matches.Length, page, pageSize, totalPages);
    }

    private static void Validate(EnglishVocabularyDataset dataset)
    {
        if (dataset.SchemaVersion != 1) throw new InvalidOperationException("Unsupported English vocabulary schema version.");
        if (!DateOnly.TryParse(dataset.AsOf, CultureInfo.InvariantCulture, DateTimeStyles.None, out _)) throw new InvalidOperationException("English vocabulary asOf must be an ISO date.");
        if (dataset.Assignment.OfficialGradeAssignment) throw new InvalidOperationException("WakaRoute grade assignment must not be marked official.");
        if (dataset.Items.Count != 2450) throw new InvalidOperationException("English vocabulary dataset must contain 2,450 entries.");
        if (dataset.Items.Count(item => item.Stage == "elementary-review" && item.RecommendedGrade is null) != 650) throw new InvalidOperationException("Elementary review must contain 650 entries.");
        for (var grade = 1; grade <= 3; grade++)
            if (dataset.Items.Count(item => item.Stage == "junior-high" && item.RecommendedGrade == grade) != 600) throw new InvalidOperationException($"Grade {grade} must contain 600 entries.");
        if (dataset.Items.Select(item => item.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() != 2450 || dataset.Items.Select(item => item.Lemma).Distinct(StringComparer.OrdinalIgnoreCase).Count() != 2450)
            throw new InvalidOperationException("English vocabulary IDs and lemmas must be unique.");
        if (!dataset.Items.Select(item => item.Sequence).SequenceEqual(Enumerable.Range(1, 2450))) throw new InvalidOperationException("English vocabulary sequence must be contiguous.");
    }
}
