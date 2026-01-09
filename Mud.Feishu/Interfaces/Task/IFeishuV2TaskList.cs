// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.TaskList;

namespace Mud.Feishu.Interfaces;

/// <summary>
/// <para>飞书清单可以用于组织和管理属于同一个项目的多个任务。</para>
/// </summary>
[HttpClientApi(TokenManage = nameof(ITokenManager), IsAbstract = true)]
[Header(Consts.Authorization)]
public interface IFeishuV2TaskList
{
    /// <summary>
    /// <para>创建一个清单。清单可以用于组织和管理属于同一个项目的多个任务。</para>
    /// <para>创建时，必须填写清单的名字。同时，可以设置通过members字段设置清单的协作成员。</para>
    /// </summary>
    /// <param name="createTaskListRequest">创建任务列表请求体。</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/task/v2/tasklists")]
    Task<FeishuApiResult<TaskListOperationResult>?> CreateTaskListAsync(
        [Body] CreateTaskListRequest createTaskListRequest,
        [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// <para>获取一个清单的详细信息，包括清单名，所有者，清单成员等。</para>
    /// </summary>
    /// <param name="tasklist_guid">任务清单全局唯一GUID，示例值："d300a75f-c56a-4be9-80d1-e47653028ceb"。</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/task/v2/tasklists/{tasklist_guid}")]
    Task<FeishuApiResult<TaskListOperationResult>?> GetTaskListByIdAsync(
        [Path] string tasklist_guid,
        [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>更新清单，可以更新清单的名字和所有者。</para>
    /// <para>更新清单时，将update_fields字段中填写所有要修改的清单字段名，同时在tasklist字段中填写要修改的字段的新值即可。</para>
    /// </summary>
    /// <param name="tasklist_guid">任务清单全局唯一GUID，示例值："d300a75f-c56a-4be9-80d1-e47653028ceb"。</param>
    /// <param name="updateTaskListRequest">更新任务列表请求体</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Patch("/open-apis/task/v2/tasklists/{tasklist_guid}")]
    Task<FeishuApiResult<TaskListOperationResult>?> UpdateTaskListByIdAsync(
        [Path] string tasklist_guid,
        [Body] UpdateTaskListRequest updateTaskListRequest,
        [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>删除一个清单。删除清单后，不可对该清单做任何操作，也无法再访问到清单。清单被删除后不可恢复。</para>
    /// </summary>
    /// <param name="tasklist_guid">任务清单全局唯一GUID，示例值："d300a75f-c56a-4be9-80d1-e47653028ceb"。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Delete("/open-apis/task/v2/tasklists/{tasklist_guid}")]
    Task<FeishuNullDataApiResult?> DeleteTaskListByIdAsync(
         [Path] string tasklist_guid,
         CancellationToken cancellationToken = default);


    /// <summary>
    /// 向一个清单添加1个或多个协作成员。成员信息通过设置members字段实现。
    /// </summary>
    /// <param name="tasklist_guid">任务清单全局唯一GUID，示例值："d300a75f-c56a-4be9-80d1-e47653028ceb"。</param>
    /// <param name="addTaskListMemberRequest">添加清单成员请求体</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/task/v2/tasklists/{tasklist_guid}/add_members")]
    Task<FeishuApiResult<TaskListOperationResult>?> AddTaskListMemberByIdAsync(
       [Path] string tasklist_guid,
       [Body] AddTaskListMemberRequest addTaskListMemberRequest,
       [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
       CancellationToken cancellationToken = default);

    /// <summary>
    /// 移除清单的一个或多个协作成员。通过设置members字段表示要移除的成员信息。
    /// </summary>
    /// <param name="tasklist_guid">任务清单全局唯一GUID，示例值："d300a75f-c56a-4be9-80d1-e47653028ceb"。</param>
    /// <param name="removeTaskListMemberRequest">移除清单成员请求体</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/task/v2/tasklists/{tasklist_guid}/remove_members")]
    Task<FeishuApiResult<TaskListOperationResult>?> RemoveTaskListMemberByIdAsync(
         [Path] string tasklist_guid,
         [Body] RemoveTaskListMemberRequest removeTaskListMemberRequest,
         [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
         CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>分页获取一个清单的任务列表，返回任务的摘要信息。</para>
    /// </summary>
    /// <param name="completed">特定完成状态的任务，填写“true”表示返回已经完成的任务；“false”表示只返回未完成的任务；不填写表示不按完成状态过滤。</param>
    /// <param name="created_from">任务创建的起始时间戳（ms），闭区间，不填写默认为首个任务的创建时间戳，示例值：1675742789470</param>
    /// <param name="created_to">任务创建的结束时间戳（ms），闭区间，不填写默认为最后创建任务的创建时间戳，示例值：1675742789470</param>
    /// <param name="tasklist_guid">任务清单全局唯一GUID。 示例值："e297ddff-06ca-4166-b917-4ce57cd3a7a0"</param>
    /// <param name="page_size">分页大小，即本次请求所返回的用户信息列表内的最大条目数。默认值：10</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/task/v2/tasklists/{tasklist_guid}/tasks")]
    Task<FeishuApiPageListResult<TaskSummary>?> GetTaskListPageListByIdAsync(
        [Path] string tasklist_guid,
        [Query("page_size")] int page_size = Consts.PageSize,
        [Query("page_token")] string? page_token = null,
        [Query("completed")] bool? completed = null,
        [Query("created_from")] string? created_from = null,
        [Query("created_to")] string? created_to = null,
        [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>获取调用身份所有可读取的清单列表。</para>
    /// </summary>
    /// <param name="tasklist_guid">任务清单全局唯一GUID。 示例值："e297ddff-06ca-4166-b917-4ce57cd3a7a0"</param>
    /// <param name="page_size">分页大小，即本次请求所返回的用户信息列表内的最大条目数。默认值：10</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/task/v2/tasklists/{tasklist_guid}/tasks")]
    Task<FeishuApiPageListResult<TaskListInfo>?> GetTaskListPageListByIdAsync(
       [Path] string tasklist_guid,
       [Query("page_size")] int page_size = Consts.PageSize,
       [Query("page_token")] string? page_token = null,
       [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
       CancellationToken cancellationToken = default);
}
