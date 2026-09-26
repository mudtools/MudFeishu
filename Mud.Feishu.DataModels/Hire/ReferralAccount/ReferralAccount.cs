// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 内推账户信息（account）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class ReferralAccount
{
    /// <summary>
    /// <para>账户 ID，注册内推账户后获取</para>
    /// <para>必填：否</para>
    /// <para>示例值：6942778198054125570</para>
    /// </summary>
    [JsonPropertyName("account_id")]
    public string? AccountId { get; set; }

    /// <summary>
    /// <para>账户余额信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("assets")]
    public ReferralAccountAssets? Assets { get; set; }

    /// <summary>
    /// <para>账户状态：1 可用 / 2 已停用</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("status")]
    public int? Status { get; set; }

    /// <summary>
    /// <para>账户绑定的内推人信息，仅「查询内推账户」接口返回</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("referrer")]
    public ReferralAccountReferrer? Referrer { get; set; }
}
