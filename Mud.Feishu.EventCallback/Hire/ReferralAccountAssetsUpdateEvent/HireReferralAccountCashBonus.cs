// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Hire;

/// <summary>
/// 内推账户现金奖励信息。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class HireReferralAccountCashBonus
{
    /// <summary>
    /// <para>货币类型，可参考飞书开放平台枚举常量中的「货币（currency）枚举定义」</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("currency_type")]
    public string? CurrencyType { get; set; }

    /// <summary>
    /// <para>金额，保留两位小数</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("amount")]
    public double? Amount { get; set; }
}
