// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Search;


/// <summary></summary>
[HttpJsonSerializable(SerializerClassName = "Search")]
public class ItemMetadata
{
    /// <summary>
    /// <para>该条数据记录对应的标题</para>
    /// <para>**示例值**："工单：无法创建文章"</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// <para>该条数据记录对应的跳转url</para>
    /// <para>**示例值**："http://www.abc.com.cn"</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("source_url")]
    public string SourceUrl { get; set; } = string.Empty;

    /// <summary>
    /// <para>数据项的创建时间。Unix 时间，单位为秒</para>
    /// <para>**示例值**：1618831236</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("create_time")]
    public int? CreateTime { get; set; }

    /// <summary>
    /// <para>数据项的更新时间。Unix 时间，单位为秒</para>
    /// <para>**示例值**：1618831236</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("update_time")]
    public int? UpdateTime { get; set; }

    /// <summary>
    /// <para>移动端搜索命中的跳转地址。如果您PC端和移动端有不同的跳转地址，可以在这里写入移动端专用的url，我们会在搜索时为您选择合适的地址</para>
    /// <para>**示例值**："https://www.feishu.cn"</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("source_url_mobile")]
    public string? SourceUrlMobile { get; set; }
}
