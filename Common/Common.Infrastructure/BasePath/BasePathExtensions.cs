using System;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace Common.Infrastructure.BasePath;

public static class BasePathExtensions
{
    public static void UseFreverBasePath(this IApplicationBuilder appBuilder, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(appBuilder);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = new FreverBasePathOptions();
        configuration.Bind(options);
        options.Validate();

        if (!options.IsApplicable)
            return;

        var optionsBasePath = options.BasePath;
        appBuilder.UsePathBase(optionsBasePath);
        var latest = Regex.Replace(optionsBasePath, @"(\d*\.\d*)", "latest");

        if (latest.Equals(optionsBasePath))
            return;

        appBuilder.UsePathBase(latest);
    }
}