namespace Mud.Feishu.DataModels;

public class I18nName
{
    [JsonPropertyName("zh_cn")]
    public string ZhCn { get; set; }

    [JsonPropertyName("ja_jp")]
    public string JaJp { get; set; }

    [JsonPropertyName("en_us")]
    public string EnUs { get; set; }
}