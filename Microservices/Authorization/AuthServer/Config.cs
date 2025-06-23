// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using AuthServer.Services;
using AuthServerShared;
using IdentityModel;
using IdentityServer4.Models;
using static IdentityModel.OidcConstants;

namespace AuthServer;

public static class Config
{
    public static IEnumerable<IdentityResource> GetIdentityResources()
    {
        return new List<IdentityResource> {new IdentityResources.OpenId(), new IdentityResources.Profile()};
    }

    public static IEnumerable<ApiResource> GetApis()
    {
        return new List<ApiResource>
               {
                   new(
                       "friends_factory.creators_api",
                       "Friends factory Creators API",
                       new[]
                       {
                           JwtClaimTypes.Subject,
                           JwtClaimTypes.Email,
                           JwtClaimTypes.Name,
                           JwtClaimTypes.EmailVerified,
                           Claims.UserId,
                           Claims.PrimaryGroupId
                       }
                   )
               };
    }

    public static IEnumerable<Client> GetClients(string clientSecret, string allowedRedirectUrls)
    {
        return new List<Client>
               {
                   new()
                   {
                       ClientId = "Server",
                       AllowedGrantTypes = new List<string>
                                           {
                                               GrantType.ResourceOwnerPassword,
                                               GrantType.Implicit,
                                               AuthConstants.GrantType.PhoneNumberToken,
                                               AuthConstants.GrantType.AppleAuthToken,
                                               AuthConstants.GrantType.GoogleAuthToken,
                                               AuthConstants.GrantType.EmailToken
                                           },
                       AllowAccessTokensViaBrowser = true,
                       AllowOfflineAccess = true,
                       //Access token life time is 2592000 seconds (30 days)
                       AccessTokenLifetime = 2592000,
                       //Identity token life time is 2592000 seconds (30 days)
                       IdentityTokenLifetime = 2592000,
                       //Authorization token life time is 2592000 seconds (30 days)
                       AuthorizationCodeLifetime = 2592000,
                       RefreshTokenUsage = TokenUsage.ReUse,
                       UpdateAccessTokenClaimsOnRefresh = true,
                       RefreshTokenExpiration = TokenExpiration.Sliding,
                       //Absolute refresh token life time is 0 seconds (0 days)
                       AbsoluteRefreshTokenLifetime = 0,
                       //Sliding refresh token life time is 2592000 seconds (30 days)
                       SlidingRefreshTokenLifetime = 2592000,

                       //Device code life time is 2592000 seconds (30 days)
                       DeviceCodeLifetime = 2592000,
                       ClientSecrets = {new Secret(clientSecret.Sha256())},
                       AllowedScopes =
                       {
                           "friends_factory.creators_api",
                           StandardScopes.OfflineAccess,
                           StandardScopes.Email,
                           StandardScopes.Profile
                       },
                       RedirectUris = allowedRedirectUrls.Split(";").ToList()
                   }
               };
    }
}