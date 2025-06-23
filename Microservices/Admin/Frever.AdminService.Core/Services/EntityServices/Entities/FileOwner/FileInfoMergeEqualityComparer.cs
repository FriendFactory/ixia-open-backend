using System.Collections.Generic;
using Common.Infrastructure.Utils;
using Common.Models;
using Common.Models.Files;

namespace Frever.AdminService.Core.Services.EntityServices;

public class FileInfoMergeEqualityComparer : IEqualityComparer<FileInfo>
{
    public static readonly IEqualityComparer<FileInfo> Instance = new FileInfoMergeEqualityComparer();

    private FileInfoMergeEqualityComparer() { }

    public bool Equals(FileInfo x, FileInfo y)
    {
        if (x == null || y == null)
            return x == y;

        return x.File == y.File && x.Resolution == y.Resolution && x.Platform == y.Platform && VersionsEqual(x, y) && TagsEqual(x, y);
    }

    private static bool VersionsEqual(FileInfo x, FileInfo y)
    {
        if (x.UnityVersion.ParseVersion() == y.UnityVersion.ParseVersion())
            return true;
        if (x.UnityVersion == null && (y.UnityVersion?.Contains(Constants.DefaultAssetUnityVersion) ?? false))
            return true;
        return y.UnityVersion == null && (x.UnityVersion?.Contains(Constants.DefaultAssetUnityVersion) ?? false);
    }

    //Currently, it only compares whether there is data in Tags property,
    //that is, it allows only 1 file with null value and 1 file with a field containing data
    private static bool TagsEqual(FileInfo x, FileInfo y)
    {
        if (IsNullOrEmpty(x.Tags) && IsNullOrEmpty(y.Tags))
            return true;

        return x.Tags is {Length: > 0} && y.Tags is {Length: > 0};

        static bool IsNullOrEmpty<T>(T[] array)
        {
            return array == null || array.Length == 0;
        }
    }

    public int GetHashCode(FileInfo obj)
    {
        return obj.File.GetHashCode() ^ (obj.Resolution ?? 0).GetHashCode();
    }
}