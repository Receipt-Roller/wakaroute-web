using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using wakaroute_web.Models;
using wakaroute_web.Services.Manabu2;

namespace wakaroute_web.Services.UnderstandingMaps;

public sealed class Manabu2UnderstandingMapCatalog : IUnderstandingMapCatalog
{
    private const string CacheKey = "manabu2-understanding-maps-v1";

    private readonly IReadOnlyDictionary<string, UnderstandingMapViewModel> _templates;
    private readonly Manabu2CatalogClient _client;
    private readonly Manabu2Options _options;
    private readonly IMemoryCache _cache;
    private readonly ILogger<Manabu2UnderstandingMapCatalog> _logger;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public Manabu2UnderstandingMapCatalog(
        IEnumerable<IUnderstandingMapProvider> providers,
        Manabu2CatalogClient client,
        IOptions<Manabu2Options> options,
        IMemoryCache cache,
        ILogger<Manabu2UnderstandingMapCatalog> logger)
    {
        _templates = providers.ToDictionary(
            provider => provider.SubjectId,
            provider => provider.GetMap(),
            StringComparer.OrdinalIgnoreCase);
        _client = client;
        _options = options.Value;
        _cache = cache;
        _logger = logger;
    }

    public async Task<UnderstandingMapViewModel> GetMapAsync(
        string subjectId,
        CancellationToken cancellationToken = default)
    {
        if (!_templates.TryGetValue(subjectId, out var template))
        {
            throw new KeyNotFoundException($"Unknown understanding map subject: {subjectId}");
        }

        if (!_client.IsConfigured)
        {
            _logger.LogInformation("Manabu2 is not configured; using the local understanding map catalog.");
            return template;
        }

        if (_cache.TryGetValue(CacheKey, out IReadOnlyDictionary<string, UnderstandingMapViewModel>? cachedMaps) &&
            cachedMaps is not null)
        {
            return cachedMaps[subjectId];
        }

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            if (_cache.TryGetValue(CacheKey, out cachedMaps) && cachedMaps is not null)
            {
                return cachedMaps[subjectId];
            }

            try
            {
                var paths = await _client.GetPathsAsync(cancellationToken);
                var fetchedAt = DateTimeOffset.UtcNow;
                var liveMaps = _templates.ToDictionary(
                    pair => pair.Key,
                    pair => Merge(pair.Value, paths, fetchedAt),
                    StringComparer.OrdinalIgnoreCase);

                var cacheMinutes = Math.Clamp(_options.CacheMinutes, 1, 60);
                _cache.Set(CacheKey, liveMaps, TimeSpan.FromMinutes(cacheMinutes));

                _logger.LogInformation(
                    "Loaded {PathCount} learning paths and {CourseCount} courses from Manabu2.",
                    paths.Count,
                    paths.Sum(path => path.Courses.Count));

                return liveMaps[subjectId];
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogWarning(exception, "Could not load Manabu2; using the local understanding map catalog.");
                return template;
            }
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private static UnderstandingMapViewModel Merge(
        UnderstandingMapViewModel template,
        IReadOnlyList<Manabu2Path> paths,
        DateTimeOffset fetchedAt)
    {
        var pathsByName = paths.ToDictionary(path => path.Name, StringComparer.Ordinal);

        var areas = template.Areas.Select(area =>
        {
            if (!pathsByName.TryGetValue(area.Name, out var path))
            {
                return area;
            }

            var nodesByTitle = area.Nodes.ToDictionary(node => node.Title, StringComparer.Ordinal);
            var nodes = path.Courses
                .Where(course => nodesByTitle.ContainsKey(course.Title))
                .Select(course =>
                {
                    var node = nodesByTitle[course.Title];
                    return node with
                    {
                        Title = course.Title,
                        Summary = string.IsNullOrWhiteSpace(course.Description) ? node.Summary : course.Description,
                        CourseId = course.Id,
                        PathId = path.Id
                    };
                })
                .ToArray();

            return area with
            {
                Description = string.IsNullOrWhiteSpace(path.Description) ? area.Description : path.Description,
                Nodes = nodes.Length > 0 ? nodes : area.Nodes
            };
        }).ToArray();

        return template with
        {
            Areas = areas,
            IsLiveCatalog = true,
            CatalogFetchedAt = fetchedAt
        };
    }
}
