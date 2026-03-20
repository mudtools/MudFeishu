// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace TaskManageDemo.Backend.Models.DTOs;

/// <summary>
/// 角色DTO
/// </summary>
public class RoleDto
{
    /// <summary>
    /// 角色ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 角色代码
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 角色名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 角色描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 是否为系统内置角色
    /// </summary>
    public bool IsSystem { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// 排序序号
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// 角色权限列表
    /// </summary>
    public List<PermissionDto> Permissions { get; set; } = new();

    /// <summary>
    /// 用户数量
    /// </summary>
    public int UserCount { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 权限DTO
/// </summary>
public class PermissionDto
{
    /// <summary>
    /// 权限ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 权限代码
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 权限名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 权限描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 权限分组
    /// </summary>
    public string Group { get; set; } = string.Empty;

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; }
}

/// <summary>
/// 创建角色请求
/// </summary>
public class CreateRoleRequest
{
    /// <summary>
    /// 角色代码
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 角色名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 角色描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 排序序号
    /// </summary>
    public int SortOrder { get; set; } = 0;

    /// <summary>
    /// 权限ID列表
    /// </summary>
    public List<int> PermissionIds { get; set; } = new();
}

/// <summary>
/// 更新角色请求
/// </summary>
public class UpdateRoleRequest
{
    /// <summary>
    /// 角色名称
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 角色描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool? IsEnabled { get; set; }

    /// <summary>
    /// 排序序号
    /// </summary>
    public int? SortOrder { get; set; }

    /// <summary>
    /// 权限ID列表
    /// </summary>
    public List<int>? PermissionIds { get; set; }
}

/// <summary>
/// 角色查询参数
/// </summary>
public class RoleQueryParameters
{
    /// <summary>
    /// 页码
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// 每页数量
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// 搜索关键词
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool? IsEnabled { get; set; }
}

/// <summary>
/// 分配角色请求
/// </summary>
public class AssignRoleRequest
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// 角色ID列表
    /// </summary>
    public List<int> RoleIds { get; set; } = new();
}

/// <summary>
/// 分配权限请求
/// </summary>
public class AssignPermissionRequest
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// 权限代码列表
    /// </summary>
    public List<string> PermissionCodes { get; set; } = new();

    /// <summary>
    /// 是否授予
    /// </summary>
    public bool IsGranted { get; set; } = true;
}

/// <summary>
/// 用户权限详情DTO
/// </summary>
public class UserPermissionDetailDto
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 用户角色列表
    /// </summary>
    public List<RoleDto> Roles { get; set; } = new();

    /// <summary>
    /// 直接授予的权限
    /// </summary>
    public List<PermissionDto> GrantedPermissions { get; set; } = new();

    /// <summary>
    /// 被撤销的权限
    /// </summary>
    public List<PermissionDto> RevokedPermissions { get; set; } = new();

    /// <summary>
    /// 最终权限列表（合并计算后）
    /// </summary>
    public List<string> EffectivePermissions { get; set; } = new();
}

/// <summary>
/// 权限分组DTO
/// </summary>
public class PermissionGroupDto
{
    /// <summary>
    /// 分组名称
    /// </summary>
    public string Group { get; set; } = string.Empty;

    /// <summary>
    /// 分组下的权限列表
    /// </summary>
    public List<PermissionDto> Permissions { get; set; } = new();
}
