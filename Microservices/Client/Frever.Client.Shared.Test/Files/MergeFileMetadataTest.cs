using Common.Models.Files;
using FluentAssertions;
using Frever.Client.Shared.Files;
using Xunit;

namespace Frever.Client.Shared.Test.Files;

public class MergeFileMetadataTest
{
    [Fact]
    public void MergeFileMetadata_ShouldKeepExistingIfNoReuploading()
    {
        var current = new[]
                      {
                          new FileMetadata
                          {
                              Path = "/correct/path/1",
                              Type = "main",
                              Url = "http://someurl",
                              Version = Guid.NewGuid().ToString("N")
                          },
                          new FileMetadata
                          {
                              Path = "/correct/path/2",
                              Type = "thumb",
                              Url = "http://someurl1",
                              Version = Guid.NewGuid().ToString("N")
                          }
                      };

        var updated = new[]
                      {
                          new FileMetadata
                          {
                              Path = "/incorrect/path/1",
                              Type = "main",
                              Url = "http://wrong",
                              Version = Guid.NewGuid().ToString("N")
                          },
                          new FileMetadata
                          {
                              Path = "/incorrect/path/2",
                              Type = "thumb",
                              Url = "http://wrong1",
                              Version = Guid.NewGuid().ToString("N")
                          }
                      };

        var merged = FileMetadataMappingExtensions.MergeFileMetadata(updated, current);
        merged.Should().BeEquivalentTo(current);
    }

    [Fact]
    public void MergeFileMetadata_ShouldReplaceWithUpdatedIfSourceIsProvided()
    {
        var current = new[]
                      {
                          new FileMetadata
                          {
                              Path = "/correct/path/1",
                              Type = "main",
                              Url = "http://someurl",
                              Version = Guid.NewGuid().ToString("N")
                          },
                          new FileMetadata
                          {
                              Path = "/correct/path/2",
                              Type = "thumb",
                              Url = "http://someurl1",
                              Version = Guid.NewGuid().ToString("N")
                          }
                      };

        var updated = new[]
                      {
                          new FileMetadata
                          {
                              Path = "/new/path/1",
                              Type = "main",
                              Url = "http://wrong",
                              Version = Guid.NewGuid().ToString("N"),
                              Source = new FileSourceInfo {SourceFile = "/some/source"}
                          },
                          new FileMetadata
                          {
                              Path = "/incorrect/path/2",
                              Type = "thumb",
                              Url = "http://wrong1",
                              Version = Guid.NewGuid().ToString("N")
                          }
                      };

        var merged = FileMetadataMappingExtensions.MergeFileMetadata(updated, current);
        var expected = new[] {updated[0], current[1]};
        merged.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void MergeFileMetadata_ShouldIgnoreExtraFilesInUpdated()
    {
        var current = new[]
                      {
                          new FileMetadata
                          {
                              Path = "/correct/path/1",
                              Type = "main",
                              Url = "http://someurl",
                              Version = Guid.NewGuid().ToString("N")
                          },
                          new FileMetadata
                          {
                              Path = "/correct/path/2",
                              Type = "thumb",
                              Url = "http://someurl1",
                              Version = Guid.NewGuid().ToString("N")
                          }
                      };

        var updated = new[]
                      {
                          new FileMetadata
                          {
                              Path = "/incorrect/path/1",
                              Type = "main",
                              Url = "http://wrong",
                              Version = Guid.NewGuid().ToString("N")
                          },
                          new FileMetadata
                          {
                              Path = "/incorrect/path/2",
                              Type = "thumb",
                              Url = "http://wrong1",
                              Version = Guid.NewGuid().ToString("N")
                          },
                          new FileMetadata
                          {
                              Path = "/incorrect/path/3",
                              Type = "fishing",
                              Url = "http://wrong1",
                              Version = Guid.NewGuid().ToString("N")
                          }
                      };

        FluentActions.Invoking(() => FileMetadataMappingExtensions.MergeFileMetadata(updated, current))
                     .Should()
                     .Throw<InvalidOperationException>();
    }
}