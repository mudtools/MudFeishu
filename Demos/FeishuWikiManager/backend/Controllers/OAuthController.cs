// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FeishuWikiManager.Models;
using FeishuWikiManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mud.Feishu;
using Mud.Feishu.Abstractions;

namespace FeishuWikiManager.Controllers;

[Route("api/[controller]")]
public class OAuthController : BaseController
{
    private readonly IFeishuAppManager _feishuAppManager;
    private readonly IConfiguration _configuration;
    private readonly IFeishuTokenManagerResolver _tokenManagerResolver;
    private readonly IStateStorageService _stateStorageService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUserService _userService;
    private readonly IFeishuUserV3User _feishuUserApi;
    private readonly IFeishuCurrentUserContext _currentUserContext;
    private readonly ILogger<OAuthController> _logger;

    public OAuthController(
        IConfiguration configuration,
        IFeishuAppManager feishuAppManager,
        IFeishuTokenManagerResolver tokenManagerResolver,
        IStateStorageService stateStorageService,
        IJwtTokenService jwtTokenService,
        IUserService userService,
        IFeishuUserV3User feishuUserApi,
        IFeishuCurrentUserContext currentUserContext,
        ILogger<OAuthController> logger)
    {
        _configuration = configuration;
        _feishuAppManager = feishuAppManager;
        _tokenManagerResolver = tokenManagerResolver;
        _stateStorageService = stateStorageService;
        _jwtTokenService = jwtTokenService;
        _userService = userService;
        _feishuUserApi = feishuUserApi;
        _currentUserContext = currentUserContext;
        _logger = logger;
    }

