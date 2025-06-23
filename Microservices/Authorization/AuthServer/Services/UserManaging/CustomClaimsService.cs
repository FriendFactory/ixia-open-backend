using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AuthServer.Utils;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Validation;
using Microsoft.Extensions.Logging;

namespace AuthServer.Services.UserManaging;

internal class CustomClaimsService(IProfileService profile, ILogger<DefaultClaimsService> logger, IUserAccountService userAccountService)
    : DefaultClaimsService(profile, logger)
{
    public override async Task<IEnumerable<Claim>> GetAccessTokenClaimsAsync(
        ClaimsPrincipal subject,
        Resources resources,
        ValidatedRequest request
    )
    {
        var authClaims = await base.GetAccessTokenClaimsAsync(subject, resources, request);

        var userId = ((ClaimsIdentity) request.Subject.Identity)?.Claims.First(x => x.Type == "sub").Value;

        var mainDbClaims = await userAccountService.GetClaimsByIdAsync(userId);

        var allClaims = new List<Claim>(authClaims);
        allClaims.AddRange(mainDbClaims);

        var result = allClaims.Distinct(new ClaimEqualityComparer()).ToList();

        return result;
    }
}