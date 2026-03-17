using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FeishuWikiManager.Models;
using Microsoft.IdentityModel.Tokens;

namespace FeishuWikiManager.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(JwtOptions options)
    {
        _options = options;
    }

    public string GenerateToken(string openId, string unionId, string name)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, openId),
            new Claim(JwtRegisteredClaimNames.UniqueName, unionId),
            new Claim(ClaimTypes.Name, name),
            new Claim("open_id", openId),
            new Claim("union_id", unionId),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_options.ExpirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public bool ValidateToken(string token, out ClaimsPrincipal? principal)
    {
        principal = null;

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_options.Secret);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _options.Issuer,
                ValidAudience = _options.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.Zero
            };

            principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public (string openId, string unionId, string name)? GetUserFromToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);

            var openId = jwtToken.Claims.FirstOrDefault(c => c.Type == "open_id")?.Value;
            var unionId = jwtToken.Claims.FirstOrDefault(c => c.Type == "union_id")?.Value;
            var name = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

            if (openId != null && unionId != null && name != null)
            {
                return (openId, unionId, name);
            }
        }
        catch
        {
        }

        return null;
    }
}
