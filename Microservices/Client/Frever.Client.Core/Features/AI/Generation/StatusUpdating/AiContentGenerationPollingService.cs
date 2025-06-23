using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Frever.Client.Core.Features.AI.Generation.StatusUpdating;

public class AiContentGenerationPollingService(
    ILogger<AiContentGenerationPollingService> logger,
    IPollingJobManager jobManager,
    IPollingIntervalStrategy pollingStrategy,
    IServiceProvider serviceProvider
) : BackgroundService
{
    private static readonly string InstanceId = Guid.NewGuid().ToString();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Polling job processing service started.");

        while (!stoppingToken.IsCancellationRequested)
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);

                await ProcessPendingJobsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation("Polling job processing service is stopping.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred in the polling job processing service loop.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }

        logger.LogInformation("Polling job processing service stopped.");
    }

    private async Task ProcessPendingJobsAsync(CancellationToken stoppingToken)
    {
        var jobs = await jobManager.GetJobsDueForPolling(25);
        if (jobs == null || jobs.Count == 0)
            return;

        using var services = serviceProvider.CreateScope();
        var uploadingService = services.ServiceProvider.GetRequiredService<IAiGeneratedContentUploadingService>();

        logger.LogInformation("Processing {JobCount} jobs due for polling.", jobs.Count);
        foreach (var job in jobs)
            await ProcessJobAsync(job, uploadingService);
    }

    private async Task ProcessJobAsync(PollingJob job, IAiGeneratedContentUploadingService service)
    {
        if (!await jobManager.AcquireLock(job.ContentId, InstanceId))
        {
            logger.LogDebug("Could not acquire lock for job {JobId}. Another worker is likely processing.", job.ContentId);
            return;
        }

        logger.LogDebug("Processing job {JobId}. InstanceId: {InstanceId}", job.ContentId, InstanceId);

        try
        {
            var currentJobState = await jobManager.GetJob(job.ContentId);
            if (currentJobState is not {Status: PollingJobStatus.InProgress})
            {
                logger.LogDebug(
                    "Job {JobId} state changed while acquiring lock. Current status: {Status}",
                    job.ContentId,
                    currentJobState?.Status
                );
                return;
            }

            var config = pollingStrategy.GetConfigForContentType(currentJobState.ContentType);
            if (pollingStrategy.IsTimeout(currentJobState, config))
            {
                logger.LogWarning("Job {JobId} timed out.", currentJobState.ContentId);
                await service.SetContentGenerationFailed(currentJobState.ContentId);
                await jobManager.MarkJobAsTimedOut(currentJobState.ContentId);
                return;
            }

            if (DateTime.UtcNow < currentJobState.NextPollTime)
            {
                logger.LogDebug(
                    "Job {JobId} not yet due for polling. Next poll time: {NextPollTimeUtc}",
                    currentJobState.ContentId,
                    currentJobState.NextPollTime
                );
                return;
            }

            var result = await service.TryUploadGeneratedContent(job.ContentId, job.ContentType, job.ResultKey, job.GroupId);
            currentJobState.LastPolled = DateTime.UtcNow;
            currentJobState.PollAttempts++;

            if (result == PollingJobStatus.InProgress)
            {
                var nextInterval = pollingStrategy.CalculateNextInterval(currentJobState, config);
                currentJobState.NextPollTime = DateTime.UtcNow.Add(nextInterval);
                await jobManager.IncrementPollAttempts(currentJobState.ContentId, currentJobState.LastPolled, currentJobState.NextPollTime);
            }
            else
            {
                logger.LogInformation(
                    "Job {JobId} for content type {ContentType} completed with status {Status}.",
                    currentJobState.ContentId,
                    currentJobState.ContentType,
                    result.ToString()
                );

                if (result == PollingJobStatus.Completed)
                    await jobManager.MarkJobAsCompleted(currentJobState.ContentId);
                else
                    await jobManager.MarkJobAsFailed(currentJobState.ContentId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing job {JobId}", job.ContentId);
            await jobManager.UpdateJobStatus(job.ContentId, PollingJobStatus.InProgress, DateTime.UtcNow.Add(TimeSpan.FromSeconds(10)));
        }
        finally
        {
            await jobManager.ReleaseLock(job.ContentId);
        }
    }
}