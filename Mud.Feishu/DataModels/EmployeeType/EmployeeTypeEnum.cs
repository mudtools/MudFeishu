namespace Mud.Feishu.DataModels.EmployeeType;

/// <summary>
/// 人员类型信息
/// </summary>
public class EmployeeTypeEnum
{
    /// <summary>
    /// 枚举 ID。
    /// </summary>
    [JsonPropertyName("enum_id")]
    public string? EnumId { get; set; }

    /// <summary>
    /// 枚举值。
    /// </summary>
    [JsonPropertyName("enum_value")]
    public string? EnumValue { get; set; }

    /// <summary>
    /// 枚举内容。
    /// </summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    /// <summary>
    /// 枚举类型。
    /// <para>0：系统预定义枚举</para>
    /// <para>1：用户自定义枚举</para>
    /// </summary>
    [JsonPropertyName("enum_type")]
    public int EnumType { get; set; }

    /// <summary>
    /// 枚举状态。
    /// <para>0：已停用</para>
    /// <para>1：启用</para>
    /// </summary>
    [JsonPropertyName("enum_status")]
    public int EnumStatus { get; set; }

    /// <summary>
    /// 枚举内容的国际化配置。
    /// </summary>
    [JsonPropertyName("i18n_content")]
    public List<I18nContent>? I18nContent { get; set; }
}