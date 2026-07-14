// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.HelpDesk;

namespace Mud.Feishu;

/// <summary>
/// 飞书服务台工单API是开放平台基于飞书服务台的工单功能模块开放的查看/创建/修改/删除等API，开发者可以基于这些API对服务台工单对应的功能模块进行操作。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/helpdesk-v1/ticket-management/ticket/start_service"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "HelpDesk", InheritedFrom = nameof(FeishuV1HelpDeskTicket))]
[Token(FeishuTokenTypes.UserAccessToken, Name = Consts.Authorization)]
public interface IFeishuUserV1HelpDeskTicket : IFeishuV1HelpDeskTicket, ICurrentUserId
{
    /// <summary>
    /// 更新工单详情
    /// <para>用于更新服务台工单详情。只会更新数据，不会触发相关操作。如修改工单状态到关单，不会关闭聊天页面。仅支持自建应用。要更新的工单字段必须至少输入一项。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/helpdesk-v1/ticket-management/ticket/update">接口文档</see></para>
    /// </summary>   
    /// <param name="request">更新工单请求体</param>
    /// <param name="ticket_id">
    /// <para>工单 ID。可通过[查询全部工单详情](<see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/helpdesk-v1/ticket/list">获取</see>)</para>
    /// <para>示例值：123456</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Put("/open-apis/helpdesk/v1/tickets/{ticket_id}")]
    Task<FeishuNullDataApiResult?> UpdateTicketAsync(
       [Path] string ticket_id,
       [Body] UpdateTicketRequest request,
       CancellationToken cancellationToken = default);




    /// <summary>
    /// 创建工单自定义字段
    /// <para>用于创建自定义字段。注意事项：user_access_token 访问，需要操作者是当前服务台的管理员或所有者</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/helpdesk-v1/ticket-management/ticket_customized_field/create-ticket-customized-field">接口文档</see></para>
    /// </summary>   
    /// <param name="request">创建工单自定义字段请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/helpdesk/v1/ticket_customized_fields")]
    Task<FeishuNullDataApiResult?> CreateCustomizedFieldAsync(
      [Body] CreateCustomizedFieldRequest request,
      CancellationToken cancellationToken = default);


    /// <summary>
    /// 删除工单自定义字段
    /// <para>用于删除工单自定义字段。注意事项：user_access_token 访问，需要操作者是当前服务台的管理员或所有者</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/helpdesk-v1/ticket-management/ticket_customized_field/create-ticket-customized-field">接口文档</see></para>
    /// </summary>   
    /// <param name="ticket_customized_field_id">
    /// <para>工单自定义字段ID</para>
    /// <para>示例值：6948728206392295444</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Delete("/open-apis/helpdesk/v1/ticket_customized_fields/{ticket_customized_field_id}")]
    Task<FeishuNullDataApiResult?> DeleteCustomizedFieldAsync(
       [Path] string ticket_customized_field_id,
       CancellationToken cancellationToken = default);



    /// <summary>
    /// 更新工单自定义字段
    /// <para>用于更新自定义字段。注意事项：user_access_token 访问，需要操作者是当前服务台的管理员或所有者</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/helpdesk-v1/ticket-management/ticket_customized_field/update-ticket-customized-field">接口文档</see></para>
    /// </summary>   
    /// <param name="ticket_customized_field_id">
    /// <para>工单自定义字段ID</para>
    /// <para>示例值：6948728206392295444</para>
    /// </param>
    /// <param name="request">更新工单自定义字段请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Patch("/open-apis/helpdesk/v1/ticket_customized_fields/{ticket_customized_field_id}")]
    Task<FeishuNullDataApiResult?> UpdateCustomizedFieldAsync(
         [Path] string ticket_customized_field_id,
         [Body] UpdateCustomizedFieldRequest request,
         CancellationToken cancellationToken = default);

}
