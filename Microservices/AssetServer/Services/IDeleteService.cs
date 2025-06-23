using System;
using System.Threading.Tasks;

namespace AssetServer.Services;

public interface IDeleteService
{
    Task DeleteAsset(Type assetType, long id);
}