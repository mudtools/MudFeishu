namespace Mud.Feishu.DataModels.Users;

public class DepartmentNameInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("i18n_name")]
    public I18nName I18nName { get; set; }
}