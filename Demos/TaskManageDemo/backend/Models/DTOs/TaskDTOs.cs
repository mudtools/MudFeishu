// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;

namespace TaskManageDemo.Backend.Models.DTOs;

/// <summary>
/// 创建任务请求
/// </summary>
public class CreateTaskRequest
{
    /// <summary>
    /// 任务标题
    /// </summary>
    [Required(ErrorMessage = "任务标题不能为空")]
    [StringLength(500, ErrorMessage = "任务标题不能超过500个字符")]
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// 任务描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 截止时间
    /// </summary>
    public DateTime? DueTime { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 优先级 (0: 无, 1: 低, 2: 中, 3: 高, 4: 紧急)
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// 负责人ID列表
    /// </summary>
    public List<string>? AssigneeIds { get; set; }

    /// <summary>
    /// 关注人ID列表
    /// </summary>
    public List<string>? FollowerIds { get; set; }

    /// <summary>
    /// 所属清单ID
    /// </summary>
    public string? TaskListGuid { get; set; }

    /// <summary>
    /// 父任务ID（创建子任务时使用）
    /// </summary>
    public string? ParentTaskGuid { get; set; }
}

/// <summary>
/// 更新任务请求
/// </summary>
public class UpdateTaskRequest
{
    /// <summary>
    /// 任务标题
    /// </summary>
    [StringLength(500, ErrorMessage = "任务标题不能超过500个字符")]
    public string? Summary { get; set; }

    /// <summary>
    /// 任务描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 截止时间
    /// </summary>
    public DateTime? DueTime { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 优先级
    /// </summary>
    public int? Priority { get; set; }

    /// <summary>
    /// 是否完成
    /// </summary>
    public bool? IsCompleted { get; set; }
}

/// <summary>
/// 任务分配请求
/// </summary>
public class AssignTaskRequest
{
    /// <summary>
    /// 负责人ID列表
    /// </summary>
    public List<string> AssigneeIds { get; set; } = new();

    /// <summary>
    /// 关注人ID列表
    /// </summary>
    public List<string> FollowerIds { get; set; } = new();
}

/// <summary>
/// 任务状态变更请求
/// </summary>
public class UpdateTaskStatusRequest
{
    /// <summary>
    /// 是否完成
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// 完成时间
    /// </summary>
    public DateTime? CompletedTime { get; set; }
}

/// <summary>
/// 任务查询参数
/// </summary>
public class TaskQueryParameters
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
    /// 状态筛选
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// 是否完成
    /// </summary>
    public bool? IsCompleted { get; set; }

    /// <summary>
    /// 优先级
    /// </summary>
    public int? Priority { get; set; }

    /// <summary>
    /// 负责人ID
    /// </summary>
    public string? AssigneeId { get; set; }

    /// <summary>
    /// 清单ID
    /// </summary>
    public string? TaskListGuid { get; set; }

    /// <summary>
    /// 搜索关键词
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 开始时间（筛选截止时间）
    /// </summary>
    public DateTime? DueTimeFrom { get; set; }

    /// <summary>
    /// 结束时间（筛选截止时间）
    /// </summary>
    public DateTime? DueTimeTo { get; set; }

    /// <summary>
    /// 排序字段
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// 是否降序
    /// </summary>
    public bool IsDescending { get; set; } = true;
}

/// <summary>
/// 任务DTO
/// </summary>
public class TaskDto
{
    /// <summary>
    /// 本地ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 飞书任务ID
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
    /// 状态
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// 是否完成
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// 优先级
    /// </summary>
    public int Priority { get; set; }

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
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 创建者ID
    /// </summary>
    public string? CreatorId { get; set; }

    /// <summary>
    /// 创建者名称
    /// </summary>
    public string? CreatorName { get; set; }

    /// <summary>
    /// 所属清单ID
    /// </summary>
    public string? TaskListGuid { get; set; }

    /// <summary>
    /// 任务成员
    /// </summary>
    public List<TaskMemberDto> Members { get; set; } = new();
}

/// <summary>
/// 任务成员DTO
/// </summary>
public class TaskMemberDto
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public string FeishuId { get; set; } = string.Empty;

    /// <summary>
    /// 用户姓名
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 头像URL
    /// </summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// 角色
    /// </summary>
    public string Role { get; set; } = string.Empty;
}

/// <summary>
/// 任务清单DTO
/// </summary>
public class TaskListDto
{
    /// <summary>
    /// 本地ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 飞书清单ID
    /// </summary>
    public string TaskListGuid { get; set; } = string.Empty;

    /// <summary>
    /// 清单名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 清单描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 清单成员
    /// </summary>
    public List<TaskMemberDto> Members { get; set; } = new();
}

/// <summary>
/// 创建任务清单请求
/// </summary>
public class CreateTaskListRequestDto
{
    /// <summary>
    /// 清单名称
    /// </summary>
    [Required(ErrorMessage = "清单名称不能为空")]
    [StringLength(200, ErrorMessage = "清单名称不能超过200个字符")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 清单描述
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// 更新任务清单请求
/// </summary>
public class UpdateTaskListRequestDto
{
    /// <summary>
    /// 清单名称
    /// </summary>
    [StringLength(200, ErrorMessage = "清单名称不能超过200个字符")]
    public string? Name { get; set; }

    /// <summary>
    /// 清单描述
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// 任务清单成员信息
/// </summary>
public class TaskListMemberInfo
{
    /// <summary>
    /// 成员ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 成员类型
    /// </summary>
    public string Type { get; set; } = string.Empty;
}

/// <summary>
/// 添加清单成员请求
/// </summary>
public class AddTaskListMembersRequest
{
    /// <summary>
    /// 成员列表
    /// </summary>
    public TaskListMemberInfo[] Members { get; set; } = Array.Empty<TaskListMemberInfo>();
}

/// <summary>
/// 移除清单成员请求
/// </summary>
public class RemoveTaskListMembersRequest
{
    /// <summary>
    /// 成员列表
    /// </summary>
    public TaskListMemberInfo[] Members { get; set; } = Array.Empty<TaskListMemberInfo>();
}
