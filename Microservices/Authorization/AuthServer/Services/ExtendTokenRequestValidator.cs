using System.Collections.Generic;
using System.Threading.Tasks;
using AuthServer.Services.UserManaging;
using IdentityServer4.Validation;
using Microsoft.Extensions.Configuration;

namespace AuthServer.Services;

public class ExtendTokenRequestValidator(IConfiguration config) : ICustomTokenRequestValidator
{
    public Task ValidateAsync(CustomTokenRequestValidationContext context)
    {
        context.Result.CustomResponse ??= new Dictionary<string, object>();

        var urlData = config.GetExternalUrlConfiguration();

        context.Result.CustomResponse.Add("server_url", urlData.Main);
        context.Result.CustomResponse.Add("asset_server", urlData.Asset);
        context.Result.CustomResponse.Add("transcoding_server", urlData.Transcoding);
        context.Result.CustomResponse.Add("video_server", urlData.Video);
        context.Result.CustomResponse.Add("social_server", urlData.Social);
        context.Result.CustomResponse.Add("notification_server", urlData.Notification);
        context.Result.CustomResponse.Add("assetmanager_server", urlData.AssetManager);
        context.Result.CustomResponse.Add("client_server", urlData.Client);
        context.Result.CustomResponse.Add("chat_server", urlData.Chat);

        return Task.CompletedTask;
    }
}