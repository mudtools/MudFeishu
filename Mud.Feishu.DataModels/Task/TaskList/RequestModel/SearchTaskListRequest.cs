// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Tasks;

namespace Mud.Feishu.DataModels.TasksList;

/// <summary>
/// 搜索清单请求体
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/task-v2/tasklist/search"/></para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Task")]
public class SearchTaskListRequest
{
    /// <summary>
    /// <para>搜索关键字，长度范围 0 ~ 50 字符。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("query")]
    public string? Query { get; set; }

    /// <summary>
    /// <para>过滤参数，包括创建时间、创建人，不设置时不过滤。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("filter")]
    public TaskListSearchFilter? Filter { get; set; }
}

/// <summary>
/// 清单搜索过滤器
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Task")]
public class TaskListSearchFilter
{
    /// <summary>
    /// <para>按清单创建时间范围过滤，start_time 需小于 end_time。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("create_time")]
    public TaskSearchTimeRange? CreateTime { get; set; }

    /// <summary>
    /// <para>创建人 ID 列表，ID 类型需与查询参数 user_id_type 保持一致。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string[]? UserId { get; set; }
}

/// <summary>
/// 搜索清单响应体
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/task-v2/tasklist/search"/></para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Task")]
public class SearchTaskListResult
{
    /// <summary>
    /// <para>搜索命中的清单列表。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("items")]
    public TaskListSearchItem[]? Items { get; set; }

    /// <summary>
    /// <para>搜索命中的清单总数。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("total")]
    public int? Total { get; set; }

    /// <summary>
    /// <para>是否还有更多项。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("has_more")]
    public bool? HasMore { get; set; }

    /// <summary>
    /// <para>分页标记，当 has_more 为 true 时会同时返回新的 page_token。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("page_token")]
    public string? PageToken { get; set; }

    /// <summary>
    /// <para>搜索补充提示信息。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("notice")]
    public string? Notice { get; set; }
}

/// <summary>
/// 搜索命中的清单条目
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Task")]
public class TaskListSearchItem
{
    /// <summary>
    /// <para>清单 ID。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>包含清单基本信息的卡片，搜索关键词命中的文本片段使用 &lt;h&gt;&lt;/h&gt; 标签包裹标注。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("display_info")]
    public string? DisplayInfo { get; set; }

    /// <summary>
    /// <para>清单元数据。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("meta_data")]
    public TaskListSearchMetaData? MetaData { get; set; }
}

/// <summary>
/// 搜索命中的清单元数据
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Task")]
public class TaskListSearchMetaData
{
    /// <summary>
    /// <para>清单的跳转链接。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("app_link")]
    public string? AppLink { get; set; }

    /// <summary>
    /// <para>清单的图标资源 key。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("avatar")]
    public string? Avatar { get; set; }

    /// <summary>
    /// <para>清单描述。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }
}
