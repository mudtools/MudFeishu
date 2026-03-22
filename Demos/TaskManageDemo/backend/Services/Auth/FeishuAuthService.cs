// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu;
using Mud.Feishu.Abstractions;
using TaskManageDemo.Backend.Data;
using TaskManageDemo.Backend.Models.DTOs;

namespace TaskManageDemo.Backend.Services.Auth;

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

    public OAuthUrlResponse GetOAuthUrl(string? state = null, string? redirectUri = null)
    {
        var appId = _feishuAppManager.DefaultConfig.AppId;
        var configuredRedirectUri = redirectUri ?? _configuration["OAuth:RedirectUri"];

        if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(configuredRedirectUri))
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
                      $"redirect_uri={Uri.EscapeDataString(configuredRedirectUri)}&" +
                      $"response_type=code&" +
                      $"scope={Uri.EscapeDataString(scopeString)}&" +
                      $"state={Uri.EscapeDataString(state ?? "default")}";

        return new OAuthUrlResponse
        {
            AuthUrl = authUrl,
            State = state ?? "default"
        };
    }

    public async Task<LoginResponse?> LoginWithCodeAsync(string code, string state, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始飞书登录流程: State={State}", state);

            var redirectUri = _configuration["OAuth:RedirectUri"];

            // 使用授权码获取用户访问令牌
            var tokenResult = await _userTokenManager.GetUserTokenWithCodeAsync(code, redirectUri ?? string.Empty);

            if (tokenResult == null || tokenResult.Code != 0)
            {
                _logger.LogError("获取用户访问令牌失败: {Message}", tokenResult?.Msg ?? "未知错误");
                return null;
            }

            // 设置当前用户ID
            _feishuUserApi.CurrentUserId = tokenResult.UserId;

            // 获取用户信息
            var userInfoResult = await _feishuUserApi.GetUserInfoAsync();

            if (userInfoResult?.Data == null)
            {
                _logger.LogError("获取用户信息失败: {Message}", userInfoResult?.Msg ?? "未知错误");
                return null;
            }

            var feishuUser = userInfoResult.Data;

            var existingUser = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.OpenId == feishuUser.OpenId, cancellationToken);

            if (existingUser != null && existingUser.IsFeishuBound)
            {
                _logger.LogInformation("飞书用户已绑定本地账户，直接登录: {OpenId}", feishuUser.OpenId);

                existingUser.FeishuAccessToken = tokenResult.AccessToken;
                existingUser.FeishuRefreshToken = tokenResult.RefreshToken;
                existingUser.TokenExpiresAt = tokenResult.AccessTokenExpireTime > 0
                    ? DateTimeOffset.FromUnixTimeMilliseconds(tokenResult.AccessTokenExpireTime).DateTime
                    : null;
                existingUser.LastLoginAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync(cancellationToken);

                var permissions = await _permissionService.GetUserPermissionsAsync(existingUser.Id, cancellationToken);

                var token = _jwtTokenService.GenerateToken(
                    existingUser.Id.ToString(),
                    existingUser.Name,
                    existingUser.OpenId ?? string.Empty,
                    existingUser.Role ?? UserRoles.User,
                    permissions);

                _logger.LogInformation("飞书登录成功（已绑定）: {UserId}, {Name}",
                    existingUser.Id, existingUser.Name);

                return new LoginResponse
                {
                    AccessToken = token,
                    ExpiresIn = 3600,
                    User = new UserDto
                    {
                        Id = existingUser.Id,
                        FeishuId = existingUser.FeishuId,
                        Name = existingUser.Name,
                        AvatarUrl = existingUser.AvatarUrl,
                        Role = existingUser.Role ?? UserRoles.User,
                        Permissions = permissions
                    },
                    IsFirstLogin = false,
                    IsFeishuBound = true
                };
            }

            _logger.LogInformation("飞书用户未绑定本地账户，需要注册/绑定: {OpenId}", feishuUser.OpenId);

            var tempToken = _jwtTokenService.GenerateTempToken(
                feishuUser.OpenId ?? string.Empty,
                feishuUser.Name ?? "未知用户");

            return new LoginResponse
            {
                AccessToken = tempToken,
                ExpiresIn = 3600,
                User = new UserDto
                {
                    Id = 0,
                    FeishuId = feishuUser.OpenId,
                    Name = feishuUser.Name ?? "未知用户",
                    AvatarUrl = feishuUser.AvatarUrl,
                    Role = UserRoles.User,
                    Permissions = new List<string>()
                },
                IsFirstLogin = true,
                IsFeishuBound = false
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

    public async Task<FeishuUserInfoForRegistration?> GetUserInfoByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        try
        {
            var redirectUri = _configuration["OAuth:RedirectUri"];

            var tokenResult = await _userTokenManager.GetUserTokenWithCodeAsync(code, redirectUri ?? string.Empty);

            if (tokenResult == null || tokenResult.Code != 0)
            {
                _logger.LogError("获取用户访问令牌失败: {Message}", tokenResult?.Msg ?? "未知错误");
                return null;
            }

            _feishuUserApi.CurrentUserId = tokenResult.UserId;

            var userInfoResult = await _feishuUserApi.GetUserInfoAsync();

            if (userInfoResult?.Data == null)
            {
                _logger.LogError("获取用户信息失败: {Message}", userInfoResult?.Msg ?? "未知错误");
                return null;
            }

            var feishuUser = userInfoResult.Data;

            _logger.LogInformation("成功获取飞书用户信息用于注册: {OpenId}", feishuUser.OpenId);

            return new FeishuUserInfoForRegistration
            {
                FeishuId = feishuUser.OpenId ?? string.Empty,
                OpenId = feishuUser.OpenId,
                UnionId = feishuUser.UnionId,
                Name = feishuUser.Name ?? "未知用户",
                EnglishName = feishuUser.EnName,
                AvatarUrl = feishuUser.AvatarUrl,
                Email = feishuUser.Email,
                Mobile = feishuUser.Mobile,
                DepartmentId = null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据授权码获取飞书用户信息失败");
            return null;
        }
    }

    private async Task<(User user, bool isFirstLogin)> GetOrCreateUserAsync(
        string openId,
        string unionId,
        string name,
        string? avatar,
        string? email,
        CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.OpenId == openId, cancellationToken)
            .ConfigureAwait(false);

        if (user != null)
        {
            // 更新用户信息
            user.LastLoginAt = DateTime.UtcNow;
            user.Name = name;
            user.AvatarUrl = avatar;
            user.Email = email;
            user.UnionId = unionId;
            user.OpenId = openId;
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
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
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // 为新用户初始化默认权限
        await _permissionService.InitializeDefaultPermissionsAsync(user.Id, cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation("创建新用户: {UserId}, {Name}", user.Id, user.Name);
        return (user, true);
    }
}
