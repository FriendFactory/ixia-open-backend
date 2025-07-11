﻿// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using AuthServer.Contracts;
using AuthServer.Models;
using AuthServer.Quickstart.Account;
using AuthServer.Services.EmailAuth;
using AuthServer.Services.UserManaging;
using AuthServer.Services.UserManaging.NicknameSuggestion;
using Common.Infrastructure;
using FluentValidation;
using IdentityModel;
using IdentityServer4.Events;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace IdentityServer4.Quickstart.UI;

[SecurityHeaders]
[AllowAnonymous]
public class AccountController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IIdentityServerInteractionService interaction,
    IClientStore clientStore,
    IAuthenticationSchemeProvider schemeProvider,
    IEventService events,
    IHostEnvironment environment,
    IUserAccountService userAccountService,
    IEmailAuthService emailAuthService,
    INicknameSuggestionService nicknameSuggestionService,
    ICredentialValidateService credentialValidateService
) : Controller
{
    private readonly IEmailAuthService _emailAuthService = emailAuthService ?? throw new ArgumentNullException(nameof(emailAuthService));

    [HttpPost]
    public async Task<IActionResult> StoreEmailForAppleId([FromBody] AppleEmailInfoRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            await userAccountService.StoreEmailForAppleId(request);

            return NoContent();
        }
        catch (ValidationException)
        {
            return BadRequest(new {Ok = false, Error = "Invalid request"});
        }
    }

    [HttpPost]
    public async Task<IActionResult> LoginWithApple([FromBody] LoginWithAppleRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            var content = await userAccountService.LoginWithApple(request);

            return new ContentResult {Content = content, ContentType = "application/json", StatusCode = 200};
        }
        catch (AppErrorWithStatusCodeException ex)
        {
            return StatusCode((int) ex.StatusCode, ex.Message);
        }
    }

    [HttpPost]
    public async Task<IActionResult> LoginWithGoogle([FromBody] LoginWithGoogleRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            var content = await userAccountService.LoginWithGoogle(request);

            return new ContentResult {Content = content, ContentType = "application/json", StatusCode = 200};
        }
        catch (AppErrorWithStatusCodeException ex)
        {
            return StatusCode((int) ex.StatusCode, ex.Message);
        }
    }

    [HttpPost]
    public async Task<IActionResult> ValidateLoginInfo([FromBody] AuthenticationInfo request)
    {
        if (!request.IsValid())
            return BadRequest("Request model is not valid");

        var result = await userAccountService.CheckLoginInfo(request);

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> ValidateRegistrationInfo([FromBody] AuthenticationInfo request)
    {
        if (!request.IsValid())
            return BadRequest("Request model is not valid");

        var result = await userAccountService.CheckRegistrationInfo(request);

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Register([FromBody] RegisterUserViewModel request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));

        var result = await userAccountService.RegisterAccount(request);
        if (!result.Ok)
            return BadRequest(result);

        return new ContentResult {Content = result.Jwt, ContentType = "application/json", StatusCode = 200};
    }

    [HttpPut]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public async Task<IActionResult> Update([FromBody] UpdateAccountRequest request)
    {
        var groupIdStr = User.FindFirst(x => x.Type.Equals("PrimaryGroupId"))?.Value;

        if (groupIdStr == null)
            throw AppErrorWithStatusCodeException.NotAuthorized("Not authorized", "NotAuthorized");

        var groupId = long.Parse(groupIdStr);

        await userAccountService.UpdateAccount(groupId, request);

        return NoContent();
    }

    [HttpPost]
    public async Task<IActionResult> RegisterTemporaryAccount([FromBody] TemporaryAccountRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));

        ArgumentNullException.ThrowIfNull(request);

        var content = await userAccountService.RegisterTemporaryAccount(request);

        return new ContentResult {Content = content, ContentType = "application/json", StatusCode = 200};
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public async Task<IActionResult> VerifyParentEmail([FromBody] VerifyParentEmailRequest request)
    {
        if (request == null)
            throw AppErrorWithStatusCodeException.BadRequest("Parent email is required", "ParentEmailIsRequired");

        await new AssignParentEmailRequestValidator().ValidateAndThrowAsync(request);
        await _emailAuthService.SendParentEmailVerification(new VerifyEmailRequest {Email = request.ParentEmail});

        return NoContent();
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public async Task<IActionResult> AssignParentEmail([FromBody] AssignParentEmailRequest request)
    {
        await new VerifyParentEmailRequestValidator().ValidateAndThrowAsync(request);

        var isCodeValid = await _emailAuthService.ValidateParentEmailCode(request.ParentEmail, request.VerificationCode);
        if (!isCodeValid)
            throw AppErrorWithStatusCodeException.BadRequest("Verification code is not valid", "VerificationCodeInvalid");

        var groupIdStr = User.FindFirst(x => x.Type.Equals("PrimaryGroupId"))?.Value;

        if (groupIdStr == null)
            throw AppErrorWithStatusCodeException.NotAuthorized("Not authorized", "NotAuthorized");

        var groupId = long.Parse(groupIdStr);

        await userAccountService.AssignParentEmail(groupId, request.ParentEmail);

        var newCode = await _emailAuthService.GenerateVerificationCode(request.ParentEmail);

        return Ok(new {NewEmailCode = newCode});
    }

    [HttpDelete]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public async Task<IActionResult> RevokeParentConsent()
    {
        var groupIdStr = User.FindFirst(x => x.Type.Equals("PrimaryGroupId"))?.Value;

        if (groupIdStr == null)
            throw AppErrorWithStatusCodeException.NotAuthorized("Not authorized", "NotAuthorized");

        var groupId = long.Parse(groupIdStr);

        await userAccountService.RemoveParentEmail(groupId);

        return NoContent();
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public async Task<IActionResult> ConfigureParentalConsent([FromBody] ConfigureParentalConsentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var groupIdStr = User.FindFirst(x => x.Type.Equals("PrimaryGroupId"))?.Value;

        if (groupIdStr == null)
            throw AppErrorWithStatusCodeException.NotAuthorized("Not authorized", "NotAuthorized");

        var groupId = long.Parse(groupIdStr);

        await userAccountService.ConfigureParentalConsent(groupId, request.ParentalConsent);

        return NoContent();
    }

    [HttpPost]
    public async Task<IActionResult> CheckIfParentEmailBound([FromBody] CheckParentEmailStatusRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.UserName))
            throw AppErrorWithStatusCodeException.BadRequest("User name is required", "UsernameRequired");

        var isLoginByEmailAvailable = await userAccountService.IsLoginByEmailAvailable(request.UserName);
        return Ok(new CheckParentEmailStatusResult {IsLoginByParentEmailAvailable = isLoginByEmailAvailable});
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public async Task<IActionResult> VerifyMyAccountEmail()
    {
        var groupIdStr = User.FindFirst(x => x.Type.Equals("PrimaryGroupId"))?.Value;

        if (groupIdStr == null)
            throw AppErrorWithStatusCodeException.NotAuthorized("Not authorized", "NotAuthorized");

        var groupId = long.Parse(groupIdStr);

        await userAccountService.SendVerificationCodeToParentEmail(groupId);
        return NoContent();
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public async Task<IActionResult> VerifyMyAccountEmailCode([FromBody] MyAccountEmailCodeVerificationRequest request)
    {
        var groupIdStr = User.FindFirst(x => x.Type.Equals("PrimaryGroupId"))?.Value;

        if (groupIdStr == null)
            throw AppErrorWithStatusCodeException.NotAuthorized("Not authorized", "NotAuthorized");

        var groupId = long.Parse(groupIdStr);

        var isValid = await userAccountService.VerifyParentEmailCode(groupId, request.VerificationCode);
        if (!isValid)
            throw AppErrorWithStatusCodeException.BadRequest("Verification code is not valid", "VerificationCodeInvalid");

        return Ok(new {isValid = true});
    }

    //TODO: drop in 1.9 version
    [HttpPost]
    public async Task<IActionResult> ValidatePasswordStrength([FromBody] ValidatePasswordRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = await credentialValidateService.ValidatePassword(request.Password, null);

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> ValidatePassword([FromBody] ValidatePasswordRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Password);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Username);

        var result = await credentialValidateService.ValidatePassword(request.Password, request.Username);

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> SuggestNicknames([FromQuery] int count = 10)
    {
        var suggestions = await nicknameSuggestionService.SuggestNickname(string.Empty, count);
        return Ok(suggestions);
    }

    [HttpGet]
    public IActionResult GetEnvInfo()
    {
        return Ok($"Env {environment.EnvironmentName} with app name {environment.ApplicationName}");
    }

    [HttpGet]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public IActionResult Profile()
    {
        var id = User.FindFirst(x => x.Type.Equals(JwtClaimTypes.Subject))?.Value;

        if (id == null)
            return StatusCode(500, "Token expired");

        var name = User.FindFirst(x => x.Type.Equals(JwtClaimTypes.Name))?.Value;

        return Ok(new {Id = id, Name = name});
    }

    /// <summary>
    ///     Entry point into the login workflow
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Login(string returnUrl)
    {
        // build a model so we know what to show on the login page
        var vm = await BuildLoginViewModelAsync(returnUrl);

        if (vm.IsExternalLoginOnly)
            // we only have one option for logging in and it's an external provider
            return RedirectToAction("Challenge", "External", new {provider = vm.ExternalLoginScheme, returnUrl});

        return View(vm);
    }

    /// <summary>
    ///     Handle postback from username/password login
    /// </summary>
    [HttpPost]
    // [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginInputModel model, string button)
    {
        // check if we are in the context of an authorization request
        var context = await interaction.GetAuthorizationContextAsync(model.ReturnUrl);

        // the user clicked the "cancel" button
        if (button != "login")
        {
            if (context != null)
            {
                // if the user cancels, send a result back into IdentityServer as if they
                // denied the consent (even if this client does not require consent).
                // this will send back an access denied OIDC error response to the client.
                await interaction.GrantConsentAsync(context, ConsentResponse.Denied);

                // we can trust model.ReturnUrl since GetAuthorizationContextAsync returned non-null
                if (await clientStore.IsPkceClientAsync(context.ClientId))
                    // if the client is PKCE then we assume it's native, so this change in how to
                    // return the response is for better UX for the end user.
                    return View("Redirect", new RedirectViewModel {RedirectUrl = model.ReturnUrl});

                return Redirect(model.ReturnUrl);
            }

            // since we don't have a valid context, then we just go back to the home page
            return Redirect("~/");
        }

        if (ModelState.IsValid)
        {
            var result = await signInManager.PasswordSignInAsync(model.Username, model.Password, model.RememberLogin, true);

            if (result.Succeeded)
            {
                var user = await userManager.FindByNameAsync(model.Username);
                await events.RaiseAsync(new UserLoginSuccessEvent(user.UserName, user.Id, user.UserName));

                if (context != null)
                {
                    if (await clientStore.IsPkceClientAsync(context.ClientId))
                        // if the client is PKCE then we assume it's native, so this change in how to
                        // return the response is for better UX for the end user.
                        return View("Redirect", new RedirectViewModel {RedirectUrl = model.ReturnUrl});

                    // we can trust model.ReturnUrl since GetAuthorizationContextAsync returned non-null
                    return Redirect(model.ReturnUrl);
                }

                // request for a local page
                if (Url.IsLocalUrl(model.ReturnUrl))
                    return Redirect(model.ReturnUrl);
                if (string.IsNullOrEmpty(model.ReturnUrl))
                    return Redirect("~/");

                throw new Exception("invalid return URL");
            }

            await events.RaiseAsync(new UserLoginFailureEvent(model.Username, "invalid credentials"));
            ModelState.AddModelError(string.Empty, AccountOptions.InvalidCredentialsErrorMessage);
        }

        // something went wrong, show form with error
        var vm = await BuildLoginViewModelAsync(model);

        return View(vm);
    }

    /// <summary>
    ///     Show logout page
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Logout(string logoutId)
    {
        // build a model so the logout page knows what to display
        var vm = await BuildLogoutViewModelAsync(logoutId);

        if (vm.ShowLogoutPrompt == false)
            // if the request for logout was properly authenticated from IdentityServer, then
            // we don't need to show the prompt and can just log the user out directly.
            return await Logout(vm);

        return View(vm);
    }

    /// <summary>
    ///     Handle logout page postback
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(LogoutInputModel model)
    {
        // build a model so the logged out page knows what to display
        var vm = await BuildLoggedOutViewModelAsync(model.LogoutId);

        if (User?.Identity.IsAuthenticated == true)
        {
            // delete local authentication cookie
            await signInManager.SignOutAsync();

            // raise the logout event
            await events.RaiseAsync(new UserLogoutSuccessEvent(User.GetSubjectId(), User.GetDisplayName()));
        }

        // check if we need to trigger sign-out at an upstream identity provider
        if (vm.TriggerExternalSignout)
        {
            // build a return URL so the upstream provider will redirect back
            // to us after the user has logged out. this allows us to then
            // complete our single sign-out processing.
            var url = Url.Action("Logout", new {logoutId = vm.LogoutId});

            // this triggers a redirect to the external provider for sign-out
            return SignOut(new AuthenticationProperties {RedirectUri = url}, vm.ExternalAuthenticationScheme);
        }

        return View("LoggedOut", vm);
    }


    /*****************************************/
    /* helper APIs for the AccountController */
    /*****************************************/

    private async Task<LoginViewModel> BuildLoginViewModelAsync(string returnUrl)
    {
        var context = await interaction.GetAuthorizationContextAsync(returnUrl);

        if (context?.IdP != null)
            // this is meant to short circuit the UI and only trigger the one external IdP
            return new LoginViewModel
                   {
                       EnableLocalLogin = false,
                       ReturnUrl = returnUrl,
                       Username = context?.LoginHint,
                       ExternalProviders = new[] {new ExternalProvider {AuthenticationScheme = context.IdP}}
                   };

        var schemes = await schemeProvider.GetAllSchemesAsync();

        var providers = schemes
                       .Where(
                            x => x.DisplayName != null || x.Name.Equals(
                                     AccountOptions.WindowsAuthenticationSchemeName,
                                     StringComparison.OrdinalIgnoreCase
                                 )
                        )
                       .Select(x => new ExternalProvider {DisplayName = x.DisplayName, AuthenticationScheme = x.Name})
                       .ToList();

        var allowLocal = true;
        if (context?.ClientId != null)
        {
            var client = await clientStore.FindEnabledClientByIdAsync(context.ClientId);
            if (client != null)
            {
                allowLocal = client.EnableLocalLogin;

                if (client.IdentityProviderRestrictions != null && client.IdentityProviderRestrictions.Any())
                    providers = providers.Where(provider => client.IdentityProviderRestrictions.Contains(provider.AuthenticationScheme))
                                         .ToList();
            }
        }

        return new LoginViewModel
               {
                   AllowRememberLogin = AccountOptions.AllowRememberLogin,
                   EnableLocalLogin = allowLocal && AccountOptions.AllowLocalLogin,
                   ReturnUrl = returnUrl,
                   Username = context?.LoginHint,
                   ExternalProviders = providers.ToArray()
               };
    }

    private async Task<LoginViewModel> BuildLoginViewModelAsync(LoginInputModel model)
    {
        var vm = await BuildLoginViewModelAsync(model.ReturnUrl);
        vm.Username = model.Username;
        vm.RememberLogin = model.RememberLogin;

        return vm;
    }

    private async Task<LogoutViewModel> BuildLogoutViewModelAsync(string logoutId)
    {
        var vm = new LogoutViewModel {LogoutId = logoutId, ShowLogoutPrompt = AccountOptions.ShowLogoutPrompt};

        if (User?.Identity.IsAuthenticated != true)
        {
            // if the user is not authenticated, then just show logged out page
            vm.ShowLogoutPrompt = false;

            return vm;
        }

        var context = await interaction.GetLogoutContextAsync(logoutId);
        if (context?.ShowSignoutPrompt == false)
        {
            // it's safe to automatically sign-out
            vm.ShowLogoutPrompt = false;

            return vm;
        }

        // show the logout prompt. this prevents attacks where the user
        // is automatically signed out by another malicious web page.
        return vm;
    }

    private async Task<LoggedOutViewModel> BuildLoggedOutViewModelAsync(string logoutId)
    {
        // get context information (client name, post logout redirect URI and iframe for federated signout)
        var logout = await interaction.GetLogoutContextAsync(logoutId);

        var vm = new LoggedOutViewModel
                 {
                     AutomaticRedirectAfterSignOut = AccountOptions.AutomaticRedirectAfterSignOut,
                     PostLogoutRedirectUri = logout?.PostLogoutRedirectUri,
                     ClientName = string.IsNullOrEmpty(logout?.ClientName) ? logout?.ClientId : logout?.ClientName,
                     SignOutIframeUrl = logout?.SignOutIFrameUrl,
                     LogoutId = logoutId
                 };

        if (User?.Identity.IsAuthenticated == true)
        {
            var idp = User.FindFirst(JwtClaimTypes.IdentityProvider)?.Value;
            if (idp != null && idp != IdentityServerConstants.LocalIdentityProvider)
            {
                var providerSupportsSignout = await HttpContext.GetSchemeSupportsSignOutAsync(idp);
                if (providerSupportsSignout)
                {
                    if (vm.LogoutId == null)
                        // if there's no current logout context, we need to create one
                        // this captures necessary info from the current logged in user
                        // before we signout and redirect away to the external IdP for signout
                        vm.LogoutId = await interaction.CreateLogoutContextAsync();

                    vm.ExternalAuthenticationScheme = idp;
                }
            }
        }

        return vm;
    }
}

public class ValidatePasswordRequest
{
    public string Password { get; set; }
    public string Username { get; set; }
}