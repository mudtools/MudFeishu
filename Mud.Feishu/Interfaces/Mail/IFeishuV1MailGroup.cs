// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Mail;

namespace Mud.Feishu;


/// <summary>
/// 飞书邮件组API接口实现了邮件组管理、邮件组管理员管理、邮件组管理员管理、邮件组别名管理、邮件组权限成员管理等管理功能。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/mail-v1/mail-group/mailgroup/create"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Mail")]
[Token("TenantAccessToken", Name = Consts.Authorization)]
public interface IFeishuTenantV1MailGroup : IFeishuAppContextSwitcher
{

    /// <summary>
    /// 创建邮件组。
    /// <para><see href="https://open.feishu.cn/document/server-docs/mail-v1/mail-group/mailgroup/create?appId=cli_a98ea7d1a0ba100b">接口文档</see></para>
    /// </summary>
    /// <param name="request">创建邮件组请求对象，包含待创建的邮件组信息。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/mail/v1/mailgroups")]
    Task<FeishuApiResult<CreateMailGroupResult>?> CreateMailGroupAsync(
       [Body] CreateMailGroupRequest request,
       CancellationToken cancellationToken = default);


    /// <summary>
    /// 删除邮件组。
    /// <para><see href="https://open.feishu.cn/document/server-docs/mail-v1/mail-group/mailgroup/delete">接口文档</see></para>
    /// </summary>
    /// <param name="mailgroup_id">
    /// <para>邮件组ID或者邮件组地址</para>
    /// <para>示例值：xxxxxxxxxxxxxxx 或 test_mail_group@xxx.xx</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Delete("/open-apis/mail/v1/mailgroups/{mailgroup_id}")]
    Task<FeishuNullDataApiResult?> DeleteMailGroupAsync(
         [Path] string mailgroup_id,
         CancellationToken cancellationToken = default);


    /// <summary>
    /// 修改邮件组部分信息。
    /// <para>更新邮件组部分字段，没有填写的字段不会被更新。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/mail-v1/mail-group/mailgroup/patch">接口文档</see></para>
    /// </summary>
    /// <param name="mailgroup_id">
    /// <para>邮件组ID或者邮件组地址</para>
    /// <para>示例值：xxxxxxxxxxxxxxx 或 test_mail_group@xxx.xx</para>
    /// </param>
    /// <param name="request">更新邮件组请求对象，包含待更新的邮件组信息。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Patch("/open-apis/mail/v1/mailgroups/{mailgroup_id}")]
    Task<FeishuApiResult<UpdateMailGroupResult>?> UpdateMailGroupAsync(
        [Path] string mailgroup_id,
        [Body] UpdateMailGroupRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 修改邮件组全部信息。
    /// <para>更新邮件组所有字段。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/mail-v1/mail-group/mailgroup/patch">接口文档</see></para>
    /// </summary>
    /// <param name="mailgroup_id">
    /// <para>邮件组ID或者邮件组地址</para>
    /// <para>示例值：xxxxxxxxxxxxxxx 或 test_mail_group@xxx.xx</para>
    /// </param>
    /// <param name="request">更新邮件组请求对象，包含待更新的邮件组信息。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Put("/open-apis/mail/v1/mailgroups/{mailgroup_id}")]
    Task<FeishuApiResult<UpdateMailGroupResult>?> UpdateMailGroupInfoAsync(
        [Path] string mailgroup_id,
        [Body] UpdateMailGroupRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 查询指定邮件组。
    /// <para>获取特定邮件组信息。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/mail-v1/mail-group/mailgroup/get">接口文档</see></para>
    /// </summary>
    /// <param name="mailgroup_id">
    /// <para>邮件组ID或者邮件组地址</para>
    /// <para>示例值：xxxxxxxxxxxxxxx 或 test_mail_group@xxx.xx</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/mail/v1/mailgroups/{mailgroup_id}")]
    Task<FeishuApiResult<MailGroupInfo>?> GetMailGroupAsync(
        [Path] string mailgroup_id,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 分页批量获取邮件组。
    /// <para>分页批量获取邮件组。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/mail-v1/mail-group/mailgroup/list">接口文档</see></para>
    /// </summary>
    /// <param name="manager_user_id">
    /// <para>邮件组管理员用户ID，用于获取该用户有管理权限的邮件组</para>
    /// <para>示例值：ou_xxxxxx</para>
    /// <para>默认值：null</para>
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
    [Get("/open-apis/mail/v1/mailgroups")]
    Task<FeishuApiPageListResult<MailGroupInfo>?> GetMailGroupPageListAsync(
          [Query] string? manager_user_id = null,
          [Query] string? user_id_type = Consts.User_Id_Type,
          [Query] int page_size = Consts.PageSize_20,
          [Query] string? page_token = null,
          CancellationToken cancellationToken = default);


    /// <summary>
    /// 批量创建邮件组管理员。
    /// <para>批量创建邮件组管理员。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/mail-v1/mail-group/mailgroup-manager/batch_create">接口文档</see></para>
    /// </summary>
    /// <param name="mailgroup_id">
    /// <para>邮件组ID或邮箱地址</para>
    /// <para>示例值：xxxxxx 或 test_mail_group@xx.xx</para>
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
    /// <param name="request">批量添加邮件组管理员请求对象，包含待添加的邮件组管理员信息。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/mail/v1/mailgroups/{mailgroup_id}/managers/batch_create")]
    Task<FeishuNullDataApiResult?> BatchCreateMailgroupManagerAsync(
         [Path] string mailgroup_id,
         [Body] BatchOopsMailgroupManagerRequest request,
         [Query] string? user_id_type = Consts.User_Id_Type,
         CancellationToken cancellationToken = default);


    /// <summary>
    /// 批量删除邮件组管理员。
    /// <para>批量删除邮件组管理员。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/mail-v1/mail-group/mailgroup-manager/batch_delete">接口文档</see></para>
    /// </summary>
    /// <param name="mailgroup_id">
    /// <para>邮件组ID或邮箱地址</para>
    /// <para>示例值：xxxxxx 或 test_mail_group@xx.xx</para>
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
    /// <param name="request">批量删除邮件组管理员请求对象，包含待删除的邮件组管理员信息。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/mail/v1/mailgroups/{mailgroup_id}/managers/batch_delete")]
    Task<FeishuNullDataApiResult?> BatchDeleteMailgroupManagerAsync(
        [Path] string mailgroup_id,
        [Body] BatchOopsMailgroupManagerRequest request,
        [Query] string? user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 批量获取邮件组管理员。
    /// <para>批量获取邮件组管理员。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/mail-v1/mail-group/mailgroup-manager/list">接口文档</see></para>
    /// </summary>
    /// <param name="mailgroup_id">
    /// <para>邮件组ID或邮箱地址</para>
    /// <para>示例值：xxxxxx 或 test_mail_group@xx.xx</para>
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
    [Get("/open-apis/mail/v1/mailgroups/{mailgroup_id}/managers")]
    Task<FeishuApiPageListResult<MailgroupManager>?> GetMailgroupManagerPageListAsync(
         [Path] string mailgroup_id,
         [Query] string? user_id_type = Consts.User_Id_Type,
         [Query] int page_size = Consts.PageSize_20,
         [Query] string? page_token = null,
         CancellationToken cancellationToken = default);
}
