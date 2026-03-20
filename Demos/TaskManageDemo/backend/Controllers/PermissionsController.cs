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
/// 权限管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PermissionsController : BaseController
{
    private readonly IPermissionService _permissionService;
    private readonly IRoleService _roleService;
    private readonly ILogger<PermissionsController> _logger;

    /// <summary>
    /// 初始化权限管理控制器
    /// </summary>
    public PermissionsController(
        IPermissionService permissionService,
        IRoleService roleService,
        ILogger<PermissionsController> logger)
    {
        _permissionService = permissionService;
        _roleService = roleService;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有权限列表
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限列表</returns>
    [HttpGet]
    [RequirePermission("user:manage")]
    public async Task<ActionResult<ApiResponse<List<PermissionDto>>>> GetAllPermissions(CancellationToken cancellationToken)
    {
        try
        {
            var permissions = await _permissionService.GetAllPermissionsAsync(cancellationToken);
            return Success(permissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取权限列表失败");
            return Fail<List<PermissionDto>>("获取权限列表失败");
        }
    }

    /// <summary>
    /// 获取权限分组列表
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限分组列表</returns>
    [HttpGet("groups")]
    [RequirePermission("user:manage")]
    public async Task<ActionResult<ApiResponse<List<PermissionGroupDto>>>> GetPermissionGroups(CancellationToken cancellationToken)
    {
        try
        {
            var groups = await _permissionService.GetPermissionGroupsAsync(cancellationToken);
            return Success(groups);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取权限分组失败");
            return Fail<List<PermissionGroupDto>>("获取权限分组失败");
        }
    }

    /// <summary>
    /// 获取用户权限详情
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户权限详情</returns>
    [HttpGet("users/{userId}")]
    [RequirePermission("user:manage")]
    public async Task<ActionResult<ApiResponse<UserPermissionDetailDto>>> GetUserPermissions(
        int userId,
        CancellationToken cancellationToken)
    {
        try
        {
            var permissions = await _permissionService.GetUserPermissionDetailAsync(userId, cancellationToken);
            if (permissions == null)
            {
                return NotFoundResult<UserPermissionDetailDto>("用户不存在");
            }

            return Success(permissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户权限详情失败: {UserId}", userId);
            return Fail<UserPermissionDetailDto>("获取用户权限详情失败");
        }
    }

    /// <summary>
    /// 为用户分配权限
    /// </summary>
    /// <param name="request">分配请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分配结果</returns>
    [HttpPost("users/assign")]
    [RequirePermission("user:manage")]
    public async Task<ActionResult<ApiResponse<bool>>> AssignUserPermissions(
        [FromBody] AssignPermissionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            foreach (var permissionCode in request.PermissionCodes)
            {
                if (request.IsGranted)
                {
                    await _permissionService.GrantPermissionAsync(request.UserId, permissionCode, null, cancellationToken);
                }
                else
                {
                    await _permissionService.RevokePermissionAsync(request.UserId, permissionCode, cancellationToken);
                }
            }

            return Success(true, request.IsGranted ? "权限授予成功" : "权限撤销成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "分配用户权限失败: {UserId}", request.UserId);
            return Fail<bool>("分配用户权限失败");
        }
    }

    /// <summary>
    /// 获取用户的角色列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色列表</returns>
    [HttpGet("users/{userId}/roles")]
    [RequirePermission("user:manage")]
    public async Task<ActionResult<ApiResponse<List<RoleDto>>>> GetUserRoles(
        int userId,
        CancellationToken cancellationToken)
    {
        try
        {
            var roles = await _roleService.GetUserRolesAsync(userId, cancellationToken);
            return Success(roles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户角色失败: {UserId}", userId);
            return Fail<List<RoleDto>>("获取用户角色失败");
        }
    }

    /// <summary>
    /// 为用户分配角色
    /// </summary>
    /// <param name="request">分配请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分配结果</returns>
    [HttpPost("users/roles/assign")]
    [RequirePermission("user:manage")]
    public async Task<ActionResult<ApiResponse<bool>>> AssignUserRoles(
        [FromBody] AssignRoleRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _roleService.AssignRolesToUserAsync(request.UserId, request.RoleIds, null, cancellationToken);
            if (!result)
            {
                return NotFoundResult<bool>("用户不存在");
            }

            return Success(true, "角色分配成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "分配用户角色失败: {UserId}", request.UserId);
            return Fail<bool>("分配用户角色失败");
        }
    }

    /// <summary>
    /// 移除用户的角色
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="roleIds">角色ID列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>移除结果</returns>
    [HttpDelete("users/{userId}/roles")]
    [RequirePermission("user:manage")]
    public async Task<ActionResult<ApiResponse<bool>>> RemoveUserRoles(
        int userId,
        [FromBody] List<int> roleIds,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _roleService.RemoveRolesFromUserAsync(userId, roleIds, cancellationToken);
            return Success(result, "角色移除成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "移除用户角色失败: {UserId}", userId);
            return Fail<bool>("移除用户角色失败");
        }
    }

    /// <summary>
    /// 初始化权限数据
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>初始化结果</returns>
    [HttpPost("initialize")]
    [RequirePermission("user:manage")]
    public async Task<ActionResult<ApiResponse<bool>>> InitializePermissions(CancellationToken cancellationToken)
    {
        try
        {
            await _permissionService.InitializePermissionsAsync(cancellationToken);
            await _roleService.InitializeDefaultRolesAsync(cancellationToken);
            return Success(true, "权限数据初始化成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "初始化权限数据失败");
            return Fail<bool>("初始化权限数据失败");
        }
    }
}
