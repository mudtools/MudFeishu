// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.TaskList;

namespace Mud.Feishu;

/// <summary>
/// <para>飞书清单可以用于组织和管理属于同一个项目的多个任务。</para>
/// </summary>
[HttpClientApi(TokenManage = nameof(ITokenManager), IsAbstract = true)]
[Header("Authorization")]
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
    [Post("https://open.feishu.cn/open-apis/task/v2/tasklists")]
    Task<FeishuApiResult<TaskListOperationResult>?> CreateTaskListAsync(
        [Body] CreateTaskListRequest createTaskListRequest,
        [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// <para>获取一个清单的详细信息，包括清单名，所有者，清单成员等。</para>
    /// </summary>
    /// <param name="tasklist_guid">清单全局唯一GUID，示例值："d300a75f-c56a-4be9-80d1-e47653028ceb"。</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("https://open.feishu.cn/open-apis/task/v2/tasklists/{tasklist_guid}")]
    Task<FeishuApiResult<TaskListOperationResult>?> GetTaskListByIdAsync(
        [Path] string tasklist_guid,
        [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>更新清单，可以更新清单的名字和所有者。</para>
    /// <para>更新清单时，将update_fields字段中填写所有要修改的清单字段名，同时在tasklist字段中填写要修改的字段的新值即可。</para>
    /// </summary>
    /// <param name="tasklist_guid">清单全局唯一GUID，示例值："d300a75f-c56a-4be9-80d1-e47653028ceb"。</param>
    /// <param name="updateTaskListRequest">更新任务列表请求体</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Patch("https://open.feishu.cn/open-apis/task/v2/tasklists/{tasklist_guid}")]
    Task<FeishuApiResult<TaskListOperationResult>?> UpdateTaskListByIdAsync(
        [Path] string tasklist_guid,
        [Body] UpdateTaskListRequest updateTaskListRequest,
        [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>删除一个清单。删除清单后，不可对该清单做任何操作，也无法再访问到清单。清单被删除后不可恢复。</para>
    /// </summary>
    /// <param name="tasklist_guid">清单全局唯一GUID，示例值："d300a75f-c56a-4be9-80d1-e47653028ceb"。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Delete("https://open.feishu.cn/open-apis/task/v2/tasklists/{tasklist_guid}")]
    Task<FeishuNullDataApiResult?> DeleteTaskListByIdAsync(
         [Path] string tasklist_guid,
         CancellationToken cancellationToken = default);


    /// <summary>
    /// 向一个清单添加1个或多个协作成员。成员信息通过设置members字段实现。
    /// </summary>
    /// <param name="tasklist_guid">清单全局唯一GUID，示例值："d300a75f-c56a-4be9-80d1-e47653028ceb"。</param>
    /// <param name="addTaskListMemberRequest">添加清单成员请求体</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("https://open.feishu.cn/open-apis/task/v2/tasklists/{tasklist_guid}/add_members")]
    Task<FeishuApiResult<TaskListOperationResult>?> AddTaskListMemberByIdAsync(
       [Path] string tasklist_guid,
       [Body] AddTaskListMemberRequest addTaskListMemberRequest,
       [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
       CancellationToken cancellationToken = default);
}
