using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Common.Infrastructure.RequestId;
using Frever.Client.Core.Features.Localizations;
using Frever.ClientService.Contract.Locales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Frever.ClientService.Api.Features.Localizations;

[AllowAnonymous]
[Route("api")]
public class LocalizationController : ControllerBase
{
    private readonly ILocalizationService _localizationService;

    public LocalizationController(ILocalizationService localizationService)
    {
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
    }

    [HttpGet]
    [Route("country")]
    [ProducesResponseType(typeof(CountryDto[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCountryList()
    {
        var result = await _localizationService.GetCountryList();

        return Ok(result);
    }

    [HttpGet]
    [Route("country/{iso}")]
    [ProducesResponseType(typeof(CountryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCountryByIso([FromRoute] string iso)
    {
        var list = await _localizationService.GetCountryList();

        iso = iso.Trim().ToLowerInvariant();

        var result = list.FirstOrDefault(
            r => StringComparer.OrdinalIgnoreCase.Equals(r.Iso2Code, iso) || StringComparer.OrdinalIgnoreCase.Equals(r.Iso3Code, iso)
        );

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpGet]
    [Route("language/crew")]
    [ProducesResponseType(typeof(LanguageDto[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCrewLanguages()
    {
        var result = await _localizationService.GetCrewLanguages();

        return Ok(result);
    }

    [HttpGet]
    [AllowAnonymous]
    [Route("localization/{isoCode}/for-start-up")]
    public async Task<IActionResult> GetStartUpLocalization(string isoCode)
    {
        var result = await _localizationService.GetStartUpLocalization(isoCode);

        return File(Encoding.UTF8.GetBytes(result), "text/csv");
    }

    [HttpGet]
    [AllowAnonymous]
    [Route("localization/{isoCode}")]
    public async Task<IActionResult> GetLocalization(string isoCode)
    {
        var version = Request.Headers[HttpContextHeaderAccessor.XLocalizationVersion].FirstOrDefault();

        var result = await _localizationService.GetLocalization(isoCode, version);
        if (!result.IsModified)
            return StatusCode((int) HttpStatusCode.NotModified);

        Response.Headers.Append(HttpContextHeaderAccessor.XLocalizationVersion, result.Response.Version);

        return File(Encoding.UTF8.GetBytes(result.Response.Value), "text/csv", $"{result.Response.Version}.csv");
    }
}