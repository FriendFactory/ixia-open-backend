using Newtonsoft.Json;

namespace Frever.Video.Core.Test.Utils;

public static class CopyViaSerialization
{
    public static T CopyThroughJson<T>(this T value)
    {
        return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value));
    }
}