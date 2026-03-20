// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using TaskManageDemo.Backend.Data;
using TaskManageDemo.Backend.Models.DTOs;
using TaskManageDemo.Backend.Models.Entities;

namespace TaskManageDemo.Backend.Services.Auth;

/// <summary>
/// 飞书 OAuth 认证服务接口
/// </summary>
public interface IFeishuAuthService
{
    /// <summary>
    /// 获取飞书 OAuth 授权链接
    /// </summary>
    Task<OAuthUrlResponse> GetOAuthUrlAsync(string? redirectUri = null, string? state = null);

    /// <summary>
    /// 使用授权码登录
    /// </summary>
    Task<LoginResponse?> LoginWithCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取飞书用户信息
    /// </summary>
    Task<FeishuUserInfo?> GetFeishuUserInfoAsync(string accessToken, CancellationToken cancellationToken = default);
}

/// <summary>
/// 飞书 OAuth 认证服务实现
/// </summary>
public class FeishuAuthService : IFeishuAuthService
{
    private readonly TaskManageDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IPermissionService _permissionService;
    private readonly ILogger<FeishuAuthService> _logger;

    private readonly string _appId;
    private readonly string _appSecret;
    private readonly string _baseOAuthUrl = "https://open.feishu.cn/open-apis/authen/v1";

    public FeishuAuthService(
        TaskManageDbContext dbContext,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        IPermissionService permissionService,
        ILogger<FeishuAuthService> logger)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _permissionService = permissionService;
        _logger = logger;

