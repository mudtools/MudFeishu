// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Mail;

namespace Mud.Feishu.Interfaces;


/// <summary>
/// 飞书邮箱邮件API接口实现了修改、查询、删除等邮箱邮件管理功能。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-folder/get"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token("TenantAccessToken", Name = Consts.Authorization)]
public interface IFeishuV1MailMessage : IFeishuAppContextSwitcher
{

    /// <summary>
    /// 批量删除邮件。
    /// <para>批量将邮件移动到已删除文件夹。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-message/batch_trash">接口文档</see></para>
    /// </summary>
    /// <param name="user_mailbox_id">
    /// <para>用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。</para>
    /// <para>示例值：user@example.com</para>
    /// </param>
    /// <param name="request">批量删除用户邮箱邮件请求对象，包含待删除的邮件ID列表。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/messages/batch_trash")]
    Task<FeishuNullDataApiResult?> BatchTrashUserMailboxMessageAsync(
         [Path] string user_mailbox_id,
         [Body] BatchTrashUserMailboxMessageRequest request,
         CancellationToken cancellationToken = default);



    /// <summary>
    /// 批量修改邮件。
    /// <para>批量修改邮件标签、所属文件夹、已读未读状态，可进行加旗标、归档、移至垃圾邮件等操作。不支持移入邮件进入已删除文件夹，如需，请使用批量删除邮件接口。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-message/batch_modify">接口文档</see></para>
    /// </summary>
    /// <param name="user_mailbox_id">
    /// <para>用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。</para>
    /// <para>示例值：user@example.com</para>
    /// </param>
    /// <param name="request">批量修改用户邮箱邮件请求对象，包含待修改的邮件ID列表和需要修改的属性信息。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/messages/batch_modify")]
    Task<FeishuNullDataApiResult?> BatchModifyUserMailboxMessageAsync(
        [Path] string user_mailbox_id,
        [Body] BatchModifyUserMailboxMessageRequest request,
        CancellationToken cancellationToken = default);



    /// <summary>
    /// 删除邮件。
    /// <para>移动邮件到已删除文件夹。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-message/trash">接口文档</see></para>
    /// </summary>
    /// <param name="user_mailbox_id">
    /// <para>用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。</para>
    /// <para>示例值：user@example.com</para>
    /// </param>
    /// <param name="message_id">
    /// <para>邮件ID，可通过列出邮件接口获得</para>
    /// <para>示例值：NzR3Zkd5NGhBTS9NVkZnSklidDVGT3VoQmM4PQ==</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/messages/{message_id}/trash")]
    Task<FeishuNullDataApiResult?> DeleteUserMailboxMessageAsync(
       [Path] string user_mailbox_id,
       [Path] string message_id,
       CancellationToken cancellationToken = default);


    /// <summary>
    /// 修改邮件。
    /// <para>修改邮件标签、所属文件夹、已读未读状态，可为邮件添加旗标、归档、移入垃圾邮件等操作。不支持移动邮件到已删除文件夹，如需，请使用删除邮件接口。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-message/modify">接口文档</see></para>
    /// </summary>
    /// <param name="user_mailbox_id">
    /// <para>用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。</para>
    /// <para>示例值：user@example.com</para>
    /// </param>
    /// <param name="message_id">
    /// <para>邮件ID，可通过列出邮件接口获得</para>
    /// <para>示例值：NzR3Zkd5NGhBTS9NVkZnSklidDVGT3VoQmM4PQ==</para>
    /// </param>
    /// <param name="request">修改用户邮箱邮件请求对象，包含需要修改的属性信息。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Put("/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/messages/{message_id}/modify")]
    Task<FeishuNullDataApiResult?> ModifyUserMailboxMessageAsync(
       [Path] string user_mailbox_id,
       [Path] string message_id,
       [Body] ModifyUserMailboxMessageRequest request,
       CancellationToken cancellationToken = default);


    /// <summary>
    /// 批量获取邮件详情。
    /// <para>通过指定邮件ID，获取对应邮件的标签、文件夹、摘要、正文、html、附件等信息。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-message/batch_get">接口文档</see></para>
    /// </summary>
    /// <param name="user_mailbox_id">
    /// <para>用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。</para>
    /// <para>示例值：user@example.com</para>
    /// </param>
    /// <param name="request">批量获取用户邮箱邮件请求对象，包含待获取的邮件ID列表。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/messages/batch_get")]
    Task<FeishuApiResult<BatchGetUserMailboxMessageResult>?> BatchGetUserMailboxMessageAsync(
      [Path] string user_mailbox_id,
      [Body] BatchGetUserMailboxMessageRequest request,
      CancellationToken cancellationToken = default);
}
