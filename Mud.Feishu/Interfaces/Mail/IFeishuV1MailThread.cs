// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Mail;

namespace Mud.Feishu.Interfaces;


/// <summary>
/// 飞书邮件会话API接口实现了修改、查询、删除等邮件模板功能。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-thread/batch_trash"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token("TenantAccessToken", Name = Consts.Authorization)]
public interface IFeishuV1MailThread : IFeishuAppContextSwitcher
{

    /// <summary>
    /// 批量删除邮件会话。
    /// <para>批量将指定的邮件会话移入已删除文件夹。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-thread/batch_trash">接口文档</see></para>
    /// </summary>
    /// <param name="user_mailbox_id">
    /// <para>用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。</para>
    /// <para>示例值：user@example.com</para>
    /// </param>
    /// <param name="batchTrashUserMailboxThreadRequest">批量删除邮件会话请求对象，包含待删除的邮件会话 ID 列表。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/threads/batch_trash")]
    Task<FeishuNullDataApiResult?> BatchTrashUserMailboxThreadAsync(
      [Path] string user_mailbox_id,
      [Body] BatchTrashUserMailboxThreadRequest batchTrashUserMailboxThreadRequest,
      CancellationToken cancellationToken = default);



    /// <summary>
    /// 批量修改邮件会话。
    /// <para>批量修改邮件会话的标签、所属文件夹和已读未读状态，支持为邮件会话添加旗标、归档、移入垃圾邮件文件夹。</para>
    /// <para>注意，接口不支持将邮件会话移入已删除文件夹，如需，请使用批量删除邮件会话接口。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-thread/batch_modify">接口文档</see></para>
    /// </summary>
    /// <param name="user_mailbox_id">
    /// <para>用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。</para>
    /// <para>示例值：user@example.com</para>
    /// </param>
    /// <param name="batchModifyUserMailboxThreadRequest">批量修改邮件会话请求对象，包含待修改的邮件会话 ID 列表。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/threads/batch_modify")]
    Task<FeishuNullDataApiResult?> BatchModifyUserMailboxThreadAsync(
     [Path] string user_mailbox_id,
     [Body] BatchModifyUserMailboxThreadRequest batchModifyUserMailboxThreadRequest,
     CancellationToken cancellationToken = default);


    /// <summary>
    /// 删除邮件会话。
    /// <para>将指定的邮件会话移入已删除文件夹</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-thread/trash">接口文档</see></para>
    /// </summary>
    /// <param name="user_mailbox_id">
    /// <para>用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。</para>
    /// <para>示例值：user@example.com</para>
    /// </param>
    /// <param name="thread_id">
    /// <para>邮件会话ID。可通过发送邮件、回复邮件的接口返回值或获取邮件详情接口查询获得。</para>
    /// <para>示例值：th_xxxxxxxxxxxx</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/threads/{thread_id}/trash")]
    Task<FeishuNullDataApiResult?> TrashUserMailboxThreadAsync(
        [Path] string user_mailbox_id,
        [Path] string thread_id,
        CancellationToken cancellationToken = default);



    /// <summary>
    /// 修改邮件会话。
    /// <para>修改邮件会话的标签、所属文件夹和已读未读状态，支持为邮件会话添加旗标、归档、移入垃圾邮件文件夹。</para>
    /// <para>注意，接口不支持将邮件会话移入已删除文件夹，如需，请使用删除邮件会话接口。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-thread/modify">接口文档</see></para>
    /// </summary>
    /// <param name="user_mailbox_id">
    /// <para>用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。</para>
    /// <para>示例值：user@example.com</para>
    /// </param>
    /// <param name="thread_id">
    /// <para>邮件会话ID。可通过发送邮件、回复邮件的接口返回值或获取邮件详情接口查询获得。</para>
    /// <para>示例值：th_xxxxxxxxxxxx</para>
    /// </param>
    /// <param name="modifyUserMailboxThreadRequest">修改邮件会话请求对象，包含待修改的邮件会话信息。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/threads/{thread_id}/modify")]
    Task<FeishuNullDataApiResult?> ModifyUserMailboxThreadAsync(
         [Path] string user_mailbox_id,
         [Path] string thread_id,
         [Body] ModifyUserMailboxThreadRequest modifyUserMailboxThreadRequest,
         CancellationToken cancellationToken = default);
}
