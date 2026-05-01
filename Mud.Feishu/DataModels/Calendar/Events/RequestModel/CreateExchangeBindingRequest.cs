// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Calendar;

/// <summary>
/// 将 Exchange 账户绑定到飞书账户请求体
/// </summary>
public class CreateExchangeBindingRequest
{
    /// <summary>
    /// <para>Exchange 的 admin 账户。</para>
    /// <para>必填：否</para>
    /// <para>示例值：email_admin_example@outlook.com</para>
    /// <para>最大长度：500</para>
    /// <para>最小长度：1</para>
    /// </summary>
    [JsonPropertyName("admin_account")]
    public string? AdminAccount { get; set; }

    /// <summary>
    /// <para>需绑定的 Exchange 账户。</para>
    /// <para>必填：否</para>
    /// <para>示例值：email_account_example@outlook.com</para>
    /// <para>最大长度：500</para>
    /// <para>最小长度：1</para>
    /// </summary>
    [JsonPropertyName("exchange_account")]
    public string? ExchangeAccount { get; set; }

    /// <summary>
    /// <para>用户 ID，即 Exchange 账户绑定的飞书账户 ID。关于用户 ID 可参见[用户相关的 ID 概念](https://open.feishu.cn/document/home/user-identity-introduction/introduction)。</para>
    /// <para>必填：否</para>
    /// <para>示例值：ou_xxxxxxxxxxxxxxxxxx</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }
}