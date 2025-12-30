// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Task;

namespace Mud.Feishu.Interfaces;

/// <summary>
/// <para>飞书任务是一款飞书自带的通用任务/项目管理工具，拥有强大的协作能力。</para>
/// <para>可以轻松地在飞书App的任务中心，群组，文档等场景中快捷创建任务。</para>
/// <para>同时也可以将任务分享给感兴趣的成员，或者关注和跟进一些感兴趣的任务。</para>
/// </summary>
[HttpClientApi(TokenManage = nameof(ITokenManager), IsAbstract = true)]
[Header(Consts.Authorization)]
public interface IFeishuV2Task
{
    /// <summary>
    /// <para>创建一个任务，在创建任务时，支持填写任务的基本信息（如标题、描述、负责人等），</para>
    /// <para>此外，还可以设置任务的开始时间、截止时间提醒等条件，</para>
    /// <para>此外，还可以通过传入 tasklists 字段将新任务加到多个清单中。</para>
    /// <para>创建任务时，可以通过设置members字段来设置任务的负责人和关注人。</para>
    /// </summary>
    /// <param name="createTaskRequest">创建任务请求体。</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/task/v2/tasks")]
    Task<FeishuApiResult<TaskOperationResult>?> CreateTaskAsync(
      [Body] CreateTaskRequest createTaskRequest,
      [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>用于修改任务的标题、描述、截止时间等信息。</para>
    /// </summary>
    /// <param name="task_guid">要更新的任务全局唯一ID。 示例值："e297ddff-06ca-4166-b917-4ce57cd3a7a0"</param>
    /// <param name="updateTaskRequest">更新任务请求体。</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Patch("/open-apis/task/v2/tasks/{task_guid}")]
    Task<FeishuApiResult<TaskOperationResult>?> UpdateTaskAsync(
        [Path] string task_guid,
        [Body] UpdateTaskRequest updateTaskRequest,
        [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>用于获取任务详情，包括任务标题、描述、时间、成员等信息。</para>
    /// </summary>
    /// <param name="task_guid">任务全局唯一ID。 示例值："e297ddff-06ca-4166-b917-4ce57cd3a7a0"</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/task/v2/tasks/{task_guid}")]
    Task<FeishuApiResult<TaskOperationResult>?> GetTaskByIdAsync(
        [Path] string task_guid,
        [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>删除一个任务。删除后任务无法再被获取到。</para>
    /// </summary>
    /// <param name="task_guid">要删除的任务全局唯一ID。 示例值："e297ddff-06ca-4166-b917-4ce57cd3a7a0"</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Delete("/open-apis/task/v2/tasks/{task_guid}")]
    Task<FeishuNullDataApiResult?> DeleteTaskByIdAsync(
       [Path] string task_guid,
       CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>添加任务成员</para>
    /// <para>添加任务的负责人或者关注人。一次性可以添加多个成员。返回任务的实体中会返回最终任务成员的列表。</para>
    /// </summary>
    /// <param name="task_guid">任务全局唯一ID。 示例值："e297ddff-06ca-4166-b917-4ce57cd3a7a0"</param>
    /// <param name="addMembersRequest">添加任务成员请求体。</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/task/v2/tasks/{task_guid}/add_members")]
    Task<FeishuApiResult<TaskOperationResult>?> AddMembersByIdAsync(
       [Path] string task_guid,
       [Body] AddMembersRequest addMembersRequest,
       [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
       CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>移除任务成员</para>
    /// <para>移除任务成员。一次性可以移除多个成员。可以移除任务的负责人或者关注人。</para>
    /// <para>移除时，如果要移除的成员不是任务成员，会被自动忽略。本接口返回移除成员后的任务数据，包含移除后的任务成员列表。</para>
    /// </summary>
    /// <param name="task_guid">任务全局唯一ID。 示例值："e297ddff-06ca-4166-b917-4ce57cd3a7a0"</param>
    /// <param name="removeMembersRequest">移除任务成员请求体。</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/task/v2/tasks/{task_guid}/remove_members")]
    Task<FeishuApiResult<TaskOperationResult>?> RemoveMembersByIdAsync(
      [Path] string task_guid,
      [Body] RemoveMembersRequest removeMembersRequest,
      [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// 列取一个任务所在的所有清单的信息，包括清单的GUID和所在自定义分组的GUID。
    /// </summary>
    /// <param name="task_guid">任务全局唯一ID。 示例值："e297ddff-06ca-4166-b917-4ce57cd3a7a0"</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/task/v2/tasks/{task_guid}/tasklists")]
    Task<FeishuApiResult<TaskGuidTaskListsResult>?> GetTaskListsByIdAsync(
     [Path] string task_guid,
     CancellationToken cancellationToken = default);

    /// <summary>
    /// 将一个任务加入清单。返回任务的详细信息，包括任务所在的所有清单信息。
    /// <para>如果任务已经在该清单，接口将返回成功。</para>
    /// </summary>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="addTasklistRequest">任务加入清单请求体</param>
    /// <param name="task_guid">任务全局唯一ID。 示例值："e297ddff-06ca-4166-b917-4ce57cd3a7a0"</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/task/v2/tasks/{task_guid}/add_tasklist")]
    Task<FeishuApiResult<AddTaskListResult>?> AddTaskListsByIdAsync(
       [Path] string task_guid,
       [Body] AddTasklistRequest addTasklistRequest,
       [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
       CancellationToken cancellationToken = default);

    /// <summary>
    /// 将任务从一个清单中移出。返回任务详情。
    /// <para>如果任务不在清单中，接口将返回成功。</para>
    /// </summary>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="removeTasklistRequest">移除任务清单请求体</param>
    /// <param name="task_guid">任务全局唯一ID。 示例值："e297ddff-06ca-4166-b917-4ce57cd3a7a0"</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/task/v2/tasks/{task_guid}/remove_tasklist")]
    Task<FeishuApiResult<RemoveTaskListResult>?> RemoveTaskListsByIdAsync(
      [Path] string task_guid,
      [Body] RemoveTasklistRequest removeTasklistRequest,
      [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// 为一个任务添加提醒。提醒是基于任务的截止时间计算得到的一个时刻。
    /// <para>为了设置提醒，任务必须首先拥有截止时间(due)。可以在创建任务时设置截止时间，或者通过更新任务设置一个截止时间。</para>
    /// </summary>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="addTaskReminderRequest">添加任务提醒请求体</param>
    /// <param name="task_guid">任务全局唯一ID。 示例值："e297ddff-06ca-4166-b917-4ce57cd3a7a0"</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/task/v2/tasks/{task_guid}/add_reminders")]
    Task<FeishuApiResult<AddTaskReminderResult>?> AddTaskReminderByIdAsync(
      [Path] string task_guid,
      [Body] AddTaskReminderRequest addTaskReminderRequest,
      [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// 将一个提醒从任务中移除。
    /// <para>如果要移除的提醒本来就不存在，本接口将直接返回成功。</para>
    /// </summary>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="removeReminderRequest">移除任务提醒请求体</param>
    /// <param name="task_guid">任务全局唯一ID。 示例值："e297ddff-06ca-4166-b917-4ce57cd3a7a0"</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/task/v2/tasks/{task_guid}/remove_reminders")]
    Task<FeishuApiResult<RemoveTaskReminderResult>?> RemoveTaskReminderByIdAsync(
     [Path] string task_guid,
     [Body] RemoveReminderRequest removeReminderRequest,
     [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
     CancellationToken cancellationToken = default);

    /// <summary>
    /// 为一个任务添加一个或多个依赖。可以添加任务的前置依赖和后置依赖。
    /// <para>存在依赖关系的任务如果在同一个清单，可以通过清单的甘特图来展示其依赖关系。</para>
    /// </summary>
    /// <param name="addTaskReminderRequest">添加任务依赖请求体</param>
    /// <param name="task_guid">任务全局唯一ID。 示例值："e297ddff-06ca-4166-b917-4ce57cd3a7a0"</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/task/v2/tasks/{task_guid}/add_dependencies")]
    Task<FeishuApiResult<TaskDependenciesOpreationResult>?> AddTaskDependenciesByIdAsync(
      [Path] string task_guid,
      [Body] AddTaskDependenciesRequest addTaskReminderRequest,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// 从一个任务移除一个或者多个依赖。移除时只需要输入要移除的task_guid即可。
    /// </summary>
    /// <param name="removeTaskDependenciesRequest">移除任务依赖请求体</param>
    /// <param name="task_guid">任务全局唯一ID。 示例值："e297ddff-06ca-4166-b917-4ce57cd3a7a0"</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>

    [Post("/open-apis/task/v2/tasks/{task_guid}/remove_dependencies")]
    Task<FeishuApiResult<TaskDependenciesOpreationResult>?> RemoveTaskDependenciesByIdAsync(
         [Path] string task_guid,
         [Body] RemoveTaskDependenciesRequest removeTaskDependenciesRequest,
         CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>给一个任务创建一个子任务。</para>
    /// <para>接口功能除了额外需要输入父任务的GUID之外，和创建任务接口功能完全一致。</para>
    /// </summary>
    /// <param name="createSubTaskRequest">创建子任务请求体。</param>
    /// <param name="task_guid">任务全局唯一ID。 示例值："e297ddff-06ca-4166-b917-4ce57cd3a7a0"</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/task/v2/tasks/{task_guid}/subtasks")]
    Task<FeishuApiResult<CreateSubTaskResult>?> CreateSubTaskAsync(
        [Path] string task_guid,
        [Body] CreateSubTaskRequest createSubTaskRequest,
        [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>分页获取一个任务的子任务列表。</para>
    /// </summary>
    /// <param name="task_guid">任务全局唯一ID。 示例值："e297ddff-06ca-4166-b917-4ce57cd3a7a0"</param>
    /// <param name="page_size">分页大小，即本次请求所返回的用户信息列表内的最大条目数。默认值：10</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/task/v2/tasks")]
    Task<FeishuApiPageListResult<SubTaskInfo>?> GetSubTasksPageListByIdAsync(
          [Path] string task_guid,
          [Query("page_size")] int page_size = 10,
          [Query("page_token")] string? page_token = null,
          [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
          CancellationToken cancellationToken = default);
}
