// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using TaskManageDemo.Backend.Models.DTOs;

namespace TaskManageDemo.Backend.Services.Approval;

/// <summary>
/// 审批服务接口
/// </summary>
public interface IApprovalService
{
    /// <summary>
    /// 创建审批实例
    /// </summary>
    Task<ApprovalInstanceDto?> CreateApprovalAsync(
        CreateApprovalRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取审批实例详情
    /// </summary>
    Task<ApprovalInstanceDto?> GetApprovalAsync(
        string instanceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 取消审批实例
    /// </summary>
    Task<bool> CancelApprovalAsync(
        string instanceId,
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户审批列表
    /// </summary>
    Task<PagedResponse<ApprovalInstanceDto>> GetUserApprovalsAsync(
        string userId,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 创建任务延期审批
    /// </summary>
    Task<ApprovalInstanceDto?> CreateTaskDelayApprovalAsync(
        string taskGuid,
        string userId,
        DateTime newDueTime,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 创建任务删除审批
    /// </summary>
    Task<ApprovalInstanceDto?> CreateTaskDeleteApprovalAsync(
        string taskGuid,
        string userId,
        string reason,
        CancellationToken cancellationToken = default);
}
