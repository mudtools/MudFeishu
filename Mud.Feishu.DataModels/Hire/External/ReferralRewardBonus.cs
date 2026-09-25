// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 内推奖励额度（bonus_amount）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class ReferralRewardBonus
{
    /// <summary>
    /// <para>奖励发放形式：1 积分 / 2 现金</para>
    /// <para>必填：是</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("bonus_type")]
    public int? BonusType { get; set; }

    /// <summary>
    /// <para>积分奖励数额（bonus_type 为 1 积分时必填）</para>
    /// <para>必填：否</para>
    /// <para>示例值：100</para>
    /// </summary>
    [JsonPropertyName("point_bonus")]
    public long? PointBonus { get; set; }

    /// <summary>
    /// <para>现金奖励（bonus_type 为 2 现金时必填）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("cash")]
    public ReferralRewardCash? Cash { get; set; }
}
