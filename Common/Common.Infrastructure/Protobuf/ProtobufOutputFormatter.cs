using System;
using System.Linq;
using System.Threading.Tasks;
using Frever.Protobuf;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;

namespace Common.Infrastructure.Protobuf;

public class ProtobufOutputFormatter : OutputFormatter
{
    public const string ProtobufMimeType = "application/vnd.google.protobuf";

    public ProtobufOutputFormatter()
    {
        SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse(ProtobufMimeType));
    }

    protected override bool CanWriteType(Type type)
    {
        return true;
    }

    public override bool CanWriteResult(OutputFormatterCanWriteContext context)
    {
        return context.HttpContext.Request.Headers.Accept.Any(v => v.Contains(ProtobufMimeType, StringComparison.OrdinalIgnoreCase));
    }

    public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
    {
        var buffer = ProtobufConvert.SerializeObject(context.Object);
        await context.HttpContext.Response.Body.WriteAsync(buffer);
    }
}