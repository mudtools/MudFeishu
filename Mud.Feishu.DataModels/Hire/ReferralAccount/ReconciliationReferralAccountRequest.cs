// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 内推账户提现数据对账请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class ReconciliationReferralAccountRequest
{
    /// <summary>
    /// <para>对账时段的起始交易时间，毫秒时间戳</para>
    /// <para>必填：是</para>
    /// <para>示例值：1685416831621</para>
    /// </summary>
    [JsonPropertyName("start_trans_time")]
    public string? StartTransTime { get; set; }

    /// <summary>
    /// <para>对账时段的截止交易时间，毫秒时间戳</para>
    /// <para>必填：是</para>
    /// <para>示例值：1685416831622</para>
    /// </summary>
    [JsonPropertyName("end_trans_time")]
    public string? EndTransTime { get; set; }

    /// <summary>
    /// <para>交易信息，即调用方系统的内推账户积分变动信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("trade_details")]
    public ReferralAccountTradeDetail[]? TradeDetails { get; set; }
}
