using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Frever.AdminService.Core.Services.InAppPurchases.OfferGenerationInfo;

public interface IInAppOfferGenerationInfoService
{
    Task<JObject> GetOfferGenerationDebugInfo(long groupId);
}