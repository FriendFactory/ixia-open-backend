namespace Common.Models;

public static class CreatePageContentType
{
    public const string Video = "videos";
    public const string Hashtag = "hashtags";
    public const string Song = "songs";
    public const string Image = "image";

    public static string[] GetAllContentTypes()
    {
        return [Video, Hashtag, Song, Image];
    }
}

public static class CreatePageContentQuery
{
    public const string PopularVideoRemixes = "popular_video_remixes";
    public const string PopularHashtags = "popular_hashtags";
    public const string PopularSongs = "popular_songs";

    public static string[] GetAllContentQueries()
    {
        return
        [
            PopularVideoRemixes, PopularHashtags, PopularSongs
        ];
    }

    public static string[] GetTypeQueries(string contentType)
    {
        return contentType switch
               {
                   CreatePageContentType.Video   => [PopularVideoRemixes],
                   CreatePageContentType.Hashtag => [PopularHashtags],
                   CreatePageContentType.Song    => [PopularSongs],
                   _                             => []
               };
    }
}