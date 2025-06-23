using System.Threading.Tasks;
using Frever.Client.Core.Features.Social.DataAccess;

namespace Frever.Client.Core.Features.Social.PublicProfiles;

public class PublicProfileService(IMainDbRepository mainDbRepository) : IPublicProfileService
{
    public Task<PublicProfile> GetPublicProfile(string nickname)
    {
        return string.IsNullOrWhiteSpace(nickname) ? null : mainDbRepository.GetPublicProfile(nickname);
    }
}