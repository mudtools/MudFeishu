// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Search;


/// <summary></summary>
public class SchemaDisplay
{
    /// <summary>
    /// <para>搜索数据的展示卡片</para>
    /// <para>卡片详细信息请参考 [通用模块接入指南](/document/uAjLw4CM/ukTMukTMukTM/search-v2/common-template-intergration-handbook) "请求创建数据范式"部分</para>
    /// <para>**示例值**："search_common_card"</para>
    /// <para>**可选值有**：</para>
    /// <para>search_common_card:普通 common 卡片</para>
    /// <para>必填：是</para>
    /// <para>可选值：<list type="bullet">
    /// <item>search_common_card：普通common卡片</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("card_key")]
    public string CardKey { get; set; } = string.Empty;

    /// <summary>
    /// <para>数据字段名称和展示字段名称的映射关系。如果没有设置，则只会展示 与展示字段名称同名的 数据字段</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("fields_mapping")]
    public SchemaDisplayFieldMapping[]? FieldsMapping { get; set; }


}