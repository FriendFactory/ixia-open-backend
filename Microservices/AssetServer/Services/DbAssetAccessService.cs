using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Common.Infrastructure.Utils;
using Common.Models.Database.Interfaces;
using Common.Models.Files;
using Frever.Shared.MainDb;
using Microsoft.EntityFrameworkCore;

namespace AssetServer.Services
{
    internal sealed class DbAssetAccessService(IWriteDb db) : IAssetAccessService
    {
        private static readonly MethodInfo GetAssetFromDbMethod = typeof(DbAssetAccessService).GetMethod(nameof(GetAssetFromDbCore));

        private readonly IWriteDb _db = db ?? throw new ArgumentNullException(nameof(db));

        public async Task<FileInfo> GetAssetFileFromDb(
            Type assetType,
            long id,
            Platform? platform,
            FileType fileType,
            Resolution? resolution = null,
            FileExtension? extension = null
        )
        {
            var method = GetAssetFromDbMethod.MakeGenericMethod(assetType);

            var fileInfo = await method.InvokeAsync<FileInfo>(
                               this,
                               id,
                               platform,
                               fileType,
                               resolution,
                               extension
                           );

            return fileInfo;
        }

        public async Task<FileInfo> GetAssetFromDbCore<T>(
            long id,
            Platform? platform,
            FileType fileType,
            Resolution? resolution = null,
            FileExtension? extension = null
        )
            where T : class, IFileOwner
        {
            var entity = await _db.Set<T>().FirstOrDefaultAsync(e => e.Id == id);

            var file = entity?.Files?.FirstOrDefault(f => f.File == fileType && (resolution == null || f.Resolution == resolution));

            return file;
        }
    }
}