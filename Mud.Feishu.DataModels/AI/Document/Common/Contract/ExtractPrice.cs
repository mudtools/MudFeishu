// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.AI;

/// <summary>
/// <para>总交易金额</para>
/// </summary>
public class ExtractPrice
{
    /// <summary>
    /// <para>交易金额</para>
    /// <para>必填：否</para>
    /// <para>示例值：200000</para>
    /// </summary>
    [JsonPropertyName("contract_price")]
    public float? ContractPrice { get; set; }

    /// <summary>
    /// <para>从原文中抽取的交易金额</para>
    /// <para>必填：否</para>
    /// <para>示例值："200000"</para>
    /// </summary>
    [JsonPropertyName("contract_price_original")]
    public string? ContractPriceOriginal { get; set; }

    /// <summary>
    /// <para>原文中描述交易金额的文字</para>
    /// <para>必填：否</para>
    /// <para>示例值：本合同项下总金额共计￥200000（贰拾万元整）</para>
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}