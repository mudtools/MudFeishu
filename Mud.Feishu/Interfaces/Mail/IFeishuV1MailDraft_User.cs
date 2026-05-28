// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Mail;

namespace Mud.Feishu;


/// <summary>
/// 飞书邮箱草稿API接口实现了修改、查询、删除等邮件草稿功能。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-draft/update"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Mail")]
[Token("UserAccessToken", Name = Consts.Authorization)]
public interface IFeishuUserV1MailDraft : IFeishuAppContextSwitcher, ICurrentUserId
{

    /// <summary>
    /// 更新草稿。
    /// <para>更新草稿内容。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-draft/update">接口文档</see></para>
    /// </summary>
    /// <param name="user_mailbox_id">
    /// <para>用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。</para>
    /// <para>示例值：user@example.com</para>
    /// </param>
    /// <param name="draft_id">
    /// <para>草稿ID，可通过创建草稿或列出草稿接口获得</para>
    /// <para>示例值：268dce11-85f7-427d-8756-6be3abc850fd</para>
    /// </param>
    /// <param name="updateUserMailboxDraftRequest">更新草稿请求体。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Put("/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/drafts/{draft_id}")]
    Task<FeishuApiResult<UserMailboxDraftOopsResult>?> UpdateUserMailboxDraftAsync(
       [Path] string user_mailbox_id,
       [Path] string draft_id,
       [Body] UserMailboxDraftRequest updateUserMailboxDraftRequest,
       CancellationToken cancellationToken = default);



    /// <summary>
    /// 发送草稿。
    /// <para>将指定的草稿发送出去。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-draft/send">接口文档</see></para>
    /// </summary>
    /// <param name="user_mailbox_id">
    /// <para>用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。</para>
    /// <para>示例值：user@example.com</para>
    /// </param>
    /// <param name="draft_id">
    /// <para>草稿ID，可通过创建草稿或列出草稿接口获得</para>
    /// <para>示例值：268dce11-85f7-427d-8756-6be3abc850fd</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/drafts/{draft_id}/send")]
    Task<FeishuApiResult<SendUserMailboxDraftResult>?> SendUserMailboxDraftAsync(
      [Path] string user_mailbox_id,
      [Path] string draft_id,
      CancellationToken cancellationToken = default);


    /// <summary>
    /// 分页列出草稿列表。
    /// <para>分页列出用户草稿箱中的草稿，只会返回草稿ID信息，不会返回草稿内容。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-draft/list">接口文档</see></para>
    /// </summary>
    /// <param name="user_mailbox_id">
    /// <para>用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。</para>
    /// <para>示例值：user@example.com</para>
    /// </param>
    /// <param name="page_size">分页大小，即本次请求所返回的信息列表内的最大条目数。默认值：20</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/drafts")]
    Task<FeishuApiPageListResult<DraftId>?> GetUserMailboxDraftPageListAsync(
        [Path] string user_mailbox_id,
        [Query] int page_size = Consts.PageSize_20,
        [Query] string? page_token = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取草稿内容。
    /// <para>更具用户指定的草稿ID，获取草稿详细信息。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-draft/get">接口文档</see></para>
    /// </summary>
    /// <param name="user_mailbox_id">
    /// <para>用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。</para>
    /// <para>示例值：user@example.com</para>
    /// </param>
    /// <param name="draft_id">
    /// <para>草稿ID，可通过创建草稿或列出草稿接口获得</para>
    /// <para>示例值：268dce11-85f7-427d-8756-6be3abc850fd</para>
    /// </param>
    /// <param name="format">
    /// <para>需要获取的草稿内容样式，取值：metadata / full（默认）/ raw</para>
    /// <para>示例值：full</para>
    /// <list type="bullet">
    /// <item>metadata：草稿元数据信息，包括邮件摘要、主题、收发件人等信息</item>
    /// <item>raw：获取草稿EML</item>
    /// <item>full：邮件全文，获取包括纯文本、HTML等在内的邮件全文信息</item>
    /// </list>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/drafts/{draft_id}")]
    Task<FeishuApiResult<GetUserMailboxDraftResult>?> GetUserMailboxDraftAsync(
       [Path] string user_mailbox_id,
       [Path] string draft_id,
       [Query] string? format = null,
       CancellationToken cancellationToken = default);


    /// <summary>
    /// 删除草稿。
    /// <para>删除指定邮箱账户下的单份邮件草稿。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-draft/delete">接口文档</see></para>
    /// </summary>
    /// <param name="user_mailbox_id">
    /// <para>用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。</para>
    /// <para>示例值：user@example.com</para>
    /// </param>
    /// <param name="draft_id">
    /// <para>草稿ID，可通过创建草稿或列出草稿接口获得</para>
    /// <para>示例值：268dce11-85f7-427d-8756-6be3abc850fd</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Delete("/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/drafts/{draft_id}")]
    Task<FeishuNullDataApiResult?> DeleteUserMailboxDraftAsync(
        [Path] string user_mailbox_id,
        [Path] string draft_id,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 创建草稿。
    /// <para>根据指定的内容创建草稿。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-draft/create">接口文档</see></para>
    /// </summary>
    /// <param name="user_mailbox_id">
    /// <para>用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。</para>
    /// <para>示例值：user@example.com</para>
    /// </param>
    /// <param name="createUserMailboxDraftRequest">创建草稿请求体。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/drafts")]
    Task<FeishuApiResult<UserMailboxDraftOopsResult>?> CreateUserMailboxDraftAsync(
      [Path] string user_mailbox_id,
      [Body] UserMailboxDraftRequest createUserMailboxDraftRequest,
      CancellationToken cancellationToken = default);
}
