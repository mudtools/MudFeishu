// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Spark;

/// <summary>
/// 按天的 AI 额度消耗数据点（open_api_credit_usage_point）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Spark")]
public class CreditUsagePoint
{
    /// <summary>
    /// <para>数据点所在自然日的起始时间戳（该自然日 0 点），单位：秒</para>
    /// <para>必填：否</para>
    /// <para>示例值：1690848000</para>
    /// </summary>
    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; set; }

    /// <summary>
    /// <para>当日 AI 消耗量（总额度，含企业 + 个人）</para>
    /// <para>必填：否</para>
    /// <para>示例值：1250.5</para>
    /// </summary>
    [JsonPropertyName("credit_usage")]
    public double CreditUsage { get; set; }

    /// <summary>
    /// <para>当日企业额度消耗</para>
    /// <para>必填：否</para>
    /// <para>示例值：820.3</para>
    /// </summary>
    [JsonPropertyName("credit_usage_enterprise")]
    public double CreditUsageEnterprise { get; set; }

    /// <summary>
    /// <para>当日个人额度消耗</para>
    /// <para>必填：否</para>
    /// <para>示例值：430.2</para>
    /// </summary>
    [JsonPropertyName("credit_usage_personal")]
    public double CreditUsagePersonal { get; set; }
}
