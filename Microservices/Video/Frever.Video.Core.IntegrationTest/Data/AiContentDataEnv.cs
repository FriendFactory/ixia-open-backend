using Frever.Common.IntegrationTesting;
using Frever.Shared.MainDb.Entities;

namespace Frever.Video.Core.IntegrationTest.Data;

public static class AiContentDataEnv
{
    public static async Task<AiGeneratedContent> WithAiContent(this DataEnvironment data, User user)
    {
        ArgumentNullException.ThrowIfNull(data);

        var aiImage = new AiGeneratedImage
                      {
                          Files = [],
                          Prompt = "test",
                          GroupId = user.MainGroupId,
                          NumOfCharacters = 0
                      };
        data.Db.AiGeneratedImage.Add(aiImage);
        await data.Db.SaveChangesAsync();

        var aiContent = new AiGeneratedContent {Type = "Image", GroupId = user.MainGroupId, AiGeneratedImageId = aiImage.Id};
        data.Db.AiGeneratedContent.Add(aiContent);
        await data.Db.SaveChangesAsync();
        return aiContent;
    }
}