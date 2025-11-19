namespace Mud.Feishu.DataModels.Employees;

/// <summary>
/// 员工国际化配置。
/// </summary>
public class EmployeeI18nContent
{
    /// <summary>
    /// 默认值最小长度：1字符
    /// </summary>
    [JsonPropertyName("default_value")]
    public required string DefaultValue { get; set; }

    /// <summary>
    /// 国际化值，key为zh_cn, ja_jp, en_us, value为对应的值。
    /// </summary>
    [JsonPropertyName("i18n_value")]
    public I18nName? I18nValue { get; set; }
}

