// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Auth;

/// <summary>
/// 撤销用户授权事件
/// <para>当用户 user_access_token 或 refresh_token 被撤销后，会触发此事件。</para>
/// <para>事件类型:auth.user_access_token.revoked_v4</para>
/// <para>使用时请继承：<see cref="AuthUserAccessTokenRevokedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/authentication-management/auth-v4/user_access_token/events/revoked</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.AuthUserAccessTokenRevoked, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "Auth")]
public class AuthUserAccessTokenRevokedResult : IEventResult
{
    /// <summary>
    /// <para>撤销 token 的类型。可能值有：</para>
    /// <para>- `user_access_token `：仅撤销 user_access_token</para>
    /// <para>- `refresh_token `：仅撤销 refresh_token</para>
    /// <para>- `user_access_token refresh_token`：	user_access_token 和 refresh_token 都被撤销</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 长度范围：`0` ～ `10000` 字符</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("revoke_token_type")]
    public string? RevokeTokenType { get; set; }

    /// <summary>
    /// <para>撤销 token 的原因。可能值有：</para>
    /// <para>- `Revoked by unknown action.`：未知动作，缺省值</para>
    /// <para>- `Revoked by user action.`：被用户撤销</para>
    /// <para>- `Revoked by administrator action.`：被管理员撤销</para>
    /// <para>- `Revoked by security and risk control action.`：因风控和安全被撤销</para>
    /// <para>- `Revoked by restriction action.`：因限制动作被撤销，包括用户账号冻结、离职、应用不可用、用户对应用无权限等</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 长度范围：`0` ～ `10000` 字符</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("revoke_reason")]
    public string? RevokeReason { get; set; }

    /// <summary>
    /// <para>用户 open_id</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("open_id")]
    public string? OpenId { get; set; }

    /// <summary>
    /// <para>用户 union_id</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("union_id")]
    public string? UnionId { get; set; }

    /// <summary>
    /// <para>租户内用户的唯一标识</para>
    /// <para>**字段权限要求**：</para>
    /// <para>获取用户 user ID(contact:user.employee_id:readonly)</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }
}
