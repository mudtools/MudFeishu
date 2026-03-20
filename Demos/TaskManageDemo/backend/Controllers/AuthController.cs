// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;
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
    private readonly ILogger<AuthController> _logger;

    /// <summary>
    /// 初始化认证控制器
    /// </summary>
    public AuthController(
        IFeishuAuthService feishuAuthService,
        IUserService userService,
        ILogger<AuthController> logger)
    {
        _feishuAuthService = feishuAuthService;
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// 获取飞书 OAuth 授权链接
    /// </summary>
    /// <param name="request">请求参数</param>
    /// <returns>授权链接</returns>
    [HttpGet("oauth-url")]
    public ActionResult<ApiResponse<OAuthUrlResponse>> GetOAuthUrl([FromQuery] OAuthUrlRequest request)
    {
        var response = _feishuAuthService.GetOAuthUrlAsync(request.RedirectUri, request.State).Result;
        return Success(response);
    }

    /// <summary>
    /// 使用飞书授权码登录
    /// </summary>
    /// <param name="request">登录请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>登录响应</returns>
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.Code))
        {
            return BadRequestResult<LoginResponse>("授权码不能为空");
        }

        try
        {
            var response = await _feishuAuthService.LoginWithCodeAsync(request.Code, cancellationToken);

            if (response == null)
            {
                return Fail<LoginResponse>("登录失败，请检查授权码是否有效");
            }

            var message = response.IsFirstLogin ? "登录成功，欢迎首次使用！" : "登录成功";
            return Success(response, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "飞书登录失败");
            return Fail<LoginResponse>("登录失败，请稍后重试");
        }
    }

    /// <summary>
    /// 飞书 OAuth 回调处理
    /// </summary>
    /// <param name="code">授权码</param>
    /// <param name="state">状态码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>登录结果</returns>
    [HttpGet("callback")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Callback(
        [FromQuery] string code,
        [FromQuery] string? state,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(code))
        {
            return BadRequestResult<LoginResponse>("授权码不能为空");
        }

        try
        {
            var response = await _feishuAuthService.LoginWithCodeAsync(code, cancellationToken);

            if (response == null)
            {
                return Fail<LoginResponse>("登录失败，请检查授权码是否有效");
            }

            // 可以在这里重定向到前端页面，带上 token
            // 例如: return Redirect($"{frontendUrl}/auth/callback?token={response.AccessToken}");

            var message = response.IsFirstLogin ? "登录成功，欢迎首次使用！" : "登录成功";
            return Success(response, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "飞书回调处理失败");
            return Fail<LoginResponse>("登录失败，请稍后重试");
        }
    }

    /// <summary>
    /// 获取当前登录用户信息
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>当前用户信息</returns>
    [HttpGet("me")]
    [RequirePermission("user:read")]
    public async Task<ActionResult<ApiResponse<CurrentUserInfo>>> GetCurrentUser(CancellationToken cancellationToken)
    {
        try
        {
            // 从当前用户上下文获取飞书ID
            var feishuId = User.FindFirst("FeishuId")?.Value;
            if (string.IsNullOrEmpty(feishuId))
            {
                return Fail<CurrentUserInfo>("无法获取当前用户信息");
            }

            var userInfo = await _userService.GetCurrentUserAsync(feishuId, cancellationToken);
            if (userInfo == null)
            {
                return Fail<CurrentUserInfo>("用户不存在");
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
    /// 退出登录
    /// </summary>
    /// <returns>退出结果</returns>
    [HttpPost("logout")]
    public ActionResult<ApiResponse<bool>> Logout()
    {
        // 由于使用飞书的 token，服务端不需要特殊处理
        // 前端清除本地存储的 token 即可
        return Success(true, "退出登录成功");
    }

    /// <summary>
    /// 刷新访问令牌
    /// </summary>
    /// <returns>刷新结果（飞书 token 刷新需要重新登录）</returns>
    [HttpPost("refresh")]
    public ActionResult<ApiResponse<bool>> RefreshToken()
    {
        // 飞书 OAuth token 刷新逻辑
        // 注意：飞书的用户访问令牌一般有效期较短，需要引导用户重新授权
        return Success(true, "请重新登录以刷新令牌");
    }
}
