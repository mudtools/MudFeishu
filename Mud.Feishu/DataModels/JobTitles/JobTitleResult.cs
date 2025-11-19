using System.Text.Json.Serialization;

namespace Mud.Feishu.DataModels.JobTitles;

/// <summary>
/// 飞书职务API返回结果包装类
/// </summary>
/// <remarks>
/// 该类用于封装从飞书API获取的职务信息结果，作为API响应的标准包装格式。
/// 主要用于 GetTenantJobTitleById 和 GetUserJobTitleById 接口的返回值。
/// </remarks>
public class JobTitleResult
{
    /// <summary>
    /// 职务信息对象
    /// </summary>
    /// <value>
    /// 包含职务的详细信息，包括职务ID、名称、多语言名称和启用状态等。
    /// </value>
    [JsonPropertyName("job_title")]
    public JobTitle? JobTitle { get; set; }
}
