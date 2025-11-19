using System.Text.Json.Serialization;

namespace Mud.Feishu.DataModels.WorkCites;

/// <summary>
/// 飞书工作城市API返回结果包装类
/// </summary>
/// <remarks>
/// 该类用于封装从飞书API获取的工作城市信息结果，作为API响应的标准包装格式。
/// 主要用于 GetTenantWorkCityById 和 GetUserWorkCityById 接口的返回值。
/// 提供统一的数据结构，便于客户端解析和处理工作城市信息。
/// </remarks>
public class WorkCityResult
{
    /// <summary>
    /// 工作城市信息对象
    /// </summary>
    /// <value>
    /// 包含工作城市的详细信息，包括工作城市ID、名称、多语言名称和启用状态等。
    /// 该对象封装了完整的工作城市属性信息，可用于显示和管理。
    /// </value>
    [JsonPropertyName("work_city")]
    public WorkCity? WorkCity { get; set; }
}
