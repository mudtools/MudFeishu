namespace Mud.Feishu.DataModels.JobLevel;

/// <summary>
/// 创建职级响应体。
/// </summary>
public class JobLevelResult
{
    /// <summary>
    /// 职级ID。
    /// </summary>
    [JsonPropertyName("job_level_id")]
    public string? JobLevelId { get; set; }

    /// <summary>
    /// 职级名称。通用名称，如果未设置多语言名称，则默认展示该名称。
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// 职级描述。字符长度上限 5,000。通用描述，如果未设置多语言描述，则默认展示该描述。
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// 职级排序。数值越小，排序越靠前。
    /// </summary>
    [JsonPropertyName("order")]
    public int Order { get; set; }

    /// <summary>
    /// 是否启用该职级。
    /// </summary>
    [JsonPropertyName("status")]
    public bool Status { get; set; }

    /// <summary>
    /// 多语言职级名称。
    /// </summary>
    [JsonPropertyName("i18n_name")]
    public List<I18nContent> I18nName { get; set; } = [];

    /// <summary>
    /// 多语言职级描述。
    /// </summary>
    [JsonPropertyName("i18n_description")]
    public List<I18nContent> I18nDescription { get; set; } = [];
}
