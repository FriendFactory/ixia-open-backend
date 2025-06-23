namespace Frever.ClientService.Contract.InAppPurchases;

public class CompleteInAppPurchaseResponse
{
    public bool Ok { get; set; }

    public string ErrorCode { get; set; }

    public string ErrorMessage { get; set; }
}