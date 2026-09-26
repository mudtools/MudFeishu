// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.ApprovalTask;

namespace Mud.Feishu;

/// <summary>
/// 审批任务（以用户身份调用）：支持同意、拒绝、转交、退回、加签审批任务，获取任务列表，以及订阅/退订任务状态变更事件。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/approval-v4/task/introduction"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Approval")]
[Token(FeishuTokenTypes.UserAccessToken, Name = Consts.Authorization)]
public interface IFeishuUserV4ApprovalTask : IFeishuAppContextSwitcher, ICurrentUserId
{
    /// <summary>
    /// 加签审批任务。通过调用该接口在当前节点增加审批人。
    /// <para>官方文档：<see href="https://open.feishu.cn/api-explorer?from=op_doc_tab&amp;apiName=add_sign&amp;project=approval&amp;resource=task&amp;version=v4"/></para>
    /// </summary>
    /// <param name="addSignTaskRequest">加签审批任务请求体</param>
    /// <param name="user_id_type">用户 ID 类型</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/approval/v4/tasks/add_sign")]
    Task<FeishuNullDataApiResult?> AddSignApprovalTaskAsync(
        [Body] AddSignTaskRequest addSignTaskRequest,
        [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 转交审批任务。对于单个审批任务进行转交操作。转交后审批流程流转给被转交人。
    /// <para>官方文档：<see href="https://open.feishu.cn/api-explorer?from=op_doc_tab&amp;apiName=forward&amp;project=approval&amp;resource=task&amp;version=v4"/></para>
    /// </summary>
    /// <param name="forwardTaskRequest">转交审批任务请求体</param>
    /// <param name="user_id_type">用户 ID 类型</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/approval/v4/tasks/forward")]
    Task<FeishuNullDataApiResult?> ForwardApprovalTaskAsync(
        [Body] ForwardTaskRequest forwardTaskRequest,
        [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取审批任务列表。通过设置任务分组、审批定义 Code（审批流程的唯一标识）等，查询任务列表。任务分组包括待办审批、已办审批等。
    /// <para>官方文档：<see href="https://open.feishu.cn/api-explorer?from=op_doc_tab&amp;apiName=list&amp;project=approval&amp;resource=task&amp;version=v4"/></para>
    /// </summary>
    /// <param name="page_size">分页大小，即本次请求所返回的最大条目数。默认值：100</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="topic">任务分组，用于查询指定分组的审批任务。可选值：1：待办审批；2：已办审批。</param>
    /// <param name="locale">语言可选值，默认为审批定义配置的默认语言。示例值："zh-CN"</param>
    /// <param name="definition_code">审批定义 Code，用于筛选指定审批定义的任务。示例值："7C468A54-8745-2245-9675-08B7C63E7A85"</param>
    /// <param name="start_timestamp">开始时间，Unix 时间戳（毫秒）。与 end_timestamp 参数构成查询条件。示例值："1547654251506"</param>
    /// <param name="end_timestamp">结束时间，Unix 时间戳（毫秒）。与 start_timestamp 参数构成查询条件。示例值："1547654251506"</param>
    /// <param name="user_id_type">用户 ID 类型</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/approval/v4/tasks")]
    Task<FeishuApiResult<GetApprovalTaskPageListResult>?> GetApprovalTaskPageListAsync(
        [Query("page_size")] int page_size = Consts.PageSize_100,
        [Query("page_token")] string? page_token = null,
        [Query("topic")] string? topic = null,
        [Query("locale")] string? locale = null,
        [Query("definition_code")] string? definition_code = null,
        [Query("start_timestamp")] string? start_timestamp = null,
        [Query("end_timestamp")] string? end_timestamp = null,
        [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 同意审批任务。对于单个审批任务进行同意操作。同意后审批流程会流转到下一个审批人。
    /// <para>官方文档：<see href="https://open.feishu.cn/api-explorer?from=op_doc_tab&amp;apiName=pass&amp;project=approval&amp;resource=task&amp;version=v4"/></para>
    /// </summary>
    /// <param name="passTaskRequest">同意审批任务请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/approval/v4/tasks/pass")]
    Task<FeishuNullDataApiResult?> PassApprovalTaskAsync(
        [Body] PassTaskRequest passTaskRequest,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 拒绝审批任务。对于单个审批任务进行拒绝操作。拒绝后审批流程结束。
    /// <para>官方文档：<see href="https://open.feishu.cn/api-explorer?from=op_doc_tab&amp;apiName=refuse&amp;project=approval&amp;resource=task&amp;version=v4"/></para>
    /// </summary>
    /// <param name="refuseTaskRequest">拒绝审批任务请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/approval/v4/tasks/refuse")]
    Task<FeishuNullDataApiResult?> RefuseApprovalTaskAsync(
        [Body] RefuseTaskRequest refuseTaskRequest,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 退回审批任务。从当前审批任务，退回到已审批的一个或多个任务节点。退回后，已审批节点重新生成审批任务。
    /// <para>官方文档：<see href="https://open.feishu.cn/api-explorer?from=op_doc_tab&amp;apiName=rollback&amp;project=approval&amp;resource=task&amp;version=v4"/></para>
    /// </summary>
    /// <param name="rollbackTaskRequest">退回审批任务请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/approval/v4/tasks/rollback")]
    Task<FeishuNullDataApiResult?> RollbackApprovalTaskAsync(
        [Body] RollbackTaskRequest rollbackTaskRequest,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 订阅审批任务状态变更事件。当应用订阅审批事件后，对于事件 type 为审批任务状态变更事件的事件，需要调用该接口指定需要接收通知的审批任务范围，指定后才可以接收到对应范围内的事件。
    /// <para>官方文档：<see href="https://open.feishu.cn/api-explorer?from=op_doc_tab&amp;apiName=subscription&amp;project=approval&amp;resource=task&amp;version=v4"/></para>
    /// </summary>
    /// <para>订阅类型可选值：INVOLVED_APPROVAL（参与审批订阅）、MANAGED_APPROVAL（管理审批订阅）。</para>
    /// <param name="subscriptionRequest">订阅审批任务状态变更事件请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/approval/v4/tasks/subscription")]
    Task<FeishuNullDataApiResult?> SubscribeTaskStatusEventAsync(
        [Body] TaskSubscriptionRequest subscriptionRequest,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 退订审批任务状态变更事件。当不再希望收到任务状态变更事件时，调用此接口，该接口用于撤销订阅审批任务状态变更事件中的操作。
    /// <para>官方文档：<see href="https://open.feishu.cn/api-explorer?from=op_doc_tab&amp;apiName=unsubscription&amp;project=approval&amp;resource=task&amp;version=v4"/></para>
    /// </summary>
    /// <param name="subscription_type">订阅类型，可选值：INVOLVED_APPROVAL、MANAGED_APPROVAL；不传表示取消所有类别的订阅。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Delete("/open-apis/approval/v4/tasks/subscription")]
    Task<FeishuNullDataApiResult?> UnsubscribeTaskStatusEventAsync(
        [Query("subscription_type")] string? subscription_type = null,
        CancellationToken cancellationToken = default);
}