        // 从配置读取飞书应用信息
        var feishuApps = _configuration.GetSection("FeishuApps").Get<List<FeishuAppConfig>>();
        var defaultApp = feishuApps?.FirstOrDefault(a => a.IsDefault) ?? feishuApps?.FirstOrDefault();
        _appId = defaultApp?.AppId ?? string.Empty;
        _appSecret = defaultApp?.AppSecret ?? string.Empty;
    }

    public Task<OAuthUrlResponse> GetOAuthUrlAsync(string? redirectUri = null, string? state = null)
    {
        var encodedRedirectUri = Uri.EscapeDataString(redirectUri ?? "/");
        var encodedState = Uri.EscapeDataString(state ?? Guid.NewGuid().ToString("N"));

        // 飞书 OAuth 授权链接
        var url = $"https://open.feishu.cn/open-apis/authen/v1/index?app_id={_appId}&redirect_uri={encodedRedirectUri}&state={encodedState}";

        return Task.FromResult(new OAuthUrlResponse
        {
            Url = url,
            State = state
        });
    }

    public async Task<LoginResponse?> LoginWithCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. 获取访问令牌
            var tokenResponse = await GetAccessTokenAsync(code, cancellationToken);
            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                _logger.LogWarning("获取飞书访问令牌失败");
                return null;
            }

            // 2. 获取用户信息
            var feishuUserInfo = await GetFeishuUserInfoAsync(tokenResponse.AccessToken, cancellationToken);
            if (feishuUserInfo == null)
            {
                _logger.LogWarning("获取飞书用户信息失败");
                return null;
            }

            // 3. 查找或创建本地用户
            var (user, isFirstLogin) = await FindOrCreateUserAsync(feishuUserInfo, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("创建本地用户失败");
                return null;
            }

            // 4. 更新最后登录时间
            user.LastLoginAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);

            // 5. 获取用户权限
            var permissions = await _permissionService.GetUserPermissionsAsync(user.Id, cancellationToken);

            // 6. 生成访问令牌（使用飞书 token 作为系统 token）
            var accessToken = tokenResponse.AccessToken;

            _logger.LogInformation("用户登录成功: {UserId} ({FeishuId}), 首次登录: {IsFirstLogin}",
                user.Id, user.FeishuId, isFirstLogin);

            return new LoginResponse
            {
                AccessToken = accessToken,
                TokenType = "Bearer",
                ExpiresIn = tokenResponse.ExpiresIn,
                IsFirstLogin = isFirstLogin,
                User = new UserDto
                {
                    Id = user.Id,
                    FeishuId = user.FeishuId,
                    OpenId = user.OpenId,
                    UnionId = user.UnionId,
                    Name = user.Name,
                    EnglishName = user.EnglishName,
                    Email = user.Email,
                    Mobile = user.Mobile,
                    AvatarUrl = user.AvatarUrl,
                    DepartmentId = user.DepartmentId,
                    Position = user.Position,
                    Role = user.Role ?? UserRoles.User,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "飞书登录处理失败");
            return null;
        }
    }

    public async Task<FeishuUserInfo?> GetFeishuUserInfoAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.GetAsync($"{_baseOAuthUrl}/user_info", cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<FeishuUserInfoResponse>(content);

            if (result?.Code != 0 || result.Data == null)
            {
                _logger.LogWarning("获取飞书用户信息失败: {Code}, {Message}", result?.Code, result?.Message);
                return null;
            }

            return new FeishuUserInfo
            {
                OpenId = result.Data.OpenId,
                UnionId = result.Data.UnionId,
                UserId = result.Data.UserId,
                Name = result.Data.Name,
                EnName = result.Data.EnName,
                Email = result.Data.Email,
                Mobile = result.Data.Mobile,
                AvatarUrl = result.Data.AvatarUrl,
                AvatarThumb = result.Data.AvatarThumb,
                AvatarMiddle = result.Data.AvatarMiddle,
                AvatarBig = result.Data.AvatarBig,
                DepartmentId = result.Data.DepartmentId,
                DepartmentName = result.Data.DepartmentName,
                Position = result.Data.Position
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取飞书用户信息异常");
            return null;
        }
    }

    private async Task<FeishuTokenResponse?> GetAccessTokenAsync(string code, CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var request = new
            {
                grant_type = "authorization_code",
                code = code
            };

            // 使用应用级访问令牌
            var appToken = await GetAppAccessTokenAsync(cancellationToken);
            if (string.IsNullOrEmpty(appToken))
            {
                _logger.LogWarning("获取应用访问令牌失败");
                return null;
            }

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", appToken);

            var content = new StringContent(
                JsonSerializer.Serialize(request),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync($"{_baseOAuthUrl}/access_token", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<FeishuTokenResponse>(responseContent);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取飞书访问令牌异常");
            return null;
        }
    }

    private async Task<string?> GetAppAccessTokenAsync(CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var request = new
            {
                app_id = _appId,
                app_secret = _appSecret
            };

            var content = new StringContent(
                JsonSerializer.Serialize(request),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync(
                "https://open.feishu.cn/open-apis/auth/v3/app_access_token/internal",
                content,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<FeishuAppTokenResponse>(responseContent);

            return result?.AppAccessToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取飞书应用访问令牌异常");
            return null;
        }
    }

    private async Task<(User? user, bool isFirstLogin)> FindOrCreateUserAsync(FeishuUserInfo feishuUserInfo, CancellationToken cancellationToken)
    {
        // 优先使用 UnionId 查找，其次是 OpenId，最后是 UserId
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u =>
                (!string.IsNullOrEmpty(feishuUserInfo.UnionId) && u.UnionId == feishuUserInfo.UnionId) ||
                (!string.IsNullOrEmpty(feishuUserInfo.OpenId) && u.OpenId == feishuUserInfo.OpenId) ||
                u.FeishuId == feishuUserInfo.UserId,
                cancellationToken);

        if (user != null)
        {
            // 更新用户信息
            user.FeishuId = feishuUserInfo.UserId ?? user.FeishuId;
            user.OpenId = feishuUserInfo.OpenId ?? user.OpenId;
            user.UnionId = feishuUserInfo.UnionId ?? user.UnionId;
            user.Name = feishuUserInfo.Name ?? user.Name;
            user.EnglishName = feishuUserInfo.EnName ?? user.EnglishName;
            user.Email = feishuUserInfo.Email ?? user.Email;
            user.Mobile = feishuUserInfo.Mobile ?? user.Mobile;
            user.AvatarUrl = feishuUserInfo.AvatarUrl ?? user.AvatarUrl;
            user.DepartmentId = feishuUserInfo.DepartmentId ?? user.DepartmentId;
            user.Position = feishuUserInfo.Position ?? user.Position;
            user.UpdatedAt = DateTime.UtcNow;
            user.IsActive = true;

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("更新已有用户信息: {UserId}", user.Id);
            return (user, false);
        }

        // 创建新用户
        user = new User
        {
            FeishuId = feishuUserInfo.UserId ?? feishuUserInfo.OpenId ?? Guid.NewGuid().ToString(),
            OpenId = feishuUserInfo.OpenId,
            UnionId = feishuUserInfo.UnionId,
            Name = feishuUserInfo.Name ?? "未知用户",
            EnglishName = feishuUserInfo.EnName,
            Email = feishuUserInfo.Email,
            Mobile = feishuUserInfo.Mobile,
            AvatarUrl = feishuUserInfo.AvatarUrl,
            DepartmentId = feishuUserInfo.DepartmentId,
            Position = feishuUserInfo.Position,
            Role = UserRoles.User,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            LastSyncedAt = DateTime.UtcNow
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // 为新用户初始化默认权限
        await _permissionService.InitializeDefaultPermissionsAsync(user.Id, cancellationToken);

        _logger.LogInformation("创建新用户: {UserId} ({Name})", user.Id, user.Name);
        return (user, true);
    }
}

/// <summary>
/// 飞书应用配置
/// </summary>
public class FeishuAppConfig
{
    public string AppKey { get; set; } = string.Empty;
    public string AppId { get; set; } = string.Empty;
    public string AppSecret { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public string? EncryptKey { get; set; }
    public string? VerificationToken { get; set; }
}

/// <summary>
/// 飞书用户信息
/// </summary>
public class FeishuUserInfo
{
    public string? OpenId { get; set; }
    public string? UnionId { get; set; }
    public string? UserId { get; set; }
    public string? Name { get; set; }
    public string? EnName { get; set; }
    public string? Email { get; set; }
    public string? Mobile { get; set; }
    public string? AvatarUrl { get; set; }
    public string? AvatarThumb { get; set; }
    public string? AvatarMiddle { get; set; }
    public string? AvatarBig { get; set; }
    public string? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string? Position { get; set; }
}

/// <summary>
/// 飞书用户信息响应
/// </summary>
public class FeishuUserInfoResponse
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("msg")]
    public string? Message { get; set; }

    [JsonPropertyName("data")]
    public FeishuUserInfoData? Data { get; set; }
}

