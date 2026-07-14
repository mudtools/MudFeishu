// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Search;


/// <summary></summary>
[HttpJsonSerializable(SerializerClassName = "Search")]
public class SchemaSearchOptions
{
    /// <summary>
    /// <para>是否支持语义切词召回。默认不支持（推荐使用在长文本的场景）</para>
    /// <para>**示例值**：true</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("enable_semantic_match")]
    public bool? EnableSemanticMatch { get; set; }

    /// <summary>
    /// <para>是否支持精确匹配。默认不支持（推荐使用在短文本、需要精确查找的场景）</para>
    /// <para>**示例值**：false</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("enable_exact_match")]
    public bool? EnableExactMatch { get; set; }

    /// <summary>
    /// <para>是否支持前缀匹配（短文本的默认的分词/召回策略。前缀长度为 1-12）</para>
    /// <para>**示例值**：false</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("enable_prefix_match")]
    public bool? EnablePrefixMatch { get; set; }

    /// <summary>
    /// <para>是否支持数据后缀匹配。默认不支持（推荐使用在短文本、有数字后缀查找的场景。后缀长度为3-12）</para>
    /// <para>**示例值**：false</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("enable_number_suffix_match")]
    public bool? EnableNumberSuffixMatch { get; set; }

    /// <summary>
    /// <para>是否支持驼峰英文匹配。默认不支持（推荐使用在短文本，且包含驼峰形式英文的查找场景）</para>
    /// <para>**示例值**：false</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("enable_camel_match")]
    public bool? EnableCamelMatch { get; set; }
}
