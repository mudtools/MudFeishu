// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.ApprovalQuery;

namespace Mud.Feishu.Interfaces;

/// <summary>
/// 通过不同条件查询审批系统中符合条件的审批实例、审批抄送、审批抄送列表(适用于原生审批及三方审批)。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/approval-v4/approval-search/query-2"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuV4ApprovalQuery : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 根据用户和任务分组查询任务列表。
    /// </summary>
    /// <param name="user_id">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="topic">审批主题，用于查询指定主题的审批任务。如「待办」、「已办」等
    /// <para>1:待办审批,2:已办审批,3:已发起审批,17:未读知会,18:已读知会</para>
    /// <list type="bullet">
    /// <item>1：待办审批</item>
    /// <item>2：已办审批</item>
    /// <item>3：已发起审批</item>
    /// <item>17：未读知会</item>
    /// <item>18：已读知会</item>
    /// </list>
    /// </param>
    /// <param name="page_size">分页大小，即本次请求所返回的用户信息列表内的最大条目数。默认值：10</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/approval/v4/tasks/query")]
    Task<FeishuApiResult<ApprovalInstancesTaskUserQueryResult>?> GetTasksPageListByUserIdAsync(
       [Query("user_id")] string user_id,
       [Query("topic")] string topic,
       [Query("page_size")] int page_size = Consts.PageSize_10,
       [Query("page_token")] string? page_token = null,
       [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
       CancellationToken cancellationToken = default);
}
