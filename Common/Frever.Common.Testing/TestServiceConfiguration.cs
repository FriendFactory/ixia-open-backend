using AuthServer.Permissions.Services;
using AuthServerShared;
using Common.Infrastructure.Caching;
using Common.Infrastructure.RequestId;
using Frever.Client.Shared.Social.Services;
using Frever.ClientService.Contract.Social;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;
using Frever.Videos.Shared.MusicGeoFiltering;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit.Abstractions;

namespace Frever.Common.Testing;

public static class TestServiceConfiguration
{
    public static void AddCommonTestServices(this IServiceCollection services, ITestOutputHelper testOut)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(testOut);
        var configuration = GetConfiguration();

        services.AddLogging(
            c =>
            {
                c.ClearProviders();
                c.SetMinimumLevel(LogLevel.Trace);
                c.AddXunit(testOut);
            }
        );

        AddRedis(services, configuration);
        AddCurrentUser(services);

        services.AddSingleton<TransactionMockManager>();
    }

    public static void AddUnitTestServices(this IServiceCollection services, ITestOutputHelper testOut)
    {
        services.AddCommonTestServices(testOut);
        AddLocation(services);
        AddUserPermissionService(services);
        AddSocialSharedService(services);
        AddHeaderAccessor(services);
    }

    public static IConfiguration GetConfiguration()
    {
        return new ConfigurationBuilder().AddEnvironmentVariables().Build();
    }

    public static void SetCurrentUser(this IServiceProvider provider, User user)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(user);

        string[] accessScopes = [];

        var db = provider.GetService<IWriteDb>();

        if (db != null)
        {
            var userRoles = db.UserRole.Where(ur => ur.GroupId == user.MainGroupId).ToArray();
            var roleIds = userRoles.Select(r => r.RoleId).ToHashSet();
            var scopes = db.RoleAccessScope.Where(rac => roleIds.Contains(rac.RoleId)).Select(rac => rac.AccessScope).ToHashSet();

            accessScopes = scopes.ToArray();
        }


        var currentUser = new UserInfo(
            user.Id,
            user.MainGroupId,
            false,
            false,
            [],
            accessScopes
        );

        var mock = provider.GetRequiredService<TestCurrentUserProvider>();
        mock.CurrentUser = currentUser;
    }

    private static void AddHeaderAccessor(IServiceCollection services)
    {
        var mock = new Mock<IHeaderAccessor>();

        mock.Setup(i => i.GetRequestId()).Returns(Guid.NewGuid().ToString("N"));
        mock.Setup(i => i.GetUnityVersion()).Returns("2024.0.1");
        mock.Setup(i => i.GetRequestExperimentsHeader()).Returns("x=y");

        services.AddSingleton(mock);
        services.AddSingleton(mock.Object);
    }

    private static void AddSocialSharedService(IServiceCollection services)
    {
        var mock = new Mock<ISocialSharedService>(MockBehavior.Strict);
        mock.Setup(i => i.GetBlocked(It.IsAny<long>(), It.IsAny<long[]>())).Returns(Task.FromResult<long[]>([]));

        mock.Setup(s => s.GetGroupShortInfo(It.IsAny<long[]>()))
            .Returns(
                 (long[] groupIds) =>
                 {
                     var result = new Dictionary<long, GroupShortInfo>();
                     foreach (var id in groupIds)
                         result[id] = new GroupShortInfo {Id = id, Nickname = $"Group-{id}", Files = []};

                     return Task.FromResult(result);
                 }
             );

        services.AddSingleton(mock);
        services.AddSingleton(mock.Object);
    }

    private static void AddUserPermissionService(IServiceCollection services)
    {
        var mock = new Mock<IUserPermissionService>();
        mock.Setup(i => i.EnsureCurrentUserActive()).Returns(Task.CompletedTask);
        services.AddSingleton(mock.Object);
    }

    private static void AddLocation(IServiceCollection services)
    {
        var location = new Mock<ICurrentLocationProvider>();
        location.Setup(i => i.Get()).Returns(Task.FromResult(new LocationInfo {Lat = 30.233m, Lon = 12.223m, CountryIso3Code = "usa"}));
        services.AddSingleton(location.Object);
    }

    private static void AddCurrentUser(IServiceCollection services)
    {
        var user = new UserInfo(
            2992,
            2993,
            false,
            false,
            [],
            []
        );


        services.AddScoped(_ => new TestCurrentUserProvider {CurrentUser = user});
        services.AddTransient(provider => provider.GetRequiredService<TestCurrentUserProvider>().CurrentUser);
    }

    private static void AddRedis(IServiceCollection services, IConfiguration configuration)
    {
        var redisSettings = configuration.BindRedisSettings();
        services.AddRedis(redisSettings, Guid.NewGuid().ToString("N"));
    }
}