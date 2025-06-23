namespace AuthServer.Contracts;

public class CheckParentEmailStatusRequest
{
    public string UserName { get; set; }
}

public class CheckParentEmailStatusResult
{
    public bool IsLoginByParentEmailAvailable { get; set; }
}