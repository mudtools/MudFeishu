// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

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
