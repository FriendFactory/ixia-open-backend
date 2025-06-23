using System;
using System.Collections.Generic;

namespace AssetServer.Services.Storage;

public class S3FileInfo
{
    public static readonly S3FileInfo NotFound = new();

    private S3FileInfo() { }

    public S3FileInfo(string key, IDictionary<string, string> tags)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(key));

        Key = key;
        Tags = tags ?? throw new ArgumentNullException(nameof(tags));
        IsFileExists = true;
        TagInfo = new FileTagInfo(tags);
    }

    public bool IsFileExists { get; }

    public string Key { get; }

    public IDictionary<string, string> Tags { get; }

    public FileTagInfo TagInfo { get; }
}