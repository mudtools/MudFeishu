// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.AI;

/// <summary>
/// 提取文件中的合同字段响应体
/// </summary>
public class RecognizeContractFieldResult
{
    /// <summary>
    /// <para>文件的唯一id</para>
    /// <para>必填：否</para>
    /// <para>示例值：121345678</para>
    /// </summary>
    [JsonPropertyName("file_id")]
    public string? FileId { get; set; }

    /// <summary>
    /// <para>总交易金额</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("price")]
    public ExtractPrice? Price { get; set; }



    /// <summary>
    /// <para>期限相关信息，包括开始日期、结束日期、有效时长</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("time")]
    public ExtractTime? Time { get; set; }


    /// <summary>
    /// <para>盖章份数</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("copy")]
    public ExtractCopy? Copy { get; set; }


    /// <summary>
    /// <para>币种</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("currency")]
    public ExtractCurrency? Currency { get; set; }


    /// <summary>
    /// <para>合同标题</para>
    /// <para>必填：否</para>
    /// <para>示例值：项目活动框架协议</para>
    /// </summary>
    [JsonPropertyName("header")]
    public string? Header { get; set; }

    /// <summary>
    /// <para>主体信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("body_info")]
    public ContractBodyInfo[]? BodyInfos { get; set; }


    /// <summary>
    /// <para>银行信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("bank_info")]
    public ContractBankInfo[]? BankInfos { get; set; }

}
