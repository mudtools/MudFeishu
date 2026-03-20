// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Mud.Feishu;
using Mud.Feishu.Abstractions;
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
    OAuthUrlResponse GetOAuthUrl(string? state = null);

    /// <summary>
    /// 使用授权码登录
    /// </summary>
    Task<LoginResponse?> LoginWithCodeAsync(string code, string state, CancellationToken cancellationToken = default);

    /// <summary>
    /// 刷新飞书用户令牌
    /// </summary>
    Task<TokenRefreshResponse?> RefreshTokenAsync(string openId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取飞书用户详细信息
    /// </summary>
    Task<FeishuUserDetail?> GetUserDetailAsync(string openId, CancellationToken cancellationToken = default);
}

/// <summary>
/// 飞书 OAuth 认证服务实现
/// </summary>
public class FeishuAuthService : IFeishuAuthService
{
    private readonly TaskManageDbContext _dbContext;
    private readonly IFeishuAppManager _feishuAppManager;
    private readonly IFeishuUserTokenManager _userTokenManager;
    private readonly IFeishuUserV3User _feishuUserApi;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPermissionService _permissionService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<FeishuAuthService> _logger;

    public FeishuAuthService(
        TaskManageDbContext dbContext,
        IFeishuAppManager feishuAppManager,
        IFeishuUserV3User feishuUserApi,
        IJwtTokenService jwtTokenService,
        IPermissionService permissionService,
        IConfiguration configuration,
        ILogger<FeishuAuthService> logger)
    {
        _dbContext = dbContext;
        _feishuAppManager = feishuAppManager;
        _userTokenManager = feishuAppManager.DefaultUserTokenManager;
        _feishuUserApi = feishuUserApi;
        _jwtTokenService = jwtTokenService;
        _permissionService = permissionService;
        _configuration = configuration;
        _logger = logger;
    }

    public OAuthUrlResponse GetOAuthUrl(string? state = null)
    {
        var appId = _feishuAppManager.DefaultConfig.AppId;
        var redirectUri = _configuration["OAuth:RedirectUri"];

        if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(redirectUri))
        {
            throw new InvalidOperationException("飞书应用配置不完整");
        }

        // 定义需要的权限范围
        var scopes = new[]
        {
            "contact:user.base:readonly",
            "task:task",
            "task:task:readonly"
        };
        var scopeString = string.Join(" ", scopes);

        var authUrl = $"https://accounts.feishu.cn/open-apis/authen/v1/authorize?" +
                      $"client_id={appId}&" +
                      $"redirect_uri={Uri.EscapeDataString(redirectUri)}&" +
                      $"response_type=code&" +
                      $"scope={Uri.EscapeDataString(scopeString)}&" +
                      $"state={state}";

        _logger.LogInformation("生成飞书授权URL成功");

