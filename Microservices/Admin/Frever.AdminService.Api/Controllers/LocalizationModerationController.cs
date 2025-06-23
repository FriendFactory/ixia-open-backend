using System;
using System.Threading.Tasks;
using Frever.AdminService.Core.Services.Localizations;
using Frever.AdminService.Core.Utils;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

#pragma warning disable CA2007

namespace Frever.AdminService.Api.Controllers;

[ApiController]
[Route("api/localization/moderation")]
public class LocalizationModerationController(ILocalizationModerationService localizationService) : ControllerBase
{
    private readonly ILocalizationModerationService _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));

    [HttpGet]
    public async Task<IActionResult> GetLocalization(
        ODataQueryOptions<LocalizationDto> options,
        [FromQuery] string isoCode,
        [FromQuery] string value
    )
    {
        var result = await _localizationService.GetLocalization(options, isoCode, value);

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> SaveLocalization([FromBody] LocalizationDto model)
    {
        await _localizationService.SaveLocalization(model);

        return NoContent();
    }

    [HttpDelete]
    [Route("{key}")]
    public async Task<IActionResult> DeleteLocalizationByKey([FromRoute] string key)
    {
        await _localizationService.DeleteLocalizationByKey(key);

        return NoContent();
    }

    [HttpPost]
    [Route("export")]
    public async Task<IActionResult> ExportLocalizationToCsv([FromBody] string[] keys)
    {
        var result = await _localizationService.ExportLocalizationToCsv(keys);

        return File(result, "text/csv");
    }

    [HttpPost]
    [Route("import/{type}")]
    public async Task<IActionResult> ImportLocalizationFromCsv([FromForm] IFormFile file, [FromRoute] ImportType type)
    {
        await _localizationService.ImportLocalizationFromCsv(file, type);

        return NoContent();
    }
}