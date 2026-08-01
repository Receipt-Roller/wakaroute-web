using wakaroute_web.Models;

namespace wakaroute_web.Services.UnderstandingMaps;

public interface IUnderstandingMapProvider
{
    string SubjectId { get; }

    UnderstandingMapViewModel GetMap();
}
