// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Mail;

namespace Mud.Feishu;


/// <summary>
/// 飞书公共邮箱API接口实现公共邮箱管理、公共邮箱成员管理以及公共邮箱别名管理等管理功能。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-folder/get"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Mail", InheritedFrom = nameof(FeishuV1MailPublicMailbox))]
[Token("TenantAccessToken", Name = Consts.Authorization)]
public interface IFeishuTenantV1MailPublicMailbox : IFeishuV1MailPublicMailbox
{

    /// <summary>
    /// 创建公共邮箱。
    /// <para><see href="https://open.feishu.cn/document/server-docs/mail-v1/public-mailbox/public_mailbox/create">接口文档</see></para>
    /// </summary>
    /// <param name="request">创建公共邮箱请求对象，包含待创建的公共邮箱信息。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/mail/v1/public_mailboxes")]
    Task<FeishuApiResult<PublicMailboxOopsResult>?> CreatePublicMailboxAsync(
         [Body] CreatePublicMailboxRequest request,
         CancellationToken cancellationToken = default);


    /// <summary>
    /// 修改公共邮箱部分信息。
    /// <para>更新公共邮箱部分字段，没有填写的字段不会被更新。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/mail-v1/public-mailbox/public_mailbox/patch">接口文档</see></para>
    /// </summary>
    /// <param name="public_mailbox_id">
    /// <para>公共邮箱唯一标识或公共邮箱地址</para>
    /// <para>示例值：xxxxxxxxxxxxxxx 或 test_public_mailbox@xxx.xx</para>
    /// </param>
    /// <param name="request">修改公共邮箱部分信息请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Patch("/open-apis/mail/v1/public_mailboxes/{public_mailbox_id}")]
    Task<FeishuApiResult<UpdatePublicMailboxResult>?> UpdatePublicMailboxPartialAsync(
        [Path] string public_mailbox_id,
        [Body] UpdatePublicMailboxRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 修改公共邮箱全部信息。
    /// <para>更新公共邮箱全部字段。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/mail-v1/public-mailbox/public_mailbox/update">接口文档</see></para>
    /// </summary>
    /// <param name="public_mailbox_id">
    /// <para>公共邮箱唯一标识或公共邮箱地址</para>
    /// <para>示例值：xxxxxxxxxxxxxxx 或 test_public_mailbox@xxx.xx</para>
    /// </param>
    /// <param name="request">修改公共邮箱部分信息请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Put("/open-apis/mail/v1/public_mailboxes/{public_mailbox_id}")]
    Task<FeishuApiResult<UpdatePublicMailboxResult>?> UpdatePublicMailboxAsync(
        [Path] string public_mailbox_id,
        [Body] UpdatePublicMailboxRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 查询指定公共邮箱。
    /// <para>获取公共邮箱信息。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/mail-v1/public-mailbox/public_mailbox/get">接口文档</see></para>
    /// </summary>
    /// <param name="public_mailbox_id">
    /// <para>公共邮箱唯一标识或公共邮箱地址</para>
    /// <para>示例值：xxxxxxxxxxxxxxx 或 test_public_mailbox@xxx.xx</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/mail/v1/public_mailboxes/{public_mailbox_id}")]
    Task<FeishuApiResult<PublicMailboxOopsResult>?> GetPublicMailboxAsync(
          [Path] string public_mailbox_id,
          CancellationToken cancellationToken = default);


    /// <summary>
    /// 将公共邮箱移至回收站。
    /// <para>将公共邮箱移至回收站。</para>
    /// <para><see href="https://open.feishu.cn/document/mail-v1/public-mailbox/public_mailbox/remove_to_recycle_bin">接口文档</see></para>
    /// </summary>
    /// <param name="public_mailbox_id">
    /// <para>公共邮箱唯一标识或公共邮箱地址</para>
    /// <para>示例值：xxxxxxxxxxxxxxx 或 test_public_mailbox@xxx.xx</para>
    /// </param>
    /// <param name="request">将公共邮箱移至回收站请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Delete("/open-apis/mail/v1/public_mailboxes/{public_mailbox_id}/remove_to_recycle_bin")]
    Task<FeishuNullDataApiResult?> RemoveToRecycleBinPublicMailboxAsync(
         [Path] string public_mailbox_id,
         [Body] RemoveToRecycleBinPublicMailboxRequest request,
         CancellationToken cancellationToken = default);


    /// <summary>
    /// 永久删除公共邮箱。
    /// <para>该接口会永久删除公共邮箱地址。可用于释放邮箱回收站的公共邮箱地址，一旦删除，该邮箱地址将无法恢复。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/mail-v1/public-mailbox/public_mailbox/delete">接口文档</see></para>
    /// </summary>
    /// <param name="public_mailbox_id">
    /// <para>公共邮箱唯一标识或公共邮箱地址</para>
    /// <para>示例值：xxxxxxxxxxxxxxx 或 test_public_mailbox@xxx.xx</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Delete("/open-apis/mail/v1/public_mailboxes/{public_mailbox_id}")]
    Task<FeishuNullDataApiResult?> DeletePublicMailboxAsync(
        [Path] string public_mailbox_id,
        CancellationToken cancellationToken = default);

}
