using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Infrastructure.Database;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace Frever.Shared.MainDb;

public interface IWriteDb
{
    DbSet<AiWorkflowMetadata> AiWorkflowMetadata { get; set; }
    DbSet<AiLlmPrompt> AiLlmPrompt { get; set; }
    DbSet<AiOpenAiKey> AiOpenAiKey { get; set; }
    DbSet<AiOpenAiAgent> AiOpenAiAgent { get; set; }
    DbSet<AiArtStyle> AiArtStyle { get; set; }
    DbSet<AiCharacter> AiCharacter { get; set; }
    DbSet<AiCharacterImage> AiCharacterImage { get; set; }
    DbSet<AiGeneratedContent> AiGeneratedContent { get; set; }
    DbSet<AiGeneratedImage> AiGeneratedImage { get; set; }
    DbSet<AiGeneratedImagePerson> AiGeneratedImagePerson { get; set; }
    DbSet<AiGeneratedImageSource> AiGeneratedImageSource { get; set; }
    DbSet<AiGeneratedVideo> AiGeneratedVideo { get; set; }
    DbSet<AiGeneratedVideoClip> AiGeneratedVideoClip { get; set; }
    DbSet<AiMakeUp> AiMakeUp { get; set; }
    DbSet<AiSpeakerMode> AiSpeakerMode { get; set; }
    DbSet<AiLanguageMode> AiLanguageMode { get; set; }
    DbSet<AppleSignInEmail> AppleSignInEmail { get; set; }
    DbSet<AssetStoreTransaction> AssetStoreTransactions { get; set; }
    DbSet<BlockedUser> BlockedUser { get; set; }
    DbSet<Comment> Comments { get; set; }
    DbSet<CommentLike> CommentLikes { get; set; }
    DbSet<Country> Country { get; set; }
    DbSet<ContentRow> ContentRow { get; set; }
    DbSet<DeviceBlacklist> DeviceBlacklist { get; set; }
    DbSet<ExternalSong> ExternalSongs { get; set; }
    DbSet<FavoriteSound> FavoriteSound { get; set; }
    DbSet<Follower> Follower { get; set; }
    DbSet<FollowerStats> FollowerStats { get; set; }
    DbSet<Gender> Gender { get; set; }
    DbSet<GeoCluster> GeoCluster { get; set; }
    DbSet<Group> Group { get; set; }
    DbSet<GroupBioLink> GroupBioLink { get; set; }
    DbSet<HardCurrencyExchangeOffer> HardCurrencyExchangeOffer { get; set; }
    DbSet<Hashtag> Hashtag { get; set; }
    DbSet<InAppPurchaseOrder> InAppPurchaseOrder { get; set; }
    DbSet<InAppProduct> InAppProduct { get; set; }
    DbSet<InAppUserSubscription> InAppUserSubscription { get; set; }
    DbSet<InAppProductDetails> InAppProductDetails { get; set; }
    DbSet<InAppProductPriceTier> InAppProductPriceTier { get; set; }
    DbSet<Language> Language { get; set; }
    DbSet<Like> Like { get; set; }
    DbSet<Localization> Localization { get; set; }
    DbSet<Notification> Notification { get; set; }
    DbSet<NotificationAndGroup> NotificationAndGroup { get; set; }
    DbSet<Platform> Platform { get; set; }
    DbSet<PromotedSong> PromotedSong { get; set; }
    DbSet<Readiness> Readiness { get; set; }
    DbSet<Role> Role { get; set; }
    DbSet<RoleAccessScope> RoleAccessScope { get; set; }
    DbSet<Song> Song { get; set; }
    DbSet<StorageFile> StorageFile { get; set; }
    DbSet<User> User { get; set; }
    DbSet<UserAndGroup> UserAndGroup { get; set; }
    DbSet<UserRole> UserRole { get; set; }
    DbSet<UserSound> UserSound { get; set; }
    DbSet<UserActivity> UserActivities { get; set; }
    DbSet<UserActionSetting> UserActionSettings { get; set; }
    DbSet<VerticalCategory> VerticalCategory { get; set; }
    DbSet<Video> Video { get; set; }
    DbSet<VideoKpi> VideoKpi { get; set; }
    DbSet<VideoAndHashtag> VideoAndHashtag { get; set; }
    DbSet<VideoGroupTag> VideoGroupTag { get; set; }
    DbSet<VideoReportReason> VideoReportReason { get; set; }
    DbSet<VideoReport> VideoReport { get; set; }
    DbSet<VideoView> VideoView { get; set; }

    IModel Model { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = new());
    Task<IQueryable<Video>> GetGroupAvailableVideoQuery(long groupId, long currentGroupId);

    IQueryable<BalanceInfo> GetGroupBalanceInfo(long[] groupIds);
    IQueryable<GroupActiveSubscriptionInfo> GetGroupActiveSubscriptions(bool excludeRefilledDailyTokens);

    Task<int> GetTaggedGroupVideoCount(long groupId, long currentGroupId);

    Task<IDbContextTransaction> BeginTransaction();
    Task<int> ExecuteSqlInterpolatedAsync(FormattableString sql, CancellationToken cancellationToken = default);

    DbSet<T> Set<T>()
        where T : class;

    Task<int> ExecuteSqlRawAsync(string sql, params object[] parameters);

    IQueryable<TResult> SqlQueryRaw<TResult>([NotParameterized] string sql, params object[] parameters);

    EntityEntry<TEntity> Entry<TEntity>(TEntity entity)
        where TEntity : class;

    NpgsqlConnection GetDbConnection();
    Task<NestedTransaction> BeginTransactionSafe();
    Task<NestedTransaction> BeginTransactionSafe(IsolationLevel isolationLevel);
}