// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FeishuOAuthDemo.Models;
using FeishuOAuthDemo.Services;
using Microsoft.AspNetCore.Mvc;
using Mud.Feishu;
using Mud.HttpUtils;

namespace FeishuOAuthDemo.Controllers;

/// <summary>
/// 飞书OAuth认证控制器
/// </summary>
/// <remarks>
/// 演示完整的飞书OAuth认证流程，包括：
/// <list type="bullet">
///   <item><description>获取授权URL</description></item>
///   <item><description>处理OAuth回调</description></item>
///   <item><description>令牌刷新</description></item>
///   <item><description>令牌状态检查</description></item>
///   <item><description>登出（清除令牌缓存）</description></item>
/// </list>
/// </remarks>
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

    /// <summary>
    /// 获取飞书授权URL
    /// </summary>
    /// <returns>授权URL和state参数</returns>
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
                          $"scope=contact:user.base:readonly&" +
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

    /// <summary>
    /// 处理飞书OAuth回调
    /// </summary>
    /// <param name="request">回调请求（包含code和state）</param>
    /// <returns>登录结果和JWT令牌</returns>
    [HttpPost("feishu/callback")]
    public async Task<IActionResult> HandleFeishuCallback([FromBody] AuthCallbackRequest request)
    {
        try
        {
            _logger.LogInformation("收到飞书OAuth回调，Code: {Code}, State: {State}", request.Code[..Math.Min(8, request.Code.Length)] + "...", request.State);

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

            _logger.LogInformation("开始使用授权码获取用户访问令牌");
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

            _logger.LogInformation("成功获取用户访问令牌");

            _logger.LogInformation("开始获取用户信息");
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
            _logger.LogInformation("成功获取用户信息: {Name} ({OpenId})", feishuUser.Name ?? "未知", feishuUser.OpenId ?? "未知");

            var userId = await _userService.GetOrCreateUserAsync(
                feishuUser.OpenId ?? string.Empty,
                feishuUser.UnionId ?? string.Empty,
                feishuUser.Name ?? "未知用户",
                feishuUser.AvatarUrl ?? string.Empty,
                feishuUser.Email
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
            return StatusCode(500, new LoginResponse
            {
                Success = false,
                Message = $"登录失败: {ex.Message}"
            });
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
                return BadRequest(new { success = false, message = "令牌不能为空" });
            }

            var isValid = _jwtTokenService.ValidateToken(token, out var principal);

            if (!isValid || principal == null)
            {
                return Unauthorized(new { success = false, message = "令牌无效或已过期" });
            }

            var userInfo = _jwtTokenService.GetUserFromToken(token);
            return Ok(new
            {
                success = true,
                message = "令牌有效",
                user = new
                {
                    openId = userInfo?.openId,
                    unionId = userInfo?.unionId,
                    name = userInfo?.name
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证令牌失败");
            return StatusCode(500, new { success = false, message = "验证令牌失败" });
        }
    }

    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    /// <returns>用户信息</returns>
    [HttpGet("user/me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        try
        {
            var authHeader = Request.Headers.Authorization.FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return Unauthorized(new { success = false, message = "缺少授权令牌" });
            }

            var token = authHeader["Bearer ".Length..].Trim();
            var userInfo = _jwtTokenService.GetUserFromToken(token);

            if (userInfo == null)
            {
                return Unauthorized(new { success = false, message = "令牌无效" });
            }

            var user = await _userService.GetUserByIdAsync(userInfo.Value.openId);
            if (user == null)
            {
                return NotFound(new { success = false, message = "用户不存在" });
            }

            return Ok(new
            {
                success = true,
                user = new
                {
                    userId = user.UserId,
                    openId = user.OpenId,
                    unionId = user.UnionId,
                    name = user.Name,
                    avatar = user.Avatar,
                    email = user.Email,
                    createdAt = user.CreatedAt,
                    lastLoginAt = user.LastLoginAt
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取当前用户信息失败");
            return StatusCode(500, new { success = false, message = "获取用户信息失败" });
        }
    }

    /// <summary>
    /// 刷新用户令牌
    /// </summary>
    /// <remarks>
    /// 使用缓存的刷新令牌自动刷新用户访问令牌。
    /// 如果刷新令牌也过期，则需要重新授权。
    /// 
    /// 使用方式：
    /// 1. 从JWT令牌中获取用户的openId
    /// 2. 调用此接口刷新飞书用户访问令牌
    /// </remarks>
    /// <returns>刷新结果</returns>
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken()
    {
        try
        {
            var authHeader = Request.Headers.Authorization.FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return Unauthorized(new RefreshTokenResponse
                {
                    Success = false,
                    Message = "缺少授权令牌"
                });
            }

            var token = authHeader["Bearer ".Length..].Trim();
            var userInfo = _jwtTokenService.GetUserFromToken(token);

            if (userInfo == null)
            {
                return Unauthorized(new RefreshTokenResponse
                {
                    Success = false,
                    Message = "令牌无效"
                });
            }

            var openId = userInfo.Value.openId;

            _logger.LogInformation("开始刷新用户令牌，OpenId: {OpenId}", openId);

            var canRefresh = await _userTokenManager.CanRefreshTokenAsync(openId);
            if (!canRefresh)
            {
                _logger.LogWarning("用户令牌无法刷新，需要重新授权，OpenId: {OpenId}", openId);
                return BadRequest(new RefreshTokenResponse
                {
                    Success = false,
                    Message = "刷新令牌已过期，请重新授权"
                });
            }

            var newToken = await _userTokenManager.RefreshUserTokenAsync(openId);

            if (newToken == null || newToken.Code != 0)
            {
                _logger.LogError("刷新用户令牌失败: {Message}", newToken?.Msg ?? "未知错误");
                return BadRequest(new RefreshTokenResponse
                {
                    Success = false,
                    Message = $"刷新令牌失败: {newToken?.Msg ?? "未知错误"}"
                });
            }

            _logger.LogInformation("用户令牌刷新成功，OpenId: {OpenId}", openId);

            return Ok(new RefreshTokenResponse
            {
                Success = true,
                Message = "令牌刷新成功",
                AccessToken = newToken.AccessToken
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刷新用户令牌失败");
            return StatusCode(500, new RefreshTokenResponse
            {
                Success = false,
                Message = $"刷新令牌失败: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// 检查用户令牌状态
    /// </summary>
    /// <remarks>
    /// 返回用户飞书访问令牌的当前状态，包括：
    /// - 是否有有效的访问令牌
    /// - 是否可以刷新令牌
    /// - 令牌过期时间信息
    /// </remarks>
    /// <returns>令牌状态信息</returns>
    [HttpGet("token-status")]
    public async Task<IActionResult> GetTokenStatus()
    {
        try
        {
            var authHeader = Request.Headers.Authorization.FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return Unauthorized(new TokenStatusResponse
                {
                    Success = false,
                    Message = "缺少授权令牌"
                });
            }

            var token = authHeader["Bearer ".Length..].Trim();
            var userInfo = _jwtTokenService.GetUserFromToken(token);

            if (userInfo == null)
            {
                return Unauthorized(new TokenStatusResponse
                {
                    Success = false,
                    Message = "令牌无效"
                });
            }

            var openId = userInfo.Value.openId;

            var hasValidToken = await _userTokenManager.HasValidTokenAsync(openId);
            var canRefresh = await _userTokenManager.CanRefreshTokenAsync(openId);

            var tokenInfo = await _userTokenManager.GetTokenInfoAsync(openId);

            TokenExpirationInfo? expirationInfo = null;
            if (tokenInfo != null)
            {
                var now = DateTime.UtcNow;
                expirationInfo = new TokenExpirationInfo
                {
                    AccessTokenExpiresAt = tokenInfo.AccessTokenExpireTime > 0
                        ? DateTimeOffset.FromUnixTimeMilliseconds(tokenInfo.AccessTokenExpireTime).UtcDateTime
                        : null,
                    RefreshTokenExpiresAt = tokenInfo.RefreshTokenExpireTime > 0
                        ? DateTimeOffset.FromUnixTimeMilliseconds(tokenInfo.RefreshTokenExpireTime).UtcDateTime
                        : null,
                    AccessTokenExpired = !tokenInfo.IsAccessTokenValid(0),
                    RefreshTokenExpired = !tokenInfo.IsRefreshTokenValid()
                };
            }

            return Ok(new TokenStatusResponse
            {
                Success = true,
                Message = hasValidToken ? "令牌有效" : (canRefresh ? "令牌已过期，可刷新" : "令牌已失效，需重新授权"),
                HasValidToken = hasValidToken,
                CanRefresh = canRefresh,
                TokenInfo = expirationInfo
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查令牌状态失败");
            return StatusCode(500, new TokenStatusResponse
            {
                Success = false,
                Message = $"检查令牌状态失败: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// 登出
    /// </summary>
    /// <remarks>
    /// 清除用户令牌缓存。
    /// 注意：JWT令牌是无状态的，此接口主要清除飞书用户令牌的缓存。
    /// 前端应同时删除本地存储的JWT令牌。
    /// </remarks>
    /// <returns>登出结果</returns>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        try
        {
            var authHeader = Request.Headers.Authorization.FirstOrDefault();
            string? openId = null;

            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
            {
                var token = authHeader["Bearer ".Length..].Trim();
                var userInfo = _jwtTokenService.GetUserFromToken(token);
                openId = userInfo?.openId;
            }

            if (!string.IsNullOrEmpty(openId))
            {
                var removed = await _userTokenManager.RemoveTokenAsync(openId);
                _logger.LogInformation("用户登出，OpenId: {OpenId}, 令牌缓存清除: {Removed}", openId, removed);
            }

            return Ok(new LogoutResponse
            {
                Success = true,
                Message = "登出成功"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "登出失败");
            return StatusCode(500, new LogoutResponse
            {
                Success = false,
                Message = $"登出失败: {ex.Message}"
            });
        }
    }
}
