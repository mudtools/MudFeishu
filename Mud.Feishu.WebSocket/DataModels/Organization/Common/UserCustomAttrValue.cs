// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.WebSocket.DataModels.Organization;

/// <summary>
/// 用户自定义属性值类，用于表示飞书平台中用户自定义字段的各种可能值
/// </summary>
public class UserCustomAttrValue
{
    /// <summary>
    /// <para>- 字段类型为 TEXT 时，该参数返回定义的字段值。</para>
    /// <para>- 字段类型为 HREF 时，该参数返回定义的网页标题。</para>
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>
    /// <para>字段类型为 HREF 时，该参数返回定义的默认 URL。</para>
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// <para>字段类型为 HREF 时，如果为 PC 端设置了 URL，则该参数返回定义的 PC 端 URL。</para>
    /// </summary>
    [JsonPropertyName("pc_url")]
    public string? PcUrl { get; set; }

    /// <summary>
    /// <para>字段类型为 `ENUMERATION` 或 `PICTURE_ENUM` 时，该参数返回定义的选项 ID。</para>
    /// </summary>
    [JsonPropertyName("option_id")]
    public string? OptionId { get; set; }

    /// <summary>
    /// <para>选项类型的值，即用户详情或自定义字段中选中的选项值。</para>
    /// </summary>
    [JsonPropertyName("option_value")]
    public string? OptionValue { get; set; }

    /// <summary>
    /// <para>选项类型为图片时，图片的名称。</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>选项类型为图片时，图片的链接。</para>
    /// </summary>
    [JsonPropertyName("picture_url")]
    public string? PictureUrl { get; set; }

    /// <summary>
    /// <para>字段类型为 `GENERIC_USER` 时，该参数返回定义的引用人员信息。</para>
    /// </summary>
    [JsonPropertyName("generic_user")]
    public CustomAttrGenericUser? GenericUser { get; set; }
}