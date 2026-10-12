// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Application;

/// <summary>
/// 获取应用使用概览响应体数据。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class GetAppUsageOverviewResult
{
    /// <summary>
    /// <para>指标列表，包括应用的活跃用户数、累计用户数、新增用户数、访问页面数、打开次数</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("items")]
    public List<AppUsageMetricItem>? Items { get; set; }
}

/// <summary>
/// 获取消息推送概览响应体数据。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class GetMessagePushOverviewResult
{
    /// <summary>
    /// <para>指标列表，包括消息推送给用户的次数、消息触达的人数、消息 1 小时阅读量、消息 12 小时阅读量</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("items")]
    public List<AppUsageMetricItem>? Items { get; set; }
}

/// <summary>
/// 应用使用指标条目。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class AppUsageMetricItem
{
    /// <summary>
    /// <para>指标名称</para>
    /// <para>必填：是</para>
    /// <para>示例值：app_active_user_cnt</para>
    /// </summary>
    [JsonPropertyName("metric_name")]
    public string? MetricName { get; set; }

    /// <summary>
    /// <para>指标值</para>
    /// <para>必填：是</para>
    /// <para>示例值：100</para>
    /// </summary>
    [JsonPropertyName("metric_value")]
    public int? MetricValue { get; set; }
}
