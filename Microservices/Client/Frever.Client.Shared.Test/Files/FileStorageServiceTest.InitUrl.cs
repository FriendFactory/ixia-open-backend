using Common.Models.Files;
using FluentAssertions;
using Frever.Client.Shared.Files;
using Moq;
using Xunit;

namespace Frever.Client.Shared.Test.Files;

public partial class FileStorageServiceTest
{
    [Fact]
    public async Task InitUrls_MustWork()
    {
        var storageBackend = new Mock<IFileStorageBackend>();
        storageBackend.Setup(s => s.MakeCdnUrl(It.IsAny<string>(), It.IsAny<bool>()))
                      .Returns((string url, bool signed) => $"https://test.cdn.com/{url}?signed={signed}");
        var factory = new Mock<IFileUploaderFactory>();


        var service = new FileStorageService([new TestFileOwnerFileConfig()], storageBackend.Object, factory.Object);

        var correct = new TestFileOwner
                      {
                          Id = 100,
                          Files =
                          [
                              new FileMetadata {Type = "main", Version = "1", Path = "/storage/test-entity/100/main/content.jpg"},
                              new FileMetadata
                              {
                                  Type = "thumbnail512", Version = "1", Path = "/storage/test-entity/100/thumbnail/content.jpg"
                              }
                          ]
                      };

        await service.InitUrls<TestFileOwner>(new[] {correct});

        correct.Files.Should()
               .AllSatisfy(
                    file =>
                    {
                        file.Url.Should().NotBeNullOrWhiteSpace();
                        file.Version.Should().NotBeNullOrWhiteSpace();
                        file.Type.Should().NotBeNullOrWhiteSpace();
                        file.Url.Should().Contain($"signed={file.Type == "main"}");
                    }
                );
    }
}