    [HttpGet("feishu/url")]
    public IActionResult GetFeishuAuthUrl()
    {
        try
        {
            var appId = _feishuAppManager.DefaultConfig.AppId;
            var redirectUri = _configuration["OAuth:RedirectUri"];

            if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(redirectUri))
            {
                return BadRequestResult("飞书应用配置不完整");
            }

            var state = _stateStorageService.GenerateState();

            var scopes = new[]
            {
                "contact:user.base:readonly",
                "wiki:wiki:readonly",
                "wiki:wiki",
                "docs:doc:readonly",
                "docs:doc",
                "drive:drive:readonly",
                "drive:drive"
            };
            var scopeString = string.Join(" ", scopes);

            var authUrl = $"https://accounts.feishu.cn/open-apis/authen/v1/authorize?" +
                          $"client_id={appId}&" +
                          $"redirect_uri={Uri.EscapeDataString(redirectUri)}&" +
                          $"response_type=code&" +
                          $"scope={Uri.EscapeDataString(scopeString)}&" +
                          $"state={state}";

            _logger.LogInformation("生成飞书授权URL成功，State: {State}", state);

            return Ok(new AuthUrlResponse
            {
                Success = true,
                Message = "生成授权URL成功",
                Url = authUrl,
                State = state
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成飞书授权URL失败");
            return ServerError("生成授权URL失败", ex);
        }
    }

    [HttpPost("feishu/callback")]
    public async Task<IActionResult> HandleFeishuCallback([FromBody] AuthCallbackRequest request)
    {
        try
        {
            _logger.LogInformation("收到飞书OAuth回调，Code: {Code}, State: {State}",
                request.Code.Length > 8 ? request.Code[..8] + "..." : request.Code, request.State);

            if (string.IsNullOrEmpty(request.Code) || string.IsNullOrEmpty(request.State))
            {
                return BadRequestResult("缺少必要参数");
            }

            if (!_stateStorageService.ValidateState(request.State))
            {
                _logger.LogWarning("State验证失败: {State}", request.State);
                return BadRequestResult("State验证失败，可能存在CSRF攻击");
            }

            _stateStorageService.RemoveState(request.State);

            var redirectUri = _configuration["OAuth:RedirectUri"];

            _logger.LogInformation("开始使用授权码获取用户访问令牌");
            var tokenResult = await _tokenManagerResolver.GetUserTokenManager().GetUserTokenWithCodeAsync(request.Code, redirectUri ?? string.Empty);

            if (tokenResult == null || tokenResult.Code != 0)
            {
                _logger.LogError("获取用户访问令牌失败: {Message}", tokenResult?.Msg ?? "未知错误");
                return BadRequestResult($"获取用户访问令牌失败: {tokenResult?.Msg ?? "未知错误"}");
            }

            _logger.LogInformation("成功获取用户访问令牌");

            // GetUserTokenWithCodeAsync 内部已处理 OAuth v2 场景：
            // 当 OAuth 端点不返回 OpenId 时，会自动用 access_token 调用用户信息 API 获取 OpenId，
            // 并完成令牌缓存。因此此处 tokenResult.OpenId 一定有值。
            // 必须在调用 GetUserInfoAsync 前设置用户上下文，否则 SDK 无法找到用户令牌。
            _currentUserContext.SetUser(tokenResult.OpenId!, tokenResult.UnionId, tokenResult.OpenId, null);

            _logger.LogInformation("开始获取用户信息");
            var userInfoResult = await _feishuUserApi.GetUserInfoAsync();

            if (userInfoResult?.Data == null)
            {
                _logger.LogError("获取用户信息失败: {Message}", userInfoResult?.Msg ?? "未知错误");
                return BadRequestResult($"获取用户信息失败: {userInfoResult?.Msg ?? "未知错误"}");
            }

            var feishuUser = userInfoResult.Data;

            _logger.LogInformation("成功获取用户信息: {Name} ({OpenId})", feishuUser.Name ?? "未知", feishuUser.OpenId ?? "未知");

            var userId = await _userService.GetOrCreateUserAsync(
                feishuUser.OpenId ?? string.Empty,
                feishuUser.UnionId ?? string.Empty,
                feishuUser.Name ?? "未知用户",
                feishuUser.AvatarUrl ?? string.Empty,
                feishuUser.Email
            );

            await _userService.UpdateUserTokenAsync(
                userId,
                tokenResult.AccessToken,
                tokenResult.RefreshToken,
                tokenResult.AccessTokenExpireTime > 0
                    ? DateTimeOffset.FromUnixTimeMilliseconds(tokenResult.AccessTokenExpireTime).DateTime
                    : null
            );

            _logger.LogInformation("用户处理完成，本地用户ID: {UserId}", userId);

            var jwtToken = _jwtTokenService.GenerateToken(
                feishuUser.OpenId ?? string.Empty,
                feishuUser.UnionId ?? string.Empty,
                feishuUser.Name ?? "未知用户"
            );

            _logger.LogInformation("JWT令牌生成成功");

            return Ok(new LoginResponse
            {
                Success = true,
                Message = "登录成功",
                Token = jwtToken,
                User = new UserInfoResponse
                {
                    OpenId = feishuUser.OpenId ?? string.Empty,
                    UnionId = feishuUser.UnionId ?? string.Empty,
                    Name = feishuUser.Name ?? "未知用户",
                    Avatar = feishuUser.AvatarUrl,
                    Email = feishuUser.Email
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理飞书OAuth回调失败");
            return ServerError("登录失败", ex);
        }
    }

    /// <summary>
    /// 验证JWT令牌
    /// </summary>
    /// <param name="token">JWT令牌</param>
    /// <returns>验证结果</returns>
    [HttpPost("validate-token")]
    public IActionResult ValidateToken([FromBody] string token)
    {
        try
        {
            if (string.IsNullOrEmpty(token))
            {
                return Fail("令牌不能为空");
            }

            var isValid = _jwtTokenService.ValidateToken(token, out var principal);

            if (!isValid || principal == null)
            {
                return UnauthorizedResult("令牌无效或已过期");
            }

            var userInfo = _jwtTokenService.GetUserFromToken(token);
            return Success(new
            {
                openId = userInfo?.openId,
                unionId = userInfo?.unionId,
                name = userInfo?.name
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证令牌失败");
            return ServerError("验证令牌失败", ex);
        }
    }

    [HttpGet("status")]
    [Authorize]
    public async Task<IActionResult> GetTokenStatus()
    {
        try
        {
            var openId = CurrentOpenId;

            if (string.IsNullOrEmpty(openId))
            {
                return UnauthorizedResult();
            }

            var user = await _userService.GetUserByOpenIdAsync(openId);

            if (user == null)
            {
                return NotFoundResult("用户不存在");
            }

            var hasValidToken = !string.IsNullOrEmpty(user.FeishuAccessToken) &&
                               user.TokenExpiresAt.HasValue &&
                               user.TokenExpiresAt.Value > DateTime.UtcNow;

            var canRefresh = !string.IsNullOrEmpty(user.FeishuRefreshToken);

            return Ok(new TokenStatusResponse
            {
                Success = true,
                HasValidToken = hasValidToken,
                CanRefresh = canRefresh,
                TokenInfo = new TokenExpirationInfo
                {
                    AccessTokenExpiresAt = user.TokenExpiresAt,
                    AccessTokenExpired = user.TokenExpiresAt.HasValue && user.TokenExpiresAt.Value <= DateTime.UtcNow,
                    RefreshTokenExpired = false
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取Token状态失败");
            return ServerError("获取Token状态失败", ex);
        }
    }

    [HttpPost("refresh")]
    [Authorize]
    public async Task<IActionResult> RefreshToken()
    {
        try
        {
            var openId = CurrentOpenId;

            if (string.IsNullOrEmpty(openId))
            {
                return UnauthorizedResult();
            }

            var user = await _userService.GetUserByOpenIdAsync(openId);

            if (user == null)
            {
                return UnauthorizedResult("用户不存在");
            }

            var canRefresh = await _tokenManagerResolver.GetUserTokenManager().CanRefreshTokenAsync(openId);

            if (!canRefresh)
            {
                return BadRequestResult("无法刷新Token，请重新登录");
            }

            var newToken = await _tokenManagerResolver.GetUserTokenManager().RefreshUserTokenAsync(openId);

            if (newToken == null)
            {
                return BadRequestResult("刷新Token失败");
            }

            await _userService.UpdateUserTokenAsync(
                user.Id,
                newToken.AccessToken,
                newToken.RefreshToken,
                newToken.AccessTokenExpireTime > 0
                    ? DateTimeOffset.FromUnixTimeMilliseconds(newToken.AccessTokenExpireTime).DateTime
                    : null
            );

            _logger.LogInformation("Token刷新成功: {OpenId}", openId);

            return Ok(new RefreshTokenResponse
            {
                Success = true,
                Message = "Token刷新成功",
                AccessToken = newToken.AccessToken,
                RefreshToken = newToken.RefreshToken
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刷新Token失败");
            return ServerError("刷新Token失败", ex);
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        try
        {
            var openId = CurrentOpenId;

            if (!string.IsNullOrEmpty(openId))
            {
                await _userService.ClearUserTokenAsync(openId);
                _logger.LogInformation("用户登出成功: {OpenId}", openId);
            }

            return Success("登出成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "登出失败: {Message}", ex.Message);
            return ServerError("登出失败", ex);
        }
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        try
        {
            var openId = CurrentOpenId;

            if (string.IsNullOrEmpty(openId))
            {
                return UnauthorizedResult();
            }

            var user = await _userService.GetUserByOpenIdAsync(openId);

            if (user == null)
            {
                return NotFoundResult("用户不存在");
            }

            return Success(new UserInfoResponse
            {
                OpenId = user.OpenId,
                UnionId = user.UnionId,
                Name = user.Name,
                Avatar = user.Avatar,
                Email = user.Email
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户信息失败");
            return ServerError("获取用户信息失败", ex);
        }
    }

    [HttpGet("me/detail")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUserDetail()
    {
        try
        {
            var openId = CurrentOpenId;

            if (string.IsNullOrEmpty(openId))
            {
                return UnauthorizedResult();
            }

            // 从飞书获取详细用户信息
            var feishuUser = await _userService.GetDetailedUserInfoFromFeishuAsync(openId);

            if (feishuUser == null)
            {
                // 如果无法从飞书获取，返回本地数据库中的基本信息
                var localUser = await _userService.GetUserByOpenIdAsync(openId);
                if (localUser == null)
                {
                    return NotFoundResult("用户不存在");
                }

                return Success(new DetailedUserInfoResponse
                {
                    OpenId = localUser.OpenId,
                    UnionId = localUser.UnionId,
                    Name = localUser.Name,
                    Avatar = localUser.Avatar,
                    Email = localUser.Email
                });
            }

            return Success(new DetailedUserInfoResponse
            {
                OpenId = feishuUser.OpenId ?? string.Empty,
                UnionId = feishuUser.UnionId ?? string.Empty,
                UserId = feishuUser.UserId ?? string.Empty,
                Name = feishuUser.Name ?? string.Empty,
                EnName = feishuUser.EnName,
                Nickname = feishuUser.Nickname,
                Avatar = feishuUser.AvatarUrl,
                AvatarThumb = feishuUser.AvatarThumb,
                AvatarMiddle = feishuUser.AvatarMiddle,
                AvatarBig = feishuUser.AvatarBig,
                Email = feishuUser.Email,
                Mobile = feishuUser.Mobile,
                EnterpriseEmail = feishuUser.EnterpriseEmail,
                EmployeeNo = feishuUser.EmployeeNo,
                TenantKey = feishuUser.TenantKey
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取详细用户信息失败");
            return ServerError("获取详细用户信息失败", ex);
        }
    }
}
