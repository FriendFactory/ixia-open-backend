using System;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace Common.Infrastructure.Protobuf;

public static class ProtobufHttpRequestExtensions
{
    public static bool IsProtobufAccepted(this HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return request.Headers.Accept.Any(v => v.StartsWith(ProtobufOutputFormatter.ProtobufMimeType, StringComparison.OrdinalIgnoreCase));
    }
}