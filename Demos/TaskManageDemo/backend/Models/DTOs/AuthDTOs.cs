// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Security.Claims;

namespace TaskManageDemo.Backend.Models.DTOs;

/// <summary>
/// 用户信息
/// </summary>
public class UserInfo
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// 用户名称
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 飞书用户ID
    /// </summary>
    public string FeishuId { get; set; } = string.Empty;

    /// <summary>
    /// 部门ID
    /// </summary>
    public string? DepartmentId { get; set; }

    /// <summary>
    /// 角色
    /// </summary>
    public string Role { get; set; } = "user";

    /// <summary>
    /// 权限列表
    /// </summary>
    public List<string> Permissions { get; set; } = new();
}

/// <summary>
/// 用户角色
/// </summary>
public static class UserRoles
{
    /// <summary>
    /// 管理员
    /// </summary>
    public const string Admin = "admin";

    /// <summary>
    /// 普通用户
    /// </summary>
    public const string User = "user";

    /// <summary>
    /// 部门管理员
    /// </summary>
    public const string DepartmentAdmin = "department_admin";
}

/// <summary>
/// 权限定义
/// </summary>
public static class Permissions
{
    public const string TaskCreate = "task:create";
    public const string TaskRead = "task:read";
    public const string TaskUpdate = "task:update";
    public const string TaskDelete = "task:delete";
    public const string TaskAssign = "task:assign";

    public const string TaskListCreate = "tasklist:create";
    public const string TaskListRead = "tasklist:read";
    public const string TaskListUpdate = "tasklist:update";
    public const string TaskListDelete = "tasklist:delete";

    public const string TemplateCreate = "template:create";
    public const string TemplateRead = "template:read";
    public const string TemplateUpdate = "template:update";
    public const string TemplateDelete = "template:delete";

    public const string StatisticsView = "statistics:view";
    public const string UserManage = "user:manage";
    public const string DepartmentManage = "department:manage";
}
