using System;

namespace Frever.Client.Shared.Files;

public class StorageReference
{
    private const string EncodingPrefix = "sr:";

    public required string Key { get; set; }
    public string Bucket { get; set; }

    public static string Encode(string key, string bucket = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return string.IsNullOrWhiteSpace(bucket) ? $"{EncodingPrefix}{key}" : $"{EncodingPrefix}{bucket}|{key}";
    }

    public static StorageReference Decode(string input, string defaultBucket)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input);

        if (!input.StartsWith(EncodingPrefix, StringComparison.Ordinal))
            throw new ArgumentException("Input string is not a valid encoded StorageReference.", nameof(input));

        var encodedValue = input[EncodingPrefix.Length..];
        var parts = encodedValue.Split('|', 2);
        var bucket = parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[0]) ? parts[0] : defaultBucket;
        var key = parts.Length == 2 ? parts[1] : parts[0];

        return new StorageReference {Bucket = bucket, Key = key};
    }

    public static bool TryDecode(string input, string defaultBucket, out StorageReference storageReference)
    {
        storageReference = null;
        if (string.IsNullOrWhiteSpace(input) || !input.StartsWith(EncodingPrefix, StringComparison.Ordinal))
            return false;

        storageReference = Decode(input, defaultBucket);
        return true;
    }
}