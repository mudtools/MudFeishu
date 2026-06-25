// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Search;

/// <summary>
/// <para>数据项索引对象</para>
/// </summary>
public class DataItemIndex
{

    /// <summary>
    /// <para>item 在 datasource 中的唯一标识，只允许英文字母、数字和下划线</para>
    /// <para>**示例值**："my_item_01010111"</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 最大长度：`128` 字符</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// <para>item 的访问权限控制。 acl 字段为空数组，则默认数据不可见。如果数据是全员可见，需要设置 access="allow"; type="user"; value="everyone"</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("acl")]
    public DateItemAcl[] Acl { get; set; } = [];


    /// <summary>
    /// <para>item 的元信息</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("metadata")]
    public ItemMetadata Metadata { get; set; } = new();


    /// <summary>
    /// <para>结构化数据（以 json 字符串传递），这些字段是搜索结果的展示字段(特殊字段无须在此另外指定);具体格式可参参考 [接入指南](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/search-v2/common-template-intergration-handbook) **请求创建数据项**部分。这里的示例遵循了”创建数据范式“部分中的数据范式示例，请按自己定义的数据范式填写数据</para>
    /// <para>**示例值**："{"description":"问题出现的环境和复现方法描述……", "priority":"HIGH"}"</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("structured_data")]
    public string StructuredData { get; set; } = string.Empty;

    /// <summary>
    /// <para>非结构化数据，如文档文本，飞书搜索会用来做召回</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("content")]
    public ItemContent? Content { get; set; }
}
