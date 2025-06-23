using System;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using AssetServer.Shared.Messages;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using JsonSerializer = Amazon.Lambda.Serialization.Json.JsonSerializer;

[assembly: LambdaSerializer(typeof(JsonSerializer))]

namespace AssetServer.AssetCopyingLambda
{
    public class Function
    {
        private static readonly JsonSerializerSettings JsonSerializerSettings =
            new JsonSerializerSettings {ContractResolver = new CamelCasePropertyNamesContractResolver()};

        private static readonly AmazonS3Client S3Client = new AmazonS3Client(new EnvironmentVariablesAWSCredentials());


        public async Task Handler(SQSEvent input, ILambdaContext context)
        {
            foreach (var sqsMessage in input.Records)
            {
                context.Logger.LogLine("Processing message: ");
                context.Logger.LogLine(sqsMessage.Body);
                var copyEvent = JsonConvert.DeserializeObject<CopyAssetMessage>(sqsMessage.Body, JsonSerializerSettings);

                if (copyEvent == null)
                {
                    context.Logger.LogLine("ERROR: error serializing copy event");

                    return;
                }

                context.Logger.LogLine("Copy asset message successfully deserialized");
                context.Logger.LogLine($"Copying from {copyEvent.FromKey} to {copyEvent.ToKey} on bucket {copyEvent.Bucket}");

                try
                {
                    await S3Client.CopyObjectAsync(copyEvent.Bucket, copyEvent.FromKey, copyEvent.Bucket, copyEvent.ToKey);

                    if (copyEvent.Tags?.Any() ?? false)
                    {
                        await S3Client.PutObjectTaggingAsync(
                            new PutObjectTaggingRequest
                            {
                                BucketName = copyEvent.Bucket,
                                Key = copyEvent.ToKey,
                                Tagging = new Tagging
                                          {
                                              TagSet = copyEvent.Tags.Keys.Select(key => new Tag {Key = key, Value = copyEvent.Tags[key]})
                                                                .ToList()
                                          }
                            }
                        );
                    }

                    context.Logger.LogLine("Asset copied successfully");
                }
                catch (Exception ex)
                {
                    context.Logger.LogLine($"ERROR: Error copying file: {ex.Message}");
                    context.Logger.LogLine(ex.ToString());
                }
            }
        }
    }
}