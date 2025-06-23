using System.Collections.Generic;
using AssetServer.Models.AssetFileSettingsDTO;

namespace AssetServer.Services;

public interface IConfigsService
{
    List<AssetFilesSettings> GetConfigsInfo();
}