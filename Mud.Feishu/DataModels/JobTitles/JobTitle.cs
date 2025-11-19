// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

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