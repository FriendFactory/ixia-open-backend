using System.Linq;
using Frever.Shared.MainDb;
using Microsoft.EntityFrameworkCore;

namespace Frever.AdminService.Core.Services.AiContent.DataAccess;

public class PersistentAiContentRepository(IWriteDb db) : IAiContentRepository
{
    public IQueryable<AiGeneratedContentDto> GetAiGeneratedContent()
    {
        return db.AiGeneratedContent.Select(a => new AiGeneratedContentDto
                                                 {
                                                     Id = a.Id,
                                                     Type = a.Type,
                                                     CreatedAt = a.CreatedAt,
                                                     GroupId = a.GroupId,
                                                     ExternalSongId = a.ExternalSongId,
                                                     IsLipSync = a.IsLipSync,
                                                     RemixedFromAiGeneratedContentId =
                                                         a.RemixedFromAiGeneratedContentId,
                                                     Image =
                                                         a.AiGeneratedImageId == null
                                                             ? null
                                                             : new AiGeneratedImageDto
                                                               {
                                                                   Id = a.GeneratedImage.Id,
                                                                   Files = a.GeneratedImage.Files,
                                                                   Prompt = a.GeneratedImage.Prompt,
                                                                   Seed = a.GeneratedImage.Seed,
                                                                   GroupId = a.GeneratedImage.GroupId,
                                                                   AiMakeupId = a.GeneratedImage.AiMakeupId,
                                                                   NumOfCharacters = a.GeneratedImage.NumOfCharacters,
                                                                   ShortPromptSummary =
                                                                       a.GeneratedImage.ShortPromptSummary,
                                                                   Persons =
                                                                       a.GeneratedImage.GeneratedImagePerson
                                                                        .Select(p => new AiGeneratedImagePersonDto
                                                                                     {
                                                                                         Id = p.Id,
                                                                                         Files = p.Files,
                                                                                         GenderId = p.GenderId,
                                                                                         ParticipantGroupId =
                                                                                             p.ParticipantGroupId,
                                                                                         AiGeneratedImageId =
                                                                                             p.AiGeneratedImageId,
                                                                                         ParticipantAiCharacterSelfieId =
                                                                                             p.ParticipantAiCharacterSelfieId
                                                                                     }
                                                                         )
                                                                        .ToArray(),
                                                                   Sources =
                                                                       a.GeneratedImage.GeneratedImageSource
                                                                        .Select(s => new AiGeneratedImageSourceDto
                                                                                     {
                                                                                         Id = s.Id,
                                                                                         Files = s.Files,
                                                                                         Type = s.Type,
                                                                                         AiGeneratedImageId =
                                                                                             s.AiGeneratedImageId
                                                                                     }
                                                                         )
                                                                        .ToArray()
                                                               },
                                                     Video = a.AiGeneratedVideoId == null
                                                                 ? null
                                                                 : new AiGeneratedVideoDto
                                                                   {
                                                                       Id = a.GeneratedVideo.Id,
                                                                       Files = a.GeneratedVideo.Files,
                                                                       Length = a.GeneratedVideo.LengthSec,
                                                                       Tts = a.GeneratedVideo.Tts,
                                                                       Type = a.GeneratedVideo.Type,
                                                                       Workflow = a.GeneratedVideo.Workflow,
                                                                       GroupId = a.GeneratedVideo.GroupId,
                                                                       ExternalSongId =
                                                                           a.GeneratedVideo.ExternalSongId,
                                                                       IsLipSync = a.GeneratedVideo.IsLipSync,
                                                                       Clips = a.GeneratedVideo.GeneratedVideoClip
                                                                                .Select(c => new AiGeneratedVideoClipDto
                                                                                            {
                                                                                                Files = c.Files,
                                                                                                Id = c.Id,
                                                                                                Prompt = c.Prompt,
                                                                                                Seed = c.Seed,
                                                                                                Tts = c.Tts,
                                                                                                Type = c.Type,
                                                                                                Workflow = c.Workflow,
                                                                                                LengthSec = c.LengthSec,
                                                                                                ShortPromptSummary =
                                                                                                    c.ShortPromptSummary,
                                                                                                AiGeneratedImageId =
                                                                                                    c.AiGeneratedImageId,
                                                                                                Image =
                                                                                                    c.AiGeneratedImageId ==
                                                                                                    null
                                                                                                        ? null
                                                                                                        : new
                                                                                                            AiGeneratedImageDto
                                                                                                            {
                                                                                                                Id =
                                                                                                                    c.GeneratedImage
                                                                                                                       .Id,
                                                                                                                Files =
                                                                                                                    c.GeneratedImage
                                                                                                                       .Files,
                                                                                                                Prompt =
                                                                                                                    c.GeneratedImage
                                                                                                                       .Prompt,
                                                                                                                Seed =
                                                                                                                    c.GeneratedImage
                                                                                                                       .Seed,
                                                                                                                GroupId =
                                                                                                                    c.GeneratedImage
                                                                                                                       .GroupId,
                                                                                                                AiMakeupId =
                                                                                                                    c.GeneratedImage
                                                                                                                       .AiMakeupId,
                                                                                                                NumOfCharacters =
                                                                                                                    c.GeneratedImage
                                                                                                                       .NumOfCharacters,
                                                                                                                ShortPromptSummary =
                                                                                                                    c.GeneratedImage
                                                                                                                       .ShortPromptSummary,
                                                                                                                Persons =
                                                                                                                    c
                                                                                                                       .GeneratedImage
                                                                                                                       .GeneratedImagePerson
                                                                                                                       .Select(p => new
                                                                                                                                AiGeneratedImagePersonDto
                                                                                                                                {
                                                                                                                                    Id =
                                                                                                                                        p.Id,
                                                                                                                                    Files =
                                                                                                                                        p.Files,
                                                                                                                                    GenderId =
                                                                                                                                        p.GenderId,
                                                                                                                                    ParticipantGroupId =
                                                                                                                                        p.ParticipantGroupId,
                                                                                                                                    AiGeneratedImageId =
                                                                                                                                        p.AiGeneratedImageId,
                                                                                                                                    ParticipantAiCharacterSelfieId =
                                                                                                                                        p.ParticipantAiCharacterSelfieId
                                                                                                                                }
                                                                                                                        )
                                                                                                                       .ToArray(),
                                                                                                                Sources =
                                                                                                                    c.GeneratedImage
                                                                                                                       .GeneratedImageSource
                                                                                                                       .Select(s => new
                                                                                                                                AiGeneratedImageSourceDto
                                                                                                                                {
                                                                                                                                    Id =
                                                                                                                                        s.Id,
                                                                                                                                    Files =
                                                                                                                                        s.Files,
                                                                                                                                    Type =
                                                                                                                                        s.Type,
                                                                                                                                    AiGeneratedImageId =
                                                                                                                                        s.AiGeneratedImageId
                                                                                                                                }
                                                                                                                        )
                                                                                                                       .ToArray()
                                                                                                            }
                                                                                            }
                                                                                 )
                                                                                .ToArray()
                                                                   }
                                                 }
                  )
                 .AsSplitQuery()
                 .AsNoTracking();
    }
}