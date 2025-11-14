namespace Mud.Feishu.DataModels.Users;


/// <summary>
/// 自定义字段取值。
/// </summary>
public class CustomAttributeValue
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
