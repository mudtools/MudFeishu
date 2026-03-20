// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace TaskManageDemo.Backend.Models.Entities;

/// <summary>
/// 任务模板实体
/// </summary>
public class TaskTemplate
{
    /// <summary>
    /// 本地数据库ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 模板名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 模板描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 默认任务标题
    /// </summary>
    public string? DefaultSummary { get; set; }

    /// <summary>
    /// 默认任务描述
    /// </summary>
    public string? DefaultDescription { get; set; }

    /// <summary>
    /// 默认优先级
    /// </summary>
    public int DefaultPriority { get; set; } = 0;

    /// <summary>
    /// 默认截止时间偏移（天数）
    /// </summary>
    public int? DefaultDueDays { get; set; }

    /// <summary>
    /// 检查项列表（JSON格式）
    /// </summary>
    public string? CheckItems { get; set; }

    /// <summary>
    /// 创建者ID
    /// </summary>
    public string? CreatorId { get; set; }

    /// <summary>
    /// 是否公开
    /// </summary>
    public bool IsPublic { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
