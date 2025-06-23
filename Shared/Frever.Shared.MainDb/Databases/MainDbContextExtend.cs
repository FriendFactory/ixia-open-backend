using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Common.Infrastructure.Database;
using Common.Infrastructure.Utils;
using Common.Models.Database.Interfaces;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL.Storage.Internal.Mapping;
using Npgsql.NameTranslation;
using NpgsqlTypes;

[assembly: InternalsVisibleTo("Server.Tests")]

namespace Frever.Shared.MainDb;

public partial class MainDbContext
{
    private static readonly Dictionary<NpgsqlDbType, Func<DateTime, DateTime>> FixDateFn =
        new() {{NpgsqlDbType.Timestamp, DateUtils.FixKindToLocal}, {NpgsqlDbType.TimestampTz, DateUtils.FixKindToUniversal}};

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        ExtendSetup(modelBuilder);

        new DbContextExtendFileInfo().ExtendEveryModel(modelBuilder);
    }

    public override int SaveChanges()
    {
        SetCreatedAndModifiedTime();

        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        SetCreatedAndModifiedTime();

        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new())
    {
        SetCreatedAndModifiedTime();

        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = new())
    {
        SetCreatedAndModifiedTime();

        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void UpdateVideoModifiedTime()
    {
        var entries = ChangeTracker.Entries()
                                   .Where(e => e.Entity is Video)
                                   .Where(e => e.State is EntityState.Added or EntityState.Modified);

        foreach (var entityEntry in entries)
            ((Video) entityEntry.Entity).ModifiedTime = DateTime.UtcNow;
    }


    private void SetCreatedAndModifiedTime()
    {
        var addedEntities = ChangeTracker.Entries().Where(e => e.State == EntityState.Added).ToList();

        addedEntities.ForEach(
            e =>
            {
                if (e.HasCreateDateProperty() && e.Entity is not AssetStoreTransaction)
                    e.Property(nameof(ITimeChangesTrackable.CreatedTime)).CurrentValue = DateTime.UtcNow;

                FixDateTimeKindOnTimeTrackableEntity(e);
            }
        );

        var editedEntities = ChangeTracker.Entries().Where(e => e.State == EntityState.Modified).ToList();

        editedEntities.ForEach(
            e =>
            {
                if (e.HasModifiedDateProperty())
                    e.Property(nameof(ITimeChangesTrackable.ModifiedTime)).CurrentValue = DateTime.UtcNow;

                FixDateTimeKindOnTimeTrackableEntity(e);
            }
        );

        UpdateVideoModifiedTime();
    }

    private static void FixDateTimeKindOnTimeTrackableEntity(EntityEntry e)
    {
        foreach (var prop in e.Properties)
            if (prop.IsModified && (prop.Metadata.ClrType == typeof(DateTime) || prop.Metadata.ClrType == typeof(DateTime?)))
                if (prop.CurrentValue is DateTime value && value.Kind != DateTimeKind.Utc)
                    if (prop.Metadata.FindTypeMapping() is NpgsqlTypeMapping mapping)
                    {
                        FixDateFn.TryGetValue(mapping.NpgsqlDbType, out var fix);
                        fix ??= DateUtils.FixKindToUniversal;
                        // Use reflection set value
                        // Seems updating using prop.CurrentValue compares new and existing value
                        // and DateTime with Unspecified Kind is equal to the value with Utc kind
                        prop.Metadata.PropertyInfo.SetValue(e.Entity, fix(value));
                    }
    }

    public static void RegisterGlobalTypes()
    {
        NpgsqlConnection.GlobalTypeMapper.UseNetTopologySuite(geographyAsDefault: true);

        NpgsqlConnection.GlobalTypeMapper.MapEnum<NotificationType>(nameof(NotificationType), new NpgsqlNullNameTranslator());
        NpgsqlConnection.GlobalTypeMapper.MapEnum<FollowerState>(nameof(FollowerState), new CustomNameTranslator());
        NpgsqlConnection.GlobalTypeMapper.MapEnum<AssetStoreAssetType>(nameof(AssetStoreAssetType), new CustomNameTranslator());
        NpgsqlConnection.GlobalTypeMapper.MapEnum<AssetStoreTransactionType>(nameof(AssetStoreTransactionType), new CustomNameTranslator());
        NpgsqlConnection.GlobalTypeMapper.MapEnum<CharacterAccess>(nameof(CharacterAccess), new CustomNameTranslator());
        NpgsqlConnection.GlobalTypeMapper.MapEnum<UserActionType>(nameof(UserActionType), new CustomNameTranslator());
        NpgsqlConnection.GlobalTypeMapper.MapEnum<VideoAccess>(nameof(VideoAccess), new NpgsqlNullNameTranslator());
    }

    #region for testing

    private void ExtendSetup(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Video>(
            entity =>
            {
                entity.Property(e => e.Id).UseIdentityAlwaysColumn();
                entity.Property(e => e.Duration).IsRequired();
                entity.Property(e => e.Size).IsRequired();
                entity.Property(e => e.Watermark).IsRequired();
                entity.Property(e => e.CreatedTime).IsRequired().ValueGeneratedOnAdd();
                entity.Property(e => e.FrameRate).IsRequired();
                entity.Property(e => e.GroupId).IsRequired();
                entity.Property(e => e.LevelId);
                entity.Property(e => e.VerticalCategoryId).IsRequired();
                entity.Property(e => e.PlatformId).IsRequired();
                entity.Property(e => e.ResolutionHeight).IsRequired();
                entity.Property(e => e.ResolutionWidth).IsRequired();
            }
        );

        modelBuilder.Entity<VideoView>(
            entity =>
            {
                entity.ToTable("Views");

                entity.HasKey(nameof(Entities.VideoView.UserId), nameof(Entities.VideoView.VideoId), nameof(Entities.VideoView.Time));

                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.VideoId).IsRequired();
                entity.Property(e => e.Time).IsRequired();
            }
        );

        modelBuilder.Entity<VideoKpi>(e => { e.HasKey(a => a.VideoId); });

        modelBuilder.Entity<Like>(
            builder =>
            {
                builder.ToTable("Likes");
                builder.HasKey(nameof(Entities.Like.VideoId), nameof(Entities.Like.UserId));
            }
        );
    }

    #endregion for testing
}

internal class CustomNameTranslator : INpgsqlNameTranslator
{
    public string TranslateMemberName(string clrName)
    {
        return clrName;
    }

    public string TranslateTypeName(string clrName)
    {
        return clrName;
    }
}