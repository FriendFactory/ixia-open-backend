namespace AuthServer.DataAccess.Entities;

public class AspNetUserClaims
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public string ClaimType { get; set; }
    public string ClaimValue { get; set; }
}