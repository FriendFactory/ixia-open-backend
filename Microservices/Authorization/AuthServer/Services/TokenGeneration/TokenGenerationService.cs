using System;
using System.Linq;
using System.Threading.Tasks;
using AuthServer.Models;
using Common.Infrastructure;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;
using IdentityServer4;
using IdentityServer4.Configuration;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Services.TokenGeneration;

public class TokenGenerationService(
    IUserClaimsPrincipalFactory<ApplicationUser> principalFactory,
    ITokenService tokenService,
    UserManager<ApplicationUser> userManager,
    IdentityServerConfiguration configuration,
    IdentityServerOptions options,
    IWriteDb mainDb,
    IHttpContextAccessor contextAccessor,
    IServiceProvider services
) : ITokenGenerationService
{
    private readonly IdentityServerConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    private readonly IHttpContextAccessor _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
    private readonly IWriteDb _mainDb = mainDb ?? throw new ArgumentNullException(nameof(mainDb));
    private readonly IdentityServerOptions _options = options ?? throw new ArgumentNullException(nameof(options));

    private readonly IUserClaimsPrincipalFactory<ApplicationUser> _principalFactory =
        principalFactory ?? throw new ArgumentNullException(nameof(principalFactory));

    private readonly ITokenService _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
    private readonly UserManager<ApplicationUser> _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));

    public async Task<string> GenerateByGroupId(long groupId)
    {
        var user = await _mainDb.User.FirstOrDefaultAsync(u => u.MainGroupId == groupId);
        if (user == null)
            throw AppErrorWithStatusCodeException.BadRequest("Account not found", "AccountNotFound");

        return await GenerateForUser(user);
    }

    public async Task<string> GenerateByEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(email));

        var user = await _mainDb.User.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
            throw AppErrorWithStatusCodeException.BadRequest("Account not found", "AccountNotFound");

        return await GenerateForUser(user);
    }

    private async Task<string> GenerateForUser(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var group = await _mainDb.Group.FirstOrDefaultAsync(g => g.Id == user.MainGroupId);
        if (group == null)
            throw AppErrorWithStatusCodeException.BadRequest("Account not found", "AccountNotFound");

        if (group.IsBlocked || group.DeletedAt != null)
            throw AppErrorWithStatusCodeException.BadRequest("Account is disabled", "AccountDisabled");

        // Add extra secure check here if needed. For example uncomment code below:
        // if (user.IsEmployee && !group.IsStarCreator)
        //     throw AppErrorWithStatusCodeException.BadRequest("Account not found", "AccountNotFound");

        return await GenerateJwt(user.IdentityServerId);
    }

    private async Task<string> GenerateJwt(Guid identityServerId)
    {
        var user = await _userManager.FindByIdAsync(identityServerId.ToString());
        if (user == null)
            throw AppErrorWithStatusCodeException.BadRequest("Not found", "NotFound");

        var principal = await _principalFactory.CreateAsync(user);

        var identityUser = new IdentityServerUser(user.Id)
                           {
                               AdditionalClaims = principal.Claims.ToArray(),
                               AuthenticationTime = DateTime.UtcNow,
                               IdentityProvider = IdentityServerConstants.LocalIdentityProvider
                           };

        var request = new TokenCreationRequest
                      {
                          Subject = identityUser.CreatePrincipal(),
                          IncludeAllIdentityClaims = true,
                          Resources = new Resources(Config.GetIdentityResources(), Config.GetApis())
                      };

        request.ValidatedRequest = new ValidatedRequest
                                   {
                                       Subject = request.Subject,
                                       Options = _options,
                                       ClientClaims = identityUser.AdditionalClaims,
                                       AccessTokenLifetime = (int) TimeSpan.FromHours(48).TotalMilliseconds
                                   };
        request.ValidatedRequest.SetClient(Config.GetClients(_configuration.ClientSecret, _configuration.AllowedRedirectUrls).First());

        var needRestore = false;

        if (_contextAccessor.HttpContext == null)
        {
            _contextAccessor.HttpContext = new DefaultHttpContext {RequestServices = services};
            needRestore = true;
        }

        var token = await _tokenService.CreateAccessTokenAsync(request);
        token.Issuer = _configuration.IssuerUrl;

        var encoded = await _tokenService.CreateSecurityTokenAsync(token);

        if (needRestore)
            _contextAccessor.HttpContext = null;

        return encoded;
    }
}