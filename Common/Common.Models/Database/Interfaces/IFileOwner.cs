using System.Collections.Generic;
using Common.Models.Files;
using Newtonsoft.Json;

namespace Common.Models.Database.Interfaces;

/// <summary>
///     Entity has thumbnail or main file  stored in S3
/// </summary>
public interface IFileOwner : IEntity
{
    [JsonIgnore] string FilesInfo { get; set; }

    List<FileInfo> Files { get; set; }
}