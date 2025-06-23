using System;
using Frever.Cache.Configuration;
using Frever.Cache.Strategies;
using Frever.ClientService.Contract.StorageFiles;
using Frever.Shared.MainDb.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.Client.Core.Features.StorageFiles;

public static class ServiceConfiguration
{
    public static void AddStorageFiles(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IStorageFileRepository, PersistentStorageFileRepository>();
        services.AddScoped<IStorageFileService, StorageFileService>();
        services.AddFreverCaching(o => { o.InMemory.Blob<StorageFileDto[]>(SerializeAs.Protobuf, false, typeof(StorageFile)); });
    }
}