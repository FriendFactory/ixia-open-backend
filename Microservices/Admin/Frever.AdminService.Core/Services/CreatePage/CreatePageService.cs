using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AssetStoragePathProviding;
using Common.Infrastructure;
using Common.Infrastructure.Aws.Crypto;
using Common.Infrastructure.CloudFront;
using Common.Models;
using Frever.AdminService.Core.Utils;
using Frever.Cache.Resetting;
using Frever.Client.Shared.Files;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;
using Microsoft.AspNet.OData.Query;
using Microsoft.EntityFrameworkCore;

namespace Frever.AdminService.Core.Services.CreatePage;

public interface ICreatePageService
{
    Task<ResultWithCount<CreatePageRowResponse>> GetCreatePageRows(ODataQueryOptions<CreatePageRowResponse> options);
    Task<CreatePageRowResponse> GetCreatePageRow(long id);
    Task SaveCreatePageRow(CreatePageRowRequest request);
}

public sealed class CreatePageService : ICreatePageService
{
    private static VideoNamingHelper _staticNamingHelper;
    private static CloudFrontConfiguration _staticConfig;

    private readonly IWriteDb _writeDb;
    private readonly ICacheReset _cacheReset;
    private readonly IFileStorageService _fileStorage;

    public CreatePageService(
        IWriteDb writeDb,
        VideoNamingHelper videoNamingHelper,
        CloudFrontConfiguration cloudFrontConfiguration,
        ICacheReset cacheReset,
        IFileStorageService fileStorage
    )
    {
        _writeDb = writeDb;
        _cacheReset = cacheReset;
        _fileStorage = fileStorage;

        _staticNamingHelper = videoNamingHelper;
        _staticConfig = cloudFrontConfiguration;
    }

    public Task<ResultWithCount<CreatePageRowResponse>> GetCreatePageRows(ODataQueryOptions<CreatePageRowResponse> options)
    {
        return GetCreatePageRowResponseFromDb().ExecuteODataRequestWithCount(options);
    }

    public async Task<CreatePageRowResponse> GetCreatePageRow(long id)
    {
        var row = await GetCreatePageRowResponseFromDb().FirstOrDefaultAsync(e => e.Id == id);
        if (row == null)
            throw AppErrorWithStatusCodeException.NotFound("Content row not found", "ContentRowNotFound");

        var ids = row.Content.Select(e => e.Id).ToArray();

        var content = row.ContentType != CreatePageContentType.Image
                          ? await GetDbContent(row.ContentType, ids).ToArrayAsync()
                          : await GetGeneratedContent(ids);

        row.Content = ids.Join(content, i => i, e => e.Id, (i, e) => e).ToArray();
        return row;
    }

    public async Task SaveCreatePageRow(CreatePageRowRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Title);

        if (!CreatePageContentType.GetAllContentTypes().Contains(request.ContentType))
            throw AppErrorWithStatusCodeException.BadRequest("Content type is not supported", "ContentTypeNotSupported");

        if ((request.ContentQuery == null && (request.ContentIds == null || request.ContentIds.Length == 0)) ||
            (request.ContentQuery != null && request.ContentIds is {Length: > 0}))
            throw AppErrorWithStatusCodeException.BadRequest("ContentQuery or ContentIds must have value", "WrongContent");

        if (request.ContentQuery != null && !CreatePageContentQuery.GetTypeQueries(request.ContentType).Contains(request.ContentQuery))
            throw AppErrorWithStatusCodeException.BadRequest("Content query is not supported", "ContentQueryNotSupported");

        if (request.ContentIds is {Length: > 0})
        {
            var dbIds = await GetDbContent(request.ContentType, request.ContentIds).Select(e => e.Id).ToArrayAsync();
            if (dbIds.Length == 0)
                throw AppErrorWithStatusCodeException.BadRequest("Content Ids are not supported", "ContentIdNotSupported");

            request.ContentIds = request.ContentIds.Where(e => dbIds.Contains(e)).ToArray();
        }

        var row = request.Id == 0 ? await CreatePageRow() : await _writeDb.ContentRow.FindAsync(request.Id);
        if (row == null)
            throw AppErrorWithStatusCodeException.NotFound("Content row not found", "ContentRowNotFound");

        row.Title = request.Title;
        row.SortOrder = request.SortOrder;
        row.TestGroup = request.TestGroup;
        row.ContentType = request.ContentType;
        row.ContentIds = request.ContentIds;
        row.ContentQuery = request.ContentQuery;
        row.IsEnabled = request.IsEnabled;

        await _writeDb.SaveChangesAsync();
        await _cacheReset.ResetOnDependencyChange(typeof(ContentRow), null);
    }

    private IQueryable<CreatePageRowResponse> GetCreatePageRowResponseFromDb()
    {
        return _writeDb.ContentRow.AsNoTracking()
                       .Select(
                            e => new CreatePageRowResponse
                                 {
                                     Id = e.Id,
                                     Title = e.Title,
                                     SortOrder = e.SortOrder,
                                     TestGroup = e.TestGroup,
                                     ContentType = e.ContentType,
                                     ContentQuery = e.ContentQuery,
                                     IsEnabled = e.IsEnabled,
                                     Content = e.ContentIds.Select(i => new ContentShortResponse {Id = i}).ToArray()
                                 }
                        );
    }

    private IQueryable<ContentShortResponse> GetDbContent(string type, IEnumerable<long> ids)
    {
        return type switch
               {
                   CreatePageContentType.Hashtag => _writeDb.Hashtag.Where(e => ids.Contains(e.Id))
                                                            .Select(e => new ContentShortResponse {Id = e.Id, Title = e.Name}),
                   CreatePageContentType.Song => _writeDb.ExternalSongs.Where(e => ids.Contains(e.Id))
                                                         .Select(e => new ContentShortResponse {Id = e.Id, Title = e.SongName}),
                   CreatePageContentType.Video => _writeDb.Video.Where(e => ids.Contains(e.Id))
                                                          .Select(
                                                               e => new ContentShortResponse {Id = e.Id, ThumbnailUrl = GetThumbnailUrl(e)}
                                                           ),
                   CreatePageContentType.Image => _writeDb.AiGeneratedContent
                                                          .Where(e => ids.Contains(e.Id) && e.Type == AiGeneratedContent.KnownTypeImage)
                                                          .Select(e => new ContentShortResponse {Id = e.Id}),
                   _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
               };
    }

    private async Task<ContentShortResponse[]> GetGeneratedContent(IEnumerable<long> ids)
    {
        var content = await _writeDb.AiGeneratedContent.Where(e => ids.Contains(e.Id))
                                    .Join(_writeDb.AiGeneratedImage, e => e.AiGeneratedImageId, i => i.Id, (i, e) => new {i.Id, Image = e})
                                    .AsNoTracking()
                                    .ToArrayAsync();

        await _fileStorage.InitUrls<AiGeneratedImage>(content.Select(e => e.Image));

        return content.Select(e => new ContentShortResponse {Id = e.Id, Files = e.Image.Files}).ToArray();
    }

    private static string GetThumbnailUrl(IVideoNameSource video)
    {
        return FreverAmazonCloudFrontSigner.SignUrlCanned(
            _staticNamingHelper.GetVideoThumbnailUrl(video),
            _staticConfig.CloudFrontCertKeyPairId,
            DateTime.Now.AddDays(10)
        );
    }

    private async Task<ContentRow> CreatePageRow()
    {
        var row = new ContentRow();
        await _writeDb.ContentRow.AddAsync(row);
        return row;
    }
}