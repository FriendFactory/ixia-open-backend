using AuthServerShared.PhoneNormalization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace AuthServerShared;

public static class Extensions
{
    /// <summary>
    ///     Inject UserInfo instance correspondent to httpContext
    ///     httpContext.User=>UserInfo
    /// </summary>
    public static void AddUserInfo(this IServiceCollection services)
    {
        services.AddScoped(
            provider =>
            {
                var httpContext = provider.GetRequiredService<IHttpContextAccessor>().HttpContext;

                return httpContext?.User == null ? null : UserInfoFabric.ConvertToUserInfo(httpContext.User);
            }
        );
    }

    public static void AddPhoneNormalizationService(this IServiceCollection services)
    {
        services.AddSingleton<IPhoneNormalizationService, PhoneNormalizationService>();
    }
}