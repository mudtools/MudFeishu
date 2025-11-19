// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Users;


/// <summary>
/// 自定义字段取值。
/// </summary>
public class CustomUserAttributeValue
{

    /// <summary>
    /// 自定义字段类型为 TEXT 时，该参数必填，用于定义字段值。
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>
    /// 自定义字段类型为 HREF 时，该参数必填，用于定义默认 URL。例如，手机端跳转小程序，PC端跳转网页。
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// 自定义字段类型为 HREF 时，该参数用于定义 PC 端 URL。
    /// <para>注意：请以 http:// 或 https:// 开头。</para>
    /// </summary>
    [JsonPropertyName("pc_url")]
    public string? PcUrl { get; set; }

    /// <summary>
    /// 自定义字段类型为 ENUMERATION 或 PICTURE_ENUM 时，该参数用于定义选项 ID。
    /// </summary>
    [JsonPropertyName("option_id")]
    public string? OptionId { get; set; }

    /// <summary>
    /// 自定义字段类型为 GENERIC_USER 时，该参数用于定义引用人员。
    /// </summary>
    [JsonPropertyName("generic_user")]
    public GenericUser? GenericUser { get; set; }
}
