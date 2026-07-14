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


    /// <summary>
    /// 发送工单消息
    /// <para>用于发送工单消息。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/helpdesk-v1/ticket-management/ticket-message/create">接口文档</see></para>
    /// </summary>   
    /// <param name="ticket_id">
    /// <para>工单 ID。可通过[查询全部工单详情](<see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/helpdesk-v1/ticket/list">获取</see>)</para>
    /// <para>示例值：123456</para>
    /// </param>
    /// <param name="request">发送工单消息请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/helpdesk/v1/tickets/{ticket_id}/messages")]
    Task<FeishuApiResult<CreateTicketMessageResult>?> CreateTicketMessageAsync(
          [Path] string ticket_id,
          [Body] CreateTicketMessageRequest request,
          CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取工单消息详情
    /// <para>用于获取服务台工单消息详情。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/helpdesk-v1/ticket-management/ticket-message/list">接口文档</see></para>
    /// </summary>   
    /// <param name="ticket_id">
    /// <para>工单 ID。可通过[查询全部工单详情](<see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/helpdesk-v1/ticket/list">获取</see>)</para>
    /// <para>示例值：123456</para>
    /// </param>
    /// <param name="time_start">
    /// <para>起始时间</para>
    /// <para>示例值：1617960686</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="time_end">
    /// <para>结束时间</para>
    /// <para>示例值：1617960687</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="page">
    /// <para>页数ID</para>
    /// <para>示例值：1</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="page_size">
    /// <para>消息数量，最大200，默认20</para>
    /// <para>示例值：10</para>
    /// <para>默认值：10</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/helpdesk/v1/tickets/{ticket_id}/messages")]
    Task<FeishuApiResult<GetTicketMessageListResult>?> GetTicketMessageListAsync(
        [Path] string ticket_id,
        [Query] int? time_start = null,
        [Query] int? time_end = null,
        [Query] int? page = null,
        [Query] int? page_size = 10,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 服务台机器人向工单绑定的群内发送消息
    /// <para>通过服务台机器人给指定用户的服务台专属群或私聊发送消息，支持文本、富文本、卡片、图片。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/helpdesk-v1/ticket-management/ticket-message/create-2">接口文档</see></para>
    /// </summary>  
    /// <param name="request">服务台机器人向工单绑定的群内发送消息请求体</param>
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
    [Post("/open-apis/helpdesk/v1/message")]
    Task<FeishuApiResult<CreateTicketMessageResult>?> CreateBotMessageAsync(
        [Body] CreateBotMessageRequest request,
        [Query] string? user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);



    /// <summary>
    /// 获取指定工单自定义字段
    /// <para>用于获取工单自定义字段详情。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/helpdesk-v1/ticket-management/ticket_customized_field/get-ticket-customized-field">接口文档</see></para>
    /// </summary>   
    /// <param name="ticket_customized_field_id">
    /// <para>工单自定义字段ID</para>
    /// <para>示例值：6948728206392295444</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/helpdesk/v1/ticket_customized_fields/{ticket_customized_field_id}")]
    Task<FeishuApiResult<GetCustomizedFieldResult>?> GetCustomizedFieldAsync(
      [Path] string ticket_customized_field_id,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取全部工单自定义字段
    /// <para>用于获取全部工单自定义字段。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/helpdesk-v1/ticket-management/ticket_customized_field/get-ticket-customized-field">接口文档</see></para>
    /// </summary>
    /// <param name="request">获取工单自定义字段请求体</param>
    /// <param name="page_token">
    /// <para>必填：否</para>
    /// <para>分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</para>
    /// <para>**示例值**："6948728206392295444"</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="page_size">
    /// <para>必填：否</para>
    /// <para>分页大小</para>
    /// <para>**示例值**：10；默认为20</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 最大值：`100`</para>
    /// <para>默认值：10</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/helpdesk/v1/ticket_customized_fields")]
    Task<FeishuApiResult<GetCustomizedFieldListResult>?> GetCustomizedFieldPageListAsync(
         [Body] GetCustomizedFieldRequest request,
         [Query] string? page_token = null,
         [Query] int? page_size = 10,
         CancellationToken cancellationToken = default);
}
