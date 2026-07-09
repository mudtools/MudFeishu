// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Mail;

namespace Mud.Feishu.Interfaces;

/// <summary>
/// 飞书邮箱联系人API接口实现了修改、查询、删除等邮箱联系人管理功能。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/mail-v1/user_mailbox-mail_contact/create"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuV1MailContact : IFeishuAppContextSwitcher
{

    /// <summary>
    /// 创建邮箱联系人。
    /// <para>创建邮箱联系人。使用 tenant_access_token 时，需要申请邮箱联系人资源的数据权限。</para>
    /// <para><see href="https://open.feishu.cn/document/mail-v1/user_mailbox-mail_contact/create">接口文档</see></para>
    /// </summary>
    /// <param name="user_mailbox_id">
    /// <para>用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。</para>
    /// <para>示例值：user@example.com</para>
    /// </param>
    /// <param name="request">创建用户邮箱收信规则请求对象，包含待创建的收信规则信息。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/mail_contacts")]
    Task<FeishuApiResult<CreateUserMailboxContactResult>?> CreateUserMailboxContactAsync(
          [Path] string user_mailbox_id,
          [Body] CreateUserMailboxContactRequest request,
          CancellationToken cancellationToken = default);


    /// <summary>
    /// 创建邮箱联系人。
    /// <para>创建邮箱联系人。使用 tenant_access_token 时，需要申请邮箱联系人资源的数据权限。</para>
    /// <para><see href="https://open.feishu.cn/document/mail-v1/user_mailbox-mail_contact/create">接口文档</see></para>
    /// </summary>
    /// <param name="user_mailbox_id">
    /// <para>用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。</para>
    /// <para>示例值：user@example.com</para>
    /// </param>
    /// <param name="mail_contact_id">
    /// <para>邮箱联系人 id，获取方式见 [列出邮箱联系人](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-mail_contact/list)</para>
    /// <para>示例值：123</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Delete("/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/mail_contacts/{mail_contact_id}")]
    Task<FeishuNullDataApiResult?> DeleteUserMailboxContactAsync(
         [Path] string user_mailbox_id,
         [Path] string mail_contact_id,
         CancellationToken cancellationToken = default);


    /// <summary>
    /// 修改邮箱联系人信息。
    /// <para>修改一个邮箱联系人的信息。使用 tenant_access_token 时，需要申请邮箱联系人资源的数据权限。</para>
    /// <para><see href="https://open.feishu.cn/document/mail-v1/user_mailbox-mail_contact/patch">接口文档</see></para>
    /// </summary>
    /// <param name="user_mailbox_id">
    /// <para>用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。</para>
    /// <para>示例值：user@example.com</para>
    /// </param>
    /// <param name="mail_contact_id">
    /// <para>邮箱联系人 id，获取方式见 [列出邮箱联系人](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-mail_contact/list)</para>
    /// <para>示例值：123</para>
    /// </param>
    /// <param name="request">更新用户邮箱收信规则请求对象，包含待更新的收信规则信息。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Patch("/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/mail_contacts/{mail_contact_id}")]
    Task<FeishuNullDataApiResult?> UpdateUserMailboxContactAsync(
         [Path] string user_mailbox_id,
         [Path] string mail_contact_id,
         [Body] UpdateUserMailboxContactRequest request,
         CancellationToken cancellationToken = default);




    /// <summary>
    /// 分页列出邮箱联系人。
    /// <para>分页列出邮箱联系人，使用 tenant_access_token 时，需要申请邮箱联系人资源的数据权限。</para>
    /// <para><see href="https://open.feishu.cn/document/mail-v1/user_mailbox-mail_contact/list">接口文档</see></para>
    /// </summary>
    /// <param name="user_mailbox_id">
    /// <para>用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。</para>
    /// <para>示例值：user@example.com</para>
    /// </param>
    /// <param name="page_size">分页大小，即本次请求所返回的信息列表内的最大条目数。默认值：20</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/mail_contacts")]
    Task<FeishuApiPageListResult<MailboxContactInfo>?> GetUserMailboxContactPageListAsync(
        [Path] string user_mailbox_id,
        [Query] int page_size = Consts.PageSize_20,
        [Query] string? page_token = null,
        CancellationToken cancellationToken = default);
}
