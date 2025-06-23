using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Amazon.S3;
using AssetStoragePathProviding;
using AuthServer.Permissions.Services;
using Common.Infrastructure;
using Common.Infrastructure.Aws;
using Common.Infrastructure.Caching;
using Common.Infrastructure.Caching.CacheKeys;
using Common.Infrastructure.EmailSending;
using Frever.AdminService.Core.Services.VideoModeration;
using Frever.AdminService.Core.Utils;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Frever.AdminService.Core.Services.AccountModeration;

public class AccountHardDeletionService : IAccountHardDeletionService
{
    private const string DeletedEmailPostfix = "@deleted.mail";

    private readonly IAccountModerationRepository _accountModerationRepository;
    private readonly ICache _cache;
    private readonly IEmailSendingService _emailSendingService;
    private readonly IFileBucketPathService _fileBucketPathService;
    private readonly HardDeleteAccountDataHelper _hardDeleteAccountDataHelper;
    private readonly HardDeleteAccountSettings _hardDeleteAccountSettings;
    private readonly AccountModerationServiceOptions _options;
    private readonly IAmazonS3 _s3;
    private readonly ILogger _logger;

    public AccountHardDeletionService(
        ICache cache,
        IFileBucketPathService fileBucketPathService,
        IAmazonS3 s3,
        IOptions<HardDeleteAccountSettings> hardDeleteAccountSettingsOptions,
        IEmailSendingService emailSendingService,
        AccountModerationServiceOptions options,
        IAccountModerationRepository accountModerationRepository,
        HardDeleteAccountDataHelper hardDeleteAccountDataHelper,
        ILoggerFactory loggerFactory
    )
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _s3 = s3 ?? throw new ArgumentNullException(nameof(s3));
        _fileBucketPathService = fileBucketPathService ?? throw new ArgumentNullException(nameof(fileBucketPathService));

        _emailSendingService = emailSendingService ?? throw new ArgumentNullException(nameof(emailSendingService));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _options.Validate();

        _accountModerationRepository = accountModerationRepository ?? throw new ArgumentNullException(nameof(accountModerationRepository));
        _hardDeleteAccountDataHelper = hardDeleteAccountDataHelper ?? throw new ArgumentNullException(nameof(hardDeleteAccountDataHelper));
        ArgumentNullException.ThrowIfNull(hardDeleteAccountSettingsOptions);

        _hardDeleteAccountSettings = hardDeleteAccountSettingsOptions.Value;

