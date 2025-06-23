using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.MediaConvert;
using Amazon.MediaConvert.Model;
using Amazon.Runtime;
using Frever.Video.Contract.Messages;

namespace VideoServer.CreateConversionJobLambda;

public class MediaConverterJobCreator
{
    private static readonly IAmazonMediaConvert MediaConvert = new AmazonMediaConvertClient(new EnvironmentVariablesAWSCredentials());

    private static bool isMediaConvertConfiguredAsRegional;

    private readonly ILambdaLogger _log;
    private readonly string _requestId;

    public MediaConverterJobCreator(ILambdaLogger log, string requestId)
    {
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _requestId = requestId;
    }

    public async Task CreateConversionJob(CreateConversionJobMessage message)
    {
        _log.LogLine($"{_requestId}: Create job to convert video Id={message.VideoId} from {message.SourceBucketPath}");
        try
        {
            var mediaConvertService = await GetConfiguredMediaConvertService();
            var jobTemplate = (await mediaConvertService.GetJobTemplateAsync(new GetJobTemplateRequest {Name = message.JobTemplateName}))
               .JobTemplate;

            var mediaInput = CreateMediaInput(jobTemplate, message.SourceBucketPath, message.HasLandscapeOrientation);

            var createJobParams = new CreateJobRequest
                                  {
                                      JobTemplate = jobTemplate.Name,
                                      Role = message.RoleArn,
                                      Settings = new JobSettings
                                                 {
                                                     Inputs = new List<Input> {mediaInput}, OutputGroups = new List<OutputGroup>()
                                                 },
                                      UserMetadata = message.UserMetadata
                                  };

            if (!string.IsNullOrWhiteSpace(message.Queue))
                createJobParams.Queue = message.Queue;

            jobTemplate.Settings.Inputs = jobTemplate.Settings.Inputs;
            foreach (var og in jobTemplate.Settings.OutputGroups)
            {
                if (og.OutputGroupSettings.HlsGroupSettings != null)
                    og.OutputGroupSettings.HlsGroupSettings.Destination = message.DestinationBucketPath;
                if (og.OutputGroupSettings.FileGroupSettings != null)
                    og.OutputGroupSettings.FileGroupSettings.Destination = message.DestinationBucketPath;
            }

            createJobParams.Settings.OutputGroups = jobTemplate.Settings.OutputGroups;
            createJobParams.StatusUpdateInterval = StatusUpdateInterval.SECONDS_10;

            var jobResponse = await mediaConvertService.CreateJobAsync(createJobParams);

            _log.LogLine($"{_requestId}: Job create: status {jobResponse.HttpStatusCode}. Job {jobResponse.Job?.Id}");
        }
        catch (Exception ex)
        {
            _log.LogLine($"{_requestId}: Error creating job for video Id={message.VideoId} src: {message.SourceBucketPath}");
            _log.LogLine(ex.ToString());

            throw;
        }
    }

    private Input CreateMediaInput(JobTemplate jobTemplate, string sourceFile, bool hasLandscapeOrientation)
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

        _log.LogLine($"{_requestId}: Video has hasLandscapeOrientation = {hasLandscapeOrientation}");
        result.VideoSelector = new VideoSelector
                               {
                                   ColorSpace = ColorSpace.FOLLOW,
                                   AlphaBehavior = AlphaBehavior.DISCARD,
                                   Rotate = hasLandscapeOrientation ? InputRotate.DEGREES_90 : InputRotate.DEGREE_0
                               };

        _log.LogLine($"{_requestId}: video rotation set to {result?.VideoSelector?.Rotate}");

        return result;
    }

    private static async Task<IAmazonMediaConvert> GetConfiguredMediaConvertService()
    {
        if (!isMediaConvertConfiguredAsRegional)
        {
            var endpoints = await MediaConvert.DescribeEndpointsAsync(new DescribeEndpointsRequest());
            var endpoint = endpoints.Endpoints[0].Url;
            ((AmazonMediaConvertConfig) MediaConvert.Config).ServiceURL = endpoint;
            isMediaConvertConfiguredAsRegional = true;
        }

        return MediaConvert;
    }
}