namespace Mud.Feishu.DataModels;

/// <summary>
/// 国际化配置。
/// </summary>
public class I18nContent
{
    /// <summary>
    /// 语言版本。例如：zh_cn：中文  en_us：英文 ja_jp：日文
    /// </summary>
    [JsonPropertyName("locale")]
    public string? Locale { get; set; }

    /// <summary>
    /// 语言版本对应的内容。示例值："专家（中文）"
    /// </summary>
    [JsonPropertyName("value")]
    public string? Value { get; set; }
}