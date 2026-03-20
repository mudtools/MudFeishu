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
/// 角色管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class RolesController : BaseController
{
    private readonly IRoleService _roleService;
    private readonly ILogger<RolesController> _logger;

    /// <summary>
    /// 初始化角色管理控制器
    /// </summary>
    public RolesController(IRoleService roleService, ILogger<RolesController> logger)
    {
        _roleService = roleService;
        _logger = logger;
    }

    /// <summary>
    /// 获取角色列表
    /// </summary>
    /// <param name="parameters">查询参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色列表</returns>
    [HttpGet]
    [RequirePermission("user:manage")]
    public async Task<ActionResult<ApiResponse<PagedResponse<RoleDto>>>> GetRoles(
        [FromQuery] RoleQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        try
        {
            var (roles, total) = await _roleService.GetRolesAsync(parameters, cancellationToken);
            return Paged(roles, total, parameters.Page, parameters.PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取角色列表失败");
            return Fail<PagedResponse<RoleDto>>("获取角色列表失败");
        }
    }

    /// <summary>
    /// 获取所有启用的角色（用于下拉选择）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色列表</returns>
    [HttpGet("all")]
    [RequirePermission("user:manage")]
    public async Task<ActionResult<ApiResponse<List<RoleDto>>>> GetAllRoles(CancellationToken cancellationToken)
    {
        try
        {
            var roles = await _roleService.GetAllEnabledRolesAsync(cancellationToken);
            return Success(roles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取所有角色失败");
            return Fail<List<RoleDto>>("获取所有角色失败");
        }
    }

    /// <summary>
    /// 获取角色详情
    /// </summary>
    /// <param name="id">角色ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色详情</returns>
    [HttpGet("{id}")]
    [RequirePermission("user:manage")]
    public async Task<ActionResult<ApiResponse<RoleDto>>> GetRole(int id, CancellationToken cancellationToken)
    {
        try
        {
            var role = await _roleService.GetRoleByIdAsync(id, cancellationToken);
            if (role == null)
            {
                return NotFoundResult<RoleDto>("角色不存在");
            }

            return Success(role);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取角色详情失败: {RoleId}", id);
            return Fail<RoleDto>("获取角色详情失败");
        }
    }

    /// <summary>
    /// 创建角色
    /// </summary>
    /// <param name="request">创建请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>创建的角色</returns>
    [HttpPost]
    [RequirePermission("user:manage")]
    public async Task<ActionResult<ApiResponse<RoleDto>>> CreateRole(
        [FromBody] CreateRoleRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var role = await _roleService.CreateRoleAsync(request, cancellationToken);
            return Created(role, "角色创建成功");
        }
        catch (ArgumentException ex)
        {
            return BadRequestResult<RoleDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建角色失败");
            return Fail<RoleDto>("创建角色失败");
        }
    }

    /// <summary>
    /// 更新角色
    /// </summary>
    /// <param name="id">角色ID</param>
    /// <param name="request">更新请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新后的角色</returns>
    [HttpPut("{id}")]
    [RequirePermission("user:manage")]
    public async Task<ActionResult<ApiResponse<RoleDto>>> UpdateRole(
        int id,
        [FromBody] UpdateRoleRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var role = await _roleService.UpdateRoleAsync(id, request, cancellationToken);
            if (role == null)
            {
                return NotFoundResult<RoleDto>("角色不存在");
            }

            return Updated(role, "角色更新成功");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestResult<RoleDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新角色失败: {RoleId}", id);
            return Fail<RoleDto>("更新角色失败");
        }
    }

    /// <summary>
    /// 删除角色
    /// </summary>
    /// <param name="id">角色ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删除结果</returns>
    [HttpDelete("{id}")]
    [RequirePermission("user:manage")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteRole(int id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _roleService.DeleteRoleAsync(id, cancellationToken);
            if (!result)
            {
                return NotFoundResult<bool>("角色不存在");
            }

            return Deleted("角色删除成功");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestResult<bool>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除角色失败: {RoleId}", id);
            return Fail<bool>("删除角色失败");
        }
    }

    /// <summary>
    /// 获取角色的权限列表
    /// </summary>
    /// <param name="id">角色ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限列表</returns>
    [HttpGet("{id}/permissions")]
    [RequirePermission("user:manage")]
    public async Task<ActionResult<ApiResponse<List<PermissionDto>>>> GetRolePermissions(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            var permissions = await _roleService.GetRolePermissionsAsync(id, cancellationToken);
            return Success(permissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取角色权限失败: {RoleId}", id);
            return Fail<List<PermissionDto>>("获取角色权限失败");
        }
    }

    /// <summary>
    /// 为角色分配权限
    /// </summary>
    /// <param name="id">角色ID</param>
    /// <param name="permissionIds">权限ID列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分配结果</returns>
    [HttpPost("{id}/permissions")]
    [RequirePermission("user:manage")]
    public async Task<ActionResult<ApiResponse<bool>>> AssignPermissions(
        int id,
        [FromBody] List<int> permissionIds,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _roleService.AssignPermissionsToRoleAsync(id, permissionIds, cancellationToken);
            if (!result)
            {
                return NotFoundResult<bool>("角色不存在");
            }

            return Success(true, "权限分配成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "分配权限失败: {RoleId}", id);
            return Fail<bool>("分配权限失败");
        }
    }

    /// <summary>
    /// 获取角色的用户列表
    /// </summary>
    /// <param name="id">角色ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户列表</returns>
    [HttpGet("{id}/users")]
    [RequirePermission("user:manage")]
    public async Task<ActionResult<ApiResponse<List<UserDto>>>> GetRoleUsers(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            var users = await _roleService.GetRoleUsersAsync(id, cancellationToken);
            return Success(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取角色用户失败: {RoleId}", id);
            return Fail<List<UserDto>>("获取角色用户失败");
        }
    }
}
