// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Mail;

namespace Mud.Feishu;


/// <summary>
/// 飞书邮箱邮件API接口实现了修改、查询、删除等邮箱邮件管理功能。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-folder/get"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Mail", InheritedFrom = nameof(FeishuV1MailMessage))]
[Token(FeishuTokenTypes.UserAccessToken, Name = Consts.Authorization)]
public interface IFeishuUserV1MailMessage : IFeishuV1MailMessage, ICurrentUserId
{
    /// <summary>
    /// 发送邮件。
    /// <para>发送邮件使用 base64url 编码。与普通 base64 的区别是将「+/」替换为「-_」。</para>
    /// <para><see href="https://open.feishu.cn/document/mail-v1/user_mailbox-message/get">接口文档</see></para>
    /// </summary>
    /// <param name="user_mailbox_id">
    /// <para>用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。</para>
    /// <para>示例值：user@example.com</para>
    /// </param>
    /// <param name="request">发送用户邮箱邮件请求对象，包含邮件主题、正文、收件人等信息。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/messages/send")]
    Task<FeishuApiResult<SendUserMailboxMessageResult>?> SendUserMailboxMessageAsync(
      [Path] string user_mailbox_id,
      [Body] SendUserMailboxMessageRequest request,
      CancellationToken cancellationToken = default);
}
