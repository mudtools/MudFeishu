// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Tasks;

/// <summary>
/// 搜索任务请求体
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/task-v2/task/search"/></para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Task")]
public class SearchTaskRequest
{
    /// <summary>
    /// <para>搜索关键字，长度范围 0 ~ 50 字符。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("query")]
    public string? Query { get; set; }

    /// <summary>
    /// <para>搜索过滤器，不设置时不过滤。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("filter")]
    public TaskSearchFilter? Filter { get; set; }
}

/// <summary>
/// 任务搜索过滤器
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Task")]
public class TaskSearchFilter
{
    /// <summary>
    /// <para>创建人 ID 列表，ID 类型需与查询参数 user_id_type 保持一致。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("creator_ids")]
    public string[]? CreatorIds { get; set; }

    /// <summary>
    /// <para>负责人 ID 列表，ID 类型需与查询参数 user_id_type 保持一致。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("assignee_ids")]
    public string[]? AssigneeIds { get; set; }

    /// <summary>
    /// <para>关注人 ID 列表，ID 类型需与查询参数 user_id_type 保持一致。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("follower_ids")]
    public string[]? FollowerIds { get; set; }

    /// <summary>
    /// <para>是否已完成。true 表示仅搜索已完成任务，false 表示仅搜索未完成任务，不填表示不过滤。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("is_completed")]
    public bool? IsCompleted { get; set; }

    /// <summary>
    /// <para>按任务截止时间范围过滤。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("due_time")]
    public TaskSearchTimeRange? DueTime { get; set; }
}

/// <summary>
/// 任务搜索时间区间
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Task")]
public class TaskSearchTimeRange
{
    /// <summary>
    /// <para>开始时间，ISO8601 格式，需小于 end_time。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("start_time")]
    public string? StartTime { get; set; }

    /// <summary>
    /// <para>结束时间，ISO8601 格式，需大于 start_time。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("end_time")]
    public string? EndTime { get; set; }
}

/// <summary>
/// 设置任务的父任务请求体
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/task-v2/task/set_ancestor_task"/></para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Task")]
public class SetAncestorTaskRequest
{
    /// <summary>
    /// <para>父任务的 GUID。设置为空字符串时表示将该任务转为独立任务（取消父任务）。</para>
    /// <para>必填：否</para>
    /// <para>示例值：e297ddff-06ca-4166-b917-4ce57cd3a7a0</para>
    /// </summary>
    [JsonPropertyName("ancestor_guid")]
    public string? AncestorGuid { get; set; }

    /// <summary>
    /// <para>此次调用中使用的用户 ID 类型，需与请求体中的 user_id 保持一致。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("user_id_type")]
    public string? UserIdType { get; set; }

    /// <summary>
    /// <para>操作人的 ID，ID 类型需与 user_id_type 保持一致。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("target_user_id")]
    public string? TargetUserId { get; set; }
}