        _logger = loggerFactory.CreateLogger("Frever.Admin.AccountHardDeletionService");
    }

    public async Task HardDeleteUserData(long groupId)
    {
        var user = await _accountModerationRepository.GetUserByMainGroup(groupId);

        if (user == null)
            throw AppErrorWithStatusCodeException.NotFound("User is not found", "UserNotFound");

        var replacement = Guid.NewGuid().ToString("N");
        await EraseGroupAndUserDataInMainDb(groupId, replacement);
        await DeleteAccountFromAuthDb(user.IdentityServerId);
        await DeleteAssetFiles(user);

        await _hardDeleteAccountDataHelper.HardDeleteAccountData(groupId);
    }

    public async Task HardDeleteGroups()
    {
        var groups = await GetDeletedGroupsAsync();
        var notDeletedGroupIds = new List<long>();

        foreach (var (groupId, email) in groups)
            try
            {
                if (!string.IsNullOrWhiteSpace(email))
                {
                    var emailMessage = EmailTemplate("Your account and personal data on Frever has now been permanently deleted");

                    var emailAddress = new EmailAddressAttribute();
                    if (emailAddress.IsValid(email))
                        await _emailSendingService.SendEmail(
                            new SendEmailParams {Body = emailMessage, Subject = "Frever account deletion", To = [email]}
                        );
                    else
                        _logger.LogWarning("Email address of ({GroupId}) is not valid, so skip sending email", groupId);
                }

                await HardDeleteGroup(groupId);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error {HardDeleteGroupName}({GroupId})", nameof(HardDeleteGroup), groupId);
                notDeletedGroupIds.Add(groupId);
            }

        if (notDeletedGroupIds.Count > 0)
        {
            var emailMessage = EmailTemplate(
                $"{Environment.NewLine} " + $"Environment: {_hardDeleteAccountSettings.EnvironmentInfo} " + $"{Environment.NewLine} " +
                $"Deletion of Group ID {string.Join(", ", notDeletedGroupIds)} failed"
            );

            var subject = $"({_hardDeleteAccountSettings.EnvironmentInfo}) Deletion of groups";

            await _emailSendingService.SendEmail(
                new SendEmailParams
                {
                    Body = emailMessage, Subject = subject, To = [_hardDeleteAccountSettings.DeletionErrorEmailRecipients]
                }
            );
        }
    }

    private async Task HardDeleteGroup(long groupId)
    {
        var deletedAt = DateTime.UtcNow;
        await _accountModerationRepository.SetGroupDeletedInMainDb(groupId, deletedAt);

        await ResetGroupCache(groupId);

        await HardDeleteUserData(groupId);
    }

    private async Task EraseGroupAndUserDataInMainDb(long groupId, string replacement)
    {
        if (string.IsNullOrWhiteSpace(replacement))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(replacement));

        using var scope = _logger.BeginScope($"EraseGroupData(groupId={groupId}, replacement={replacement})");

        var group = await _accountModerationRepository.GetGroup(groupId);

        if (group == null)
            throw AppErrorWithStatusCodeException.NotFound("Group is not found", "GroupNotFound");

        group.DeletedAt = DateTime.UtcNow;
        group.NickName = replacement;
        group.BirthDate = null;
        group.Gender = null;

        await _accountModerationRepository.UpdateGroup(group);

        var user = await _accountModerationRepository.GetUserByMainGroup(groupId);

        if (user == null)
            throw AppErrorWithStatusCodeException.NotFound("User is not found", "UserNotFound");

        user.Email = $"{replacement}{DeletedEmailPostfix}";
        user.PhoneNumber = null;
        user.AppleId = string.IsNullOrWhiteSpace(user.AppleId) ? null : $"AppleID={replacement}";
        user.GoogleId = string.IsNullOrWhiteSpace(user.GoogleId) ? null : $"GoogleID={replacement}";

        await _accountModerationRepository.UpdateUser(user);
    }

    private async Task DeleteAccountFromAuthDb(Guid identityServerId)
    {
        _logger.LogInformation("DeleteAccountFromAuthDb(identityServerId={IdentityServerId})", identityServerId);

        await _accountModerationRepository.DeleteAuthUser(identityServerId);
    }

    private async Task DeleteAssetFiles(User user)
    {
        using var scope = _logger.BeginScope($"DeleteAssetFiles(groupId={user.MainGroupId})");

        var userSoundQuery = _accountModerationRepository.GetDeletedUserSoundsForAccount(user.MainGroupId).AsNoTracking().Select(v => v.Id);

        await foreach (var userSoundId in userSoundQuery.ToPaginatedEnumerableAsync())
        {
            var folder = _fileBucketPathService.GetAssetMainFolder(typeof(UserSound), userSoundId);

            _logger.LogInformation("Delete UserSound in {Folder}", folder);

            await _s3.DeleteFolder(_options.Bucket, folder, m => _logger.LogInformation(m));
        }
    }

    private Task<Dictionary<long, string>> GetDeletedGroupsAsync()
    {
        var deletedAt = DateTime.UtcNow.AddDays(-_hardDeleteAccountSettings.DeletedDaysAgo);
        var limit = deletedAt.AddDays(-7);

        var result = _accountModerationRepository.GetUsers()
                                                 .Where(e => e.MainGroup.DeletedAt != null)
                                                 .Where(e => e.MainGroup.DeletedAt <= deletedAt && e.MainGroup.DeletedAt >= limit)
                                                 .Select(e => new {e.MainGroupId, e.Email})
                                                 .Where(e => !e.Email.Contains(DeletedEmailPostfix))
                                                 .ToDictionaryAsync(k => k.MainGroupId, v => v.Email);

        return result;
    }

    private async Task ResetGroupCache(long groupId)
    {
        await _cache.DeleteKeys(FreverUserPermissionService.GroupCacheKey(groupId));

        var publicInfix = VideoCacheKeys.PublicPrefix.GetKeyWithoutVersion();
        await _cache.DeleteKeysWithInfix(publicInfix);
    }

    private static string EmailTemplate(string body)
    {
        return string.Join(
            Environment.NewLine,
            $"{body}",
            Environment.NewLine,
            "Best Regards,",
            "Team Frever"
        );
    }
}