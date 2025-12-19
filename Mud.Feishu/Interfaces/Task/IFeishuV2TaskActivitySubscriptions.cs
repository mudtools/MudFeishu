// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.TaskActivitySubscriptions;

namespace Mud.Feishu;

/// <summary>
/// 任务清单动态订阅
/// </summary>
[HttpClientApi(TokenManage = nameof(ITokenManager), IsAbstract = true)]
[Header("Authorization")]
public interface IFeishuV2TaskActivitySubscriptions
{
    /// <summary>
    /// <para>为一个清单创建一个订阅。每个订阅可以包含1个或多个订阅者（目前只支持普通群组）。</para>
    /// <para>订阅创建后，如清单发生相应的事件，则会向订阅里的订阅者发送通知消息。</para>
    /// <para>一个清单最多可以创建50个订阅。每个订阅最大支持50个订阅者。订阅者目前仅支持"chat"类型。</para>
    /// </summary>
    /// <param name="createActivitySubscriptionsRequest">创建动态订阅请求体</param>
    /// <param name="tasklist_guid">任务清单全局唯一GUID，示例值："d300a75f-c56a-4be9-80d1-e47653028ceb"。</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("https://open.feishu.cn/open-apis/task/v2/tasklists/{tasklist_guid}/activity_subscriptions")]
    Task<FeishuApiResult<TasklistActivitySubscriptionResult>?> CreateActivitySubscriptionsAsync(
        [Path] string tasklist_guid,
        [Body] CreateActivitySubscriptionsRequest createActivitySubscriptionsRequest,
        [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>提供一个清单的GUID和一个订阅的GUID，获取该订阅的详细信息，包括名称，订阅者，可通知的event key列表等。</para>
    /// </summary>
    /// <param name="activity_subscription_guid">订阅GUID。可以通过创建动态订阅接口创建，或者通过列取动态订阅查询得到。示例值："33991879-704f-444f-81d7-55a6aa7be80c"</param>
    /// <param name="tasklist_guid">任务清单全局唯一GUID，示例值："d300a75f-c56a-4be9-80d1-e47653028ceb"。</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("https://open.feishu.cn/open-apis/task/v2/tasklists/{tasklist_guid}/activity_subscriptions/{activity_subscription_guid}")]
    Task<FeishuApiResult<TasklistActivitySubscriptionResult>?> GetActivitySubscriptionsByIdAsync(
       [Path] string tasklist_guid,
       [Path] string activity_subscription_guid,
       [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
       CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>给定一个清单的GUID，获取其所有的订阅信息。结果按照订阅的创建时间排序。</para>
    /// </summary>
    /// <param name="limit">返回结果的最大数量，默认值：50</param>
    /// <param name="tasklist_guid">任务清单全局唯一GUID，示例值："d300a75f-c56a-4be9-80d1-e47653028ceb"。</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>

    [Get("https://open.feishu.cn/open-apis/task/v2/tasklists/{tasklist_guid}/activity_subscriptions")]
    Task<FeishuApiListResult<TasklistActivitySubscriptionInfo>?> GetActivitySubscriptionsListByIdAsync(
       [Path] string tasklist_guid,
       [Query("limit")] int limit = 50,
       [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
       CancellationToken cancellationToken = default);
}
