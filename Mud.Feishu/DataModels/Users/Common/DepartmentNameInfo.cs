namespace Mud.Feishu.DataModels.Users;

/// <summary>
/// 部门名称信息。
/// 包含部门的基本名称和国际化名称信息。
/// </summary>
public class DepartmentNameInfo
{
    /// <summary>
    /// 部门名称。
    /// 部门的主要名称，通常为中文名称。
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// 国际化名称。
    /// 包含部门的多语言名称信息，支持多种语言环境。
    /// </summary>
    [JsonPropertyName("i18n_name")]
    public I18nName? I18nName { get; set; }
}