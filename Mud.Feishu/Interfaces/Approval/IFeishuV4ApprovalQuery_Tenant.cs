// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.ApprovalQuery;

namespace Mud.Feishu;

/// <summary>
/// 通过不同条件查询审批系统中符合条件的审批实例、审批抄送、审批抄送列表(适用于原生审批及三方审批)。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/approval-v4/approval-search/query-2"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(ITenantTokenManager), RegistryGroupName = "Approval")]
[Header(Consts.Authorization)]
public interface IFeishuTenantV4ApprovalQuery
{

    /// <summary>
    /// 通过不同条件查询审批系统中符合条件的审批实例列表。
    /// </summary>
    /// <param name="approvalInstancesQueryRequest">查询实例列表请求体</param>
    /// <param name="page_size">分页大小，即本次请求所返回的用户信息列表内的最大条目数。默认值：10</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/approval/v4/instances/query")]
    Task<FeishuApiResult<ApprovalInstancesQueryResult>?> GetInstancesPageListAsync(
         [Body] ApprovalInstancesQueryRequest approvalInstancesQueryRequest,
         [Query("page_size")] int page_size = Consts.PageSize,
         [Query("page_token")] string? page_token = null,
         [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
         CancellationToken cancellationToken = default);


    /// <summary>
    /// 通过不同条件查询审批系统中符合条件的审批抄送列表。
    /// </summary>
    /// <param name="approvalInstancesCcQueryReques">查询抄送列表请求体</param>
    /// <param name="page_size">分页大小，即本次请求所返回的用户信息列表内的最大条目数。默认值：10</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/approval/v4/instances/search_cc")]
    Task<FeishuApiResult<ApprovalInstancesCcQueryResult>?> GetCarbonCopyPageListAsync(
        [Body] ApprovalInstancesCcQueryRequest approvalInstancesCcQueryReques,
        [Query("page_size")] int page_size = Consts.PageSize,
        [Query("page_token")] string? page_token = null,
        [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 通过不同条件查询审批系统中符合条件的审批任务列表。
    /// </summary>
    /// <param name="approvalInstancesTaskQueryRequest">查询任务列表请求体</param>
    /// <param name="page_size">分页大小，即本次请求所返回的用户信息列表内的最大条目数。默认值：10</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/approval/v4/tasks/search")]
    Task<FeishuApiResult<ApprovalInstancesTaskQueryResult>?> GetTasksPageListAsync(
        [Body] ApprovalInstancesTaskQueryRequest approvalInstancesTaskQueryRequest,
        [Query("page_size")] int page_size = Consts.PageSize,
        [Query("page_token")] string? page_token = null,
        [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);
}