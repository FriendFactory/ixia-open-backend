using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AuthServer.Permissions.Services;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Common.Infrastructure;
using FluentValidation;
using Frever.AdminService.Core.Services.HashtagModeration.Contracts;
using Frever.AdminService.Core.Services.HashtagModeration.DataAccess;
using Frever.AdminService.Core.Utils;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Frever.AdminService.Core.Services.HashtagModeration;

public class HashtagModerationService(
    IHashtagRepository repo,
    IValidator<HashtagUpdate> validator,
    IMapper mapper,
    IUserPermissionService permissionService
) : IHashtagModerationService
{
    private static readonly JsonSerializerSettings JsonSerializerSettings =
        new() {ContractResolver = new CamelCasePropertyNamesContractResolver()};

    private readonly IHashtagRepository _repo = repo ?? throw new ArgumentNullException(nameof(repo));
    private readonly IValidator<HashtagUpdate> _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

    private readonly IUserPermissionService _permissionService =
        permissionService ?? throw new ArgumentNullException(nameof(permissionService));

    public async Task<IReadOnlyList<HashtagInfo>> GetAll(GetHashtagsRequest hashtagsRequest)
    {
        await _permissionService.EnsureHasVideoModerationAccess();

        var query = _repo.GetAll().AsNoTracking().Where(e => !e.IsDeleted);

        if (!string.IsNullOrWhiteSpace(hashtagsRequest.Name))
            query = query.Where(e => e.Name.ToLower().StartsWith(hashtagsRequest.Name.ToLower()));

        if (nameof(HashtagInfo.ChallengeSortOrder) == hashtagsRequest.OrderByColumnName)
            query = hashtagsRequest.Descending.GetValueOrDefault(false)
                        ? query.OrderByDescending(e => e.ChallengeSortOrder == 0 ? long.MinValue : e.ChallengeSortOrder).ThenBy(e => e.Id)
                        : query.OrderBy(e => e.ChallengeSortOrder == 0 ? long.MaxValue : e.ChallengeSortOrder).ThenBy(e => e.Id);
        else if (!string.IsNullOrWhiteSpace(hashtagsRequest.OrderByColumnName))
            query = query.SortBy(hashtagsRequest.OrderByColumnName, hashtagsRequest.Descending.GetValueOrDefault());

        query = query.Skip(hashtagsRequest.Skip).Take(hashtagsRequest.Take);

        return await query.ProjectTo<HashtagInfo>(_mapper.ConfigurationProvider).ToListAsync();
    }

    public async Task<bool> SoftDeleteAsync(long hashtagId, CancellationToken token = default)
    {
        await _permissionService.EnsureHasVideoModerationAccess();

        return await _repo.SoftDeleteAsync(hashtagId, token);
    }

    public async Task<HashtagInfo> UpdateByIdAsync(long hashtagId, JObject hashtagEdit, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(hashtagEdit);

        await _permissionService.EnsureHasVideoModerationAccess();

        var existing = await _repo.GetByIdAsync(hashtagId, token);

        if (existing is null)
            throw new AppErrorWithStatusCodeException("Hashtag was not found", HttpStatusCode.BadRequest);

        var edit = new HashtagUpdate();
        var existingJson = JsonConvert.SerializeObject(existing);
        JsonConvert.PopulateObject(existingJson, edit, JsonSerializerSettings);
        JsonConvert.PopulateObject(hashtagEdit.ToString(), edit, JsonSerializerSettings);

        await _validator.ValidateAndThrowAsync(edit, token);

        existing.Name = edit.Name;
        existing.ChallengeSortOrder = edit.ChallengeSortOrder;

        var result = await _repo.UpdateAsync(existing, token);

        if (!result)
            throw new AppErrorWithStatusCodeException("Hashtag was not updated", HttpStatusCode.BadRequest);

        return _mapper.Map<HashtagInfo>(existing);
    }
}