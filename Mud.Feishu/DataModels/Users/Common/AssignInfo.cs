namespace Mud.Feishu.DataModels.Users;

public class AssignInfo
{
    [JsonPropertyName("subscription_id")]
    public string SubscriptionId { get; set; }

    [JsonPropertyName("license_plan_key")]
    public string LicensePlanKey { get; set; }

    [JsonPropertyName("product_name")]
    public string ProductName { get; set; }

    [JsonPropertyName("i18n_name")]
    public I18nName I18nName { get; set; }

    [JsonPropertyName("start_time")]
    public string StartTime { get; set; }

    [JsonPropertyName("end_time")]
    public string EndTime { get; set; }
}