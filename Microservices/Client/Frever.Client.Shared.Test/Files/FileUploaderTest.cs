using System.Text;
using Common.Infrastructure.Utils;
using Common.Models.Files;
using FluentAssertions;
using Frever.Client.Shared.Files;
using Moq;
using Xunit;

namespace Frever.Client.Shared.Test.Files;

public class FileUploaderTest
{
    [Fact]
    public async Task FileUploader_VerifyUploadingWorks()
    {
        var callTimes = 0;

        var storage = new Mock<IAdvancedFileStorageService>();
        storage.Setup(s => s.Validate<TestFileOwner>(It.IsAny<IFileMetadataOwner>())).ReturnsAsync((true, new List<string>()));
        storage.Setup(s => s.MakeFilePath<TestFileOwner>(It.IsAny<long>(), It.IsAny<FileMetadata>()))
               .Returns((long id, FileMetadata file) => $"/test/{id}/{file.Type}/{file.Version}/content.jpg");

        var backend = new Mock<IFileStorageBackend>();
        backend.Setup(s => s.UploadToBucket(It.IsAny<string>(), It.IsAny<byte[]>()))
               .Returns(
                    async () =>
                    {
                        await Task.Delay(100 + Random.Shared.Next(10, 200));
                        Interlocked.Increment(ref callTimes);
                    }
                );

        var externalFileUploader = new Mock<IExternalFileDownloader>();

        var fileUploader = new ParallelFileUploader(storage.Object, backend.Object, externalFileUploader.Object);

        var origEntities = Enumerable.Range(1, 10)
                                     .Select(
                                          i => new TestFileOwner
                                               {
                                                   Id = i,
                                                   Files =
                                                   [
                                                       new FileMetadata
                                                       {
                                                           Type = "main",
                                                           Source = new FileSourceInfo {SourceBytes = Encoding.UTF8.GetBytes($"abc{i}")},
                                                           Version = Guid.NewGuid().ToString("N")
                                                       }
                                                   ]
                                               }
                                      )
                                     .ToArray();

        var entities = origEntities.ProtobufDeepClone();
        await fileUploader.UploadFilesAll<TestFileOwner>(entities);

        // Ensure file uploading started before calling WaitForCompletion method
        await Task.Delay(150);
        // callTimes.Should().BeGreaterThan(0);

        await fileUploader.WaitForCompletion();

        backend.Verify(s => s.UploadToBucket(It.IsAny<string>(), It.IsAny<byte[]>()), Times.Exactly(entities.Length));

        callTimes.Should().Be(origEntities.Length);
        entities.Should()
                .HaveSameCount(origEntities)
                .And.AllSatisfy(
                     entity =>
                     {
                         var orig = origEntities.First(a => a.Id == entity.Id);

                         entity.Files.Should()
                               .NotBeNull()
                               .And.HaveCount(1)
                               .And.AllSatisfy(
                                    f =>
                                    {
                                        f.Path.Should().NotBeNullOrWhiteSpace();
                                        f.Source.Should().BeNull();
                                        // Uploading a file should generate a new version
                                        f.Version.Should().NotBeNullOrWhiteSpace().And.NotBe(orig.Files[0].Version);
                                        f.Type.Should().Be("main");

                                        backend.Verify(s => s.UploadToBucket(f.Path, It.IsAny<byte[]>()), Times.Once);
                                    }
                                );
                     }
                 );
    }

