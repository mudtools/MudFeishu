// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Tasks;

/// <summary>
/// 搜索任务响应体
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/task-v2/task/search"/></para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Task")]
public class SearchTaskResult
{
    /// <summary>
    /// <para>搜索命中的任务列表。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("items")]
    public TaskSearchItemInfo[]? Items { get; set; }

    /// <summary>
    /// <para>搜索命中的任务总数。</para>
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
/// 搜索命中的任务条目
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Task")]
public class TaskSearchItemInfo
{
    /// <summary>
    /// <para>任务 ID。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>包含任务基本信息的卡片，搜索关键词命中的文本片段使用 &lt;h&gt;&lt;/h&gt; 标签包裹标注。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("display_info")]
    public string? DisplayInfo { get; set; }

    /// <summary>
    /// <para>任务元数据。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("meta_data")]
    public TaskSearchMetaData? MetaData { get; set; }
}

/// <summary>
/// 搜索命中的任务元数据
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Task")]
public class TaskSearchMetaData
{
    /// <summary>
    /// <para>任务的跳转链接。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("app_link")]
    public string? AppLink { get; set; }

    /// <summary>
    /// <para>任务所属清单的图标资源 key。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("avatar")]
    public string? Avatar { get; set; }

    /// <summary>
    /// <para>任务备注。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }
}
