namespace Mud.Feishu.DataModels.Users;


/// <summary>
/// 自定义字段。
/// </summary>
public class CustomAttribute
{
    /// <summary>
    /// 自定义字段类型。可选值有：TEXT：文本 HREF：网页 ENUMERATION：枚举 PICTURE_ENUM：图片 GENERIC_USER：用户
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// 自定义字段 ID。
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// 自定义字段取值。
    /// </summary>
    [JsonPropertyName("value")]
    public CustomAttributeValue? Value { get; set; }
}
