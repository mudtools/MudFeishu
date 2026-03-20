// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskManageDemo.Backend.Middleware;
using TaskManageDemo.Backend.Models.DTOs;
using TaskManageDemo.Backend.Services.Auth;

namespace TaskManageDemo.Backend.Controllers;

/// <summary>
/// 认证控制器
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : BaseController
{
    private readonly IFeishuAuthService _feishuAuthService;
    private readonly IUserService _userService;
    private readonly IStateStorageService _stateStorageService;
    private readonly ILocalAuthService _localAuthService;
    private readonly ILogger<AuthController> _logger;

    /// <summary>
    /// 初始化认证控制器
    /// </summary>
    public AuthController(
        IFeishuAuthService feishuAuthService,
        IUserService userService,
        IStateStorageService stateStorageService,
        ILocalAuthService localAuthService,
        ILogger<AuthController> logger)
    {
        _feishuAuthService = feishuAuthService;
        _userService = userService;
        _stateStorageService = stateStorageService;
        _localAuthService = localAuthService;
        _logger = logger;
    }

    /// <summary>
    /// 获取飞书 OAuth 授权链接
    /// </summary>
    [HttpGet("feishu/url")]
    public ActionResult<ApiResponse<OAuthUrlResponse>> GetFeishuAuthUrl()
    {
        try
        {
            var state = _stateStorageService.GenerateState();
            var response = _feishuAuthService.GetOAuthUrl(state);
            return Success(response, "生成授权URL成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成飞书授权URL失败");
            return Fail<OAuthUrlResponse>("生成授权URL失败");
        }
    }

    /// <summary>
    /// 处理飞书 OAuth 回调
    /// </summary>
    [HttpPost("feishu/callback")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> HandleFeishuCallback(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Code) || string.IsNullOrEmpty(request.State))
            {
                return BadRequestResult<LoginResponse>("缺少必要参数");
            }

            // 验证 State
            if (!_stateStorageService.ValidateState(request.State))
            {
                _logger.LogWarning("State验证失败: {State}", request.State);
                return BadRequestResult<LoginResponse>("State验证失败，可能存在CSRF攻击");
            }

            _stateStorageService.RemoveState(request.State);

            // 使用授权码登录
            var response = await _feishuAuthService.LoginWithCodeAsync(request.Code, request.State, cancellationToken);

            if (response == null)
            {
                return Fail<LoginResponse>("登录失败，请检查授权码是否有效");
            }

            var message = response.IsFirstLogin ? "登录成功，欢迎首次使用！" : "登录成功";
            return Success(response, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理飞书OAuth回调失败");
            return Fail<LoginResponse>("登录失败");
        }
    }

    /// <summary>
    /// 获取当前登录用户信息
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [RequirePermission("user:read")]
    public async Task<ActionResult<ApiResponse<CurrentUserInfo>>> GetCurrentUser(CancellationToken cancellationToken)
    {
        try
        {
            var openId = User.FindFirst("open_id")?.Value;
            if (string.IsNullOrEmpty(openId))
            {
                return Fail<CurrentUserInfo>("无法获取当前用户信息", 401);
            }

            var userInfo = await _userService.GetCurrentUserAsync(openId, cancellationToken);
            if (userInfo == null)
            {
                return NotFoundResult<CurrentUserInfo>("用户不存在");
            }

            return Success(userInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取当前用户信息失败");
            return Fail<CurrentUserInfo>("获取用户信息失败");
        }
    }

    /// <summary>
    /// 获取当前用户详细信息（从飞书获取）
    /// </summary>
    [HttpGet("me/detail")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<FeishuUserDetailResponse>>> GetCurrentUserDetail(CancellationToken cancellationToken)
    {
        try
        {
            var openId = User.FindFirst("open_id")?.Value;
            if (string.IsNullOrEmpty(openId))
            {
                return Fail<FeishuUserDetailResponse>("无法获取当前用户信息", 401);
            }

            // 从飞书获取详细用户信息
            var feishuUser = await _feishuAuthService.GetUserDetailAsync(openId, cancellationToken);

            if (feishuUser == null)
            {
                // 如果无法从飞书获取，返回本地数据库中的基本信息
                var localUser = await _userService.GetUserByOpenIdAsync(openId, cancellationToken);
                if (localUser == null)
                {
                    return NotFoundResult<FeishuUserDetailResponse>("用户不存在");
                }

                return Success(new FeishuUserDetailResponse
                {
                    OpenId = localUser.OpenId ?? string.Empty,
                    UnionId = localUser.UnionId ?? string.Empty,
                    UserId = localUser.FeishuId,
                    Name = localUser.Name,
                    Avatar = localUser.AvatarUrl,
                    Email = localUser.Email
                });
            }

            return Success(new FeishuUserDetailResponse
            {
                OpenId = feishuUser.OpenId,
                UnionId = feishuUser.UnionId,
                UserId = feishuUser.UserId,
                Name = feishuUser.Name,
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
            return Fail<FeishuUserDetailResponse>("获取详细用户信息失败");
        }
    }

    /// <summary>
    /// 获取 Token 状态
    /// </summary>
    [HttpGet("status")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<TokenStatusResponse>>> GetTokenStatus(CancellationToken cancellationToken)
    {
        try
        {
            var openId = User.FindFirst("open_id")?.Value;
            if (string.IsNullOrEmpty(openId))
            {
                return Fail<TokenStatusResponse>("无法获取当前用户信息", 401);
            }

            var user = await _userService.GetUserByOpenIdAsync(openId, cancellationToken);
            if (user == null)
            {
                return NotFoundResult<TokenStatusResponse>("用户不存在");
            }

            var hasValidToken = !string.IsNullOrEmpty(user.FeishuAccessToken) &&
                               user.TokenExpiresAt.HasValue &&
                               user.TokenExpiresAt.Value > DateTime.UtcNow;

            var canRefresh = !string.IsNullOrEmpty(user.FeishuRefreshToken);

            return Success(new TokenStatusResponse
            {
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
            return Fail<TokenStatusResponse>("获取Token状态失败");
        }
    }

    /// <summary>
    /// 刷新 Token
    /// </summary>
    [HttpPost("refresh")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<TokenRefreshResponse>>> RefreshToken(CancellationToken cancellationToken)
    {
        try
        {
            var openId = User.FindFirst("open_id")?.Value;
            if (string.IsNullOrEmpty(openId))
            {
                return Fail<TokenRefreshResponse>("无法获取当前用户信息", 401);
            }

            var result = await _feishuAuthService.RefreshTokenAsync(openId, cancellationToken);
            if (result == null)
            {
                return Fail<TokenRefreshResponse>("刷新Token失败，请重新登录");
            }

            return Success(result, "Token刷新成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刷新Token失败");
            return Fail<TokenRefreshResponse>("刷新Token失败");
        }
    }

    /// <summary>
    /// 退出登录
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<bool>>> Logout(CancellationToken cancellationToken)
    {
        try
        {
            var openId = User.FindFirst("open_id")?.Value;
            if (!string.IsNullOrEmpty(openId))
            {
                await _userService.ClearUserTokenAsync(openId, cancellationToken);
                _logger.LogInformation("用户登出成功: {OpenId}", openId);
            }

            return Success(true, "登出成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "登出失败");
            return Fail<bool>("登出失败");
        }
    }

    /// <summary>
    /// 用户名密码登录
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> PasswordLogin(
        [FromBody] PasswordLoginRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequestResult<LoginResponse>("用户名和密码不能为空");
            }

            var response = await _localAuthService.PasswordLoginAsync(request.Username, request.Password, cancellationToken);

            if (response == null)
            {
                return Fail<LoginResponse>("用户名或密码错误", 401);
            }

            var message = response.IsFirstLogin ? "登录成功，请绑定飞书账号" : "登录成功";
            return Success(response, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "用户名密码登录失败");
            return Fail<LoginResponse>("登录失败");
        }
    }

    /// <summary>
    /// 用户注册
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequestResult<LoginResponse>("用户名和密码不能为空");
            }

            if (request.Password != request.ConfirmPassword)
            {
                return BadRequestResult<LoginResponse>("两次输入的密码不一致");
            }

            if (request.Password.Length < 6)
            {
                return BadRequestResult<LoginResponse>("密码长度至少6位");
            }

            var response = await _localAuthService.RegisterAsync(request, cancellationToken);

            if (response == null)
            {
                return Fail<LoginResponse>("注册失败，用户名可能已存在");
            }

            return Success(response, "注册成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "用户注册失败");
            return Fail<LoginResponse>("注册失败");
        }
    }

    /// <summary>
    /// 检查飞书授权状态
    /// </summary>
    [HttpPost("feishu/check")]
    public async Task<ActionResult<ApiResponse<FeishuAuthCheckResponse>>> CheckFeishuAuth(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Code) || string.IsNullOrEmpty(request.State))
            {
                return BadRequestResult<FeishuAuthCheckResponse>("缺少必要参数");
            }

            if (!_stateStorageService.ValidateState(request.State))
            {
                _logger.LogWarning("State验证失败: {State}", request.State);
                return BadRequestResult<FeishuAuthCheckResponse>("State验证失败");
            }

            var response = await _localAuthService.CheckFeishuAuthAsync(request.Code, request.State, cancellationToken);
            return Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查飞书授权状态失败");
            return Fail<FeishuAuthCheckResponse>("检查飞书授权状态失败");
        }
    }

    /// <summary>
    /// 绑定飞书账号
    /// </summary>
    [HttpPost("feishu/bind")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<BindFeishuResponse>>> BindFeishu(
        [FromBody] BindFeishuRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userIdClaim = User.FindFirst("user_id")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Fail<BindFeishuResponse>("无法获取用户信息", 401);
            }

            if (string.IsNullOrEmpty(request.Code) || string.IsNullOrEmpty(request.State))
            {
                return BadRequestResult<BindFeishuResponse>("缺少必要参数");
            }

            var response = await _localAuthService.BindFeishuAsync(userId, request.Code, request.State, cancellationToken);
            return Success(response, response.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "绑定飞书账号失败");
            return Fail<BindFeishuResponse>("绑定飞书账号失败");
        }
    }

    /// <summary>
    /// 修改密码
    /// </summary>
    [HttpPost("password/change")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<bool>>> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userIdClaim = User.FindFirst("user_id")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Fail<bool>("无法获取用户信息", 401);
            }

            if (string.IsNullOrEmpty(request.OldPassword) || string.IsNullOrEmpty(request.NewPassword))
            {
                return BadRequestResult<bool>("密码不能为空");
            }

            if (request.NewPassword != request.ConfirmPassword)
            {
                return BadRequestResult<bool>("两次输入的新密码不一致");
            }

            if (request.NewPassword.Length < 6)
            {
                return BadRequestResult<bool>("密码长度至少6位");
            }

            var success = await _localAuthService.ChangePasswordAsync(userId, request.OldPassword, request.NewPassword, cancellationToken);

            if (!success)
            {
                return Fail<bool>("原密码错误");
            }

            return Success(true, "密码修改成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "修改密码失败");
            return Fail<bool>("修改密码失败");
        }
    }
}
