using System.Linq;
using System.Threading.Tasks;
using AuthServerShared;
using Common.Infrastructure;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Permissions.Sub13;

public interface IParentalConsentValidationService
{
    Task EnsureChatAllowed();
    Task EnsureCommentsAllowed();
    Task EnsureVideoUploadAllowed();
    Task EnsureAudioUploadAllowed();
    Task EnsureCrewCreationAllowed();
    Task EnsureVideoDescriptionAllowed();
    Task EnsureCaptionsAllowed();
    Task EnsureImageUploadAllowed();
    Task EnsureSoundUploadAllowed();
    Task EnsureInAppPurchasesAllowed();
    bool IsChatAllowed(bool isMinor, bool isParentalConsentValidated, ParentalConsent consent);
}

public class DbParentalConsentValidationService(IWriteDb writeDb, UserInfo currentUser) : IParentalConsentValidationService
{
    public async Task EnsureChatAllowed()
    {
        var consent = await GetParentalConsent();
        if (!consent.AllowChat)
            Throw("Chatting");
    }

    public bool IsChatAllowed(bool isMinor, bool isParentalConsentValidated, ParentalConsent consent)
    {
        var c = GetParentalConsent(isMinor, isParentalConsentValidated, consent);
        return c.AllowChat;
    }

    public async Task EnsureCommentsAllowed()
    {
        var consent = await GetParentalConsent();
        if (!consent.AllowComments)
            Throw("Commenting");
    }

    public async Task EnsureVideoUploadAllowed()
    {
        var consent = await GetParentalConsent();
        if (!consent.VideoUploads)
            Throw("Video uploads");
    }

    public async Task EnsureAudioUploadAllowed()
    {
        var consent = await GetParentalConsent();
        if (!consent.AudioUploads)
            Throw("Audio uploads");
    }

    public async Task EnsureVideoDescriptionAllowed()
    {
        var consent = await GetParentalConsent();
        if (!consent.AllowVideoDescription)
            Throw("Video description");
    }

    public async Task EnsureCaptionsAllowed()
    {
        var consent = await GetParentalConsent();
        if (!consent.AllowCaptions)
            Throw("Caption");
    }

    public async Task EnsureImageUploadAllowed()
    {
        var consent = await GetParentalConsent();
        if (!consent.ImageUploads)
            Throw("Image uploading");
    }

    public async Task EnsureSoundUploadAllowed()
    {
        var consent = await GetParentalConsent();
        if (!consent.AudioUploads)
            Throw("Audio uploading");
    }

    public async Task EnsureCrewCreationAllowed()
    {
        var consent = await GetParentalConsent();
        if (!consent.AllowCrewCreation)
            Throw("Crew creation");
    }

    public async Task EnsureInAppPurchasesAllowed()
    {
        var groupInfo = await writeDb.Group.Where(g => g.Id == currentUser.UserMainGroupId)
                                     .Select(g => new {g.IsMinor, g.IsParentalConsentValidated, g.ParentalConsent})
                                     .FirstOrDefaultAsync();
        if (groupInfo == null || (groupInfo.IsMinor && !groupInfo.IsParentalConsentValidated) ||
            (groupInfo.IsMinor && !groupInfo.ParentalConsent.AllowInAppPurchase))
            throw AppErrorWithStatusCodeException.BadRequest("In-app purchases are not allowed", "InAppPurchasesNotAllowed");
    }


    /// <summary>
    ///     Returns parental consent.
    ///     For non-minor groups returns consent with all allowed.
    /// </summary>
    private async Task<ParentalConsent> GetParentalConsent()
    {
        var groupInfo = await writeDb.Group.Where(g => g.Id == currentUser.UserMainGroupId)
                                     .Select(g => new {g.IsMinor, g.IsParentalConsentValidated, g.ParentalConsent})
                                     .FirstOrDefaultAsync();
        if (groupInfo == null)
            return ParentalConsent.DenyAll;

        return GetParentalConsent(groupInfo.IsMinor, groupInfo.IsParentalConsentValidated, groupInfo.ParentalConsent);
    }

    private static ParentalConsent GetParentalConsent(bool isMinor, bool isParentalConsentValidated, ParentalConsent consent)
    {
        if (!isMinor)
            return ParentalConsent.AllowAll;

        if (!isParentalConsentValidated)
            return ParentalConsent.DenyAll;

        return consent ?? ParentalConsent.DenyAll;
    }

    private static void Throw(string errorAction)
    {
        throw AppErrorWithStatusCodeException.BadRequest($"{errorAction} is not allowed. No parental consent given", "NoParentalConsent");
    }
}