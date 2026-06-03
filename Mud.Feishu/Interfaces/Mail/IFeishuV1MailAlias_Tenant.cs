// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Mail;

namespace Mud.Feishu;

/// <summary>
/// 飞书邮箱别名API接口实现了添加、查询、删除等邮箱别名管理功能。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/mail-v1/user_mailbox-alias/create"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Mail")]
[Token("TenantAccessToken", Name = Consts.Authorization)]
public interface IFeishuTenantV1MailAlias : IFeishuAppContextSwitcher
{

    /// <summary>
    /// 从回收站删除用户邮箱地址。
    /// <para>该接口会永久删除用户邮箱地址。可用于删除位于邮箱回收站中的用户邮箱地址，一旦删除，将无法恢复。</para>
    /// <para>该接口支持邮件的转移，可以将被释放邮箱的邮件转移到另外一个可以使用的邮箱中。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/mail-v1/user_mailbox-alias/delete">接口文档</see></para>
    /// </summary>
    /// <param name="user_mailbox_id">
    /// <para>用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。</para>
    /// <para>示例值：user@example.com</para>
    /// </param>
    /// <param name="transfer_mailbox">
    /// <para>用于接收转移的邮箱地址</para>
    /// <para>示例值：888888@abc.com</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Delete("/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}")]
    Task<FeishuNullDataApiResult?> DeleteUserMailboxAsync(
       [Path] string user_mailbox_id,
       [Query] string? transfer_mailbox = null,
       CancellationToken cancellationToken = default);


    /// <summary>
    /// 创建用户邮箱别名。
    /// <para>创建用户邮箱别名。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/mail-v1/user_mailbox-alias/create">接口文档</see></para>
    /// </summary>
    /// <param name="user_mailbox_id">
    /// <para>用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。</para>
    /// <para>示例值：user@example.com</para>
    /// </param>
    /// <param name="createUserMailboxAliasRequest">创建用户邮箱别名请求对象，包含待创建的邮箱别名信息。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/aliases")]
    Task<FeishuApiResult<CreateUserMailboxAliasResult>?> CreateUserMailboxAliasAsync(
         [Path] string user_mailbox_id,
         [Body] CreateUserMailboxAliasRequest createUserMailboxAliasRequest,
         CancellationToken cancellationToken = default);


    /// <summary>
    /// 删除用户邮箱别名。
    /// <para>删除用户邮箱别名。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/mail-v1/user_mailbox-alias/delete-2">接口文档</see></para>
    /// </summary>
    /// <param name="user_mailbox_id">
    /// <para>用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。</para>
    /// <para>示例值：user@example.com</para>
    /// </param>
    /// <param name="alias_id">
    /// <para>别名邮箱地址</para>
    /// <para>示例值：user_alias@xxx.xx</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Delete("/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/aliases/{alias_id}")]
    Task<FeishuNullDataApiResult?> DeleteUserMailboxAliasAsync(
        [Path] string user_mailbox_id,
        [Path] string alias_id,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取用户邮箱所有别名。
    /// <para>获取用户邮箱所有别名，注意：该接口一次性返回所有数据，分页参数无效。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/mail-v1/user_mailbox-alias/delete-2">接口文档</see></para>
    /// </summary>
    /// <param name="user_mailbox_id">
    /// <para>用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。</para>
    /// <para>示例值：user@example.com</para>
    /// </param>
    /// <param name="page_size">分页大小，即本次请求所返回的信息列表内的最大条目数。默认值：20</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/aliases")]
    Task<FeishuApiPageListResult<EmailAlias>?> GetUserMailboxAliasPageListAsync(
        [Path] string user_mailbox_id,
        [Query] int page_size = Consts.PageSize_20,
        [Query] string? page_token = null,
        CancellationToken cancellationToken = default);



    /// <summary>
    /// 查询邮箱地址状态。
    /// <para>使用邮箱状态查询接口，可以输入邮箱地址，查询出该邮箱地址对应的类型以及状态。。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/mail-v1/user/query">接口文档</see></para>
    /// </summary>
    /// <param name="request">查询邮箱地址状态请求对象，包含待查询的邮箱地址信息。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/mail/v1/users/query")]
    Task<FeishuApiResult<QueryUserMailboxAddressResult>?> QueryUserMailboxAddressAsync(
          [Body] QueryUserMailboxAddressRequest request,
          CancellationToken cancellationToken = default);
}
