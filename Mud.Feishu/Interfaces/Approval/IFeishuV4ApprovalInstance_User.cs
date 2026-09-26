// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Approval;

namespace Mud.Feishu;

/// <summary>
/// 审批实例（以用户身份调用）：支持抄送、催办、撤回当前用户身份提交的审批实例，查看已发起列表与实例详情，以及订阅/退订实例状态变更事件。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/approval-v4/instance/overview"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Approval")]
[Token(FeishuTokenTypes.UserAccessToken, Name = Consts.Authorization)]
public interface IFeishuUserV4ApprovalInstance : IFeishuAppContextSwitcher, ICurrentUserId
{
    /// <summary>
    /// 抄送审批实例。调用该接口将当前审批实例抄送给指定用户。被抄送的用户可以查看审批实例详情。
    /// <para>例如，在飞书客户端的 工作台 &gt; 审批 &gt; 审批中心 &gt; 抄送我 列表中查看到审批实例。</para>
    /// <para>官方文档：<see href="https://open.feishu.cn/api-explorer?from=op_doc_tab&amp;apiName=add_cc&amp;project=approval&amp;resource=instance&amp;version=v4"/></para>
    /// </summary>
    /// <param name="addCcInstanceRequest">抄送审批实例请求体</param>
    /// <param name="user_id_type">用户 ID 类型</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/approval/v4/instances/add_cc")]
    Task<FeishuNullDataApiResult?> AddCcInstanceAsync(
        [Body] AddCcInstanceRequest addCcInstanceRequest,
        [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取单个审批实例详情。通过审批实例 Code 获取指定审批实例的详细信息，包括审批实例的名称、创建时间、发起审批的用户、状态以及任务列表等信息。
    /// <para>官方文档：<see href="https://open.feishu.cn/api-explorer?from=op_doc_tab&amp;apiName=detail&amp;project=approval&amp;resource=instance&amp;version=v4"/></para>
    /// </summary>
    /// <param name="instance_code">审批实例 Code。示例值："7C468A54-8745-2245-9675-08B7C63E7A85"</param>
    /// <param name="locale">语言可选值，默认为审批定义配置的默认语言。示例值："zh-CN"</param>
    /// <param name="user_id_type">用户 ID 类型</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/approval/v4/instances/detail")]
    Task<FeishuApiResult<InstanceDetailResult>?> GetInstanceDetailAsync(
        [Query("instance_code")] string instance_code,
        [Query("locale")] string? locale = null,
        [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户已发起审批列表。以用户身份获取用户已发起的审批列表，与飞书中审批-&gt;审批中心-&gt;已发起一致。
    /// <para>官方文档：<see href="https://open.feishu.cn/api-explorer?from=op_doc_tab&amp;apiName=initiated&amp;project=approval&amp;resource=instance&amp;version=v4"/></para>
    /// </summary>
    /// <param name="page_size">分页大小，即本次请求所返回的最大条目数。默认值：10</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="locale">语言可选值，默认为审批定义配置的默认语言。示例值："zh-CN"</param>
    /// <param name="definition_code">审批定义 Code，用于筛选指定审批定义的已发起实例。示例值："7C468A54-8745-2245-9675-08B7C63E7A85"</param>
    /// <param name="start_timestamp">开始时间，Unix 时间戳（毫秒）。与 end_timestamp 参数构成查询条件。示例值："1547654251506"</param>
    /// <param name="end_timestamp">结束时间，Unix 时间戳（毫秒）。与 start_timestamp 参数构成查询条件。示例值："1547654251506"</param>
    /// <param name="user_id_type">用户 ID 类型</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/approval/v4/instances/initiated")]
    Task<FeishuApiResult<GetInitiatedInstancePageListResult>?> GetInitiatedInstancePageListAsync(
        [Query("page_size")] int page_size = Consts.PageSize_10,
        [Query("page_token")] string? page_token = null,
        [Query("locale")] string? locale = null,
        [Query("definition_code")] string? definition_code = null,
        [Query("start_timestamp")] string? start_timestamp = null,
        [Query("end_timestamp")] string? end_timestamp = null,
        [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 撤回审批实例。在符合撤销规则的情况下，你可以调用本接口将当前用户身份提交的审批实例撤回。
    /// <para>注意：如果撤回的是审批中的实例，则撤回后审批流程结束；如果撤回的是已通过的实例，则审批实例会变更为审批中的状态。</para>
    /// <para>官方文档：<see href="https://open.feishu.cn/api-explorer?from=op_doc_tab&amp;apiName=recall&amp;project=approval&amp;resource=instance&amp;version=v4"/></para>
    /// </summary>
    /// <param name="recallInstanceRequest">撤回审批实例请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/approval/v4/instances/recall")]
    Task<FeishuNullDataApiResult?> RecallInstanceAsync(
        [Body] RecallInstanceRequest recallInstanceRequest,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送催办消息。当需要催促审批人审批单据时，通过该接口给审批人发送催办消息。
    /// <para>官方文档：<see href="https://open.feishu.cn/api-explorer?from=op_doc_tab&amp;apiName=remind&amp;project=approval&amp;resource=instance&amp;version=v4"/></para>
    /// </summary>
    /// <param name="remindInstanceRequest">发送催办消息请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/approval/v4/instances/remind")]
    Task<FeishuNullDataApiResult?> RemindInstanceAsync(
        [Body] RemindInstanceRequest remindInstanceRequest,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 订阅审批实例状态变更事件。当应用订阅审批事件后，对于事件 type 为审批实例状态变更事件的事件，需要调用该接口指定需要接收通知的审批任务范围，指定后才可以接收到对应范围内的事件。
    /// <para>官方文档：<see href="https://open.feishu.cn/api-explorer?from=op_doc_tab&amp;apiName=subscription&amp;project=approval&amp;resource=instance&amp;version=v4"/></para>
    /// </summary>
    /// <para>订阅类型可选值：INVOLVED_APPROVAL（参与审批订阅）、MANAGED_APPROVAL（管理审批订阅）。</para>
    /// <param name="subscriptionRequest">订阅审批实例状态变更事件请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/approval/v4/instances/subscription")]
    Task<FeishuNullDataApiResult?> SubscribeInstanceStatusEventAsync(
        [Body] InstanceSubscriptionRequest subscriptionRequest,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 退订审批实例状态变更事件。当不再希望收到实例状态变更事件时，调用此接口，该接口用于撤销订阅审批实例状态变更事件中的操作。
    /// <para>官方文档：<see href="https://open.feishu.cn/api-explorer?from=op_doc_tab&amp;apiName=unsubscription&amp;project=approval&amp;resource=instance&amp;version=v4"/></para>
    /// </summary>
    /// <param name="subscription_type">订阅类型，可选值：INVOLVED_APPROVAL、MANAGED_APPROVAL；不传表示取消所有类别的订阅。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Delete("/open-apis/approval/v4/instances/subscription")]
    Task<FeishuNullDataApiResult?> UnsubscribeInstanceStatusEventAsync(
        [Query("subscription_type")] string? subscription_type = null,
        CancellationToken cancellationToken = default);
}
