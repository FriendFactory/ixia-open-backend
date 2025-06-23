using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthServer.Permissions.Services;
using AuthServerShared;
using AutoMapper;
using Common.Infrastructure;
using Common.Models;
using Common.Models.Files;
using FluentValidation;
using Frever.Client.Core.Features.AI.Generation;
using Frever.Client.Core.Features.AI.Generation.Contract;
using Frever.Client.Core.Features.AI.Metadata;
using Frever.Client.Core.Features.AI.UserGeneratedContent.Characters.Contracts;
using Frever.Client.Shared.AI.ComfyUi;
using Frever.Client.Shared.AI.ComfyUi.Contract;
using Frever.Client.Shared.Files;
using Frever.Shared.MainDb.Entities;

namespace Frever.Client.Core.Features.AI.UserGeneratedContent.Characters;

public interface IAiCharacterService
{
    Task<List<AiCharacterDto>> GetMyCharacters(int skip, int take);
    Task<ComfyUiResponse> PostCharacterImageGeneration(AiCharacterImageGenerationInput input);
    Task SaveCharacter(AiCharacterInput input);
    Task DeleteCharacter(long characterId);
}

public class AiCharacterService(
    UserInfo currentUser,
    IMapper mapper,
    IAiCharacterRepository repo,
    IFileStorageService fileStorage,
    IUserPermissionService permissionService,
    IAiMetadataService aiMetadataService,
    IAiGenerationService aiGenerationService,
    IValidator<AiCharacterInput> characterValidator
) : IAiCharacterService
{
    private const string CharacterPromptKey = "ai-character-create";

    public async Task<List<AiCharacterDto>> GetMyCharacters(int skip, int take)
    {
        await permissionService.EnsureCurrentUserActive();

        var images = await repo.GetCharacters(currentUser, skip, take);
        if (images.Length == 0)
            return [];

        var artStyles = await aiMetadataService.GetArtStyleByIdsInternal(images.Select(e => e.Character.ArtStyleId));

        var result = new List<AiCharacterDto>();

        foreach (var item in images)
        {
            var character = mapper.Map<AiCharacterDto>(item.Character);
            character.ArtStyle = artStyles.GetValueOrDefault(item.Character.ArtStyleId)?.Text;
            character.Image = mapper.Map<AiCharacterImageDto>(item);
            result.Add(character);
        }

        await fileStorage.InitUrls<AiCharacterImage>(result.Select(e => e.Image));

        return result;
    }

    public async Task<ComfyUiResponse> PostCharacterImageGeneration(AiCharacterImageGenerationInput input)
    {
        await permissionService.EnsureCurrentUserActive();

        ArgumentNullException.ThrowIfNull(input);

        var genderName = await repo.GetGenderNameById(input.GenderId);
        if (genderName == null)
            throw AppErrorWithStatusCodeException.BadRequest("Gender not found", ErrorCodes.Client.GenderNotFound);

        var artStyle = await aiMetadataService.GetArtStyleByIdInternal(input.ArtStyleId);
        if (artStyle == null)
            throw AppErrorWithStatusCodeException.BadRequest("ArtStyle not found", ErrorCodes.Client.ArtStyleNotFound);

        var imageGenerationInput = new ImageGenerationInput
                                   {
                                       FileUrls = input.FileUrls, PromptText = await GetPromptText(input, artStyle.Text, genderName)
                                   };

        if (input.FileUrls == null || input.FileUrls.Count == 0)
            return await aiGenerationService.PostTextToImageGeneration(imageGenerationInput, WorkflowKey.CharacterFromPrompt);

        return await aiGenerationService.PostImageToImageGeneration(imageGenerationInput, WorkflowKey.CharacterFromImageAndPrompt);
    }

    public async Task SaveCharacter(AiCharacterInput input)
    {
        await permissionService.EnsureCurrentUserActive();

        await characterValidator.ValidateAndThrowAsync(input);

        if (input.ArtStyleId == 0)
        {
            var artStyles = await aiMetadataService.GetArtStyles(input.GenderId, 0, 1);
            input.ArtStyleId = artStyles.FirstOrDefault()?.Id ?? 0;
        }

        await using var transaction = await repo.BeginTransaction();

        var character = mapper.Map<AiCharacter>(input);
        character.GroupId = currentUser.UserMainGroupId;
        await repo.AddCharacter(character);

        var aiCharacterImage = mapper.Map<AiCharacterImage>(input.Image);
        aiCharacterImage.AiCharacterId = character.Id;
        await repo.AddCharacterImage(aiCharacterImage);

        var uploader = fileStorage.CreateFileUploader();

        var myGroup = await repo.GetGroupById(currentUser.UserMainGroupId);
        if (myGroup is {Files: null})
        {
            myGroup.Files = CopyGroupFiles(aiCharacterImage);
            await uploader.UploadFiles<Group>(myGroup);
        }

        await uploader.UploadFiles<AiCharacterImage>(aiCharacterImage);

        await repo.SaveChanges();
        await transaction.Commit();

        await uploader.WaitForCompletion();
    }

    public async Task DeleteCharacter(long characterId)
    {
        await permissionService.EnsureCurrentUserActive();

        var character = await repo.GetCharacter(characterId, currentUser.UserMainGroupId);
        if (character == null)
            throw AppErrorWithStatusCodeException.NotFound("Character not found", ErrorCodes.Client.CharacterNotFound);

        if (character.DeletedAt.HasValue)
            return;

        await repo.MarkCharacterAsDeleted(character);
    }

    private static FileMetadata[] CopyGroupFiles(AiCharacterImage image)
    {
        return image.Files.Where(e => e.Type != "main")
                    .Select(
                         e => new FileMetadata
                              {
                                  Type = e.Type,
                                  Source = new FileSourceInfo {SourceFile = e.Source.SourceFile, SourceBytes = e.Source.SourceBytes}
                              }
                     )
                    .ToArray();
    }

    private async Task<string> GetPromptText(AiCharacterImageGenerationInput input, string artStyle, string gender)
    {
        var placeholders = new Dictionary<string, string>
                           {
                               {"ArtStyle", artStyle},
                               {"Age", input.Age.ToString()},
                               {"Ethnicity", input.Ethnicity},
                               {"Gender", gender},
                               {"HairColor", input.HairColor},
                               {"HairStyle", input.HairStyle}
                           };
        return await aiMetadataService.GetPopulatedPromptInternal(CharacterPromptKey, placeholders);
    }
}