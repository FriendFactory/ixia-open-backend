using System;

namespace Common.Infrastructure.Caching.CacheKeys;

public static class CacheKeyVersion
{
    // whenever we add another version, we need to add it here
    private const string AnotherEnvironmentVersion = "";
    private static string _keyVersion;

    public static void SetKeyVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
            throw new ArgumentNullException(nameof(version));

        _keyVersion = $"{version}/";
    }

    public static string[] AllKeyVersionedCache(this string key)
    {
        if (string.IsNullOrWhiteSpace(AnotherEnvironmentVersion))
            return [key];

        return [key, key.Replace(_keyVersion, AnotherEnvironmentVersion)];
    }

    public static string FreverVersionedCache(this string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(key));

        return $"{_keyVersion}{key}";
    }

    public static string FreverUnversionedCache(this string key)
    {
        return key;
    }

    public static string GetKeyWithoutVersion(this string versionedKey)
    {
        if (string.IsNullOrWhiteSpace(versionedKey))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(versionedKey));

        return versionedKey.Replace(_keyVersion, "");
    }
}