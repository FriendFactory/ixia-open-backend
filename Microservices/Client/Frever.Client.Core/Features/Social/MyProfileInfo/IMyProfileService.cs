using System.Threading.Tasks;
using Frever.ClientService.Contract.Social;

namespace Frever.Client.Core.Features.Social.MyProfileInfo;

public interface IMyProfileService
{
    Task<MyProfile> Me();
    Task<MyProfile> UpdateProfile(UpdateProfileRequest request);
    Task DeleteMe();
    Task<UserBalance> GetMyBalance();
    Task AddInitialBalance();
    Task SetMyStatusOnline();
    Task AddMyMyAdvertisingTracking(string appsFlyerId);
    Task DeleteMyAdvertisingTracking();
}