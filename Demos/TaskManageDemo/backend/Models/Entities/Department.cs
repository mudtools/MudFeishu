// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace TaskManageDemo.Backend.Models.Entities;

/// <summary>
/// 部门实体 - 存储从飞书同步的部门数据
/// </summary>
public class Department
{
    /// <summary>
    /// 本地数据库ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 飞书部门ID
    /// </summary>
    public string FeishuId { get; set; } = string.Empty;

    /// <summary>
    /// 部门名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 父部门ID
    /// </summary>
    public string? ParentDepartmentId { get; set; }

    /// <summary>
    /// 部门负责人ID
    /// </summary>
    public string? LeaderId { get; set; }

    /// <summary>
    /// 部门描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 排序顺序
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 是否激活
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 最后同步时间
    /// </summary>
    public DateTime LastSyncedAt { get; set; } = DateTime.UtcNow;
}
