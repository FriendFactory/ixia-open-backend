using System;
using FluentValidation;
using Frever.Video.Core.Features.ReportInappropriate.DataAccess;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.Video.Core.Features.ReportInappropriate;

public static class ServiceConfiguration
{
    public static void AddInappropriateVideoReporting(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IValidator<ReportInappropriateVideoRequest>, ReportInappropriateVideoRequestValidator>();
        services.AddScoped<IReportInappropriateVideoRepository, PersistentReportInappropriateVideoRepository>();
        services.AddScoped<IReportInappropriateVideoService, ReportInappropriateVideoService>();
    }
}