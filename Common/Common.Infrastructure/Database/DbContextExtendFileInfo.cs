using System;
using System.Collections.Generic;
using System.Linq;
using Common.Models.Database.Interfaces;
using Common.Models.Files;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Common.Infrastructure.Database;

public class DbContextExtendFileInfo
{
    public static readonly JsonSerializerSettings Settings = new()
                                                             {
                                                                 NullValueHandling = NullValueHandling.Ignore,
                                                                 ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                                                                 ContractResolver = new CamelCasePropertyNamesContractResolver()
                                                             };

    public static readonly JsonSerializerSettings FileMetadataSettings = new()
                                                                         {
                                                                             NullValueHandling = NullValueHandling.Ignore,
                                                                             ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                                                                             ContractResolver = new CamelCaseWithSkipContractResolver
                                                                                 {
                                                                                     SkipProperties =
                                                                                     [
                                                                                         nameof(FileMetadata.Source),
                                                                                         nameof(FileMetadata.Url)
                                                                                     ]
                                                                                 }
                                                                         };

    public static void IgnoreFileOwnerField(ModelBuilder modelBuilder)
    {
        IgnoreField(modelBuilder, typeof(IFileOwner), nameof(IFileOwner.Files));
    }

    public static void IgnoreField(ModelBuilder modelBuilder, Type targetInterface, string fieldName)
    {
        var types = modelBuilder.Model.GetEntityTypes()
                                .Where(x => targetInterface.IsAssignableFrom(x.ClrType))
                                .Select(x => x.ClrType)
                                .ToArray();
        foreach (var type in types)
            modelBuilder.Entity(type, entity => { entity.Ignore(fieldName); });
    }

    /// <summary>
    ///     Inject json->model conversion for every model which implement IFileOwner
    /// </summary>
    public void ExtendEveryModel(ModelBuilder modelBuilder)
    {
        IgnoreFileOwnerField(modelBuilder);

        var configureFilesMethod = typeof(DbContextExtendFileInfo).GetMethod(nameof(ConfigureFileStoringForEntity));
        foreach (var entityType in modelBuilder.Model.GetEntityTypes().Where(e => typeof(IFileOwner).IsAssignableFrom(e.ClrType)))
            configureFilesMethod.MakeGenericMethod(entityType.ClrType).Invoke(this, [modelBuilder]);

        var configureFileMetadataMethod = typeof(DbContextExtendFileInfo).GetMethod(nameof(ConfigureFileMetadataStoringForEntity));
        foreach (var entityType in modelBuilder.Model.GetEntityTypes().Where(e => typeof(IFileMetadataOwner).IsAssignableFrom(e.ClrType)))
            configureFileMetadataMethod.MakeGenericMethod(entityType.ClrType).Invoke(this, [modelBuilder]);
    }

    public void ConfigureFileStoringForEntity<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, IFileOwner
    {
        modelBuilder.Entity<TEntity>(
            builder =>
            {
                builder.Ignore(e => e.FilesInfo);

                Settings.Converters.Add(new StringEnumConverter());

                builder.Property(e => e.Files).HasJsonConversion(Settings).HasColumnName(nameof(IFileOwner.FilesInfo));
            }
        );
    }

    public void ConfigureFileMetadataStoringForEntity<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, IFileMetadataOwner
    {
        modelBuilder.Entity<TEntity>(
            builder =>
            {
                builder.Property(e => e.Files).HasJsonConversion(FileMetadataSettings).HasColumnName(nameof(IFileMetadataOwner.Files));
            }
        );
    }
}

public class CamelCaseWithSkipContractResolver : CamelCasePropertyNamesContractResolver
{
    public List<string> SkipProperties { get; set; } = new();

    protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
    {
        var props = base.CreateProperties(type, memberSerialization);

        return props.Where(p => SkipProperties == null || SkipProperties.All(s => s != p.UnderlyingName)).ToList();
    }
}