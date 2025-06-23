using System;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.Client.Shared.Files;

public interface IFileUploaderFactory
{
    IFileUploader CreateFileUploader();
}

public class FileUploaderFactory(IServiceProvider provider) : IFileUploaderFactory
{
    public IFileUploader CreateFileUploader()
    {
        return provider.GetRequiredService<IFileUploader>();
    }
}