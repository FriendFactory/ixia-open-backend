using System.Threading.Tasks;

namespace Frever.Client.Core.Features.CommercialMusic;

public interface IMusicSearchService
{
    Task<TrackInfo[]> Search(string q, int skip, int take);
}