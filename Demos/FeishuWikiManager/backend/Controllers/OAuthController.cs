// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
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
    private readonly IFeishuUserTokenManager _userTokenManager;
    private readonly IStateStorageService _stateStorageService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUserService _userService;
    private readonly IFeishuUserV3User _feishuUserApi;
    private readonly ILogger<OAuthController> _logger;

    public OAuthController(
        IConfiguration configuration,
        IFeishuAppManager feishuAppManager,
        IStateStorageService stateStorageService,
        IJwtTokenService jwtTokenService,
        IUserService userService,
        IFeishuUserV3User feishuUserApi,
        ILogger<OAuthController> logger)
    {
        _configuration = configuration;
        _feishuAppManager = feishuAppManager;
        _userTokenManager = _feishuAppManager.DefaultUserTokenManager;
        _stateStorageService = stateStorageService;
        _jwtTokenService = jwtTokenService;
        _userService = userService;
        _feishuUserApi = feishuUserApi;
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
            _logger.LogInformation("收到飞书OAuth回调，State: {State}", request.State);

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

            var tokenResult = await _userTokenManager.GetUserTokenWithCodeAsync(request.Code, redirectUri ?? string.Empty);

            if (tokenResult == null || tokenResult.Code != 0)
            {
                _logger.LogError("获取用户访问令牌失败: {Message}", tokenResult?.Msg ?? "未知错误");
                return BadRequestResult($"获取用户访问令牌失败: {tokenResult?.Msg ?? "未知错误"}");
            }

            _feishuUserApi.CurrentUserId = tokenResult.UserId;
            var userInfoResult = await _feishuUserApi.GetUserInfoAsync();

            if (userInfoResult?.Data == null)
            {
                _logger.LogError("获取用户信息失败: {Message}", userInfoResult?.Msg ?? "未知错误");
                return BadRequestResult($"获取用户信息失败: {userInfoResult?.Msg ?? "未知错误"}");
            }

            var feishuUser = userInfoResult.Data;

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

            var jwtToken = _jwtTokenService.GenerateToken(
                feishuUser.OpenId ?? string.Empty,
                feishuUser.UnionId ?? string.Empty,
                feishuUser.Name ?? "未知用户"
            );

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

            var canRefresh = await _userTokenManager.CanRefreshTokenAsync(openId);

            if (!canRefresh)
            {
                return BadRequestResult("无法刷新Token，请重新登录");
            }

            var newToken = await _userTokenManager.RefreshUserTokenAsync(openId);

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
            _logger.LogError(ex, "登出失败");
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
}
