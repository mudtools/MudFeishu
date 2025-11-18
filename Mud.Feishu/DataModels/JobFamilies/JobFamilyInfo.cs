namespace Mud.Feishu.DataModels.JobFamilies;

public class JobFamilyInfo
{

    [JsonPropertyName("job_family_id")]
    public string JobFamilyId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("parent_job_family_id")]
    public string ParentJobFamilyId { get; set; }

    [JsonPropertyName("status")]
    public bool Status { get; set; }

    [JsonPropertyName("i18n_name")]
    public List<I18nContent> I18nName { get; set; }

    [JsonPropertyName("i18n_description")]
    public List<I18nContent> I18nDescription { get; set; }
}
