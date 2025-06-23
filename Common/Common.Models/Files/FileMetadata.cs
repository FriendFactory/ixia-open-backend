namespace Common.Models.Files;

public interface IFileMetadataOwner
{
    public long Id { get; set; }
    public FileMetadata[] Files { get; set; }
}

/// <summary>
///     Marks an entity which provides configuration for files.
///     There could be many models and DTOs etc. with file configuration inherited from certain entity.
/// </summary>
public interface IFileMetadataConfigRoot : IFileMetadataOwner;

public class FileMetadata
{
    public static readonly string KnowFileTypeMain = "main";
    public static readonly string KnowFileTypePrefixThumbnail = "thumbnail";

    /// <summary>
    ///     Optional file source. Received non-null source indicates that file needs to be updated.
    ///     Received from client.
    /// </summary>
    public FileSourceInfo Source { get; set; }

    /// <summary>
    ///     Not null version of the file. Generates on each updating of file content.
    ///     Saved in database.
    /// </summary>
    public string Version { get; set; }

    /// <summary>
    ///     Non-empty file type. Should be unique for one entity.
    ///     Saved in database.
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    ///     Path (key) of stored file content.
    ///     Saved in database.
    /// </summary>
    public string Path { get; set; }

    /// <summary>
    ///     Signed URL allows to access file content from the client.
    ///     Generated on sending file to client.
    /// </summary>
    public string Url { get; set; }
}

public class FileSourceInfo
{
    /// <summary>
    ///     Source file key.
    ///     Could be:
    ///     - Signed URL
    ///     - s3://bucket/key
    /// </summary>
    public string SourceFile { get; set; }

    public byte[] SourceBytes { get; set; }
}