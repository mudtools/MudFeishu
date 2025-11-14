namespace Mud.Feishu.DataModels.EmployeeType;

/// <summary>
/// 人员类型信息
/// </summary>
public class EmployeeTypeEnum
{
    [JsonPropertyName("enum_id")]
    public string EnumId { get; set; }

    [JsonPropertyName("enum_value")]
    public string EnumValue { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; }

    [JsonPropertyName("enum_type")]
    public int EnumType { get; set; }

    [JsonPropertyName("enum_status")]
    public int EnumStatus { get; set; }

    [JsonPropertyName("i18n_content")]
    public List<I18nContent> I18nContent { get; set; }
}