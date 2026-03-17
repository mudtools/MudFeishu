namespace FeishuWikiManager.Models;

public class OAuthOptions
{
    public string RedirectUri { get; set; } = string.Empty;
    public JwtOptions Jwt { get; set; } = new();
    public int StateExpirationMinutes { get; set; } = 5;
}

public class JwtOptions
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 1440;
}
