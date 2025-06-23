using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.MediaConvert;
using Amazon.MediaConvert.Model;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using AssetStoragePathProviding;
using Common.Infrastructure.Aws;
using Microsoft.Extensions.DependencyInjection;

namespace ImageConverter
{
    public class S3Converter
    {
        private const string CONVERSION_JOB_TEMPLATE_NAME_ENV_VAR = "CONVERSION_JOB_NAME";
        private const string CONVERSION_JOB_ROLE_ARN_ENV_VAR = "CONVERSION_JOB_ROLE_ARN";

        private static IAmazonMediaConvert _mediaConvert;

        private readonly AmazonS3Client _client = new AmazonS3Client(new EnvironmentVariablesAWSCredentials());

        public async Task ConvertMedia(S3Event.S3EventNotificationRecord record, ILambdaContext context)
        {
            Log(context, $"Processing record {record.EventName}");

            var key = record.S3.Object.Key;
            if (!FileConversionHelper.IsMarkedForConversion(key))
            {
                Log(context, $"File {key} is not marked for conversion, add _convert suffix before extension");

                return;
            }

            Log(context, $"Processing {record.S3.Bucket}::{record.S3.Object.Key}...");

            try
            {
                var conversionInfo = FileConversionHelper.GetConversionInfoFromPath(record.S3.Object.Key);

                if (conversionInfo.MediaType == MediaType.Image)
                {
                    Log(context, "Media recognized as image");
                    if (conversionInfo.NeedsConversion)
                    {
                        Log(context, "Image requires conversion");
                        await ConvertImage(record, conversionInfo, context);
                    }
                }
                else if (conversionInfo.MediaType == MediaType.Video)
                {
                    Log(context, "Media recognized as video");
                    if (conversionInfo.NeedsConversion)
                    {
                        Log(context, "Video requires conversion");
                        await ConvertVideo(record, conversionInfo, context);
                    }
                }
                else
                {
                    Log(context, "Media type is not supported");
                }
            }
            catch (Exception ex)
            {
                Log(context, $"Error processing {record.S3.Object.Key}:");
                Log(context, ex.ToString());
            }
        }

        private async Task ConvertImage(
            S3Event.S3EventNotificationRecord record,
            ConversionInfo conversionInfo,
            ILambdaContext context
        )
        {
            Log(context, $"Convert image {conversionInfo.OriginalFileExtension} to {conversionInfo.TargetFileExtension}");

            var newFileName = FileConversionHelper.GetTargetFilePath(record.S3.Object.Key);

            var content = await _client.GetObjectAsync(record.S3.Bucket.Name, record.S3.Object.Key);
            Log(context, "File downloaded");

            await using var targetFile = await ImageConversionService.Convert(content.ResponseStream);

            Log(context, "File converted");

            Log(context, $"Converted file name {newFileName}");

            await _client.PutObjectAsync(
                new PutObjectRequest
                {
                    Key = newFileName,
                    BucketName = record.S3.Bucket.Name,
                    ContentType = "image/jpeg",
                    InputStream = targetFile
                }
            );

            Log(context, "File written back to bucket");
        }

