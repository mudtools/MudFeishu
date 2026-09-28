// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.OkrV2;

/// <summary>
/// OKR v2 富文本列表样式
/// </summary>
public class ContentListV2
{
    /// <summary>
    /// <para>列表类型：number 有序列表、bullet 无序列表、checkBox 任务列表、checkedBox 已完成任务列表、indent Tab 缩进</para>
    /// <para>示例值：number</para>
    /// </summary>
    [JsonPropertyName("list_type")]
    public string? ListType { get; set; }

    /// <summary>
    /// <para>列表缩进级别，支持 1-16 级</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("indent_level")]
    public int? IndentLevel { get; set; }

    /// <summary>
    /// <para>列表行号，仅对有序列表和代码块生效</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("number")]
    public int? Number { get; set; }
}
