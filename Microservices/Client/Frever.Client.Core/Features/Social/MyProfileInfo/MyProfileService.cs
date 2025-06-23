using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthServer.Permissions.Services;
using AuthServerShared;
using Common.Infrastructure.EmailSending;
using Common.Infrastructure.ModerationProvider;
using Common.Models;
using FluentValidation;
using Frever.Client.Core.Features.InAppPurchases;
using Frever.Client.Core.Features.InAppPurchases.Subscriptions;
using Frever.Client.Core.Features.Localizations;
using Frever.Client.Core.Features.Social.DataAccess;
using Frever.Client.Core.Features.Social.Profiles;
using Frever.Client.Shared.Files;
using Frever.Client.Shared.Payouts;
using Frever.ClientService.Contract.Social;
using Frever.Shared.MainDb.Entities;
using Frever.Videos.Shared.MusicGeoFiltering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ArgumentNullException = System.ArgumentNullException;
using Group = Frever.Shared.MainDb.Entities.Group;
using Platform = Common.Models.Files.Platform;

namespace Frever.Client.Core.Features.Social.MyProfileInfo;

internal sealed class MyProfileService : IMyProfileService
{
    private readonly IAppsFlyerClient _appsFlyerClient;
    private readonly ICurrencyPayoutService _currencyPayoutService;
    private readonly ICurrentLocationProvider _currentLocationProvider;
    private readonly UserInfo _currentUser;
    private readonly IEmailSendingService _emailSendingService;
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<MyProfileService> _log;
    private readonly IMainDbRepository _repo;
    private readonly IModerationProviderApi _moderationProviderApi;
    private readonly ProfileServiceOptions _options;
    private readonly IUserPermissionManagementService _permissionManagementService;
    private readonly IUserPermissionService _userPermissionService;
    private readonly IFileStorageService _fileStorage;
    private readonly IAdvancedFileStorageService _advancedFileStorage;
    private readonly IInAppSubscriptionManager _subscriptionManager;

    public MyProfileService(
        UserInfo currentUser,
        IMainDbRepository mainDbRepository,
        IUserPermissionService userPermissionService,
        IEmailSendingService emailSendingService,
        ProfileServiceOptions options,
        IModerationProviderApi moderationProviderApi,
        ILogger<MyProfileService> log,
        ICurrentLocationProvider currentLocationProvider,
        ILocalizationService localizationService,
        IAppsFlyerClient appsFlyerClient,
        IUserPermissionManagementService permissionManagementService,
        ICurrencyPayoutService currencyPayoutService,
        IFileStorageService fileStorage,
        IAdvancedFileStorageService advancedFileStorage,
        IInAppSubscriptionManager subscriptionManager
    )
    {
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _repo = mainDbRepository ?? throw new ArgumentNullException(nameof(mainDbRepository));
        _userPermissionService = userPermissionService ?? throw new ArgumentNullException(nameof(userPermissionService));
        _emailSendingService = emailSendingService ?? throw new ArgumentNullException(nameof(emailSendingService));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _moderationProviderApi = moderationProviderApi ?? throw new ArgumentNullException(nameof(moderationProviderApi));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _currentLocationProvider = currentLocationProvider ?? throw new ArgumentNullException(nameof(currentLocationProvider));
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        _appsFlyerClient = appsFlyerClient ?? throw new ArgumentNullException(nameof(appsFlyerClient));
        _permissionManagementService = permissionManagementService ?? throw new ArgumentNullException(nameof(permissionManagementService));
        _currencyPayoutService = currencyPayoutService ?? throw new ArgumentNullException(nameof(currencyPayoutService));
        _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
        _advancedFileStorage = advancedFileStorage ?? throw new ArgumentNullException(nameof(advancedFileStorage));
        _subscriptionManager = subscriptionManager ?? throw new ArgumentNullException(nameof(subscriptionManager));
    }

