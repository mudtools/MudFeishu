// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Spark;

/// <summary>
/// 单个运营指标对象（open_api_analytics_metric，含本区间值、上一等长区间值与环比）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Spark")]
public class AnalyticsMetric
{
    /// <summary>
    /// <para>本区间指标值</para>
    /// <para>必填：否</para>
    /// <para>示例值：12500</para>
    /// </summary>
    [JsonPropertyName("value")]
    public string? Value { get; set; }

    /// <summary>
    /// <para>上一等长区间的指标值（环比基准）</para>
    /// <para>必填：否</para>
    /// <para>示例值：15000</para>
    /// </summary>
    [JsonPropertyName("prev_value")]
    public string? PrevValue { get; set; }

    /// <summary>
    /// <para>环比绝对变化 = value - prev_value</para>
    /// <para>必填：否</para>
    /// <para>示例值：-2500</para>
    /// </summary>
    [JsonPropertyName("diff")]
    public string? Diff { get; set; }

    /// <summary>
    /// <para>环比变化率（小数，如 -0.714 表示下降 71.4%）；prev_value 为 0 时为 0</para>
    /// <para>必填：否</para>
    /// <para>示例值：-0.1667</para>
    /// </summary>
    [JsonPropertyName("ratio")]
    public double Ratio { get; set; }
}
