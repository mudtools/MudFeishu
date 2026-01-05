// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.ApprovalTask;

namespace Mud.Feishu;

/// <summary>
/// 原生审批实例的流程中包含多个审批节点，审批节点内根据设置的审批人情况，会生成审批任务（一个审批人对应一个审批任务），使用原生审批任务 API，可以同意、拒绝、转交以及退回审批任务。
/// 接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/approval-v4/task/introduction"/>
/// </summary>
[HttpClientApi(TokenManage = nameof(ITenantTokenManager), RegistryGroupName = "Approval")]
[Header(Consts.Authorization)]
public interface IFeishuTenantV4ApprovalTask
{

    /// <summary>
    /// 对单个审批任务进行同意操作。同意后审批流程会流转到下一个审批人。
    /// </summary>
    /// <param name="agreeApprovalTasksRequest">同意审批任务请求体</param>
    /// <param name="user_id_type">用户 ID 类型</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/approval/v4/tasks/approve")]
    Task<FeishuNullDataApiResult?> AgreeApprovalAsync(
        [Body] AgreeApprovalTasksRequest agreeApprovalTasksRequest,
        [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 对单个审批任务进行拒绝操作。拒绝后审批流程结束。
    /// </summary>
    /// <param name="rejectApprovalTaskRequest">拒绝审批任务请求体</param>
    /// <param name="user_id_type">用户 ID 类型</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/approval/v4/tasks/reject")]
    Task<FeishuNullDataApiResult?> RejectApprovalAsync(
      [Body] RejectApprovalTaskRequest rejectApprovalTaskRequest,
      [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// 对单个审批任务进行转交操作。转交后审批流程流转给被转交人。
    /// </summary>
    /// <param name="transferApprovalTasksRequest">转交审批任务请求体</param>
    /// <param name="user_id_type">用户 ID 类型</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/approval/v4/tasks/transfer")]
    Task<FeishuNullDataApiResult?> TransferApprovalAsync(
        [Body] TransferApprovalTasksRequest transferApprovalTasksRequest,
        [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 从当前审批任务，退回到已审批的一个或多个任务节点。退回后，已审批节点重新生成审批任务。
    /// </summary>
    /// <param name="rollbackApprovalInstancesRequest">退回审批任务请求体</param>
    /// <param name="user_id_type">用户 ID 类型</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/approval/v4/instances/specified_rollback")]
    Task<FeishuNullDataApiResult?> RollbackApprovalAsync(
       [Body] RollbackApprovalInstancesRequest rollbackApprovalInstancesRequest,
       [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
       CancellationToken cancellationToken = default);


    /// <summary>
    /// 对单个审批任务进行加签操作。
    /// </summary>
    /// <param name="instancesAddSignRequest">审批任务加签请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/approval/v4/instances/add_sign")]
    Task<FeishuNullDataApiResult?> InstancesAddSignAsync(
         [Body] InstancesAddSignRequest instancesAddSignRequest,
         CancellationToken cancellationToken = default);
}