    public async Task<MyProfile> Me()
    {
        await _userPermissionService.EnsureCurrentUserActive();

        var user = await _repo.GetUserById(_currentUser.UserId)
                              .Select(
                                   e => new MyProfile
                                        {
                                            BirthDate = e.MainGroup.BirthDate,
                                            DataCollectionEnabled = e.DataCollection,
                                            AnalyticsEnabled = e.AnalyticsEnabled,
                                            Nickname = e.MainGroup.NickName,
                                            TaxationCountryId = e.MainGroup.TaxationCountryId,
                                            AdvertisingTrackingEnabled = e.MainGroup.AppsFlyerIds != null,
                                            ParentalConsent = e.MainGroup.ParentalConsent,
                                            IsParentAgeValidated = e.MainGroup.IsParentalConsentValidated,
                                            Bio = e.MainGroup.Bio,
                                            EmailRedacted = e.Email,
                                            IsOnboardingCompleted = e.MainGroup.IsOnboardingCompleted,
                                            UsernameUpdateAvailableOn = e.MainGroup.NickNameUpdatedAt,
                                            HasDefaultUsername = e.MainGroup.NickNameUpdatedAt == null,
                                            HasUpdatedCredentials =
                                                e.Email != null || e.PhoneNumber != null || e.AppleId != null || e.GoogleId != null ||
                                                e.HasPassword
                                        }
                               )
                              .FirstOrDefaultAsync();

        if (user == null)
            return null;

        var isMinor = await _repo.GetUserById(_currentUser.UserId).Select(e => e.MainGroup.IsMinor).FirstOrDefaultAsync();

        if (user.UsernameUpdateAvailableOn.HasValue)
            user.UsernameUpdateAvailableOn = user.UsernameUpdateAvailableOn.Value.AddDays(Constants.UsernameUpdateIntervalDays);

        var detectedCountyIsoCode = (await _currentLocationProvider.Get()).CountryIso3Code;
        var currentCountry = await _localizationService.GetCountryByIso3Code(detectedCountyIsoCode);

        user.DetectedLocationCountry = detectedCountyIsoCode;
        user.UserBalance = await GetMyBalance();
        user.IsEmployee = await _repo.GetUserRoles(_currentUser).AnyAsync();
        user.BioLinks = await _repo.GetGroupBioLinks(_currentUser).ToDictionaryAsync(g => g.LinkType, g => g.Link);
        user.ParentalConsent ??= new ParentalConsent();
        user.IsInAppPurchaseAllowed = !isMinor || (user.IsParentAgeValidated && user.ParentalConsent.AllowInAppPurchase);
        user.IsNicknameChangeAllowed = !isMinor || !currentCountry.StrictCoppaRules;

        if (string.IsNullOrWhiteSpace(user.EmailRedacted) || !user.EmailRedacted.Contains('@'))
            return user;

        var index = user.EmailRedacted.IndexOf('@');
        var originalEmail = user.EmailRedacted;
        user.EmailRedacted = string.Concat(originalEmail.AsSpan(0, 1), "*********", user.EmailRedacted.AsSpan(index));

        return user;
    }

    public async Task<MyProfile> UpdateProfile(UpdateProfileRequest request)
    {
        await _userPermissionService.EnsureCurrentUserActive();

        await using var transaction = await _repo.BeginTransaction();

        var user = await _repo.GetUserById(_currentUser.UserId).Include(e => e.MainGroup).FirstOrDefaultAsync();
        if (user == null)
            return null;

        if (request.Files is {Length: > 0})
        {
            user.MainGroup.Files = user.MainGroup.Files == null
                                       ? request.Files
                                       : request.Files.UnionBy(user.MainGroup.Files, e => e.Type).ToArray();

            var result = await _advancedFileStorage.Validate<Group>(user.MainGroup);
            if (!result.IsValid)
                throw new ValidationException($"Invalid files: {string.Join(". ", result.Errors)}");
        }

        if (request.Bio != user.MainGroup.Bio)
        {
            if (!string.IsNullOrEmpty(request.Bio))
            {
                var moderationResult = await _moderationProviderApi.CallModerationProviderApiText(request.Bio);
                if (!moderationResult.PassedModeration)
                {
                    _log.LogInformation(
                        "Profile Bio {Bio} doesn't pass moderation: {Reason} {Error}",
                        request.Bio,
                        moderationResult.Reason,
                        moderationResult.ErrorMessage
                    );
                    throw new ValidationException("This profile bio is inappropriate.");
                }
            }

            user.MainGroup.Bio = request.Bio;
        }

        if (request.BioLinks != null)
        {
            var normalizedLinks = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (type, url) in request.BioLinks)
                normalizedLinks[type.ToLower()] = url;

            await _repo.UpdateGroupBioLinks(user.MainGroupId, normalizedLinks);
        }

