// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
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
/// 用户管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class UsersController : BaseController
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    /// <summary>
    /// 初始化用户管理控制器
    /// </summary>
    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// 获取用户列表
    /// </summary>
    /// <param name="parameters">查询参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户列表</returns>
    [HttpGet]
    [RequirePermission("user:manage")]
    public async Task<ActionResult<ApiResponse<PagedResponse<UserDto>>>> GetUsers(
        [FromQuery] UserQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        try
        {
            var (users, total) = await _userService.GetUsersAsync(parameters, cancellationToken);
            return Paged(users, total, parameters.Page, parameters.PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户列表失败");
            return Fail<PagedResponse<UserDto>>("获取用户列表失败");
        }
    }

    /// <summary>
    /// 获取用户详情
    /// </summary>
    /// <param name="id">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户详情</returns>
    [HttpGet("{id}")]
    [RequirePermission("user:manage")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUser(int id, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userService.GetUserByIdAsync(id, cancellationToken);
            if (user == null)
            {
                return NotFoundResult<UserDto>("用户不存在");
            }

            return Success(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户详情失败: {UserId}", id);
            return Fail<UserDto>("获取用户详情失败");
        }
    }

    /// <summary>
    /// 更新用户信息
    /// </summary>
    /// <param name="id">用户ID</param>
    /// <param name="request">更新请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新后的用户信息</returns>
    [HttpPut("{id}")]
    [RequirePermission("user:manage")]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUser(
        int id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userService.UpdateUserAsync(id, request, cancellationToken);
            if (user == null)
            {
                return NotFoundResult<UserDto>("用户不存在");
            }

            return Updated(user, "用户信息更新成功");
        }
        catch (ArgumentException ex)
        {
            return BadRequestResult<UserDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新用户失败: {UserId}", id);
            return Fail<UserDto>("更新用户失败");
        }
    }

    /// <summary>
    /// 删除用户（软删除/禁用）
    /// </summary>
    /// <param name="id">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删除结果</returns>
    [HttpDelete("{id}")]
    [RequirePermission("user:manage")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteUser(int id, CancellationToken cancellationToken)
    {
        try
        {
            // 检查是否是当前用户
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == id.ToString())
            {
                return Fail<bool>("不能删除当前登录的用户");
            }

            var result = await _userService.DeleteUserAsync(id, cancellationToken);
            if (!result)
            {
                return NotFoundResult<bool>("用户不存在");
            }

            return Deleted("用户已禁用");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除用户失败: {UserId}", id);
            return Fail<bool>("删除用户失败");
        }
    }

    /// <summary>
    /// 激活用户
    /// </summary>
    /// <param name="id">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>激活结果</returns>
    [HttpPost("{id}/activate")]
    [RequirePermission("user:manage")]
    public async Task<ActionResult<ApiResponse<bool>>> ActivateUser(int id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _userService.ActivateUserAsync(id, cancellationToken);
            if (!result)
            {
                return NotFoundResult<bool>("用户不存在");
            }

            return Success(true, "用户已激活");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "激活用户失败: {UserId}", id);
            return Fail<bool>("激活用户失败");
        }
    }

    /// <summary>
    /// 禁用用户
    /// </summary>
    /// <param name="id">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>禁用结果</returns>
    [HttpPost("{id}/deactivate")]
    [RequirePermission("user:manage")]
    public async Task<ActionResult<ApiResponse<bool>>> DeactivateUser(int id, CancellationToken cancellationToken)
    {
        try
        {
            // 检查是否是当前用户
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == id.ToString())
            {
                return Fail<bool>("不能禁用当前登录的用户");
            }

            var result = await _userService.DeactivateUserAsync(id, cancellationToken);
            if (!result)
            {
                return NotFoundResult<bool>("用户不存在");
            }

            return Success(true, "用户已禁用");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "禁用用户失败: {UserId}", id);
            return Fail<bool>("禁用用户失败");
        }
    }

    /// <summary>
    /// 同步飞书用户信息
    /// </summary>
    /// <param name="id">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>同步结果</returns>
    [HttpPost("{id}/sync")]
    [RequirePermission("user:manage")]
    public async Task<ActionResult<ApiResponse<UserDto>>> SyncUser(int id, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userService.GetUserByIdAsync(id, cancellationToken);
            if (user == null)
            {
                return NotFoundResult<UserDto>("用户不存在");
            }

            var syncedUser = await _userService.SyncFeishuUserAsync(user.FeishuId, cancellationToken);
            return Success(syncedUser!, "用户同步成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "同步用户失败: {UserId}", id);
            return Fail<UserDto>("同步用户失败");
        }
    }

    /// <summary>
    /// 获取用户统计数据
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户统计数据</returns>
    [HttpGet("statistics")]
    [RequirePermission("user:manage")]
    public async Task<ActionResult<ApiResponse<UserStatisticsDto>>> GetStatistics(CancellationToken cancellationToken)
    {
        try
        {
            var statistics = await _userService.GetUserStatisticsAsync(cancellationToken);
            return Success(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户统计数据失败");
            return Fail<UserStatisticsDto>("获取用户统计数据失败");
        }
    }
}
