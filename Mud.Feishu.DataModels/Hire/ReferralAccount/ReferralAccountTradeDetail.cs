// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 内推账户交易明细（trade_detail），用于对账时上报调用方系统的账户积分变动
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class ReferralAccountTradeDetail
{
    /// <summary>
    /// <para>账户 ID，通过注册内推账户接口生成</para>
    /// <para>必填：是</para>
    /// <para>示例值：6930815272790114324</para>
    /// </summary>
    [JsonPropertyName("account_id")]
    public string? AccountId { get; set; }

    /// <summary>
    /// <para>时间段内该账户在积分商城的实际充值金额</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("total_recharge_reward_info")]
    public ReferralAccountBonusAmount? TotalRechargeRewardInfo { get; set; }
}