        if (request.Files is {Length: > 0})
        {
            var uploader = _fileStorage.CreateFileUploader();
            await uploader.UploadFiles<Group>(user.MainGroup);
            await uploader.WaitForCompletion();
        }

        await _repo.UpdateGroup(user.MainGroup);
        await transaction.Commit();

        return await Me();
    }

    public async Task DeleteMe()
    {
        await _userPermissionService.EnsureCurrentUserActive();

        await using var transaction = await _repo.BeginTransaction();

        await _permissionManagementService.SoftDeleteSelf();

        await transaction.Commit();

        await DeleteAdvertisingTrackingInternal();

        await _emailSendingService.SendEmail(
            new SendEmailParams
            {
                Body = $"Group id {_currentUser.UserMainGroupId} has requested as deletion",
                Subject = "Account deletion",
                To = [_options.DeleteAccountEmail]
            }
        );
    }

    public async Task<UserBalance> GetMyBalance()
    {
        var balance = await _subscriptionManager.RenewSubscriptionTokens();

        return new UserBalance
               {
                   DailyTokens = balance.DailyTokens,
                   PermanentTokens = balance.PermanentTokens,
                   SubscriptionTokens = balance.SubscriptionTokens,
                   HardCurrencyAmount = balance.TotalTokens,
                   NextSubscriptionTokenRefresh = balance.NextSubscriptionTokenRefresh,
                   NextDailyTokenRefresh = balance.NextDailyTokenRefresh,
                   MaxDailyTokens = balance.MaxDailyTokens,
                   MaxSubscriptionTokens = balance.MaxSubscriptionTokens,
                   Subscription = balance.ActiveSubscription
               };
    }

    public async Task AddInitialBalance()
    {
        await _userPermissionService.EnsureCurrentUserActive();

        if (!await _repo.InitialAccountBalanceAdded(_currentUser))
            await _currencyPayoutService.AddInitialAccountBalance(
                _currentUser,
                Constants.InitialAccountSoftCurrency,
                Constants.InitialAccountHardCurrency
            );
        else
            _log.LogInformation("Initial account balance has already been added");


        var dailyPayout = await _subscriptionManager.GetDailyTokensAmount(_currentUser);
        await _currencyPayoutService.AddInitialDailyTokens(_currentUser, dailyPayout);
    }

    public async Task SetMyStatusOnline()
    {
        await _userPermissionService.EnsureCurrentUserActive();
    }

    public async Task AddMyMyAdvertisingTracking(string androidAppsFlyerId)
    {
        if (string.IsNullOrWhiteSpace(androidAppsFlyerId))
            throw new ArgumentNullException(nameof(androidAppsFlyerId));

        await _userPermissionService.EnsureCurrentUserActive();

        var group = await _repo.GetGroup(_currentUser.UserMainGroupId).FirstOrDefaultAsync();

        if (group is null)
            return;

        var id = new AppsFlyerId {Id = androidAppsFlyerId, Platform = (int) Platform.Android};

        group.AppsFlyerIds ??= [id];

        if (group.AppsFlyerIds.All(e => e.Id != id.Id))
            group.AppsFlyerIds.Add(id);

        await _repo.UpdateGroup(group);
    }

    public async Task DeleteMyAdvertisingTracking()
    {
        await _userPermissionService.EnsureCurrentUserActive();

        await DeleteAdvertisingTrackingInternal();
    }

    private async Task DeleteAdvertisingTrackingInternal()
    {
        var group = await _repo.GetGroup(_currentUser.UserMainGroupId).FirstOrDefaultAsync();

        if (group?.AppsFlyerIds is null)
            return;

        var ids = group.AppsFlyerIds.Where(e => e.Platform == (int) Platform.Android).ToArray();

        _log.LogInformation("Removing {AppsFlyerIds} for group {GroupId}", group.Id, string.Join(',', ids.Select(e => e.Id)));

        foreach (var item in ids)
            await _appsFlyerClient.PostUserRecordDeletion(item.Id);

        group.AppsFlyerIds = null;

        await _repo.UpdateGroup(group);
    }
}