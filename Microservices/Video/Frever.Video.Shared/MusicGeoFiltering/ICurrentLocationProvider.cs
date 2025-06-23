using System.Threading.Tasks;

namespace Frever.Videos.Shared.MusicGeoFiltering;

public interface ICurrentLocationProvider
{
    public static readonly LocationInfo UnknownLocationFakeIso3Code = new() {CountryIso3Code = "404", Lat = 0, Lon = 0};

    Task<LocationInfo> Get();
}

public class LocationInfo
{
    public string CountryIso3Code { get; set; }

    public decimal Lon { get; set; }

    public decimal Lat { get; set; }
}

public class IAmInSwedenLocationProvider : ICurrentLocationProvider
{
    public Task<LocationInfo> Get()
    {
        return Task.FromResult(new LocationInfo {CountryIso3Code = "swe", Lat = 59.334591m, Lon = 18.063240m});
    }
}