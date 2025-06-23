using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Infrastructure;
using Common.Models.Files;
using Frever.Client.Core.Features.AI.UserGeneratedContent.Content.Input;
using Frever.Client.Shared.Files;
using AiGeneratedContentEntity = Frever.Shared.MainDb.Entities.AiGeneratedContent;


namespace Frever.Client.Core.Features.AI.UserGeneratedContent.Content.Core;

public partial class AiGeneratedContentService
{
    public async Task<AiGeneratedContentFullInfo> Publish(long aiGeneratedContentId)
    {
        var draft = await GetById(aiGeneratedContentId);
        if (draft == null)
            throw AppErrorWithStatusCodeException.NotFound("AI Generated Content is not found", "NOT_FOUND");

        if (draft.GroupId != currentUser)
            throw AppErrorWithStatusCodeException.Forbidden("AI Generated Content is not accessible", "FORBIDDEN");

        if (!await TryModerateDraft(draft.Id))
        {
            var errors = await CollectModerationErrors(draft.Id);
            throw new AiContentModerationException("Draft cannot be published because it has not been passed moderation", errors.Errors);
        }

        var input = CopyContent(draft);

        var userSoundIds = input.Video?.Clips.Where(e => e.UserSoundId.HasValue).Select(e => e.UserSoundId.Value).ToArray() ?? [];
        foreach (var id in userSoundIds)
            if (await userSoundService.ContainsCopyrightedContent(id))
                throw new AiContentModerationException("Draft cannot be published because it contains sound with copyrighted content", []);

        var result = await SaveCore(
                         input,
                         entity =>
                         {
                             entity.Status = AiGeneratedContentEntity.KnownStatusPublished;
                             entity.GenerationStatus = AiGeneratedContentEntity.KnownGenerationStatusCompleted;
                             entity.DraftAiContentId = aiGeneratedContentId;
                         }
                     );

        await TransferModerationInfo(draft.Id, result.Id);

        return result;
    }

    private static AiGeneratedContentInput CopyContent(AiGeneratedContentFullInfo content)
    {
        return new AiGeneratedContentInput
               {
                   Id = 0,
                   RemixedFromAiGeneratedContentId = null,
                   Image = CopyImage(content.Image),
                   Video = CopyVideo(content.Video)
               };
    }

    private static AiGeneratedVideoInput CopyVideo(AiGeneratedVideoFullInfo video)
    {
        if (video == null)
            return null;

        return new AiGeneratedVideoInput
               {
                   Id = 0,
                   Type = video.Type,
                   Files = CopyFiles(video.Files),
                   Workflow = video.Workflow,
                   ExternalSongId = video.ExternalSongId,
                   SongId = video.SongId,
                   IsLipSync = video.IsLipSync,
                   Clips = video.Clips.Select(
                                     c => new AiGeneratedVideoClipInput
                                          {
                                              Id = 0,
                                              Image = CopyImage(c.Image),
                                              Prompt = c.Prompt,
                                              Files = CopyFiles(c.Files),
                                              UserSoundId = c.UserSoundId,
                                              Seed = c.Seed,
                                              Type = c.Type,
                                              Workflow = c.Workflow,
                                              LengthSec = c.LengthSec,
                                              ShortPromptSummary = c.ShortPromptSummary
                                          }
                                 )
                                .ToList()
               };
    }

    private static AiGeneratedImageInput CopyImage(AiGeneratedImageFullInfo source)
    {
        if (source == null)
            return null;

        return new AiGeneratedImageInput
               {
                   Id = 0,
                   Prompt = source.Prompt,
                   Seed = source.Seed,
                   ShortPromptSummary = source.ShortPromptSummary,
                   Workflow = source.Workflow,
                   AiMakeupId = source.AiMakeupId,
                   Files = CopyFiles(source.Files),
                   Persons = source.Persons.Select(
                                        person => new AiGeneratedImagePersonInput
                                                  {
                                                      Id = 0,
                                                      Files = CopyFiles(person.Files),
                                                      ParticipantAiCharacterSelfieId = person.ParticipantAiCharacterSelfieId
                                                  }
                                    )
                                   .ToList(),
                   Sources = source.Sources.Select(
                                        src => new AiGeneratedImageSourceInput {Id = 0, Type = src.Type, Files = CopyFiles(src.Files)}
                                    )
                                   .ToList()
               };
    }

    private static FileMetadata[] CopyFiles(IEnumerable<FileMetadata> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.Select(s => new FileMetadata
                                  {
                                      Type = s.Type, Source = new FileSourceInfo {SourceFile = StorageReference.Encode(s.Path)}
                                  }
                      )
                     .ToArray();
    }
}