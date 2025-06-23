using Common.Models.Files;
using FluentAssertions;
using Frever.Client.Shared.Files;
using Moq;
using Xunit;

namespace Frever.Client.Shared.Test.Files;

public partial class FileStorageServiceTest
{
    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    public async Task Validate_MustWorkForNewFiles(long id)
    {
        var storageBackend = new Mock<IFileStorageBackend>();
        var factory = new Mock<IFileUploaderFactory>();

        var service = new FileStorageService([new TestFileOwnerFileConfig()], storageBackend.Object, factory.Object);

        var correct = new TestFileOwner
                      {
                          Id = id,
                          Files =
                          [
                              new FileMetadata {Type = "main", Source = new FileSourceInfo {SourceBytes = []}},
                              new FileMetadata {Type = "thumbnail512", Source = new FileSourceInfo {SourceBytes = []}}
                          ]
                      };

        var (isValid, errors) = await service.Validate<TestFileOwner>(correct);
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    public async Task Validate_MustWorkForExistingFiles(long id)
    {
        var storageBackend = new Mock<IFileStorageBackend>();
        var factory = new Mock<IFileUploaderFactory>();

        var service = new FileStorageService(new[] {new TestFileOwnerFileConfig()}, storageBackend.Object, factory.Object);

        var correct = new TestFileOwner
                      {
                          Id = id,
                          Files =
                          [
                              new FileMetadata
                              {
                                  Type = "main",
                                  Version = Guid.NewGuid().ToString("N"),
                                  Source = id == 0 ? new FileSourceInfo {SourceBytes = []} : null
                              },
                              new FileMetadata
                              {
                                  Type = "thumbnail512",
                                  Version = Guid.NewGuid().ToString("N"),
                                  Source = id == 0 ? new FileSourceInfo {SourceBytes = []} : null
                              }
                          ]
                      };

        var (isValid, errors) = await service.Validate<TestFileOwner>(correct);
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_MustFailForNewEntityIfNoSource()
    {
        var storageBackend = new Mock<IFileStorageBackend>();
        var factory = new Mock<IFileUploaderFactory>();

        var service = new FileStorageService(new[] {new TestFileOwnerFileConfig()}, storageBackend.Object, factory.Object);

        var correct = new TestFileOwner
                      {
                          Files =
                          [
                              new FileMetadata {Type = "main", Version = Guid.NewGuid().ToString("N"), Source = null},
                              new FileMetadata {Type = "thumbnail512", Version = Guid.NewGuid().ToString("N"), Source = null}
                          ]
                      };

        var (isValid, errors) = await service.Validate<TestFileOwner>(correct);
        isValid.Should().BeFalse();
        errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Validate_MustFailForDuplicatedFiles()
    {
        var storageBackend = new Mock<IFileStorageBackend>();
        var factory = new Mock<IFileUploaderFactory>();

        var service = new FileStorageService(new[] {new TestFileOwnerFileConfig()}, storageBackend.Object, factory.Object);

        var correct = new TestFileOwner
                      {
                          Id = 10,
                          Files =
                          [
                              new FileMetadata {Type = "main", Version = Guid.NewGuid().ToString("N"), Source = null},
                              new FileMetadata {Type = "main", Version = Guid.NewGuid().ToString("N"), Source = null},
                              new FileMetadata {Type = "thumbnail512", Version = Guid.NewGuid().ToString("N"), Source = null}
                          ]
                      };

        var (isValid, errors) = await service.Validate<TestFileOwner>(correct);
        isValid.Should().BeFalse();
        errors.Should().NotBeEmpty();
    }

    // [Fact]
    // public async Task Validate_MustFailForMissingFiles()
    // {
    //     var storageBackend = new Mock<IFileStorageBackend>();
    //     var factory = new Mock<IFileUploaderFactory>();
    //
    //     var service = new FileStorageService(new[] {new TestFileOwnerFileConfig()}, storageBackend.Object, factory.Object);
    //
    //     var correct = new TestFileOwner
    //                   {
    //                       Id = 10,
    //                       Files =
    //                       [
    //                           new FileMetadata {Type = "main", Version = Guid.NewGuid().ToString("N"), Source = null}
    //                       ]
    //                   };
    //
    //     var (isValid, errors) = await service.Validate<TestFileOwner>(correct);
    //     isValid.Should().BeFalse();
    //     errors.Should().NotBeEmpty();
    // }
}

public class TestFileOwner : IFileMetadataConfigRoot
{
    public long Id { get; set; }
    public FileMetadata[] Files { get; set; } = [];
}

public class TestFileOwnerFileConfig : DefaultFileMetadataConfiguration<TestFileOwner>
{
    public TestFileOwnerFileConfig()
    {
        AddMainFile("jpg");
        AddThumbnail(512, "jpg");
    }
}