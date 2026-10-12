// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Bot;

/// <summary>
/// 搜索机器人结果条目。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Bot")]
public class SearchBotItem
{
    /// <summary>
    /// <para>机器人 ID（open_id）</para>
    /// <para>必填：否</para>
    /// <para>示例值：7890123456abcdef</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>机器人展示信息，第一行为名称，其后为描述，命中关键词会以 &lt;h&gt; 标签高亮</para>
    /// <para>必填：否</para>
    /// <para>示例值：飞书&lt;h&gt;搜索&lt;/h&gt;助手</para>
    /// </summary>
    [JsonPropertyName("display_info")]
    public string? DisplayInfo { get; set; }

    /// <summary>
    /// <para>机器人元数据</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("meta_data")]
    public SearchBotMetaData? MetaData { get; set; }
}
