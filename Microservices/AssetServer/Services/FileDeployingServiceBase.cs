using System;
using Common.Models.Database.Interfaces;
using Common.Models.Files;

namespace AssetServer.Services;

internal abstract class FileDeployingServiceBase
{
    protected static bool NeedSendFileSizeInResponse(Type assetType, FileType fileType)
    {
        return typeof(ISizeStorable).IsAssignableFrom(assetType) && fileType == FileType.MainFile;
    }
}