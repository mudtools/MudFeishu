// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Card;

/// <summary>
/// 卡片事件回调发起者信息。
/// </summary>
/// <remarks>
/// <para><c>user_id</c> 字段仅在应用开启「获取用户 ID」权限
/// （<c>contact:user.employee_id:readonly</c>）后返回。</para>
/// </remarks>
[HttpJsonSerializable(SerializerClassName = "Card")]
public class CardActionOperator
{
    /// <summary>
    /// <para>回调发起者租户 key</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("tenant_key")]
    public string? TenantKey { get; set; }

    /// <summary>
    /// <para>回调发起者 User ID</para>
    /// <para>**字段权限要求**：获取用户 ID(contact:user.employee_id:readonly)</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// <para>回调发起者 Open ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("open_id")]
    public string? OpenId { get; set; }

    /// <summary>
    /// <para>回调发起者 Union ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("union_id")]
    public string? UnionId { get; set; }
}
