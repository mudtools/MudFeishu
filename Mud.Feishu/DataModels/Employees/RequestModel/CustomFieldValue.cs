namespace Mud.Feishu.DataModels.Employees;

/// <summary>
/// 自定义字段
/// </summary>
public class CustomFieldValue
{
    /// <summary>
    /// 自定义字段类型 可选值有：1：多行文本 2：网页链接 3：枚举选项 4：人员 9：电话 10：多选枚举类型 11：人员列表
    /// </summary>
    [JsonPropertyName("field_type")]
    public string? FieldType { get; set; }

    /// <summary>
    /// 文本字段值
    /// </summary>
    [JsonPropertyName("text_value")]
    public I18nContent? TextValue { get; set; }

    /// <summary>
    /// 网页链接字段值
    /// </summary>
    [JsonPropertyName("url_value")]
    public UrlValue? UrlValue { get; set; }

    /// <summary>
    /// 枚举字段值
    /// </summary>
    [JsonPropertyName("enum_value")]
    public EnumValue? EnumValue { get; set; }

    /// <summary>
    /// 人员字段值
    /// </summary>
    [JsonPropertyName("user_values")]
    public List<UserValue>? UserValues { get; set; }

    /// <summary>
    /// 自定义字段key
    /// </summary>
    [JsonPropertyName("field_key")]
    public string? FieldKey { get; set; }
}