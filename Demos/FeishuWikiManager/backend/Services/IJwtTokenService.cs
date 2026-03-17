namespace FeishuWikiManager.Services;

public interface IJwtTokenService
{
    string GenerateToken(string openId, string unionId, string name);
    bool ValidateToken(string token, out System.Security.Claims.ClaimsPrincipal? principal);
    (string openId, string unionId, string name)? GetUserFromToken(string token);
}
