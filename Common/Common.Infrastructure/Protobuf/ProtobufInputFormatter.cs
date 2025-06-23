using System.IO;
using System.Threading.Tasks;
using Frever.Protobuf;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;

namespace Common.Infrastructure.Protobuf;

public class ProtobufInputFormatter : InputFormatter
{
    public ProtobufInputFormatter()
    {
        SupportedMediaTypes.Clear();
        SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse(ProtobufOutputFormatter.ProtobufMimeType));
    }

    public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context)
    {
        using var buffer = new MemoryStream();
        await context.HttpContext.Request.Body.CopyToAsync(buffer);

        var instance = ProtobufConvert.DeserializeObject(context.ModelType, buffer.ToArray());

        return await InputFormatterResult.SuccessAsync(instance);
    }
}