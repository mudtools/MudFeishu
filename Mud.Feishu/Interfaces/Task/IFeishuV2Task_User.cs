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
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/task-v2/task/overview"/></para>
/// </summary> 
[HttpClientApi(TokenManage = nameof(IUserTokenManager), RegistryGroupName = "Task", InheritedFrom = nameof(FeishuV2Task))]
[Header(Consts.Authorization)]
public interface IFeishuUserV2Task : IFeishuV2Task
{
    /// <summary>
    /// <para>基于调用身份，分页列出特定类型的所有任务。</para>
    /// <para>目前只支持列取任务界面上“我负责的”任务。返回的任务数据按照任务在”我负责的“界面中”自定义拖拽“的顺序排序。</para>
    /// </summary>
    /// <param name="page_size">分页大小，即本次请求所返回的用户信息列表内的最大条目数。默认值：10</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="completed">是否包含已完成任务，默认不包含。可选值：true、false</param>
    /// <param name="type">列取任务的类型，目前只支持"my_tasks"，即“我负责的”。</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/task/v2/tasks")]
    Task<FeishuApiPageListResult<ListTaskInfo>?> GetTasksPageListByIdAsync(
      [Query("page_size")] int page_size = 10,
      [Query("page_token")] string? page_token = null,
      [Query("completed")] bool? completed = null,
      [Query("type")] string? type = "my_tasks",
      [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
      CancellationToken cancellationToken = default);
}
