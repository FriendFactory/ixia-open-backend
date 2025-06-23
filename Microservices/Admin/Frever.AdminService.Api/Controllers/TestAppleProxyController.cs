using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Common.Infrastructure;
using Common.Infrastructure.InternalRequest;
using Microsoft.AspNetCore.Mvc;

namespace Frever.AdminService.Api.Controllers;

[Route("api/apple")]
[ApiController]
[InternalEndpoint]
public class TestAppleProxyController(IHttpClientFactory httpClientFactory) : ControllerBase
{
    private static readonly Dictionary<string, HttpMethod> KnownHttpMethods =
        new(StringComparer.OrdinalIgnoreCase)
        {
            {"get", HttpMethod.Get},
            {"post", HttpMethod.Post},
            {"put", HttpMethod.Put},
            {"patch", HttpMethod.Patch}
        };

    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));

    [Route("{method}/{*url}", Order = 5)]
    public async Task<IActionResult> ProxyAppleRequest([FromRoute] string method, [FromRoute] string url)
    {
        var appleApiClient = new AppleApiClient(
            _httpClientFactory,
            "caaa3a9a-af53-44e8-8302-092884afa587",
            "Y988D2ZBY7", // Admin
            Convert.FromBase64String(
                "LS0tLS1CRUdJTiBQUklWQVRFIEtFWS0tLS0tCk1JR0hBZ0VBTUJNR0J5cUdTTTQ5QWdFR0NDcUdTTTQ5QXdFSEJHMHdhd0lCQVFRZ25uMFJXQ2RuM2xkdVZzM0IKWEo2NllOSi9qdGFROEp6WFNjbE5pNjFjRE1laFJBTkNBQVE0Nk91bXdvM2lCYS9uWm1RdG01RnRxcWplalpsdwovZ2VHQk5haC9JR0xNRXFOWHd5L0V4K2VLU1ZZN1o2ZzU5Sngzb2ZUVldOS2N2amUzYkovd3lUSQotLS0tLUVORCBQUklWQVRFIEtFWS0tLS0tCg=="
            )
            // "AZ2WM43R76", // Finance
            // Convert.FromBase64String(
            //     "LS0tLS1CRUdJTiBQUklWQVRFIEtFWS0tLS0tCk1JR0hBZ0VBTUJNR0J5cUdTTTQ5QWdFR0NDcUdTTTQ5QXdFSEJHMHdhd0lCQVFRZ2tIbUhyaS90WVpXK2ZIdnYKTWpzcFA5L1NxKytiZVJSY25BTEVoZzkydmtDaFJBTkNBQVJodXovM25oOVY5d29kTXhrVTFVTEVaVnU3SmxlSgpNcDdxQUtsaEt5MTZjOHlLaVFaeGpWemZiclpTZ1pvL3h2bHhKQm9lUmFFMnNZK1JjQkJkcEs5ZgotLS0tLUVORCBQUklWQVRFIEtFWS0tLS0tCg=="
            // )
            // "P9B3T3L4AN", // Sales and report
            // Convert.FromBase64String(
            //     "LS0tLS1CRUdJTiBQUklWQVRFIEtFWS0tLS0tCk1JR0hBZ0VBTUJNR0J5cUdTTTQ5QWdFR0NDcUdTTTQ5QXdFSEJHMHdhd0lCQVFRZzExNWZrbzc1UTdvcW5vU0EKNGo5RWFxWXA3TmhyOTlyRjA5T3o3dHhkbTNPaFJBTkNBQVIxOW82TjQ3ZHpERG1XODNGREZydW82L1dDb3ZSOAo5SXh2dFdET2wzeCtOTll0cmJUMFloQVIvZTJsVnphbDdCVCt3Nm9wTUJJQnF2R0ZIVlJoTHpyagotLS0tLUVORCBQUklWQVRFIEtFWS0tLS0tCg=="
            // )
            // "3Y6M534C5N",
            // Convert.FromBase64String(
            //     "LS0tLS1CRUdJTiBQUklWQVRFIEtFWS0tLS0tCk1JR0hBZ0VBTUJNR0J5cUdTTTQ5QWdFR0NDcUdTTTQ5QXdFSEJHMHdhd0lCQVFRZ3JBUmdrUkE3bzg0NU0xZG4KU1NpTlV6QkNDelBIaFk2a01YTnJ5TUtuTjVlaFJBTkNBQVJaQ0YxNHI3V0ZtK0gvbThuVjl2YU5PS1JUWEt3VgpEWDdqYUN3WnlQbzBFeUEyTmlvNGpGNmhjbm0yV3lGd3g5Qlp0RzFHZUZhbFU2WUhBTjZKUTQwbgotLS0tLUVORCBQUklWQVRFIEtFWS0tLS0tCg=="
            // )
        );

        if (!KnownHttpMethods.TryGetValue(method, out var httpMethod))
            return BadRequest("Invalid HTTP method");

        var uri = new Uri($"https://{url}{Request.QueryString}");

        var response = await appleApiClient.CallApple(httpMethod, uri);

        var body = await response.Content.ReadAsStringAsync();

        return new ContentResult {Content = body, StatusCode = (int) response.StatusCode};
    }

    [Route("download/{method}/{*url}", Order = 1)]
    public async Task<IActionResult> ProxyDownloadAppleRequest([FromRoute] string method, [FromRoute] string url)
    {
        var appleApiClient = new AppleApiClient(
            _httpClientFactory,
            "caaa3a9a-af53-44e8-8302-092884afa587",
            "Y988D2ZBY7", // Admin
            Convert.FromBase64String(
                "LS0tLS1CRUdJTiBQUklWQVRFIEtFWS0tLS0tCk1JR0hBZ0VBTUJNR0J5cUdTTTQ5QWdFR0NDcUdTTTQ5QXdFSEJHMHdhd0lCQVFRZ25uMFJXQ2RuM2xkdVZzM0IKWEo2NllOSi9qdGFROEp6WFNjbE5pNjFjRE1laFJBTkNBQVE0Nk91bXdvM2lCYS9uWm1RdG01RnRxcWplalpsdwovZ2VHQk5haC9JR0xNRXFOWHd5L0V4K2VLU1ZZN1o2ZzU5Sngzb2ZUVldOS2N2amUzYkovd3lUSQotLS0tLUVORCBQUklWQVRFIEtFWS0tLS0tCg=="
            )
            // "AZ2WM43R76", // Finance
            // Convert.FromBase64String(
            //     "LS0tLS1CRUdJTiBQUklWQVRFIEtFWS0tLS0tCk1JR0hBZ0VBTUJNR0J5cUdTTTQ5QWdFR0NDcUdTTTQ5QXdFSEJHMHdhd0lCQVFRZ2tIbUhyaS90WVpXK2ZIdnYKTWpzcFA5L1NxKytiZVJSY25BTEVoZzkydmtDaFJBTkNBQVJodXovM25oOVY5d29kTXhrVTFVTEVaVnU3SmxlSgpNcDdxQUtsaEt5MTZjOHlLaVFaeGpWemZiclpTZ1pvL3h2bHhKQm9lUmFFMnNZK1JjQkJkcEs5ZgotLS0tLUVORCBQUklWQVRFIEtFWS0tLS0tCg=="
            // )
            // "P9B3T3L4AN", // Sales and report
            // Convert.FromBase64String(
            //     "LS0tLS1CRUdJTiBQUklWQVRFIEtFWS0tLS0tCk1JR0hBZ0VBTUJNR0J5cUdTTTQ5QWdFR0NDcUdTTTQ5QXdFSEJHMHdhd0lCQVFRZzExNWZrbzc1UTdvcW5vU0EKNGo5RWFxWXA3TmhyOTlyRjA5T3o3dHhkbTNPaFJBTkNBQVIxOW82TjQ3ZHpERG1XODNGREZydW82L1dDb3ZSOAo5SXh2dFdET2wzeCtOTll0cmJUMFloQVIvZTJsVnphbDdCVCt3Nm9wTUJJQnF2R0ZIVlJoTHpyagotLS0tLUVORCBQUklWQVRFIEtFWS0tLS0tCg=="
            // )
            // "3Y6M534C5N",
            // Convert.FromBase64String(
            //     "LS0tLS1CRUdJTiBQUklWQVRFIEtFWS0tLS0tCk1JR0hBZ0VBTUJNR0J5cUdTTTQ5QWdFR0NDcUdTTTQ5QXdFSEJHMHdhd0lCQVFRZ3JBUmdrUkE3bzg0NU0xZG4KU1NpTlV6QkNDelBIaFk2a01YTnJ5TUtuTjVlaFJBTkNBQVJaQ0YxNHI3V0ZtK0gvbThuVjl2YU5PS1JUWEt3VgpEWDdqYUN3WnlQbzBFeUEyTmlvNGpGNmhjbm0yV3lGd3g5Qlp0RzFHZUZhbFU2WUhBTjZKUTQwbgotLS0tLUVORCBQUklWQVRFIEtFWS0tLS0tCg=="
            // )
        );

        if (!KnownHttpMethods.TryGetValue(method, out var httpMethod))
            return BadRequest("Invalid HTTP method");

        var uri = new Uri($"https://{url}{Request.QueryString}");

        var response = await appleApiClient.CallAppleDownload(httpMethod, uri);

        var body = await response.Content.ReadAsByteArrayAsync();

        return new FileContentResult(body, response.Content.Headers.ContentType.ToString());
    }
}