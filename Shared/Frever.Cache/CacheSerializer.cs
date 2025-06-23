using System;
using Frever.Cache.Strategies;
using Frever.Protobuf;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Frever.Cache;

public static class CacheSerializer
{
    public static TData FromValue<TData>(this RedisValue key, SerializeAs? serializeAs = default)
    {
        if ((serializeAs ?? SerializeAs.Protobuf) == SerializeAs.Protobuf)
        {
            var data = (byte[]) key;
            return ProtobufConvert.DeserializeObject<TData>(data);
        }

        return JsonConvert.DeserializeObject<TData>(key);
    }

    public static object FromValue(this RedisValue key, Type type, SerializeAs? serializeAs = default)
    {
        if ((serializeAs ?? SerializeAs.Protobuf) == SerializeAs.Protobuf)
        {
            var data = (byte[]) key;
            return ProtobufConvert.DeserializeObject(type, data);
        }

        return JsonConvert.DeserializeObject(key, type);
    }

    public static RedisValue ToRedisValue<TData>(this TData value, SerializeAs? serializeAs = default)
    {
        if ((serializeAs ?? SerializeAs.Protobuf) == SerializeAs.Protobuf)
        {
            var data = ProtobufConvert.SerializeObject(value);
            return data;
        }

        return JsonConvert.SerializeObject(value);
    }
}