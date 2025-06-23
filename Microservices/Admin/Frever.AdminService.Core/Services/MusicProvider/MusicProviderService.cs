using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Common.Infrastructure;
using Common.Infrastructure.MusicProvider;
using FluentValidation;

namespace Frever.AdminService.Core.Services.MusicProvider;

internal sealed class MusicProviderService(
    IHttpClientFactory httpClientFactory,
    IOAuthSignatureProvider oAuthSignatureProvider,
    IValidator<MusicProviderRequest> validator
) : IMusicProviderService
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    private readonly IOAuthSignatureProvider _oAuthSignatureProvider = oAuthSignatureProvider ?? throw new ArgumentNullException(nameof(oAuthSignatureProvider));
    private readonly IValidator<MusicProviderRequest> _validator = validator ?? throw new ArgumentNullException(nameof(validator));

    public async Task<string> SendMusicProviderRequest(MusicProviderRequest request)
    {
        await _validator.ValidateAndThrowAsync(request);

        var httpMethod = Enum.Parse<MusicProviderHttpMethod>(request.HttpMethod, true);

        var signedData = _oAuthSignatureProvider.GetSignedRequestData(httpMethod, request.BaseUrl, request.QueryParameters);

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var requestMessage = new HttpRequestMessage(new HttpMethod(request.HttpMethod.ToUpper()), signedData.Url);
        if (request.Body != null)
            requestMessage.Content = new StringContent(request.Body, Encoding.UTF8, "application/json");

        var response = await client.SendAsync(requestMessage);

        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new AppErrorWithStatusCodeException(responseBody, response.StatusCode);

        return responseBody;
    }

    public async Task<SignedRequestData> SignMusicProviderUrl(MusicProviderRequest request)
    {
        await _validator.ValidateAndThrowAsync(request);

        var httpMethod = Enum.Parse<MusicProviderHttpMethod>(request.HttpMethod, true);

        var signedData = _oAuthSignatureProvider.GetSignedRequestData(httpMethod, request.BaseUrl, request.QueryParameters);

        return signedData;
    }
}