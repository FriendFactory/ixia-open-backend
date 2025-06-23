using System.Collections.Generic;

namespace Frever.Video.Core.Features.PersonalFeed;

public class FeaturedUserCacheData
{
    public ISet<long> FeaturedGroupIds { get; set; }
}