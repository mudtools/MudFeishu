// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Performance;

/// <summary>
/// 指标模板中被评估人添加指标的设置
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Performance")]
public class PerformanceCustomMetricConfig
{
    /// <summary>
    /// <para>非指标库指标的评分方式（手动评分是 0；评分公式是具体的公式 ID）</para>
    /// <para>示例值：7296701873237786643</para>
    /// </summary>
    [JsonPropertyName("default_formula_id")]
    public string? DefaultFormulaId { get; set; }

    /// <summary>
    /// <para>最少需添加的指标数量</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("least_metrics_size")]
    public int? LeastMetricsSize { get; set; }

    /// <summary>
    /// <para>添加指标的方式：1（可选用指标库的指标）/ 2（可选用自定义的指标）</para>
    /// </summary>
    [JsonPropertyName("add_metric_options")]
    public int[]? AddMetricOptions { get; set; }
}
