// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace TaskManageDemo.Backend.Models.DTOs;

/// <summary>
/// 任务统计DTO
/// </summary>
public class TaskStatisticsDto
{
    /// <summary>
    /// 总任务数
    /// </summary>
    public int TotalTasks { get; set; }

    /// <summary>
    /// 已完成任务数
    /// </summary>
    public int CompletedTasks { get; set; }

    /// <summary>
    /// 进行中任务数
    /// </summary>
    public int PendingTasks { get; set; }

    /// <summary>
    /// 逾期任务数
    /// </summary>
    public int OverdueTasks { get; set; }

    /// <summary>
    /// 完成率
    /// </summary>
    public double CompletionRate { get; set; }

    /// <summary>
    /// 按优先级分布
    /// </summary>
    public List<PriorityDistributionDto> PriorityDistribution { get; set; } = new();

    /// <summary>
    /// 按状态分布
    /// </summary>
    public List<StatusDistributionDto> StatusDistribution { get; set; } = new();
}

/// <summary>
/// 优先级分布DTO
/// </summary>
public class PriorityDistributionDto
{
    /// <summary>
    /// 优先级 (0: 无, 1: 低, 2: 中, 3: 高, 4: 紧急)
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// 优先级名称
    /// </summary>
    public string PriorityName { get; set; } = string.Empty;

    /// <summary>
    /// 任务数量
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// 占比
    /// </summary>
    public double Percentage { get; set; }
}

/// <summary>
/// 状态分布DTO
/// </summary>
public class StatusDistributionDto
{
    /// <summary>
    /// 状态
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 任务数量
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// 占比
    /// </summary>
    public double Percentage { get; set; }
}

/// <summary>
/// 用户工作量DTO
/// </summary>
public class UserWorkloadDto
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// 用户飞书ID
    /// </summary>
    public string FeishuId { get; set; } = string.Empty;

    /// <summary>
    /// 用户名称
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 分配的任务总数
    /// </summary>
    public int TotalAssigned { get; set; }

    /// <summary>
    /// 已完成任务数
    /// </summary>
    public int CompletedCount { get; set; }

    /// <summary>
    /// 进行中任务数
    /// </summary>
    public int PendingCount { get; set; }

    /// <summary>
    /// 逾期任务数
    /// </summary>
    public int OverdueCount { get; set; }

    /// <summary>
    /// 完成率
    /// </summary>
    public double CompletionRate { get; set; }
}

/// <summary>
/// 任务趋势DTO
/// </summary>
public class TaskTrendDto
{
    /// <summary>
    /// 日期
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// 创建任务数
    /// </summary>
    public int CreatedCount { get; set; }

    /// <summary>
    /// 完成任务数
    /// </summary>
    public int CompletedCount { get; set; }
}
