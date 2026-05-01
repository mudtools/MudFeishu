// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Calendar;

/// <summary>
/// 将 Exchange 账户绑定到飞书账户响应体
/// </summary>
public class CreateExchangeBindingResult
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
    /// <para>用户 ID，即 Exchange 账户绑定的飞书账户 ID。</para>
    /// <para>必填：否</para>
    /// <para>示例值：ou_xxxxxxxxxxxxxxxxxx</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// <para>Exchange 账户的同步状态。</para>
    /// <para>必填：否</para>
    /// <para>示例值：doing</para>
    /// <para>可选值：<list type="bullet">
    /// <item>doing：日历正在同步</item>
    /// <item>cal_done：日历同步完成</item>
    /// <item>timespan_done：近期时间段同步完成</item>
    /// <item>done：日程同步完成</item>
    /// <item>err：同步错误</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// <para>Exchange 绑定的唯一标识 ID，是 admin 账户、Exchange 账户、用户三元组的唯一标识 ID。你可以通过该 ID 查询绑定关系、日历同步状态，或者解除绑定关系。</para>
    /// <para>必填：是</para>
    /// <para>示例值：ZW1haWxfYWRtaW5fZXhhbXBsZUBvdXRsb29rLmNvbSBlbWFpbF9hY2NvdW50X2V4YW1wbGVAb3V0bG9vay5jb20=</para>
    /// </summary>
    [JsonPropertyName("exchange_binding_id")]
    public string ExchangeBindingId { get; set; } = string.Empty;
}