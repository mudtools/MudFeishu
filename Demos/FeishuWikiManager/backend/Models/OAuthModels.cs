namespace FeishuWikiManager.Models;

public class AuthCallbackRequest
{
    public string Code { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
}

public class LoginResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Token { get; set; }
    public UserInfoResponse? User { get; set; }
}

public class UserInfoResponse
{
    public string OpenId { get; set; } = string.Empty;
    public string UnionId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public string? Email { get; set; }
}

public class DetailedUserInfoResponse
{
    public string OpenId { get; set; } = string.Empty;
    public string UnionId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? EnName { get; set; }
    public string? Nickname { get; set; }
    public string? Avatar { get; set; }
    public string? AvatarThumb { get; set; }
    public string? AvatarMiddle { get; set; }
    public string? AvatarBig { get; set; }
    public string? Email { get; set; }
    public string? Mobile { get; set; }
    public string? EnterpriseEmail { get; set; }
    public string? EmployeeNo { get; set; }
    public string? TenantKey { get; set; }
}

public class AuthUrlResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Url { get; set; }
    public string? State { get; set; }
}

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class RefreshTokenResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
}

public class TokenStatusResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public bool HasValidToken { get; set; }
    public bool CanRefresh { get; set; }
    public TokenExpirationInfo? TokenInfo { get; set; }
}

public class TokenExpirationInfo
{
    public DateTime? AccessTokenExpiresAt { get; set; }
    public DateTime? RefreshTokenExpiresAt { get; set; }
    public bool AccessTokenExpired { get; set; }
    public bool RefreshTokenExpired { get; set; }
}

public class LogoutResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}