        private async Task ConvertVideo(
            S3Event.S3EventNotificationRecord record,
            ConversionInfo conversionInfo,
            ILambdaContext context
        )
        {
            Log(
                context,
                $"Converting video at {record.S3.Bucket.Name}:://{record.S3.Object.Key} " +
                $"from {conversionInfo.OriginalFileExtension} to {conversionInfo.TargetFileExtension}"
            );

            var jobTemplateName = Environment.GetEnvironmentVariable(CONVERSION_JOB_TEMPLATE_NAME_ENV_VAR) ?? "VideoClipConversion";
            var jobRoleArn = Environment.GetEnvironmentVariable(CONVERSION_JOB_ROLE_ARN_ENV_VAR);

            if (String.IsNullOrWhiteSpace(jobRoleArn))
            {
                Log(context, $"ERROR: {CONVERSION_JOB_ROLE_ARN_ENV_VAR} environment variable is not provided");

                return;
            }

            Log(context, $"Conversion job template name is {jobTemplateName}");

            var sourceFileKey = $"tmp-{FileConversionHelper.GetOriginalFilePath(record.S3.Object.Key)}";

            var sourceFilePath = $"s3://{record.S3.Bucket.Name}/{sourceFileKey}";
            Log(context, $"Source file path {sourceFilePath}");

            Log(context, $"Copying {record.S3.Object.Key} to {sourceFileKey}");
            await _client.CopyObjectAsync(record.S3.Bucket.Name, record.S3.Object.Key, record.S3.Bucket.Name, sourceFileKey);

            var services = BuildServices();

            var awsRetryHelper = services.GetRequiredService<IAwsRetryHelper>();
            var mediaConvert = await GetConfiguredMediaConvertService(awsRetryHelper, context);

            var jobTemplate = (await awsRetryHelper.FightAwsThrottling(
                                   () => mediaConvert.GetJobTemplateAsync(new GetJobTemplateRequest {Name = jobTemplateName}, default)
                               )).JobTemplate;

            if (jobTemplate == null)
            {
                Log(context, $"ERROR: Job template with name {jobTemplateName} is not found");

                return;
            }

            var createJobParams = new CreateJobRequest
                                  {
                                      JobTemplate = jobTemplate.Name,
                                      Role = jobRoleArn,
                                      Settings = new JobSettings
                                                 {
                                                     Inputs = new List<Input> {CreateMediaInput(jobTemplate, sourceFilePath)},
                                                     OutputGroups = new List<OutputGroup>()
                                                 },
                                      StatusUpdateInterval = StatusUpdateInterval.SECONDS_10
                                  };

            var newFileName = Path.ChangeExtension(FileConversionHelper.GetOriginalFilePath(record.S3.Object.Key), "")?.TrimEnd('.');

            var destinationBucketPath = $"s3://{record.S3.Bucket.Name}/{newFileName}";
            Log(context, $"Destination bucket path {destinationBucketPath}");

            jobTemplate.Settings.Inputs = jobTemplate.Settings.Inputs;
            foreach (var og in jobTemplate.Settings.OutputGroups)
            {
                if (og.OutputGroupSettings.HlsGroupSettings != null)
                    og.OutputGroupSettings.HlsGroupSettings.Destination = destinationBucketPath;
                if (og.OutputGroupSettings.FileGroupSettings != null)
                    og.OutputGroupSettings.FileGroupSettings.Destination = destinationBucketPath;
            }

            createJobParams.Settings.OutputGroups = jobTemplate.Settings.OutputGroups;

            var jobResponse = await awsRetryHelper.FightAwsThrottling(() => _mediaConvert.CreateJobAsync(createJobParams, default));

            Log(context, $"Job {jobResponse.Job.Id} is created, status {jobResponse.Job.Status}");
        }

        private static void Log(ILambdaContext context, string message)
        {
            LambdaLogger.Log($"{context.AwsRequestId}: {message}{Environment.NewLine}");
        }

        private static IServiceProvider BuildServices()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddAwsRetryHelpers();
            serviceCollection.AddLogging();

            return serviceCollection.BuildServiceProvider();
        }

        private async Task<IAmazonMediaConvert> GetConfiguredMediaConvertService(IAwsRetryHelper awsRetryHelper, ILambdaContext context)
        {
            if (_mediaConvert == null)
            {
                _mediaConvert = new AmazonMediaConvertClient(new EnvironmentVariablesAWSCredentials());

                var endpoints = await awsRetryHelper.FightAwsThrottling(
                                    () => _mediaConvert.DescribeEndpointsAsync(new DescribeEndpointsRequest(), default)
                                );

                var endpoint = endpoints.Endpoints[0].Url;
                ((AmazonMediaConvertConfig) _mediaConvert.Config).ServiceURL = endpoint;

                Log(context, $"MediaConvert endpoint: {endpoint}");
            }

            return _mediaConvert;
        }

        private Input CreateMediaInput(JobTemplate jobTemplate, string sourceFile)
        {
            var result = new Input();
            var inputTemplate = jobTemplate.Settings.Inputs[0];

            var jobProps = result.GetType().GetProperties();
            var templateProps = jobTemplate.GetType().GetProperties();

            foreach (var jobProperty in jobProps)
            {
                var templateProperty = templateProps.FirstOrDefault(
                    p => p.Name == jobProperty.Name && jobProperty.PropertyType.IsAssignableFrom(p.PropertyType)
                );
                if (templateProperty != null)
                    jobProperty.SetValue(result, templateProperty.GetValue(inputTemplate));
            }

            result.FileInput = sourceFile;

            return result;
        }
    }
}