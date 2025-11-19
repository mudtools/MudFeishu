using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Mud.Feishu.DataModels.WorkCites;

/// <summary>
/// 飞书工作城市信息模型
/// </summary>
/// <remarks>
/// 工作城市是用户属性之一，通过工作城市 API 仅支持查询工作城市信息。
/// 该模型包含工作城市的基本信息，包括工作城市ID、名称、多语言名称以及启用状态。
/// 工作城市通常用于标识员工的主要工作地点，便于企业进行人员管理和区域统计。
/// </remarks>
public class WorkCity
{
    /// <summary>
    /// 工作城市ID
    /// </summary>
    /// <value>
    /// 工作城市的唯一标识符，用于获取和管理特定工作城市信息。
    /// 通常为系统生成的唯一字符串。
    /// </value>
    [JsonPropertyName("work_city_id")]
    public string? WorkCityId { get; set; }

    /// <summary>
    /// 工作城市名称
    /// </summary>
    /// <value>
    /// 工作城市的显示名称，通常为主要语言的城市名称。
    /// 示例值："北京"、"上海"、"深圳"、"广州"等。
    /// </value>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// 工作城市的多语言名称
    /// </summary>
    /// <value>
    /// 包含工作城市在不同语言环境下的显示名称列表。
    /// 用于国际化支持，每个元素包含语言代码和对应的城市名称。
    /// 例如：{"zh_cn": "北京", "en": "Beijing"}。
    /// </value>
    [JsonPropertyName("i18n_name")]
    public List<I18nContent> I18nName { get; set; } = [];

    /// <summary>
    /// 工作城市启用状态
    /// </summary>
    /// <value>
    /// true表示工作城市已启用，false表示工作城市已禁用。
    /// 只有启用状态的工作城市才能分配给用户作为工作地点。
    /// 禁用的工作城市可能已不再使用或仅用于历史数据记录。
    /// </value>
    [JsonPropertyName("status")]
    public bool Status { get; set; }
}