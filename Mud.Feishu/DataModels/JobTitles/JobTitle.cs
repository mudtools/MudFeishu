using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Mud.Feishu.DataModels.JobTitles;

/// <summary>
/// 飞书职务信息模型
/// </summary>
/// <remarks>
/// 职务是用户属性之一，通过职务 API 仅支持查询职务信息。
/// 该模型包含职务的基本信息，包括职务ID、名称、多语言名称以及启用状态。
/// </remarks>
public class JobTitle
{
    /// <summary>
    /// 职务ID
    /// </summary>
    /// <value>
    /// 职务的唯一标识符，用于获取和管理特定职务信息。
    /// </value>
    [JsonPropertyName("job_title_id")]
    public string? JobTitleId { get; set; }

    /// <summary>
    /// 职务名称
    /// </summary>
    /// <value>
    /// 职务的显示名称，通常为主要语言的职务名称。
    /// 示例值："软件工程师"、"产品经理"等。
    /// </value>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// 职务的多语言名称
    /// </summary>
    /// <value>
    /// 包含职务在不同语言环境下的显示名称列表。
    /// 用于国际化支持，每个元素包含语言代码和对应的职务名称。
    /// </value>
    [JsonPropertyName("i18n_name")]
    public List<I18nContent> I18nName { get; set; } = [];

    /// <summary>
    /// 职务启用状态
    /// </summary>
    /// <value>
    /// true表示职务已启用，false表示职务已禁用。
    /// 只有启用状态的职务才能分配给用户。
    /// </value>
    [JsonPropertyName("status")]
    public bool Status { get; set; }
}