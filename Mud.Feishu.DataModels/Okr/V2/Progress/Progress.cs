// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.OkrV2;

/// <summary>
/// OKR v2 进展记录信息
/// </summary>
public class Progress
{
    /// <summary>
    /// <para>进展记录 id</para>
    /// <para>示例值：7342342398472398471</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>进展创建时间（毫秒时间戳）</para>
    /// <para>示例值：1760604634563</para>
    /// </summary>
    [JsonPropertyName("create_time")]
    public string? CreateTime { get; set; }

    /// <summary>
    /// <para>进展更新时间（毫秒时间戳）</para>
    /// <para>示例值：1760604634563</para>
    /// </summary>
    [JsonPropertyName("update_time")]
    public string? UpdateTime { get; set; }

    /// <summary>
    /// <para>进展归属者</para>
    /// </summary>
    [JsonPropertyName("owner")]
    public Owner? Owner { get; set; }

    /// <summary>
    /// <para>进展所属实体类型：2 Objective、3 Key Result</para>
    /// <para>示例值：2</para>
    /// </summary>
    [JsonPropertyName("entity_type")]
    public int? EntityType { get; set; }

    /// <summary>
    /// <para>进展所属实体 id</para>
    /// <para>示例值：7342342398472398472</para>
    /// </summary>
    [JsonPropertyName("entity_id")]
    public string? EntityId { get; set; }

    /// <summary>
    /// <para>进展内容（富文本）</para>
    /// </summary>
    [JsonPropertyName("content")]
    public ContentBlockV2? Content { get; set; }

    /// <summary>
    /// <para>进展完成度</para>
    /// </summary>
    [JsonPropertyName("progress_rate")]
    public ProgressRate? ProgressRate { get; set; }
}
