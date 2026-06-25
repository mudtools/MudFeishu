// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Search;

/// <summary>
/// 数据范式
/// </summary>
public class DataSchema
{

    /// <summary>
    /// <para>数据范式的属性定义</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("properties")]
    public SchemaProperty[] Properties { get; set; } = [];

    /// <summary>
    /// <para>数据展示相关配置</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("display")]
    public SchemaDisplay Display { get; set; } = new();


    /// <summary>
    /// <para>用户自定义数据范式的唯一标识</para>
    /// <para>**示例值**："jira_schema"</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 最大长度：`40` 字符</para>
    /// <para>- 正则校验：`^[a-zA-Z][a-zA-Z0-9-_].*$`</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("schema_id")]
    public string SchemaId { get; set; } = string.Empty;
}
