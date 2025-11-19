namespace Mud.Feishu.DataModels.Employees;

public class UrlValue
{
    [JsonPropertyName("link_text")]
    public I18nContent LinkText { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonPropertyName("pcurl")]
    public string PcUrl { get; set; }
}