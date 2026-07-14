// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.VideoConferencing;


/// <summary>
/// <para>纪要来源。目前只有**会议来源**的会返回数据。</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "VideoConferencing")]
public class GeneratedSource
{
    /// <summary>
    /// <para>来源类型</para>
    /// <para>**可选值有:**</para>
    /// <para>-`meeting`: 会议来源</para>
    /// <para>必填：否</para>
    /// <para>示例值：meeting</para>
    /// </summary>
    [JsonPropertyName("source_type")]
    public string? SourceType { get; set; }

    /// <summary>
    /// <para>来源实体id。</para>
    /// <para>-`meeting`: 对应的是会议ID（meeting id）</para>
    /// <para>必填：否</para>
    /// <para>示例值：6911188411934433028</para>
    /// </summary>
    [JsonPropertyName("source_entity_id")]
    public string? SourceEntityId { get; set; }
}
