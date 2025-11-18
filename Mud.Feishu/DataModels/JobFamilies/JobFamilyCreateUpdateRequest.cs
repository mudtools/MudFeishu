namespace Mud.Feishu.DataModels.JobFamilies;

/// <summary>
/// 职位序列创建请求体。
/// </summary>
public class JobFamilyCreateUpdateRequest
{
    /// <summary>
    /// 序列名称，租户内唯一。取值支持中、英文及符号。
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// 序列描述，描述序列详情信息。字符长度上限为 5,000。
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// 上级序列 ID。如果需要为某一序列添加子序列，则需要传入该参数值。
    /// </summary>
    [JsonPropertyName("parent_job_family_id")]
    public string? ParentJobFamilyId { get; set; }

    /// <summary>
    /// 多语言序列描述。
    /// </summary>
    [JsonPropertyName("i18n_description")]
    public List<I18nContent> I18nDescription { get; set; } = [];

    /// <summary>
    /// 多语言序列名称。
    /// </summary>
    [JsonPropertyName("i18n_name")]
    public List<I18nContent> I18nName { get; set; } = [];

    /// <summary>
    /// 是否启用序列。
    /// </summary>
    [JsonPropertyName("status")]
    public bool Status { get; set; } = true;
}
