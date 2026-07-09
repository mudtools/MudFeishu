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
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
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


    /// <summary>
    /// 查询会话下邮件信息。
    /// <para>通过用户邮箱地址和邮件会话ID，获取该会话下的所有邮件关键信息列表。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-message/list_thread_message">接口文档</see></para>
    /// </summary>
    /// <param name="user_mailbox_id">
    /// <para>用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。</para>
    /// <para>示例值：user@example.com</para>
    /// </param>
    /// <param name="thread_id">
    /// <para>路径参数</para>
    /// <para>必填：是</para>
    /// <para>邮件会话ID。可通过发送邮件、回复邮件的接口返回值或获取邮件详情接口查询获得。</para>
    /// <para>示例值：xxxxxxxxxxxx</para>
    /// </param>
    /// <param name="format">
    /// <para>必填：否</para>
    /// <para>需要获取的邮件内容</para>
    /// <para>示例值：full</para>
    /// <list type="bullet">
    /// <item>full：全文，包括标签、文件夹、主题、收发件人、纯文本、HTML等信息</item>
    /// <item>plain_text_full：全文，只返回纯文本正文内容，不返回HTML。返回内容包括标签、文件夹、主题、收发件人、纯文本等信息</item>
    /// <item>metadata：邮件元数据信息，包括标签、文件夹、主题、收发件人、摘要等信息，不返回正文内容</item>
    /// </list>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="include_spam_trash">
    /// <para>是否包含垃圾邮件和已删除邮件</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/threads/{thread_id}/messages")]
    Task<FeishuApiResult<GetThreadMessageUserMailboxMessageResult>?> GetThreadMessageUserMailboxMessageAsync(
        [Path] string user_mailbox_id,
        [Path] string thread_id,
        [Query] string? format = null,
        [Query] string? include_spam_trash = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取邮件卡片的邮件列表。
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-message/list_thread_message">接口文档</see></para>
    /// </summary>
    /// <param name="user_mailbox_id">
    /// <para>用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。</para>
    /// <para>示例值：user@example.com</para>
    /// </param>
    /// <param name="card_id">
    /// <para>必填：是</para>
    /// <para>邮件卡片ID，可通过[接收消息](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/im-v1/message/events/receive)事件的推送获取</para>
    /// <para>示例值：512ca581-6059-4449-8150-5522e6641d32</para>
    /// </param>
    /// <param name="owner_id">
    /// <para>必填：是</para>
    /// <para>邮件卡片Owner ID，可通过[接收消息]( https://open.feishu.cn/document/server-docs/im-v1/message/events/receive)事件的推送获取（与`user_id_type`无关）</para>
    /// <para>示例值：1234567890</para>
    /// </param>
    /// <param name="user_id_type">
    /// <para>必填：否</para>
    /// <para>用户 ID 类型</para>
    /// <para>示例值：open_id</para>
    /// <list type="bullet">
    /// <item>open_id：标识一个用户在某个应用中的身份。同一个用户在不同应用中的 Open ID 不同。[了解更多：如何获取 Open ID](https://open.feishu.cn/document/uAjLw4CM/ugTN1YjL4UTN24CO1UjN/trouble-shooting/how-to-obtain-openid)</item>
    /// <item>union_id：标识一个用户在某个应用开发商下的身份。同一用户在同一开发商下的应用中的 Union ID 是相同的，在不同开发商下的应用中的 Union ID 是不同的。通过 Union ID，应用开发商可以把同个用户在多个应用中的身份关联起来。[了解更多：如何获取 Union ID？](https://open.feishu.cn/document/uAjLw4CM/ugTN1YjL4UTN24CO1UjN/trouble-shooting/how-to-obtain-union-id)</item>
    /// <item>user_id：标识一个用户在某个租户内的身份。同一个用户在租户 A 和租户 B 内的 User ID 是不同的。在同一个租户内，一个用户的 User ID 在所有应用（包括商店应用）中都保持一致。User ID 主要用于在不同的应用间打通用户数据。[了解更多：如何获取 User ID？](https://open.feishu.cn/document/uAjLw4CM/ugTN1YjL4UTN24CO1UjN/trouble-shooting/how-to-obtain-user-id)</item>
    /// </list>
    /// <para>默认值：open_id</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/messages/get_by_card")]
    Task<FeishuApiResult<GetByCardUserMailboxMessageResult>?> GetByCardUserMailboxMessageAsync(
        [Path] string user_mailbox_id,
        [Query] string card_id,
        [Query] string owner_id,
        [Query] string? user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);



    /// <summary>
    /// 分页列出邮件。
    /// <para>分页列出邮件，使用应用权限访问时，需要申请邮件数据资源的数据权限。</para>
    /// <para><see href="https://open.feishu.cn/document/mail-v1/user_mailbox-message/list">接口文档</see></para>
    /// </summary>
    /// <param name="user_mailbox_id">
    /// <para>用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。</para>
    /// <para>示例值：user@example.com</para>
    /// </param>
    /// <param name="folder_id">
    /// <para>文件夹 id， 获取方式见 [列出邮箱文件夹](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-folder/list)</para>
    /// <para>示例值：INBOX 或者用户文件夹 id</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="only_unread">
    /// <para>是否只查询未读邮件</para>
    /// <para>示例值：true</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="label_id">
    /// <para>标签id，支持IMPORTANT、OTHER、FLAGGED、SCHEDULED以及自定义文件夹标签</para>
    /// <para>示例值：FLAGGED</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="page_size">分页大小，即本次请求所返回的信息列表内的最大条目数。默认值：20</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/messages")]
    Task<FeishuApiPageListResult<string>?> GetUserMailboxMessagePageListAsync(
        [Path] string user_mailbox_id,
        [Query] string? folder_id = null,
        [Query] bool? only_unread = null,
        [Query] string? label_id = null,
        [Query] int page_size = Consts.PageSize_20,
        [Query] string? page_token = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取邮件详情。
    /// <para>获取邮件详情，使用应用权限访问时，需要申请邮件数据资源的数据权限。</para>
    /// <para><see href="https://open.feishu.cn/document/mail-v1/user_mailbox-message/get">接口文档</see></para>
    /// </summary>
    /// <param name="user_mailbox_id">
    /// <para>用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。</para>
    /// <para>示例值：user@example.com</para>
    /// </param>
    /// <param name="message_id">
    /// <para>邮件ID，可通过列出邮件接口获得</para>
    /// <para>示例值：NzR3Zkd5NGhBTS9NVkZnSklidDVGT3VoQmM4PQ==</para>
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
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/messages/{message_id}")]
    Task<FeishuApiResult<GetUserMailboxMessageResult>?> GetUserMailboxMessageAsync(
         [Path] string user_mailbox_id,
         [Path] string message_id,
         [Query] string? format = null,
         CancellationToken cancellationToken = default);

}
