using System;
using System.Threading.Tasks;
using Frever.AdminService.Core.Services.AssetTransaction;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Frever.AdminService.Api.Controllers;

[ApiController]
[Route("api/transaction")]
public class AssetStoreTransactionController(IAssetStoreTransactionService service) : ControllerBase
{
    private readonly IAssetStoreTransactionService _service = service ?? throw new ArgumentNullException(nameof(service));

    /// <summary>
    ///     Increases currency supply for users by group ids
    /// </summary>
    /// <param name="currencySupplyInfo"></param>
    /// <returns>Returns a list of group ids</returns>
    [HttpPost]
    [Route("increase-currency-supply")]
    [ProducesResponseType(typeof(long[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> IncreaseUsersCurrencyAsync([FromBody] IncreaseCurrencySupplyInfo currencySupplyInfo)
    {
        var groupIds = await _service.IncreaseUsersCurrencyAsync(currencySupplyInfo).ConfigureAwait(false);

        return Ok(groupIds);
    }
}