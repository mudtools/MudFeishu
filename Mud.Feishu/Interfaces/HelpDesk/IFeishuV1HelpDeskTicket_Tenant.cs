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
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV1HelpDeskTicket : IFeishuV1HelpDeskTicket
{

    /// <summary>
    /// 创建服务台对话
    /// <para>用于创建服务台对话。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/helpdesk-v1/ticket-management/ticket/start_service">接口文档</see></para>
    /// </summary>   
    /// <param name="request">创建服务台对话请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/helpdesk/v1/start_service")]
    Task<FeishuApiResult<StartServiceTicketResult>?> StartServiceTicketAsync(
       [Body] StartServiceTicketRequest request,
       CancellationToken cancellationToken = default);


    /// <summary>
    /// 查询指定工单详情
    /// <para>用于获取单个服务台工单详情。仅支持自建应用。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/helpdesk-v1/ticket-management/ticket/get">接口文档</see></para>
    /// </summary>   
    /// <param name="ticket_id">
    /// <para>工单 ID。可通过[查询全部工单详情](<see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/helpdesk-v1/ticket/list">获取</see>)</para>
    /// <para>示例值：123456</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/helpdesk/v1/tickets/{ticket_id}")]
    Task<FeishuApiResult<GetTicketResult>?> GetTicketAsync(
        [Path] string ticket_id,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 查询全部工单详情
    /// <para>用于获取全部工单详情。仅支持自建应用。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/helpdesk-v1/ticket-management/ticket/list">接口文档</see></para>
    /// </summary>   
    /// <param name="ticket_id">
    /// <para>工单 ID。可通过[查询全部工单详情](<see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/helpdesk-v1/ticket/list">获取</see>)</para>
    /// <para>示例值：123456</para>
    /// </param>
    /// <param name="agent_id">
    /// <para>搜索条件: 客服id</para>
    /// <para>示例值：ou_b5de90429xxx</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="closed_by_id">
    /// <para>搜索条件: 关单客服id</para>
    /// <para>示例值：ou_b5de90429xxx</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="type">
    /// <para>搜索条件: 工单类型 1:bot 2:人工</para>
    /// <para>示例值：1</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="channel">
    /// <para>搜索条件: 工单渠道</para>
    /// <para>示例值：0</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="solved">
    /// <para>搜索条件: 工单是否解决 1:没解决 2:已解决</para>
    /// <para>示例值：1</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="score">
    /// <para>搜索条件: 工单评分</para>
    /// <para>示例值：1</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="status_list">
    /// <para>搜索条件: 工单状态列表</para>
    /// <para>示例值：1</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="guest_name">
    /// <para>搜索条件: 用户名称</para>
    /// <para>示例值：abc</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="guest_id">
    /// <para>搜索条件: 用户id</para>
    /// <para>示例值：ou_b5de90429xxx</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="tags">
    /// <para>搜索条件: 用户标签列表</para>
    /// <para>示例值：备注</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="page">
    /// <para>页数, 从1开始, 默认为1</para>
    /// <para>示例值：1</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="page_size">
    /// <para>当前页大小，最大为200， 默认为20。分页查询最多累计返回一万条数据，超过一万条请更改查询条件，推荐通过时间查询。</para>
    /// <para>示例值：20</para>
    /// <para>默认值：10</para>
    /// </param>
    /// <param name="create_time_start">
    /// <para>搜索条件: 工单创建起始时间 ms (也需要填上create_time_end)，相当于&gt;=create_time_start</para>
    /// <para>示例值：1616920429000</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="create_time_end">
    /// <para>搜索条件: 工单创建结束时间 ms (也需要填上create_time_start)，相当于&lt;=create_time_end</para>
    /// <para>示例值：1616920429000</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="update_time_start">
    /// <para>搜索条件: 工单修改起始时间 ms (也需要填上update_time_end)</para>
    /// <para>示例值：1616920429000</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="update_time_end">
    /// <para>搜索条件: 工单修改结束时间 ms(也需要填上update_time_start)</para>
    /// <para>示例值：1616920429000</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/helpdesk/v1/tickets")]
    Task<FeishuApiResult<GetTicketListResult>?> GetTicketListAsync(
        [Query] string? ticket_id = null,
        [Query] string? agent_id = null,
        [Query] string? closed_by_id = null,
        [Query] int? type = null,
        [Query] int? channel = null,
        [Query] int? solved = null,
        [Query] int? score = null,
        [Query] int[]? status_list = null,
        [Query] string? guest_name = null,
        [Query] string? guest_id = null,
        [Query] string[]? tags = null,
        [Query] int? page = null,
        [Query] int? page_size = 10,
        [Query] int? create_time_start = null,
        [Query] int? create_time_end = null,
        [Query] int? update_time_start = null,
        [Query] int? update_time_end = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取工单内图像
    /// <para>用于获取服务台工单消息图象。仅支持自建应用。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/helpdesk-v1/ticket-management/ticket/ticket_image">接口文档</see></para>
    /// </summary>   
    /// <param name="ticket_id">
    /// <para>工单 ID。可通过[查询全部工单详情](<see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/helpdesk-v1/ticket/list">获取</see>)</para>
    /// <para>示例值：123456</para>
    /// </param>
    /// <param name="msg_id">
    /// <para>消息ID</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/helpdesk-v1/ticket-message/list">[查询消息ID]</see></para>
    /// <para>示例值：12345</para>
    /// </param>
    /// <param name="index">
    /// <para>index，当消息类型为post时，需指定图片index，index从0开始。当消息类型为img时，无需index</para>
    /// <para>示例值：0</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回文件二进制流</returns>
    [Get("/open-apis/helpdesk/v1/ticket_images")]
    Task<byte[]?> GetTicketImageAsync(
        [Query] string ticket_id,
        [Query] string msg_id,
        [Query] int? index = null,
       CancellationToken cancellationToken = default);


    /// <summary>
    /// 回复用户在工单里的提问
    /// <para>用于回复用户提问结果至工单，需要工单仍处于进行中且未接入人工状态。仅支持自建应用。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/helpdesk-v1/ticket-management/ticket/answer_user_query">接口文档</see></para>
    /// </summary>   
    /// <param name="ticket_id">
    /// <para>工单 ID。可通过[查询全部工单详情](<see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/helpdesk-v1/ticket/list">获取</see>)</para>
    /// <para>示例值：123456</para>
    /// </param>
    /// <param name="request">回复用户在工单里的提问请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/helpdesk/v1/tickets/{ticket_id}/answer_user_query")]
    Task<FeishuNullDataApiResult?> AnswerUserQueryTicketAsync(
      [Path] string ticket_id,
      [Body] AnswerUserQueryTicketRequest request,
     CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取服务台自定义字段
    /// <para>用于获取服务台自定义字段详情。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/helpdesk-v1/ticket-management/ticket/customized_fields">接口文档</see></para>
    /// </summary>   
    /// <param name="visible_only">
    /// <para>visible only</para>
    /// <para>示例值：true</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/helpdesk/v1/customized_fields")]
    Task<FeishuApiResult<GetCustomizedFieldsListResult>?> GetCustomizedFieldsListAsync(
       [Query] bool? visible_only = null,
       CancellationToken cancellationToken = default);
}
