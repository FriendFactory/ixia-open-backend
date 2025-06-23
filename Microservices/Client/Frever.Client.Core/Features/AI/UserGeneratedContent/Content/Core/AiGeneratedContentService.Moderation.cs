using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Infrastructure;
using Common.Infrastructure.Utils;
using Common.Models.Files;
using FluentValidation.Results;
using Frever.Client.Core.Features.AI.UserGeneratedContent.Content.Data;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace Frever.Client.Core.Features.AI.UserGeneratedContent.Content.Core;

public partial class AiGeneratedContentService
{
    /// <summary>
    /// Runs moderation for non-moderated parts of draft and returns true if moderation has been passed.
    ///
    /// Method stores moderation results in draft in database.
    /// </summary>
    private async Task<bool> TryModerateDraft(long draftId)
    {
        var draft = await repo.GetOwnAiContent(currentUser).FirstOrDefaultAsync(c => c.Content.Id == draftId);
        if (draft == null)
            throw AppErrorWithStatusCodeException.NotFound("Draft not found", "NOT_FOUND");

        var sources = await GetModerationSources(draft);
        foreach (var s in sources)
        {
            s.Entity.ModerationResult = null;
            s.Entity.IsModerationPassed = false;
        }

        var moderationTasks = sources.Where(s => s.ItemModerationResult == null).Select(ModerateOneSource).ToArray();

        await Task.WhenAll(moderationTasks);

        var passed = sources.All(s => s.ItemModerationResult.IsPassed);

        foreach (var s in sources)
        {
            s.Entity.ModerationResult ??= new AiContentModerationResult {IsPassed = false, Items = []};
            s.Entity.ModerationResult.Items = s.Entity.ModerationResult.Items.Append(s.ItemModerationResult).ToArray();
            s.Entity.ModerationResult.IsPassed = s.Entity.ModerationResult.Items.All(i => i.IsPassed);

            s.Entity.IsModerationPassed = s.Entity.ModerationResult.IsPassed;
        }

        await repo.SaveChanges();

        return passed;
    }

    private async Task ModerateOneSource(ModerationSource item)
    {
        var result = item.MediaType == "text"
                         ? await moderationService.ModerateText(item.Value, item.ContentId, new Dictionary<string, decimal>())
                         : await moderationService.ModerateImage(item.Value, item.ContentId, new Dictionary<string, decimal>());

        item.ItemModerationResult = result;
    }

    private async Task<ValidationResult> CollectModerationErrors(long draftId)
    {
        var draft = await repo.GetOwnAiContent(currentUser).FirstOrDefaultAsync(c => c.Content.Id == draftId);
        if (draft == null)
            throw AppErrorWithStatusCodeException.NotFound("Draft not found", "NOT_FOUND");

        var sources = await GetModerationSources(draft);

        var result = new ValidationResult();

        foreach (var src in sources.Where(s => s.ItemModerationResult is {IsPassed: false}))
        {
            var failedCategories = src.ItemModerationResult.Response.Results.Where(r => r.Flagged)
                                      .SelectMany(r => r.Categories)
                                      .Where(c => c.Value)
                                      .Select(c => c.Key)
                                      .Distinct()
                                      .ToArray();
            var message = $"Moderation failed: {src.ContentId} violates following categories: {String.Join(", ", failedCategories)}";

            result.Errors.Add(new ValidationFailure(src.PropertyPath, message));
        }


        return new ValidationResult();
    }

