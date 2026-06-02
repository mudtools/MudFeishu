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


    /// <summary>
    /// 添加公共邮箱成员。
    /// <para>向公共邮箱添加单个成员。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/mail-v1/public-mailbox/public_mailbox-member/create">接口文档</see></para>
    /// </summary>
    /// <param name="public_mailbox_id">
    /// <para>公共邮箱唯一标识或公共邮箱地址</para>
    /// <para>示例值：xxxxxxxxxxxxxxx 或 test_public_mailbox@xxx.xx</para>
    /// </param>
    /// <param name="user_id_type">
    /// <para>用户 ID 类型</para>
    /// <para>示例值：open_id</para>
    /// <list type="bullet">
    /// <item>open_id：标识一个用户在某个应用中的身份。同一个用户在不同应用中的 Open ID 不同。[了解更多：如何获取 Open ID](https://open.feishu.cn/document/uAjLw4CM/ugTN1YjL4UTN24CO1UjN/trouble-shooting/how-to-obtain-openid)</item>
    /// <item>union_id：标识一个用户在某个应用开发商下的身份。同一用户在同一开发商下的应用中的 Union ID 是相同的，在不同开发商下的应用中的 Union ID 是不同的。通过 Union ID，应用开发商可以把同个用户在多个应用中的身份关联起来。[了解更多：如何获取 Union ID？](https://open.feishu.cn/document/uAjLw4CM/ugTN1YjL4UTN24CO1UjN/trouble-shooting/how-to-obtain-union-id)</item>
    /// <item>user_id：标识一个用户在某个租户内的身份。同一个用户在租户 A 和租户 B 内的 User ID 是不同的。在同一个租户内，一个用户的 User ID 在所有应用（包括商店应用）中都保持一致。User ID 主要用于在不同的应用间打通用户数据。[了解更多：如何获取 User ID？](https://open.feishu.cn/document/uAjLw4CM/ugTN1YjL4UTN24CO1UjN/trouble-shooting/how-to-obtain-user-id)</item>
    /// </list>
    /// <para>默认值：open_id</para>
    /// </param>
    /// <param name="request">添加公共邮箱成员请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/mail/v1/public_mailboxes/{public_mailbox_id}/members")]
    Task<FeishuApiResult<PublicMailboxMemberOopsResult>?> CreatePublicMailboxMemberAsync(
         [Path] string public_mailbox_id,
         [Body] CreatePublicMailboxMemberRequest request,
         [Query] string? user_id_type = Consts.User_Id_Type,
         CancellationToken cancellationToken = default);


    /// <summary>
    /// 删除公共邮箱单个成员。
    /// <para>删除公共邮箱单个成员。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/mail-v1/public-mailbox/public_mailbox-member/delete">接口文档</see></para>
    /// </summary>
    /// <param name="public_mailbox_id">
    /// <para>公共邮箱唯一标识或公共邮箱地址</para>
    /// <para>示例值：xxxxxxxxxxxxxxx 或 test_public_mailbox@xxx.xx</para>
    /// </param>
    /// <param name="member_id">
    /// <para>公共邮箱内成员唯一标识</para>
    /// <para>示例值：xxxxxxxxxxxxxxx</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Delete("/open-apis/mail/v1/public_mailboxes/{public_mailbox_id}/members/{member_id}")]
    Task<FeishuNullDataApiResult?> DeletePublicMailboxMemberAsync(
       [Path] string public_mailbox_id,
       [Path] string member_id,
       CancellationToken cancellationToken = default);


    /// <summary>
    /// 删除公共邮箱所有成员。
    /// <para>删除公共邮箱所有成员。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/mail-v1/public-mailbox/public_mailbox-member/clear">接口文档</see></para>
    /// </summary>
    /// <param name="public_mailbox_id">
    /// <para>公共邮箱唯一标识或公共邮箱地址</para>
    /// <para>示例值：xxxxxxxxxxxxxxx 或 test_public_mailbox@xxx.xx</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/mail/v1/public_mailboxes/{public_mailbox_id}/members/clear")]
    Task<FeishuNullDataApiResult?> DeletePublicMailboxAllMemberAsync(
         [Path] string public_mailbox_id,
         CancellationToken cancellationToken = default);


    /// <summary>
    /// 查询指定公共邮箱成员信息。
    /// <para>获取公共邮箱单个成员信息。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/mail-v1/public-mailbox/public_mailbox-member/get">接口文档</see></para>
    /// </summary>
    /// <param name="public_mailbox_id">
    /// <para>公共邮箱唯一标识或公共邮箱地址</para>
    /// <para>示例值：xxxxxxxxxxxxxxx 或 test_public_mailbox@xxx.xx</para>
    /// </param>
    /// <param name="member_id">
    /// <para>公共邮箱内成员唯一标识</para>
    /// <para>示例值：xxxxxxxxxxxxxxx</para>
    /// </param>
    /// <param name="user_id_type">
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
    [Get("/open-apis/mail/v1/public_mailboxes/{public_mailbox_id}/members/{member_id}")]
    Task<FeishuApiResult<PublicMailboxMemberOopsResult>?> GetPublicMailboxMemberAsync(
          [Path] string public_mailbox_id,
          [Path] string member_id,
          [Query] string? user_id_type = Consts.User_Id_Type,
          CancellationToken cancellationToken = default);


    /// <summary>
    /// 查询所有公共邮箱成员信息。
    /// <para>查询所有公共邮箱成员信息。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/mail-v1/public-mailbox/public_mailbox-member/list">接口文档</see></para>
    /// </summary>
    /// <param name="public_mailbox_id">
    /// <para>公共邮箱唯一标识或公共邮箱地址</para>
    /// <para>示例值：xxxxxxxxxxxxxxx 或 test_public_mailbox@xxx.xx</para>
    /// </param>
    /// <param name="user_id_type">
    /// <para>用户 ID 类型</para>
    /// <para>示例值：open_id</para>
    /// <list type="bullet">
    /// <item>open_id：标识一个用户在某个应用中的身份。同一个用户在不同应用中的 Open ID 不同。[了解更多：如何获取 Open ID](https://open.feishu.cn/document/uAjLw4CM/ugTN1YjL4UTN24CO1UjN/trouble-shooting/how-to-obtain-openid)</item>
    /// <item>union_id：标识一个用户在某个应用开发商下的身份。同一用户在同一开发商下的应用中的 Union ID 是相同的，在不同开发商下的应用中的 Union ID 是不同的。通过 Union ID，应用开发商可以把同个用户在多个应用中的身份关联起来。[了解更多：如何获取 Union ID？](https://open.feishu.cn/document/uAjLw4CM/ugTN1YjL4UTN24CO1UjN/trouble-shooting/how-to-obtain-union-id)</item>
    /// <item>user_id：标识一个用户在某个租户内的身份。同一个用户在租户 A 和租户 B 内的 User ID 是不同的。在同一个租户内，一个用户的 User ID 在所有应用（包括商店应用）中都保持一致。User ID 主要用于在不同的应用间打通用户数据。[了解更多：如何获取 User ID？](https://open.feishu.cn/document/uAjLw4CM/ugTN1YjL4UTN24CO1UjN/trouble-shooting/how-to-obtain-user-id)</item>
    /// </list>
    /// <para>默认值：open_id</para>
    /// </param>
    /// <param name="page_size">分页大小，即本次请求所返回的信息列表内的最大条目数。默认值：20</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/mail/v1/public_mailboxes/{public_mailbox_id}/members")]
    Task<FeishuApiPageListResult<PublicMailboxMemberInfo>?> GetPublicMailboxMemberPageListAsync(
         [Path] string public_mailbox_id,
         [Query] int page_size = Consts.PageSize_20,
         [Query] string? page_token = null,
         [Query] string? user_id_type = Consts.User_Id_Type,
         CancellationToken cancellationToken = default);


    /// <summary>
    /// 批量添加公共邮箱成员。
    /// <para>一次请求可以给一个公共邮箱添加多个成员。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/mail-v1/public-mailbox/public_mailbox-member/batch_create">接口文档</see></para>
    /// </summary>
    /// <param name="public_mailbox_id">
    /// <para>公共邮箱唯一标识或公共邮箱地址</para>
    /// <para>示例值：xxxxxxxxxxxxxxx 或 test_public_mailbox@xxx.xx</para>
    /// </param>
    /// <param name="user_id_type">
    /// <para>用户 ID 类型</para>
    /// <para>示例值：open_id</para>
    /// <list type="bullet">
    /// <item>open_id：标识一个用户在某个应用中的身份。同一个用户在不同应用中的 Open ID 不同。[了解更多：如何获取 Open ID](https://open.feishu.cn/document/uAjLw4CM/ugTN1YjL4UTN24CO1UjN/trouble-shooting/how-to-obtain-openid)</item>
    /// <item>union_id：标识一个用户在某个应用开发商下的身份。同一用户在同一开发商下的应用中的 Union ID 是相同的，在不同开发商下的应用中的 Union ID 是不同的。通过 Union ID，应用开发商可以把同个用户在多个应用中的身份关联起来。[了解更多：如何获取 Union ID？](https://open.feishu.cn/document/uAjLw4CM/ugTN1YjL4UTN24CO1UjN/trouble-shooting/how-to-obtain-union-id)</item>
    /// <item>user_id：标识一个用户在某个租户内的身份。同一个用户在租户 A 和租户 B 内的 User ID 是不同的。在同一个租户内，一个用户的 User ID 在所有应用（包括商店应用）中都保持一致。User ID 主要用于在不同的应用间打通用户数据。[了解更多：如何获取 User ID？](https://open.feishu.cn/document/uAjLw4CM/ugTN1YjL4UTN24CO1UjN/trouble-shooting/how-to-obtain-user-id)</item>
    /// </list>
    /// <para>默认值：open_id</para>
    /// </param>
    /// <param name="request">批量添加公共邮箱成员请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/mail/v1/public_mailboxes/{public_mailbox_id}/members/batch_create")]
    Task<FeishuApiResult<BatchCreatePublicMailboxMemberResult>?> BatchCreatePublicMailboxMemberAsync(
        [Path] string public_mailbox_id,
        [Body] BatchCreatePublicMailboxMemberRequest request,
        [Query] string? user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);



    /// <summary>
    /// 批量删除公共邮箱成员。
    /// <para>一次请求可以删除一个公共邮箱中的多个成员。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/mail-v1/public-mailbox/public_mailbox-member/batch_delete">接口文档</see></para>
    /// </summary>
    /// <param name="public_mailbox_id">
    /// <para>公共邮箱唯一标识或公共邮箱地址</para>
    /// <para>示例值：xxxxxxxxxxxxxxx 或 test_public_mailbox@xxx.xx</para>
    /// </param>
    /// <param name="request">批量删除公共邮箱成员请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Delete("/open-apis/mail/v1/public_mailboxes/{public_mailbox_id}/members/batch_delete")]
    Task<FeishuNullDataApiResult?> BatchDeletePublicMailboxMemberAsync(
        [Path] string public_mailbox_id,
        [Body] BatchDeletePublicMailboxMemberRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 创建公共邮箱别名。
    /// <para>创建公共邮箱别名。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/mail-v1/public-mailbox/public_mailbox-alias/create">接口文档</see></para>
    /// </summary>
    /// <param name="public_mailbox_id">
    /// <para>公共邮箱唯一标识或公共邮箱地址</para>
    /// <para>示例值：xxxxxxxxxxxxxxx 或 test_public_mailbox@xxx.xx</para>
    /// </param>
    /// <param name="request">创建公共邮箱别名请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/mail/v1/public_mailboxes/{public_mailbox_id}/aliases")]
    Task<FeishuApiResult<CreatePublicMailboxAliasResult>?> CreatePublicMailboxAliasAsync(
       [Path] string public_mailbox_id,
       [Body] CreatePublicMailboxAliasRequest request,
       CancellationToken cancellationToken = default);



    /// <summary>
    /// 删除公共邮箱别名。
    /// <para>删除公共邮箱别名。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/mail-v1/public-mailbox/public_mailbox-alias/delete">接口文档</see></para>
    /// </summary>
    /// <param name="public_mailbox_id">
    /// <para>公共邮箱唯一标识或公共邮箱地址</para>
    /// <para>示例值：xxxxxxxxxxxxxxx 或 test_public_mailbox@xxx.xx</para>
    /// </param>
    /// <param name="alias_id">
    /// <para>公共邮箱别名</para>
    /// <para>示例值：xxx@xx.xxx</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Delete("/open-apis/mail/v1/public_mailboxes/{public_mailbox_id}/aliases/{alias_id}")]
    Task<FeishuNullDataApiResult?> DeletePublicMailboxAliasAsync(
       [Path] string public_mailbox_id,
       [Path] string alias_id,
       CancellationToken cancellationToken = default);


    /// <summary>
    /// 查询公共邮箱的所有别名。
    /// <para>获取所有公共邮箱别名。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/mail-v1/public-mailbox/public_mailbox-alias/list">接口文档</see></para>
    /// </summary>
    /// <param name="public_mailbox_id">
    /// <para>公共邮箱唯一标识或公共邮箱地址</para>
    /// <para>示例值：xxxxxxxxxxxxxxx 或 test_public_mailbox@xxx.xx</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/mail/v1/public_mailboxes/{public_mailbox_id}/aliases")]
    Task<FeishuApiResult<GetPublicMailboxAliasListResult>?> GetPublicMailboxAliasListAsync(
      [Path] string public_mailbox_id,
      CancellationToken cancellationToken = default);
}