        return new OAuthUrlResponse
        {
            Url = authUrl,
            State = state
        };
    }

    public async Task<LoginResponse?> LoginWithCodeAsync(string code, string state, CancellationToken cancellationToken = default)
    {
        try
        {
            var redirectUri = _configuration["OAuth:RedirectUri"];

            // 1. 使用授权码获取用户访问令牌
            var tokenResult = await _userTokenManager.GetUserTokenWithCodeAsync(code, redirectUri ?? string.Empty);

            if (tokenResult == null || tokenResult.Code != 0)
            {
                _logger.LogError("获取用户访问令牌失败: {Message}", tokenResult?.Msg ?? "未知错误");
                return null;
            }

            // 2. 设置当前用户ID，用于调用飞书API
            _feishuUserApi.CurrentUserId = tokenResult.UserId;

            // 3. 获取用户信息
            var userInfoResult = await _feishuUserApi.GetUserInfoAsync();

            if (userInfoResult?.Data == null)
            {
                _logger.LogError("获取用户信息失败: {Message}", userInfoResult?.Msg ?? "未知错误");
                return null;
            }

            var feishuUser = userInfoResult.Data;
            var openId = feishuUser.OpenId ?? string.Empty;
            var unionId = feishuUser.UnionId ?? string.Empty;

            // 4. 查找或创建本地用户
            var (user, isFirstLogin) = await GetOrCreateUserAsync(
                openId,
                unionId,
                feishuUser.Name ?? "未知用户",
                feishuUser.AvatarUrl,
                feishuUser.Email
            );

            // 5. 更新用户飞书令牌
            user.FeishuAccessToken = tokenResult.AccessToken;
            user.FeishuRefreshToken = tokenResult.RefreshToken;
            user.TokenExpiresAt = tokenResult.AccessTokenExpireTime > 0
                ? DateTimeOffset.FromUnixTimeMilliseconds(tokenResult.AccessTokenExpireTime).DateTime
                : null;
            user.LastLoginAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);

            // 6. 生成 JWT Token
            var jwtToken = _jwtTokenService.GenerateToken(openId, unionId, user.Name, user.Id);

            // 7. 获取用户权限
            var permissions = await _permissionService.GetUserPermissionsAsync(user.Id, cancellationToken);

            _logger.LogInformation("用户登录成功: {UserId} ({Name}), 首次登录: {IsFirstLogin}",
                user.Id, user.Name, isFirstLogin);

            return new LoginResponse
            {
                AccessToken = jwtToken,
                TokenType = "Bearer",
                ExpiresIn = _configuration.GetSection("OAuth:Jwt").GetValue<int>("ExpirationMinutes", 60) * 60,
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

    public async Task<TokenRefreshResponse?> RefreshTokenAsync(string openId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.OpenId == openId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("用户不存在: {OpenId}", openId);
                return null;
            }

            var canRefresh = await _userTokenManager.CanRefreshTokenAsync(openId);
            if (!canRefresh)
            {
                _logger.LogWarning("无法刷新Token，请重新登录: {OpenId}", openId);
                return null;
            }

            var newToken = await _userTokenManager.RefreshUserTokenAsync(openId);
            if (newToken == null)
            {
                _logger.LogError("刷新Token失败: {OpenId}", openId);
                return null;
            }

            // 更新数据库中的令牌
            user.FeishuAccessToken = newToken.AccessToken;
            user.FeishuRefreshToken = newToken.RefreshToken;
            user.TokenExpiresAt = newToken.AccessTokenExpireTime > 0
                ? DateTimeOffset.FromUnixTimeMilliseconds(newToken.AccessTokenExpireTime).DateTime
                : null;

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Token刷新成功: {OpenId}", openId);

            return new TokenRefreshResponse
            {
                AccessToken = newToken.AccessToken,
                RefreshToken = newToken.RefreshToken,
                ExpiresIn = (int)((user.TokenExpiresAt?.ToUniversalTime() - DateTime.UtcNow)?.TotalSeconds ?? 0)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刷新Token失败: {OpenId}", openId);
            return null;
        }
    }

    public async Task<FeishuUserDetail?> GetUserDetailAsync(string openId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.OpenId == openId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("用户不存在: {OpenId}", openId);
                return null;
            }

            // 检查 Token 是否有效
            if (string.IsNullOrEmpty(user.FeishuAccessToken))
            {
                _logger.LogWarning("用户Token为空: {OpenId}", openId);
                return null;
            }

            // 设置当前用户 ID，用于调用飞书 API
            _feishuUserApi.CurrentUserId = openId;

            // 调用飞书 API 获取详细用户信息
            var result = await _feishuUserApi.GetUserInfoAsync();

            if (result?.Code != 0 || result.Data == null)
            {
                _logger.LogError("从飞书获取用户信息失败: {Message}", result?.Msg ?? "未知错误");
                return null;
            }

            var data = result.Data;

            _logger.LogInformation("成功获取用户详细信息: {OpenId}", openId);

            return new FeishuUserDetail
            {
                OpenId = data.OpenId ?? string.Empty,
                UnionId = data.UnionId ?? string.Empty,
                UserId = data.UserId ?? string.Empty,
                Name = data.Name ?? string.Empty,
                EnName = data.EnName,
                Nickname = data.Nickname,
                AvatarUrl = data.AvatarUrl,
                AvatarThumb = data.AvatarThumb,
                AvatarMiddle = data.AvatarMiddle,
                AvatarBig = data.AvatarBig,
                Email = data.Email,
                Mobile = data.Mobile,
                EnterpriseEmail = data.EnterpriseEmail,
                EmployeeNo = data.EmployeeNo,
                TenantKey = data.TenantKey
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取详细用户信息失败: {OpenId}", openId);
            return null;
        }
    }

    private async Task<(User user, bool isFirstLogin)> GetOrCreateUserAsync(
        string openId,
        string unionId,
        string name,
        string? avatar,
        string? email)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.OpenId == openId);

        if (user != null)
        {
            // 更新用户信息
            user.LastLoginAt = DateTime.UtcNow;
            user.Name = name;
            user.AvatarUrl = avatar;
            user.Email = email;
            user.UnionId = unionId;
            user.OpenId = openId;
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("用户登录更新: {UserId}, {Name}", user.Id, user.Name);
            return (user, false);
        }

        // 创建新用户
        user = new User
        {
            FeishuId = openId,
            OpenId = openId,
            UnionId = unionId,
            Name = name,
            AvatarUrl = avatar,
            Email = email,
            Role = UserRoles.User,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            LastSyncedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        // 为新用户初始化默认权限
        await _permissionService.InitializeDefaultPermissionsAsync(user.Id);

        _logger.LogInformation("创建新用户: {UserId}, {Name}", user.Id, user.Name);
        return (user, true);
    }
}

/// <summary>
/// 飞书用户详细信息
/// </summary>
public class FeishuUserDetail
{
    public string OpenId { get; set; } = string.Empty;
    public string UnionId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? EnName { get; set; }
    public string? Nickname { get; set; }
    public string? AvatarUrl { get; set; }
    public string? AvatarThumb { get; set; }
    public string? AvatarMiddle { get; set; }
    public string? AvatarBig { get; set; }
    public string? Email { get; set; }
    public string? Mobile { get; set; }
    public string? EnterpriseEmail { get; set; }
    public string? EmployeeNo { get; set; }
    public string? TenantKey { get; set; }
}

/// <summary>
/// Token 刷新响应
/// </summary>
public class TokenRefreshResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
}
