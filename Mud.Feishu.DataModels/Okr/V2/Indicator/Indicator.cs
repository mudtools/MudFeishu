// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.OkrV2;

/// <summary>
/// OKR v2 指标信息
/// </summary>
public class Indicator
{
    /// <summary>
    /// <para>指标 id</para>
    /// <para>示例值：7342342398472398473</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>指标创建时间（毫秒时间戳）</para>
    /// <para>示例值：1760604634563</para>
    /// </summary>
    [JsonPropertyName("create_time")]
    public string? CreateTime { get; set; }

    /// <summary>
    /// <para>指标更新时间（毫秒时间戳）</para>
    /// <para>示例值：1760604634563</para>
    /// </summary>
    [JsonPropertyName("update_time")]
    public string? UpdateTime { get; set; }

    /// <summary>
    /// <para>指标归属者</para>
    /// </summary>
    [JsonPropertyName("owner")]
    public Owner? Owner { get; set; }

    /// <summary>
    /// <para>指标所属实体类型：2 Objective、3 Key Result</para>
    /// <para>示例值：2</para>
    /// </summary>
    [JsonPropertyName("entity_type")]
    public int? EntityType { get; set; }

    /// <summary>
    /// <para>指标所属实体 id</para>
    /// <para>示例值：7342342398472398473</para>
    /// </summary>
    [JsonPropertyName("entity_id")]
    public string? EntityId { get; set; }

    /// <summary>
    /// <para>指标状态：-1 未定义、0 正常、1 有风险、2 已延期</para>
    /// <para>示例值：0</para>
    /// </summary>
    [JsonPropertyName("indicator_status")]
    public int? IndicatorStatus { get; set; }

    /// <summary>
    /// <para>指标状态计算方式：0 手动更新、1 根据进度和当前时间自动更新、2 根据风险最高的 Key Result 状态更新</para>
    /// <para>示例值：0</para>
    /// </summary>
    [JsonPropertyName("status_calculate_type")]
    public int? StatusCalculateType { get; set; }

    /// <summary>
    /// <para>指标起始值</para>
    /// <para>示例值：0</para>
    /// </summary>
    [JsonPropertyName("start_value")]
    public double? StartValue { get; set; }

    /// <summary>
    /// <para>指标目标值</para>
    /// <para>示例值：0</para>
    /// </summary>
    [JsonPropertyName("target_value")]
    public double? TargetValue { get; set; }

    /// <summary>
    /// <para>指标当前值</para>
    /// <para>示例值：0</para>
    /// </summary>
    [JsonPropertyName("current_value")]
    public double? CurrentValue { get; set; }

    /// <summary>
    /// <para>指标当前值计算方式：0 手动更新、1 根据 Key Result 进度自动更新、2 根据拆解的 Key Result 进度更新</para>
    /// <para>示例值：0</para>
    /// </summary>
    [JsonPropertyName("current_value_calculate_type")]
    public int? CurrentValueCalculateType { get; set; }

    /// <summary>
    /// <para>指标单位</para>
    /// </summary>
    [JsonPropertyName("unit")]
    public IndicatorUnit? Unit { get; set; }
}
