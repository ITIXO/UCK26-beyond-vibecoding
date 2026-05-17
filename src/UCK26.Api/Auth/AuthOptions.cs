namespace UCK26.Api.Auth;

public class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = "uck26";
    public string Audience { get; set; } = "uck26-spa";
    public string SigningKey { get; set; } = "";
    public int ExpiresMinutes { get; set; } = 60;
}
