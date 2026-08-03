using wakaroute_web.Models;

namespace wakaroute_web.Services.UnderstandingMaps;

public interface IUnderstandingMapCatalog
{
    Task<UnderstandingMapViewModel> GetMapAsync(
        string subjectId,
        CancellationToken cancellationToken = default);
}
