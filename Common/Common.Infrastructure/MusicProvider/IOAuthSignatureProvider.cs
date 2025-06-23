using System.Collections.Generic;

namespace Common.Infrastructure.MusicProvider;

public interface IOAuthSignatureProvider
{
    SignedRequestData GetSignedRequestData(
        MusicProviderHttpMethod httpMethod,
        string baseUrl,
        SortedDictionary<string, string> queryParameters
    );
}