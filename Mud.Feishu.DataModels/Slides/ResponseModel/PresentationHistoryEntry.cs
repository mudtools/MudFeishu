// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Slides;

/// <summary>
/// 演示文稿历史版本条目
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Slides")]
public class PresentationHistoryEntry
{
    /// <summary>
    /// <para>历史版本对应的版本号，范围 0～2147483647</para>
    /// <para>必填：否</para>
    /// <para>示例值：11</para>
    /// </summary>
    [JsonPropertyName("revision_id")]
    public int? RevisionId { get; set; }

    /// <summary>
    /// <para>历史版本编辑时间（ISO 8601）</para>
    /// <para>必填：否</para>
    /// <para>示例值：2026-06-16T11:24:12Z</para>
    /// </summary>
    [JsonPropertyName("edit_time")]
    public string? EditTime { get; set; }

    /// <summary>
    /// <para>历史版本类型</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("type")]
    public int? Type { get; set; }

    /// <summary>
    /// <para>历史版本名称</para>
    /// <para>必填：否</para>
    /// <para>示例值：版本名称</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>历史版本描述</para>
    /// <para>必填：否</para>
    /// <para>示例值：版本描述</para>
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// <para>编辑者 ID 列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("editor_ids")]
    public string[]? EditorIds { get; set; }

    /// <summary>
    /// <para>历史记录 id，用于回滚到指定历史版本</para>
    /// <para>必填：否</para>
    /// <para>示例值：51341231231</para>
    /// </summary>
    [JsonPropertyName("history_version_id")]
    public string? HistoryVersionId { get; set; }
}
