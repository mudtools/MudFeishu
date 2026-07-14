// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.AI;


/// <summary>
/// <para>值</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "AI")]
public class ContractBankEntity
{
    /// <summary>
    /// <para>账户名</para>
    /// <para>必填：否</para>
    /// <para>示例值：北京字节跳动网络技术有限公司</para>
    /// </summary>
    [JsonPropertyName("account_name")]
    public string? AccountName { get; set; }

    /// <summary>
    /// <para>银行名称</para>
    /// <para>必填：否</para>
    /// <para>示例值：中国A银行B支行</para>
    /// </summary>
    [JsonPropertyName("bank_name")]
    public string? BankName { get; set; }

    /// <summary>
    /// <para>账户ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：11230xxxxx004701</para>
    /// </summary>
    [JsonPropertyName("account_number")]
    public string? AccountNumber { get; set; }

    /// <summary>
    /// <para>电话</para>
    /// <para>必填：否</para>
    /// <para>示例值：010-8xxxx688</para>
    /// </summary>
    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    /// <summary>
    /// <para>联系人</para>
    /// <para>必填：否</para>
    /// <para>示例值：张三</para>
    /// </summary>
    [JsonPropertyName("contacts")]
    public string? Contacts { get; set; }

    /// <summary>
    /// <para>传真号码</para>
    /// <para>必填：否</para>
    /// <para>示例值：911101xxxxx684235</para>
    /// </summary>
    [JsonPropertyName("tax_number")]
    public string? TaxNumber { get; set; }

    /// <summary>
    /// <para>联系地址</para>
    /// <para>必填：否</para>
    /// <para>示例值：A市B区C园D楼3-8</para>
    /// </summary>
    [JsonPropertyName("address")]
    public string? Address { get; set; }

    /// <summary>
    /// <para>id号</para>
    /// <para>必填：否</para>
    /// <para>示例值：11230xxxxx004701</para>
    /// </summary>
    [JsonPropertyName("id_number")]
    public string? IdNumber { get; set; }

    /// <summary>
    /// <para>邮箱</para>
    /// <para>必填：否</para>
    /// <para>示例值：zhangsan.1111@bytedance.com</para>
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }
}
