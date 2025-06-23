using System.Threading.Tasks;
using Frever.AdminService.Core.Utils;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Http;

namespace Frever.AdminService.Core.Services.Localizations;

public interface ILocalizationModerationService
{
    Task<ResultWithCount<LocalizationDto>> GetLocalization(ODataQueryOptions<LocalizationDto> options, string isoCode, string value);
    Task SaveLocalization(LocalizationDto model);
    Task DeleteLocalizationByKey(string key);
    Task<byte[]> ExportLocalizationToCsv(string[] keys);
    Task ImportLocalizationFromCsv(IFormFile file, ImportType type);
}