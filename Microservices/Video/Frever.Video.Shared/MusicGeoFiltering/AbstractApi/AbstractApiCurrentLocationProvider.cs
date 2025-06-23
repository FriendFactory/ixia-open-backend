using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Common.Infrastructure.Caching.CacheKeys;
using Frever.Protobuf;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Frever.Videos.Shared.MusicGeoFiltering.AbstractApi;

public class AbstractApiCurrentLocationProvider : ICurrentLocationProvider
{
    private const int Tryouts = 10;

    private readonly AbstractApiConfiguration _configuration;
    private readonly CountryCodeLookup _countryCodeLookup;
    private readonly HttpClient _httpClient;
    private readonly IIpAddressProvider _ipAddressProvider;
    private readonly ILogger _log;
    private readonly IDatabase _redis;

    private LocationInfo _locationIso3Code;


    public AbstractApiCurrentLocationProvider(
        AbstractApiConfiguration configuration,
        IConnectionMultiplexer redisConnection,
        CountryCodeLookup countryCodeLookup,
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory,
        IIpAddressProvider ipAddressProvider
    )
    {
        if (ipAddressProvider == null)
            throw new ArgumentNullException(nameof(ipAddressProvider));
        ArgumentNullException.ThrowIfNull(redisConnection);
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _countryCodeLookup = countryCodeLookup ?? throw new ArgumentNullException(nameof(countryCodeLookup));
        _ipAddressProvider = ipAddressProvider;
        _redis = redisConnection.GetDatabase();
        _httpClient = httpClientFactory.CreateClient();
        _log = loggerFactory.CreateLogger("Frever.AbstractLocationDetection");
    }

    public async Task<LocationInfo> Get()
    {
        if (_locationIso3Code != null)
            return _locationIso3Code;

        var ipAddress = _ipAddressProvider.GetIpAddressOfConnectedClient();

        if (ipAddress.Equals(IPAddress.Loopback) && Debugger.IsAttached)
            return new LocationInfo {CountryIso3Code = "swe", Lat = 59.334591m, Lon = 18.063240m};

        _log.LogInformation("IP: {Ip}", ipAddress);

        var cachedLocation = await _redis.StringGetAsync(IpLookupCacheKey(ipAddress));
        if (cachedLocation != RedisValue.Null)
        {
            var info = ProtobufConvert.DeserializeObject<LocationInfo>(cachedLocation);
            _locationIso3Code = info;
            if (_locationIso3Code.CountryIso3Code.Length == 2)
                _locationIso3Code.CountryIso3Code = (await _countryCodeLookup.ToIso3(new[] {_locationIso3Code.CountryIso3Code})).First();

            _log.LogInformation(
                "Location for IP={ip} is got from cache: ISO={Iso3Code} Lon={lon} Lat={lat}",
                ipAddress,
                _locationIso3Code.CountryIso3Code,
                _locationIso3Code.Lon,
                _locationIso3Code.Lat
            );

            return _locationIso3Code;
        }

        var location = await LookupIpViaAbstractApi(ipAddress);

        _log.LogInformation("Country detected via IP: {CountryCode}, lat={} lon={}", location.CountryIso3Code, location.Lat, location.Lon);

        if (!(await _countryCodeLookup.GetCountryLookup()).TryGetValue(location.CountryIso3Code, out var countryIso3Code))
            throw new InvalidOperationException($"Unknown country code {location.CountryIso3Code}");

        location.CountryIso3Code = countryIso3Code;

        _locationIso3Code = location;
        _redis.StringSet(IpLookupCacheKey(ipAddress), ProtobufConvert.SerializeObject(location));
        _redis.KeyExpire(IpLookupCacheKey(ipAddress), TimeSpan.FromMinutes(30));

        return _locationIso3Code;
    }

    private static string IpLookupCacheKey(IPAddress ip)
    {
        return $"ip-to-location::v2::{ip}".FreverVersionedCache();
    }

    private async Task<LocationInfo> LookupIpViaAbstractApi(IPAddress ipAddress)
    {
        ArgumentNullException.ThrowIfNull(ipAddress);

        var url =
            $"https://ipgeolocation.abstractapi.com/v1/?api_key={_configuration.ApiKey}&ip_address={ipAddress}&fields=country_code,latitude,longitude";

        for (var tryout = 0; tryout < Tryouts; tryout++)
            try
            {
                using var response = await _httpClient.GetAsync(url);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<AstractApiIPLookupResponse>(body);

                    _log.LogInformation("AbstractAPI requested to get location by IP");

                    return new LocationInfo {Lat = data.Lat ?? 0, Lon = data.Lon ?? 0, CountryIso3Code = data.CountryCode};
                }
                else
                {
                    var body = await response.Content.ReadAsStringAsync();
                    _log.LogWarning("{n} of {total}: Error lookup country by IP: {err}", tryout, Tryouts, body);
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error determine location by IP, tryout {n} of {all}", tryout + 1, Tryouts);
                await Task.Delay(100);
            }

        throw new InvalidOperationException("Error determine user location");
    }
}

public class AstractApiIPLookupResponse
{
    [JsonProperty("country_code")] public string CountryCode { get; set; }

    [JsonProperty("latitude")] public decimal? Lat { get; set; }

    [JsonProperty("longitude")] public decimal? Lon { get; set; }
}