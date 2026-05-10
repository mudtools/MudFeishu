// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace TaskManageDemo.Backend.Models.Entities;

/// <summary>
/// 角色实体
/// </summary>
public class Role
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
    public bool IsSystem { get; set; } = false;

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 排序序号
    /// </summary>
    public int SortOrder { get; set; } = 0;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 角色权限关联
    /// </summary>
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

    /// <summary>
    /// 用户角色关联
    /// </summary>
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

/// <summary>
/// 角色权限关联实体
/// </summary>
public class RolePermission
{
    /// <summary>
    /// 关联ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 角色ID
    /// </summary>
    public int RoleId { get; set; }

    /// <summary>
    /// 权限ID
    /// </summary>
    public int PermissionId { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 导航属性 - 角色
    /// </summary>
    public Role Role { get; set; } = null!;

    /// <summary>
    /// 导航属性 - 权限
    /// </summary>
    public Permission Permission { get; set; } = null!;
}

/// <summary>
/// 用户角色关联实体
/// </summary>
public class UserRole
{
    /// <summary>
    /// 关联ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// 角色ID
    /// </summary>
    public int RoleId { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 创建人ID
    /// </summary>
    public int? CreatedBy { get; set; }

    /// <summary>
    /// 导航属性 - 用户
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// 导航属性 - 角色
    /// </summary>
    public Role Role { get; set; } = null!;
}
