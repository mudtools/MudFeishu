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


    /// <summary>
    /// 获取邮件会话详情。
    /// <para>获取指定邮件会话下的邮件列表，包含邮件元数据及主题、正文等内容。支持获取会话中位于垃圾邮件文件夹和已删除文件夹的邮件。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-thread/get">接口文档</see></para>
    /// </summary>
    /// <param name="user_mailbox_id">
    /// <para>用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。</para>
    /// <para>示例值：user@example.com</para>
    /// </param>
    /// <param name="thread_id">
    /// <para>邮件会话ID。可通过发送邮件、回复邮件的接口返回值或获取邮件详情接口查询获得。</para>
    /// <para>示例值：th_xxxxxxxxxxxx</para>
    /// </param>
    /// <param name="format">
    /// <para>需要获取的邮件内容。支持选择full/plain_text_full/metadata</para>
    /// <para>示例值：full</para>
    /// <list type="bullet">
    /// <item>full：全文，包括标签、文件夹、主题、收发件人、纯文本、HTML等信息</item>
    /// <item>plain_text_full：全文，只返回纯文本正文内容，不返回HTML。返回内容包括标签、文件夹、主题、收发件人、纯文本等信息</item>
    /// <item>metadata：邮件元数据信息，包括标签、文件夹、主题、收发件人、摘要等信息，不返回正文内容</item>
    /// </list>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="include_spam_trash">
    /// <para>获取包含来自 SPAM 和 TRASH 的邮件</para>
    /// <para>示例值：true</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/threads/{thread_id}")]
    Task<FeishuApiResult<GetUserMailboxThreadResult>?> GetUserMailboxThreadAsync(
        [Path] string user_mailbox_id,
        [Path] string thread_id,
        [Query] string? format = null,
        [Query] bool? include_spam_trash = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 分页列出邮件会话。
    /// <para>分页列出用户指定文件夹或标签下的邮件会话，按时间倒序分页获取。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-thread/list">接口文档</see></para>
    /// </summary>
    /// <param name="user_mailbox_id">
    /// <para>用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。</para>
    /// <para>示例值：user@example.com</para>
    /// </param>
    /// <param name="folder_id">
    /// <para>文件夹 id，支持INBOX、SENT、SPAM、ARCHIVED、SCHEDULED、TRASH、DRAFT以及自定义文件夹ID</para>
    /// <para>示例值：INBOX 或者用户文件夹 id</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="only_unread">
    /// <para>是否只查询未读会话</para>
    /// <para>示例值：true</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="label_id">
    /// <para>标签id，支持IMPORTANT、OTHER、FLAGGED以及自定义标签ID</para>
    /// <para>示例值：FLAGGED</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="page_size">分页大小，即本次请求所返回的信息列表内的最大条目数。默认值：20</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/threads")]
    Task<FeishuApiPageListResult<MailThreadInfo>?> GetUserMailboxThreadPageListAsync(
        [Path] string user_mailbox_id,
        [Query] string? folder_id = null,
        [Query] bool? only_unread = null,
        [Query] string? label_id = null,
        [Query] int page_size = Consts.PageSize_20,
        [Query] string? page_token = null,
        CancellationToken cancellationToken = default);
}
