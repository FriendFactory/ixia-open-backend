namespace Common.Infrastructure.RequestId;

public interface IHeaderAccessor
{
    string GetRequestId();

    string GetUnityVersion();

    string GetRequestExperimentsHeader();

    string GetContentGenerationApiKey();
}