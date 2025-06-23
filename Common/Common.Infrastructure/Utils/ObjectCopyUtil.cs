using Frever.Protobuf;
using Newtonsoft.Json;

namespace Common.Infrastructure.Utils;

public static class ObjectCopyUtil
{
    public static T JsonDeepClone<T>(this T value)
    {
        return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value));
    }

    public static T ProtobufDeepClone<T>(this T value)
    {
        return ProtobufConvert.DeserializeObject<T>(ProtobufConvert.SerializeObject(value));
    }
}