using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.Lambda.Serialization.Json;
using Amazon.S3;

[assembly: LambdaSerializer(typeof(JsonSerializer))]

namespace ImageConverter
{
    class Program
    {
        // Used for local run only. Not used when running in AWS Lambda
        static async Task Main(string[] args)
        {
            if (args.Length < 2 || String.IsNullOrWhiteSpace(args[0]) || String.IsNullOrWhiteSpace(args[1]))
            {
                Console.WriteLine($"Usage: ImageConverter source.png target.jpg");
                Environment.Exit(1);
            }

            var inputFilePath = args[0];
            var outputFilePath = args[1];

            var service = new ImageConversionService();

            using var inputImage = File.OpenRead(inputFilePath);
            using var outputImage = await ImageConversionService.Convert(inputImage);

            using var outputFile = File.OpenWrite(outputFilePath);
            await outputImage.CopyToAsync(outputFile);
        }
    }

    //it triggers by AWS Lambda, that's why it does not have any references from code
    public class Function
    {
        private static readonly EventType[] SupportedEvents =
        {
            EventType.ObjectCreatedAll, EventType.ObjectCreatedCopy, EventType.ObjectCreatedPost, EventType.ObjectCreatedPut
        };

        public async Task Handler(S3Event input, ILambdaContext context)
        {
            var converter = new S3Converter();
            foreach (var record in input.Records)
            {
                if (SupportedEvents.Any(e => e.Equals(record.EventName)))
                {
                    await converter.ConvertMedia(record, context);
                }
            }
        }
    }
}