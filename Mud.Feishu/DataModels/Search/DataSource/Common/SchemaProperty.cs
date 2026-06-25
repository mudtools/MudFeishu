// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Search;


/// <summary></summary>
public class SchemaProperty
{
    /// <summary>
    /// <para>属性名</para>
    /// <para>**示例值**："summary"</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 长度范围：`0` ～ `20` 字符</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// <para>属性类型</para>
    /// <para>**示例值**："text"</para>
    /// <para>**可选值有**：</para>
    /// <para>text:长文本类型,int:64位整数类型,tag:标签类型,timestamp:Unix 时间戳类型（单位为秒）,double:浮点数类型（小数）,tinytext:短文本类型，（utf8 编码）长度小于 140 的文本。在设置 search_options 时，与 text 类型有区别，支持更多召回策略</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 长度范围：`0` ～ `60000` 字符</para>
    /// <para>必填：是</para>
    /// <para>可选值：<list type="bullet">
    /// <item>text：长文本类型</item>
    /// <item>int：64位整数类型</item>
    /// <item>tag：标签类型</item>
    /// <item>timestamp：Unix时间戳类型（单位为秒）</item>
    /// <item>double：浮点数类型（小数）</item>
    /// <item>tinytext：短文本类型，（utf8编码）长度小于140的文本。在设置search_options时，与text类型有区别，支持更多召回策略</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// <para>该属性是否可用作搜索，默认为 false</para>
    /// <para>**示例值**：true</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("is_searchable")]
    public bool? IsSearchable { get; set; }

    /// <summary>
    /// <para>该属性是否可用作搜索结果排序，默认为 false。如果为 true，需要再配置 sortOptions</para>
    /// <para>**示例值**：false</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("is_sortable")]
    public bool? IsSortable { get; set; }

    /// <summary>
    /// <para>该属性是否可用作返回字段，为 false 时，该字段不会被召回和展示。默认为 false</para>
    /// <para>**示例值**：true</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("is_returnable")]
    public bool? IsReturnable { get; set; }

    /// <summary>
    /// <para>属性排序的可选配置，当 is_sortable 为 true 时，该字段为必填字段</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("sort_options")]
    public SchemaSortOptions? SortOptions { get; set; }


    /// <summary>
    /// <para>相关类型数据的定义和约束</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("type_definitions")]
    public SchemaTypeDefinitions? TypeDefinitions { get; set; }



    /// <summary>
    /// <para>属性搜索的可选配置，当 is_searchable 为 true 时，该字段为必填参数</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("search_options")]
    public SchemaSearchOptions? SearchOptions { get; set; }

}