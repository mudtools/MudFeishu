// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using TaskManageDemo.Backend.Models.DTOs;

namespace TaskManageDemo.Backend.Services.Auth;

/// <summary>
/// 角色服务接口
/// </summary>
public interface IRoleService
{
    /// <summary>
    /// 获取角色列表
    /// </summary>
    Task<(List<RoleDto> roles, int total)> GetRolesAsync(RoleQueryParameters parameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取所有启用的角色
    /// </summary>
    Task<List<RoleDto>> GetAllEnabledRolesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据ID获取角色
    /// </summary>
    Task<RoleDto?> GetRoleByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据代码获取角色
    /// </summary>
    Task<RoleDto?> GetRoleByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// 创建角色
    /// </summary>
    Task<RoleDto> CreateRoleAsync(CreateRoleRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新角色
    /// </summary>
    Task<RoleDto?> UpdateRoleAsync(int id, UpdateRoleRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除角色
    /// </summary>
    Task<bool> DeleteRoleAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 为角色分配权限
    /// </summary>
    Task<bool> AssignPermissionsToRoleAsync(int roleId, List<int> permissionIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取角色的权限列表
    /// </summary>
    Task<List<PermissionDto>> GetRolePermissionsAsync(int roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 为用户分配角色
    /// </summary>
    Task<bool> AssignRolesToUserAsync(int userId, List<int> roleIds, int? assignedBy = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 移除用户的角色
    /// </summary>
    Task<bool> RemoveRolesFromUserAsync(int userId, List<int> roleIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户的角色列表
    /// </summary>
    Task<List<RoleDto>> GetUserRolesAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取角色的用户列表
    /// </summary>
    Task<List<UserDto>> GetRoleUsersAsync(int roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 初始化默认角色
    /// </summary>
    Task InitializeDefaultRolesAsync(CancellationToken cancellationToken = default);
}
