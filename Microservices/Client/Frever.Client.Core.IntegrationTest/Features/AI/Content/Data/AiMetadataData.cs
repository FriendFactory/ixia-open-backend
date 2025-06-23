using Frever.Common.IntegrationTesting;
using Frever.Shared.MainDb.Entities;

namespace Frever.Client.Core.IntegrationTest.Features.AI.Data;

public static class AiMetadataData
{
    public static async Task<AiMakeUp> WithAiMakeUp(this DataEnvironment data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var makeup = new AiMakeUp
                     {
                         CategoryId = 1,
                         IsEnabled = true,
                         SortOrder = 1,
                         Files = []
                     };
        data.Db.AiMakeUp.Add(makeup);
        await data.Db.SaveChangesAsync();

        return makeup;
    }

    public static async Task<(AiCharacter, AiCharacterImage[])> WithAiCharacters(this DataEnvironment data, long groupId)
    {
        ArgumentNullException.ThrowIfNull(data);
        var character = new AiCharacter {Age = 20, GenderId = 1, GroupId = groupId};
        data.Db.AiCharacter.Add(character);
        await data.Db.SaveChangesAsync();

        var image = new AiCharacterImage
                    {
                        Status = "ready",
                        Type = "selfie",
                        AiCharacterId = character.Id,
                        AiModelRequest = "test",
                        AiModelResponse = "test"
                    };
        data.Db.AiCharacterImage.Add(image);
        await data.Db.SaveChangesAsync();

        return (character, [image]);
    }

    public static async Task<UserSound> WithUserSound(this DataEnvironment dataEnv, long groupId)
    {
        var us = new UserSound
                 {
                     Duration = 10,
                     Files = [],
                     Name = "test-user-sound" + Guid.NewGuid(),
                     Size = 1002,
                     UsageCount = 122,
                     ContainsCopyrightedContent = false,
                     ModifiedTime = DateTime.UtcNow,
                     CreatedTime = DateTime.UtcNow,
                     GroupId = groupId
                 };
        dataEnv.Db.UserSound.Add(us);
        await dataEnv.Db.SaveChangesAsync();

        return us;
    }
}