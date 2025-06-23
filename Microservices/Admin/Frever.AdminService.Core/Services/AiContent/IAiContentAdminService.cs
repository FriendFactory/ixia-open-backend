using System.Linq;
using System.Threading.Tasks;
using Frever.AdminService.Core.Services.AiContent.DataAccess;
using Frever.Client.Shared.Files;
using Frever.Shared.MainDb.Entities;

namespace Frever.AdminService.Core.Services.AiContent;

public interface IAiContentAdminService
{
    IQueryable<AiGeneratedContentDto> GetAiGeneratedContent();

    Task InitUrls(AiGeneratedContentDto aiContent);
}

public class AiContentAdminService(IAiContentRepository repo, IFileStorageService files) : IAiContentAdminService
{
    public IQueryable<AiGeneratedContentDto> GetAiGeneratedContent()
    {
        return repo.GetAiGeneratedContent();
    }

    public async Task InitUrls(AiGeneratedContentDto aiContent)
    {
        if (aiContent.Image != null)
            await InitAiImageUrls(aiContent.Image);

        if (aiContent.Video != null)
        {
            await files.InitUrls<AiGeneratedVideo>(aiContent.Video);

            foreach (var clip in aiContent.Video.Clips)
            {
                await files.InitUrls<AiGeneratedVideoClip>(clip);
                if (clip.Image != null)
                    await InitAiImageUrls(clip.Image);
            }
        }

        return;

        async Task InitAiImageUrls(AiGeneratedImageDto image)
        {
            await files.InitUrls<AiGeneratedImage>(image);

            foreach (var item in image.Persons)
                await files.InitUrls<AiGeneratedImagePerson>(item);

            foreach (var item in image.Sources)
                await files.InitUrls<AiGeneratedImageSource>(item);
        }
    }
}