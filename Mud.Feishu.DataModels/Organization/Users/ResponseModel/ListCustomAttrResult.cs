// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Users;

/// <summary>
/// 查询自定义字段列表响应体
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/contact-v3/custom_attr/list"/></para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Organization")]
public class ListCustomAttrResult
{
    /// <summary>
    /// <para>自定义字段信息集合。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("items")]
    public CustomAttrInfo[]? Items { get; set; }
}

/// <summary>
/// 自定义字段定义
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Organization")]
public class CustomAttrInfo
{
    /// <summary>
    /// <para>自定义字段 ID。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>自定义字段类型，可选值有：TEXT（文本）、HREF（网页）、ENUMERATION（枚举）、PICTURE_ENUM（图片）、GENERIC_USER（用户）。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// <para>自定义字段取值选项信息。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("options")]
    public CustomAttrOptions? Options { get; set; }

    /// <summary>
    /// <para>自定义字段的国际化名称。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("i18n_name")]
    public I18nContent[]? I18nName { get; set; }
}

/// <summary>
/// 自定义字段取值选项
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Organization")]
public class CustomAttrOptions
{
    /// <summary>
    /// <para>选项类型，由字段类型决定。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("option_type")]
    public string? OptionType { get; set; }

    /// <summary>
    /// <para>默认选项 ID。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("default_option_id")]
    public string? DefaultOptionId { get; set; }

    /// <summary>
    /// <para>选项列表。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("options")]
    public CustomAttrOption[]? Options { get; set; }
}

/// <summary>
/// 自定义字段单个选项
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Organization")]
public class CustomAttrOption
{
    /// <summary>
    /// <para>选项 ID。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>选项值。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("value")]
    public string? Value { get; set; }

    /// <summary>
    /// <para>选项的国际化名称。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("i18n_name")]
    public I18nContent[]? I18nName { get; set; }
}