    [Fact]
    public async Task FileUploader_VerifyCopyingFromBucketWorks()
    {
        var callTimes = 0;

        var storage = new Mock<IAdvancedFileStorageService>();
        storage.Setup(s => s.Validate<TestFileOwner>(It.IsAny<IFileMetadataOwner>())).ReturnsAsync((true, new List<string>()));
        storage.Setup(s => s.MakeFilePath<TestFileOwner>(It.IsAny<long>(), It.IsAny<FileMetadata>()))
               .Returns((long id, FileMetadata file) => $"/test/{id}/{file.Type}/{file.Version}/content.jpg");

        var backend = new Mock<IFileStorageBackend>();
        backend.Setup(s => s.CopyFrom(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
               .Returns(
                    async () =>
                    {
                        await Task.Delay(100 + Random.Shared.Next(10, 200));
                        Interlocked.Increment(ref callTimes);
                    }
                );

        var externalFileUploader = new Mock<IExternalFileDownloader>();

        var fileUploader = new ParallelFileUploader(storage.Object, backend.Object, externalFileUploader.Object);

        TestFileOwner[] origEntities =
        [
            new TestFileOwner
            {
                Id = 1,
                Files =
                [
                    new FileMetadata
                    {
                        Type = "main",
                        Source = new FileSourceInfo
                                 {
                                     SourceFile =
                                         "https://ff-publicfiles.s3.eu-central-1.amazonaws.com/comfyui-videos/16-flux-prompt-ec26c50751d945fc8a3fba530cbbdb35_608x1080.jpeg?X-Amz-Expires=300&X-Amz-Security-Token=IQoJb3JpZ2luX2VjECYaDGV1LWNlbnRyYWwtMSJGMEQCIGu8Ic54Jklm43o%2F4w6iPY4hNwXl70R%2BDESAtPkYgnzpAiBX8EKgO5cfUTfTdfb4WUZOfj0pc9ZjGCq9OjgmVNruSyqLBQhfEAMaDDcyMjkxMzI1MzcyOCIM%2Be5qfCgaR7Jn70gRKugEzM0pVj8HxaPgxN00QbsLLObXAyct%2FYs0P0%2Bd9VpLbI4jUowwp0%2FEn6RkCm1W8yphU7YKICEiKs%2BqqwAWaUAlTNzVzzVSXZfZmoS9yUwuClxDVmGYuzjcN32%2BB7%2FXQZvkpnNTLTpdiFnUxZBuWf7%2BQF7t0KomLYPK%2FWTshPYdva47e9r5NyiZvZSS%2Fe0cw16XHeEqjqD%2Bc%2F%2B66Anu6LM2wtxEMJvaYxKUvNOU9uRssd55aiVdHiF67yRPXtNVXYIF2TyOnVpn9vB8d%2FUPhWfKqL%2FE5L1U0cP8GeGtuFUUSTmUd4Jttt1N9Zf3x%2BTUqBZ63VitOPAXWA8QVGUkkl%2FsADdhtQg83fBDoOt26RsrCZVVz25gQrSjqqSqwu6S0V9FiwGvCB5jjoSXQ4XOBtTUXBCXXiJHzsasJRmvMuYmYA5sjeoBai1iYnY4SRvwZIbRT%2FcGsBGLVhnbnZuhZgWhNs4O52Ph6cb1mgnfd412W%2BRT8xTKyeon5mSGEnsA8z7pv80tygEpiTyX9PAij2Gj%2B%2FeM0vBWOoFHwaUJN1i7R7Q%2FSAPpuw3a26sT48TVeT%2BBTUm6UxY8ekw%2ForytYrtjsN8RBxfoUzQ4v1Jo1C%2B7Ak2th31xGTHOZDVNzQZr5HhejpTU3Q2%2FcfX%2By8vwCb2uRqSdP090b0RQ%2FkZhiPmf%2BeqBifEb%2FF7NhwpM%2BCPx%2BZFRI19qxXzSY8OYinPCrbhtSeRHkKOrX8qi8WW4iH%2B%2FHT5ixPS47mPtKRqau4TGaMV%2B3j3a5DPKd0id9uuZUP0akYfjqq%2BBBy72BdivhTJuWJi5OTAXhRicEzCdv%2Fy9BjqbAay0Zrxb7CHpn%2F4nz0xfsp8WR6WFNpsi%2F4SX1Wn7MgZq08VTnPqyMkhUYstDhwlvPM5MXrciLZulRO7DPO%2FiTQ5G0PMo2oj3diJmO2YB76D60FTXO4FGeK8dIlCctGouQwB27G7TVYTqxhF%2FMqXypO4eNAd2q8fjKTu562g6UBQyxyiWVxV5yK5UyNlHxJyXVP%2BklMi3xTdlARX4&X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Credential=ASIA2QUH43FQMCXVXDQK%2F20250226%2Feu-central-1%2Fs3%2Faws4_request&X-Amz-Date=20250226T144617Z&X-Amz-SignedHeaders=host&X-Amz-Signature=12a811042186026eb450ac092fa6e337ffe937d36575b9c4a52a1871236c9a36"
                                 },
                        Version = Guid.NewGuid().ToString("N")
                    }
                ]
            },
            new TestFileOwner
            {
                Id = 2,
                Files =
                [
                    new FileMetadata
                    {
                        Type = "main",
                        Source = new FileSourceInfo
                                 {
                                     SourceFile =
                                         "photo-realism-jpg%7C%7Cfrever-dev%7CAi-Photo%2F16%2F945ce3f7a59746d5bf53d11fd51ca79c1.jpg"
                                 },
                        Version = Guid.NewGuid().ToString("N")
                    }
                ]
            },
            new TestFileOwner
            {
                Id = 3,
                Files =
                [
                    new FileMetadata
                    {
                        Type = "main",
                        Source = new FileSourceInfo {SourceFile = "s3://frever-dev-1/ready-to-go/16/s9393444.jpg"},
                        Version = Guid.NewGuid().ToString("N")
                    }
                ]
            }
        ];


        var entities = origEntities.ProtobufDeepClone();
        await fileUploader.UploadFilesAll<TestFileOwner>(entities);

        // Ensure file uploading started before calling WaitForCompletion method
        await Task.Delay(200);

        await fileUploader.WaitForCompletion();

        backend.Verify(s => s.CopyFrom(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(entities.Length));

        // Verify file was copied from S3 signed URL
        backend.Verify(
            s => s.CopyFrom(
                "ff-publicfiles",
                "/comfyui-videos/16-flux-prompt-ec26c50751d945fc8a3fba530cbbdb35_608x1080.jpeg",
                It.IsAny<string>()
            )
        );

        // Verify file was copied from task key
        backend.Verify(s => s.CopyFrom("frever-dev", "Ai-Photo/16/945ce3f7a59746d5bf53d11fd51ca79c1.jpg", It.IsAny<string>()));

        // Verify file was copied from S3 URI
        backend.Verify(s => s.CopyFrom("frever-dev-1", "/ready-to-go/16/s9393444.jpg", It.IsAny<string>()));


        callTimes.Should().Be(origEntities.Length);
        entities.Should()
                .HaveSameCount(origEntities)
                .And.AllSatisfy(
                     entity =>
                     {
                         var orig = origEntities.First(a => a.Id == entity.Id);

                         entity.Files.Should()
                               .NotBeNull()
                               .And.HaveCount(1)
                               .And.AllSatisfy(
                                    f =>
                                    {
                                        f.Path.Should().NotBeNullOrWhiteSpace();
                                        f.Source.Should().BeNull();
                                        // Uploading a file should generate a new version
                                        f.Version.Should().NotBeNullOrWhiteSpace().And.NotBe(orig.Files[0].Version);
                                        f.Type.Should().Be("main");
                                    }
                                );
                     }
                 );
    }
}