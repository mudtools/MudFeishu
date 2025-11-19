namespace Mud.Feishu.DataModels.JobTitles;

public class JobTitle
{
    [JsonPropertyName("job_title_id")]
    public string JobTitleId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("i18n_name")]
    public List<I18nContent> I18nName { get; set; }

    [JsonPropertyName("status")]
    public bool Status { get; set; }
}