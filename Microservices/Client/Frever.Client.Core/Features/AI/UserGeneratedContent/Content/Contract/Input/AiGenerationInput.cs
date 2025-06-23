using System.Linq;
using Frever.ClientService.Contract.Ai;

namespace Frever.Client.Core.Features.AI.UserGeneratedContent.Content.Input;

public class AiGenerationInput
{
    public required AiGeneratedContentType Type { get; set; }
    public required string Workflow { get; set; }
    public string Prompt { get; set; }
    public long? SourceContentId { get; set; }
    public long? AiMakeupId { get; set; }
    public long? ExternalSongId { get; set; }
    public long? SongId { get; set; }
    public long? UserSoundId { get; set; }
    public long[] CharacterImageIds { get; set; }
    public int VideoLengthSec { get; set; } = 5;

    public AiGeneratedContentInput ToContentInput()
    {
        var input = new AiGeneratedContentInput {RemixedFromAiGeneratedContentId = SourceContentId};

        if (Type == AiGeneratedContentType.Image)
            input.Image = CreateInputImage();
        else
            input.Video = CreateInputVideo();

        return input;
    }

    private AiGeneratedVideoInput CreateInputVideo()
    {
        return new AiGeneratedVideoInput
               {
                   Type = AiGeneratedVideoType.Pan,
                   Workflow = Workflow,
                   ExternalSongId = ExternalSongId,
                   SongId = SongId,
                   Clips =
                   [
                       new AiGeneratedVideoClipInput
                       {
                           Type = AiGeneratedVideoType.Pan,
                           Workflow = Workflow,
                           Prompt = Prompt,
                           ShortPromptSummary = Prompt,
                           UserSoundId = UserSoundId,
                           LengthSec = VideoLengthSec
                       }
                   ]
               };
    }

    private AiGeneratedImageInput CreateInputImage()
    {
        return new AiGeneratedImageInput
               {
                   Prompt = Prompt,
                   ShortPromptSummary = Prompt,
                   Workflow = Workflow,
                   AiMakeupId = AiMakeupId,
                   Persons = CharacterImageIds
                           ?.Select(id => new AiGeneratedImagePersonInput {ParticipantAiCharacterSelfieId = id})
                            .ToList()
               };
    }
}