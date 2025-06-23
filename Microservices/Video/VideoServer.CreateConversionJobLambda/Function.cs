using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Frever.Video.Contract.Messages;
using Newtonsoft.Json;
using JsonSerializer = Amazon.Lambda.Serialization.Json.JsonSerializer;

[assembly: LambdaSerializer(typeof(JsonSerializer))]

namespace VideoServer.CreateConversionJobLambda;

public class Function
{
    public Task Handler(SQSEvent input, ILambdaContext context)
    {
        var jobCreator = new MediaConverterJobCreator(context.Logger, context.AwsRequestId);
        foreach (var record in input.Records)
        {
            context.Logger.LogLine($"{context.AwsRequestId}: Processing message {record.MessageId}");
            context.Logger.LogLine($"{context.AwsRequestId}: Body {record.Body}");
            var message = JsonConvert.DeserializeObject<CreateConversionJobMessage>(record.Body);

            jobCreator.CreateConversionJob(message).Wait();
        }

        return Task.CompletedTask;
    }
}