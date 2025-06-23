using System.Threading.Tasks;

namespace Frever.Client.Core.Features.Social.PublicProfiles;

public interface IPublicProfileService
{
    Task<PublicProfile> GetPublicProfile(string nickname);
}