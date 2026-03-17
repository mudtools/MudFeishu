using FeishuWikiManager.Models;
using FeishuWikiManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mud.Feishu;
using Mud.HttpUtils;
using System.Security.Claims;

namespace FeishuWikiManager.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OAuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IUserTokenManager _userTokenManager;
    private readonly IStateStorageService _stateStorageService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUserService _userService;
    private readonly IFeishuUserV3User _feishuUserApi;
    private readonly ILogger<OAuthController> _logger;

    public OAuthController(
        IConfiguration configuration,
        IUserTokenManager userTokenManager,
        IStateStorageService stateStorageService,
        IJwtTokenService jwtTokenService,
        IUserService userService,
        IFeishuUserV3User feishuUserApi,
        ILogger<OAuthController> logger)
    {
        _configuration = configuration;
        _userTokenManager = userTokenManager;
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
            var appId = _configuration["Feishu:AppId"];
            var redirectUri = _configuration["OAuth:RedirectUri"];

            if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(redirectUri))
            {
                return BadRequest(new AuthUrlResponse
                {
                    Success = false,
                    Message = "飞书应用配置不完整"
                });
            }

            var state = _stateStorageService.GenerateState();

            var authUrl = $"https://accounts.feishu.cn/open-apis/authen/v1/authorize?" +
                          $"client_id={appId}&" +
                          $"redirect_uri={Uri.EscapeDataString(redirectUri)}&" +
                          $"response_type=code&" +
                          $"scope=contact:user.base:readonly,wiki:wiki:readonly,wiki:wiki,docs:doc:readonly,docs:doc,drive:drive:readonly,drive:drive&" +
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
            return StatusCode(500, new AuthUrlResponse
            {
                Success = false,
                Message = $"生成授权URL失败: {ex.Message}"
            });
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
                return BadRequest(new LoginResponse
                {
                    Success = false,
                    Message = "缺少必要参数"
                });
            }

            if (!_stateStorageService.ValidateState(request.State))
            {
                _logger.LogWarning("State验证失败: {State}", request.State);
                return BadRequest(new LoginResponse
                {
                    Success = false,
                    Message = "State验证失败，可能存在CSRF攻击"
                });
            }

            _stateStorageService.RemoveState(request.State);

            var redirectUri = _configuration["OAuth:RedirectUri"];

            var tokenResult = await _userTokenManager.GetUserTokenWithCodeAsync(request.Code, redirectUri ?? string.Empty);

            if (tokenResult == null || tokenResult.Code != 0)
            {
                _logger.LogError("获取用户访问令牌失败: {Message}", tokenResult?.Msg ?? "未知错误");
                return BadRequest(new LoginResponse
                {
                    Success = false,
                    Message = $"获取用户访问令牌失败: {tokenResult?.Msg ?? "未知错误"}"
                });
            }

            var userInfoResult = await _feishuUserApi.GetUserInfoAsync();

            if (userInfoResult?.Data == null)
            {
                _logger.LogError("获取用户信息失败: {Message}", userInfoResult?.Msg ?? "未知错误");
                return BadRequest(new LoginResponse
                {
                    Success = false,
                    Message = $"获取用户信息失败: {userInfoResult?.Msg ?? "未知错误"}"
                });
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
                tokenResult.Expire > 0
                    ? DateTimeOffset.FromUnixTimeMilliseconds(tokenResult.Expire).DateTime
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
            return StatusCode(500, new LoginResponse
            {
                Success = false,
                Message = $"登录失败: {ex.Message}"
            });
        }
    }

    [HttpGet("status")]
    [Authorize]
    public async Task<IActionResult> GetTokenStatus()
    {
        var openId = User.FindFirst("open_id")?.Value;

        if (string.IsNullOrEmpty(openId))
        {
            return Unauthorized(new TokenStatusResponse
            {
                Success = false,
                Message = "未授权"
            });
        }

        var user = await _userService.GetUserByOpenIdAsync(openId);

        if (user == null)
        {
            return NotFound(new TokenStatusResponse
            {
                Success = false,
                Message = "用户不存在"
            });
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

    [HttpPost("refresh")]
    [Authorize]
    public async Task<IActionResult> RefreshToken()
    {
        var openId = User.FindFirst("open_id")?.Value;

        if (string.IsNullOrEmpty(openId))
        {
            return Unauthorized(new RefreshTokenResponse
            {
                Success = false,
                Message = "未授权"
            });
        }

        var user = await _userService.GetUserByOpenIdAsync(openId);

        if (user == null)
        {
            return Unauthorized(new RefreshTokenResponse
            {
                Success = false,
                Message = "用户不存在"
            });
        }

        var canRefresh = await _userTokenManager.CanRefreshTokenAsync(openId);

        if (!canRefresh)
        {
            return BadRequest(new RefreshTokenResponse
            {
                Success = false,
                Message = "无法刷新Token，请重新登录"
            });
        }

        var newToken = await _userTokenManager.RefreshUserTokenAsync(openId);

        if (newToken == null)
        {
            return BadRequest(new RefreshTokenResponse
            {
                Success = false,
                Message = "刷新Token失败"
            });
        }

        await _userService.UpdateUserTokenAsync(
            user.Id,
            newToken.AccessToken,
            newToken.RefreshToken,
            newToken.Expire > 0
                ? DateTimeOffset.FromUnixTimeMilliseconds(newToken.Expire).DateTime
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

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var openId = User.FindFirst("open_id")?.Value;

        if (!string.IsNullOrEmpty(openId))
        {
            var user = await _userService.GetUserByOpenIdAsync(openId);
            if (user != null)
            {
                await _userService.UpdateUserTokenAsync(user.Id, null, null, null);
            }
        }

        return Ok(new LogoutResponse
        {
            Success = true,
            Message = "登出成功"
        });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var openId = User.FindFirst("open_id")?.Value;

        if (string.IsNullOrEmpty(openId))
        {
            return Unauthorized();
        }

        var user = await _userService.GetUserByOpenIdAsync(openId);

        if (user == null)
        {
            return NotFound();
        }

        return Ok(new UserInfoResponse
        {
            OpenId = user.OpenId,
            UnionId = user.UnionId ?? string.Empty,
            Name = user.Name,
            Avatar = user.Avatar,
            Email = user.Email ?? string.Empty
        });
    }
}
