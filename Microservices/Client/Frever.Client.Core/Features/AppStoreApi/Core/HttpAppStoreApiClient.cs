using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace Frever.Client.Core.Features.AppStoreApi.Core;

public partial class HttpAppStoreApiClient(IHttpClientFactory httpClientFactory, AppStoreApiOptions options, ILoggerFactory loggerFactory)
    : IAppStoreApiClient
{
    private readonly HttpClient httpClient = httpClientFactory.CreateClient();
    private readonly ILogger log = loggerFactory.CreateLogger("Ixia.AppStoreApi");
}