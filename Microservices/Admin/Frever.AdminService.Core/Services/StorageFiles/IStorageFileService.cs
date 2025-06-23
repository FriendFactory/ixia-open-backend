using System.Threading.Tasks;
using Frever.AdminService.Core.Services.StorageFiles.Contracts;
using Frever.AdminService.Core.Utils;
using Microsoft.AspNet.OData.Query;

namespace Frever.AdminService.Core.Services.StorageFiles;

public interface IStorageFileService
{
    Task<ResultWithCount<StorageFileDto>> GetAll(ODataQueryOptions<StorageFileDto> options);

    Task SaveStorageFile(UploadStorageFileModel model);

    Task DeleteStorageFile(long id);
}