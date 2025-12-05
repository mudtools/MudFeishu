// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Task;

namespace Mud.Feishu;

/// <summary>
/// <para>飞书任务是一款飞书自带的通用任务/项目管理工具，拥有强大的协作能力。</para>
/// <para>可以轻松地在飞书App的任务中心，群组，文档等场景中快捷创建任务。</para>
/// <para>同时也可以将任务分享给感兴趣的成员，或者关注和跟进一些感兴趣的任务。</para>
/// </summary>
[HttpClientApi(TokenManage = nameof(ITokenManager), IsAbstract = true)]
[Header("Authorization")]
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
    [Post("https://open.feishu.cn/open-apis/task/v2/tasks")]
    Task<FeishuApiResult<CreateTaskResult>?> CreateTaskAsync(
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
    [Post("https://open.feishu.cn/open-apis/task/v2/tasks/{task_guid}")]
    Task<FeishuApiResult<UpdateTaskResult>?> UpdateTaskAsync(
        [Path] string task_guid,
        [Body] UpdateTaskRequest updateTaskRequest,
        [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);
}
