// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.AI;


/// <summary>
/// <para>银行信息</para>
/// </summary>
public class ContractBankInfo
{
    /// <summary>
    /// <para>甲乙方信息类型</para>
    /// <para>必填：否</para>
    /// <para>示例值：buy</para>
    /// <para>可选值：<list type="bullet">
    /// <item>buy_bank：甲方银行</item>
    /// <item>sell_bank：乙方银行</item>
    /// <item>third_bank：第三方银行</item>
    /// <item>unceratin_bank：其他方银行</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("bank_type")]
    public string? BankType { get; set; }

    /// <summary>
    /// <para>值</para>
    /// <para>必填：否</para>
    /// <para>示例值：value</para>
    /// </summary>
    [JsonPropertyName("value")]
    public ContractBankEntity? Value { get; set; }

}