    private async Task<ModerationSource[]> GetModerationSources(AiGeneratedContentFullData aiContent)
    {
        ArgumentNullException.ThrowIfNull(aiContent);

        var result = new List<ModerationSource>();

        if (aiContent.Image != null)
        {
            if (aiContent.Image != null)
            {
                if (!String.IsNullOrWhiteSpace(aiContent.Image.Prompt))
                    result.Add(
                        new ModerationSource
                        {
                            PropertyPath = $"{nameof(AiGeneratedContentFullInfo.Image)}.{nameof(AiGeneratedImageFullInfo.Prompt)}",
                            Entity = aiContent.Image,
                            ContentId = "prompt",
                            MediaType = "text",
                            Value = aiContent.Image.Prompt,
                            ItemModerationResult = aiContent.Image.ModerationResult?.Items.FirstOrDefault(
                                i => i.ContentId == "prompt" && i.Value == aiContent.Image.Prompt
                            )
                        }
                    );

                var file = aiContent.Image.Files?.FirstOrDefault(f => f.Type == FileMetadata.KnowFileTypeMain);

                if (file != null)
                    result.Add(
                        new ModerationSource
                        {
                            PropertyPath = $"{nameof(AiGeneratedContentFullInfo.Image)}.{nameof(AiGeneratedImageFullInfo.Files)}",
                            Entity = aiContent.Image,
                            ContentId = "main",
                            MediaType = "image",
                            Value = file.Path,
                            ItemModerationResult = aiContent.Image.ModerationResult?.Items.FirstOrDefault(
                                i => i.ContentId == "main" && i.Value == file.Path
                            )
                        }
                    );
            }
        }

        if (aiContent.Video != null)
        {
            var clips = await repo.GetAiVideoClips(aiContent.Video.Id).ToArrayAsync();

            int i = 0;
            foreach (var clip in clips)
            {
                if (!String.IsNullOrWhiteSpace(clip.Prompt))
                {
                    result.Add(
                        new ModerationSource
                        {
                            PropertyPath =
                                $"{nameof(AiGeneratedContentFullInfo.Video)}.{nameof(AiGeneratedVideoFullInfo.Clips)}[{i}].{nameof(AiGeneratedVideoClipFullInfo.Prompt)}",
                            Entity = clip,
                            ContentId = "prompt",
                            MediaType = "text",
                            Value = clip.Prompt,
                            ItemModerationResult = clip.ModerationResult?.Items.FirstOrDefault(
                                item => item.ContentId == "prompt" && item.Value == clip.Prompt
                            )
                        }
                    );
                }

                i++;
            }
        }

        return result.ToArray();
    }

    private async Task TransferModerationInfo(long sourceAiContentId, long targetAiContentId)
    {
        var source = await repo.GetOwnAiContent(currentUser).FirstOrDefaultAsync(c => c.Content.Id == sourceAiContentId);
        if (source == null)
            throw AppErrorWithStatusCodeException.NotFound("Source draft not found", "NOT_FOUND");

        var target = await repo.GetOwnAiContent(currentUser).FirstOrDefaultAsync(c => c.Content.Id == targetAiContentId);
        if (target == null)
            throw AppErrorWithStatusCodeException.NotFound("Target content not found", "NOT_FOUND");

        var src = await GetModerationSources(source);
        var dst = await GetModerationSources(target);

        foreach (var s in src.Where(a => a.ItemModerationResult != null))
        {
            var t = dst.FirstOrDefault(a => a.PropertyPath == s.PropertyPath);
            if (t == null)
                continue;

            var newResult = s.ItemModerationResult.JsonDeepClone();
            newResult.Value = t.Value;
            t.Entity.ModerationResult ??= new AiContentModerationResult {IsPassed = false, Items = []};
            t.Entity.ModerationResult.Items = t.Entity.ModerationResult.Items.Append(newResult).ToArray();
            t.Entity.ModerationResult.IsPassed = t.Entity.ModerationResult.Items.All(i => i.IsPassed);
            t.Entity.IsModerationPassed = t.Entity.ModerationResult.IsPassed;
        }

        await repo.SaveChanges();
    }

    private class ModerationSource
    {
        public required IModerationItem Entity { get; set; }
        public required string PropertyPath { get; set; }
        public required string ContentId { get; set; }
        public required string MediaType { get; set; }
        public required string Value { get; set; }
        public required AiContentItemModeration ItemModerationResult { get; set; }
    }
}