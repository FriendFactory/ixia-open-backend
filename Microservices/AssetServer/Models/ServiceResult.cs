using System.Net;

namespace AssetServer.Models;

public class ServiceResult<T>
{
    public ServiceResult(T data)
    {
        Data = data;
    }

    public ServiceResult(string errorMessage, HttpStatusCode recommendedStatusCode)
    {
        IsError = true;
        ErrorMessage = errorMessage;
        StatusCode = recommendedStatusCode;
    }

    public bool IsError { get; }
    public string ErrorMessage { get; }
    public HttpStatusCode StatusCode { get; }
    public T Data { get; }
}