public class FeishuUserInfoData
{
    [JsonPropertyName("open_id")]
    public string? OpenId { get; set; }

    [JsonPropertyName("union_id")]
    public string? UnionId { get; set; }

    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("en_name")]
    public string? EnName { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("mobile")]
    public string? Mobile { get; set; }

    [JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; set; }

    [JsonPropertyName("avatar_thumb")]
    public string? AvatarThumb { get; set; }

    [JsonPropertyName("avatar_middle")]
    public string? AvatarMiddle { get; set; }

    [JsonPropertyName("avatar_big")]
    public string? AvatarBig { get; set; }

    [JsonPropertyName("department_id")]
    public string? DepartmentId { get; set; }

    [JsonPropertyName("department_name")]
    public string? DepartmentName { get; set; }

    [JsonPropertyName("position")]
    public string? Position { get; set; }
}

/// <summary>
/// 飞书令牌响应
/// </summary>
public class FeishuTokenResponse
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("msg")]
    public string? Message { get; set; }

    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("token_type")]
    public string? TokenType { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("open_id")]
    public string? OpenId { get; set; }

    [JsonPropertyName("union_id")]
    public string? UnionId { get; set; }

    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }
}

/// <summary>
/// 飞书应用令牌响应
/// </summary>
public class FeishuAppTokenResponse
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("msg")]
    public string? Message { get; set; }

    [JsonPropertyName("app_access_token")]
    public string? AppAccessToken { get; set; }

    [JsonPropertyName("expire")]
    public int Expire { get; set; }
}
