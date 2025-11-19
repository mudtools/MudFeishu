namespace Mud.Feishu.DataModels.WorkCites;

public class WorkCity
{
    [JsonPropertyName("work_city_id")]
    public string WorkCityId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("i18n_name")]
    public List<I18nContent> I18nName { get; set; }

    [JsonPropertyName("status")]
    public bool Status { get; set; }
}