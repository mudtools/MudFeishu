// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Search;


/// <summary></summary>
[HttpJsonSerializable(SerializerClassName = "Search")]
public class SchemaDisplayFieldMapping
{
    /// <summary>
    /// <para>展示字段名称，与 card_key 有关，每个模版能展示的字段不同。该字段不能重复</para>
    /// <para>**示例值**："summary"</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("display_field")]
    public string DisplayField { get; set; } = string.Empty;

    /// <summary>
    /// <para>数据字段的名称。需要确保该字段对应在 schema 属性定义中的 is_returnable 为 true，否则无法展示。需要使用 ${xxx} 的规则来描述</para>
    /// <para>**示例值**："${description}"</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("data_field")]
    public string DataField { get; set; } = string.Empty;
}
