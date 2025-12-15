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
}
