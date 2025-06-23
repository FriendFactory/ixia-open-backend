using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.AdminService.Core.Installers;

/// <summary>
///     Use it for decomposition of services setup
///     All inherited classes will be created by Activator and run during services setup
/// </summary>
internal interface IInstaller
{
    void AddServices(IServiceCollection services, IConfiguration configuration);
}