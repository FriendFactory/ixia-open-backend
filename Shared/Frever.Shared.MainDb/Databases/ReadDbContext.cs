using System;
using System.Linq;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;

namespace Frever.Shared.MainDb;

public class ReadDbContext : MainDbContext, IReadDb
{
    public ReadDbContext(ILoggerFactory loggerFactory) : base(loggerFactory) { }
    public ReadDbContext(DbContextOptions<ReadDbContext> options, ILoggerFactory loggerFactory) : base(options, loggerFactory) { }
    public ReadDbContext(DbContextOptions options, ILoggerFactory loggerFactory, bool stub) : base(options, loggerFactory, stub) { }

    public IQueryable<Country> Country => base.Country;
    public IQueryable<ExternalPlaylist> ExternalPlaylists => base.ExternalPlaylists;
    public IQueryable<ExternalSong> ExternalSongs => base.ExternalSongs;
    public IQueryable<Follower> Follower => base.Follower;
    public IQueryable<Gender> Gender => base.Gender;
    public IQueryable<Genre> Genre => base.Genre;
    public IQueryable<GeoCluster> GeoCluster => base.GeoCluster;
    public IQueryable<Group> Group => base.Group;
    public IQueryable<Hashtag> Hashtag => base.Hashtag;
    public IQueryable<InAppProductDetails> InAppProductDetails => base.InAppProductDetails;
    public IQueryable<Language> Language => base.Language;
    public IQueryable<Localization> Localization => base.Localization;
    public IQueryable<PromotedSong> PromotedSong => base.PromotedSong;
    public IQueryable<Song> Song => base.Song;
    public IQueryable<User> User => base.User;
    public IQueryable<Video> Video => base.Video;
    public IQueryable<VideoGroupTag> VideoGroupTag => base.VideoGroupTag;
    public IQueryable<VideoAndHashtag> VideoAndHashtag => base.VideoAndHashtag;
    public IQueryable<VideoKpi> VideoKpi => base.VideoKpi;
    public IQueryable<VideoReport> VideoReport => base.VideoReport;
    public IQueryable<VideoView> VideoView => base.VideoView;

    public IQueryable<TResult> SqlQuery<TResult>(FormattableString sql)
    {
        return Database.SqlQuery<TResult>(sql);
    }

    public IQueryable<TResult> SqlQueryRaw<TResult>([NotParameterized] string sql, params object[] parameters)
    {
        return Database.SqlQueryRaw<TResult>(sql, parameters);
    }
}