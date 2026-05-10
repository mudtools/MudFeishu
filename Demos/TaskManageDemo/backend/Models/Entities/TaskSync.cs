// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace TaskManageDemo.Backend.Models.Entities;

/// <summary>
/// 任务同步实体 - 存储从飞书同步的任务数据
/// </summary>
public class TaskSync
{
    /// <summary>
    /// 本地数据库ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 飞书任务全局唯一ID
    /// </summary>
    public string TaskGuid { get; set; } = string.Empty;

    /// <summary>
    /// 任务标题
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// 任务描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 任务状态 (0: 未开始, 1: 进行中, 2: 已完成, 3: 已取消)
    /// </summary>
    public int Status { get; set; } = 0;

    /// <summary>
    /// 是否完成
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// 优先级 (0: 无, 1: 低, 2: 中, 3: 高, 4: 紧急)
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 截止时间
    /// </summary>
    public DateTime? DueTime { get; set; }

    /// <summary>
    /// 完成时间
    /// </summary>
    public DateTime? CompletedTime { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 创建者飞书ID
    /// </summary>
    public string? CreatorId { get; set; }

    /// <summary>
    /// 所属清单ID
    /// </summary>
    public string? TaskListGuid { get; set; }

    /// <summary>
    /// 父任务ID（子任务时使用）
    /// </summary>
    public string? ParentTaskGuid { get; set; }

    /// <summary>
    /// 最后同步时间
    /// </summary>
    public DateTime LastSyncedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 任务成员
    /// </summary>
    public ICollection<TaskMemberEntity> Members { get; set; } = new List<TaskMemberEntity>();

    /// <summary>
    /// 任务历史记录
    /// </summary>
    public ICollection<TaskHistory> Histories { get; set; } = new List<TaskHistory>();
}
