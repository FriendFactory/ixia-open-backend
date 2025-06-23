using System;
using System.Linq;
using System.Threading.Tasks;
using Common.Infrastructure;
using FluentValidation;
using Frever.AdminService.Core.Utils;
using Frever.Cache.Resetting;
using Frever.Client.Shared.Files;
using Frever.Shared.MainDb;
using Microsoft.AspNet.OData.Query;
using Microsoft.EntityFrameworkCore;

namespace Frever.AdminService.Core.Services.AI;

public interface IMetadataService
{
    Task<ResultWithCount<AiArtStyle>> GetArtStyles(ODataQueryOptions<AiArtStyle> options);
    Task<ResultWithCount<AiLlmPrompt>> GetLlmPrompts(ODataQueryOptions<AiLlmPrompt> options);
    Task SaveArtStyle(AiArtStyle model);
    Task SaveLlmPrompt(AiLlmPrompt model);

    Task<ResultWithCount<AiWorkflowMetadata>> GetAiWorkflowMetadata(ODataQueryOptions<AiWorkflowMetadata> options);
    Task SaveWorkflowMetadata(AiWorkflowMetadata model);
}

public class MetadataService(
    IWriteDb writeDb,
    IFileStorageService fileStorage,
    IValidator<AiArtStyle> artStyleValidator,
    IValidator<AiLlmPrompt> promptValidator,
    IValidator<AiWorkflowMetadata> workflowMetadataValidator,
    ICacheReset cacheReset
) : IMetadataService
{
    public async Task<ResultWithCount<AiArtStyle>> GetArtStyles(ODataQueryOptions<AiArtStyle> options)
    {
        var query = writeDb.AiArtStyle.Select(
            e => new AiArtStyle
                 {
                     Id = e.Id,
                     Name = e.Name,
                     Text = e.Text,
                     GenderId = e.GenderId,
                     SortOrder = e.SortOrder,
                     IsEnabled = e.IsEnabled,
                     Files = e.Files
                 }
        );

        var result = await query.ExecuteODataRequestWithCount(options);

        await fileStorage.InitUrls<Shared.MainDb.Entities.AiArtStyle>(result.Data);

        return result;
    }

    public Task<ResultWithCount<AiLlmPrompt>> GetLlmPrompts(ODataQueryOptions<AiLlmPrompt> options)
    {
        return writeDb.AiLlmPrompt.Select(e => new AiLlmPrompt {Id = e.Id, Key = e.Key, Prompt = e.Prompt})
                      .ExecuteODataRequestWithCount(options);
    }

    public async Task SaveArtStyle(AiArtStyle model)
    {
        await artStyleValidator.ValidateAndThrowAsync(model);

        var artStyle = model.Id == 0
                           ? await CreateEntity<Shared.MainDb.Entities.AiArtStyle>()
                           : await writeDb.AiArtStyle.FirstOrDefaultAsync(e => e.Id == model.Id);
        if (artStyle is null)
            throw AppErrorWithStatusCodeException.NotFound("ArtStyle not found", "ArtStyleNotFound");

        artStyle.Name = model.Name;
        artStyle.Text = model.Text;
        artStyle.SortOrder = model.SortOrder;
        artStyle.IsEnabled = model.IsEnabled;
        artStyle.Files = model.Files;
        artStyle.GenderId = model.GenderId == 0 ? artStyle.GenderId : model.GenderId;

        await writeDb.SaveChangesAsync();

        var uploader = fileStorage.CreateFileUploader();
        await uploader.UploadFiles<Shared.MainDb.Entities.AiArtStyle>(artStyle);
        await uploader.WaitForCompletion();

        await writeDb.SaveChangesAsync();
        await cacheReset.ResetOnDependencyChange(typeof(Shared.MainDb.Entities.AiArtStyle), null);
    }

    public async Task SaveLlmPrompt(AiLlmPrompt model)
    {
        await promptValidator.ValidateAndThrowAsync(model);

        var prompt = model.Id == 0
                         ? await CreateEntity<Shared.MainDb.Entities.AiLlmPrompt>()
                         : await writeDb.AiLlmPrompt.FirstOrDefaultAsync(e => e.Id == model.Id);
        if (prompt is null)
            throw AppErrorWithStatusCodeException.NotFound("Prompt not found", "PromptNotFound");

        prompt.Key = model.Key;
        prompt.Prompt = model.Prompt;

        await writeDb.SaveChangesAsync();
        await cacheReset.ResetOnDependencyChange(typeof(Shared.MainDb.Entities.AiLlmPrompt), null);
    }

    public Task<ResultWithCount<AiWorkflowMetadata>> GetAiWorkflowMetadata(ODataQueryOptions<AiWorkflowMetadata> options)
    {
        return writeDb.AiWorkflowMetadata.Select(
                           e => new AiWorkflowMetadata
                                {
                                    Id = e.Id,
                                    Description = e.Description,
                                    Key = e.Key,
                                    AiWorkflow = e.AiWorkflow,
                                    IsActive = e.IsActive,
                                    HardCurrencyPrice = e.HardCurrencyPrice,
                                    RequireBillingUnits = e.RequireBillingUnits,
                                    EstimatedLoadingTimeSec = e.EstimatedLoadingTimeSec
                                }
                       )
                      .ExecuteODataRequestWithCount(options);
    }

    public async Task SaveWorkflowMetadata(AiWorkflowMetadata model)
    {
        ArgumentNullException.ThrowIfNull(model);

        await workflowMetadataValidator.ValidateAndThrowAsync(model);

        Shared.MainDb.Entities.AiWorkflowMetadata entity;
        if (model.Id == 0)
        {
            entity = new Shared.MainDb.Entities.AiWorkflowMetadata
                     {
                         AiWorkflow = model.AiWorkflow, HardCurrencyPrice = model.HardCurrencyPrice
                     };
            writeDb.AiWorkflowMetadata.Add(entity);
        }
        else
        {
            entity = await writeDb.AiWorkflowMetadata.FirstOrDefaultAsync(e => e.Id == model.Id);
        }

        if (entity == null)
            throw AppErrorWithStatusCodeException.NotFound("Ai Workflow Metadata is not found", "NOT_FOUND");


        entity.Key = model.Key;
        entity.Description = model.Description;
        entity.AiWorkflow = model.AiWorkflow;
        entity.IsActive = model.IsActive;
        entity.HardCurrencyPrice = model.HardCurrencyPrice;
        entity.RequireBillingUnits = model.RequireBillingUnits;
        entity.EstimatedLoadingTimeSec = model.EstimatedLoadingTimeSec;

        await writeDb.SaveChangesAsync();
        await cacheReset.ResetOnDependencyChange(typeof(Shared.MainDb.Entities.AiWorkflowMetadata), null);
    }

    private async Task<T> CreateEntity<T>()
        where T : class, new()
    {
        var entity = new T();
        await writeDb.Set<T>().AddAsync(entity);
        return entity;
    }
}