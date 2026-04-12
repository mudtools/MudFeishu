// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.Services;
using Mud.Feishu.EventCallback;
using Mud.Feishu.EventCallback.Approval;
using TaskManageDemo.Backend.Data;

namespace TaskManageDemo.Backend.EventHandlers;

/// <summary>
/// 审批实例事件处理器
/// <para>处理飞书审批实例状态变更事件</para>
/// </summary>
public class FeishuApprovalInstanceEventHandler : ApprovalInstanceEventHandler
{
    private readonly TaskManageDbContext _dbContext;

    public FeishuApprovalInstanceEventHandler(
        IFeishuEventDeduplicator businessDeduplicator,
        ILogger<FeishuApprovalInstanceEventHandler> logger,
        TaskManageDbContext dbContext)
        : base(businessDeduplicator, logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    protected override async Task ProcessBusinessLogicAsync(
        EventData eventData,
        ApprovalInstanceResult? eventEntity,
        CancellationToken cancellationToken = default)
    {
        if (eventEntity == null)
        {
            _logger.LogWarning("审批实例事件实体为空，跳过处理");
            return;
        }

        var instanceCode = eventEntity.InstanceCode;
        var approvalCode = eventEntity.ApprovalCode;
        var status = eventEntity.Status;

        _logger.LogInformation("处理审批实例状态变更事件: InstanceCode={InstanceCode}, ApprovalCode={ApprovalCode}, Status={Status}",
            instanceCode, approvalCode, status);

        var statusDescription = status switch
        {
            "PENDING" => "审批中",
            "APPROVED" => "已通过",
            "REJECTED" => "已拒绝",
            "CANCELED" => "已撤回",
            "DELETED" => "已删除",
            "REVERTED" => "已撤销",
            "OVERTIME_CLOSE" => "超时被关闭",
            "OVERTIME_RECOVER" => "超时实例被恢复",
            _ => "未知状态"
        };

        _logger.LogDebug("审批状态: {StatusDescription}", statusDescription);

        var history = new ApprovalHistory
        {
            InstanceId = instanceCode ?? string.Empty,
            ApprovalCode = approvalCode ?? string.Empty,
            UserId = eventData.AppId ?? string.Empty,
            Action = status ?? string.Empty,
            ActionTime = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Add(history);
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (status == "APPROVED" || status == "REJECTED")
        {
            await UpdateRelatedTaskStatusAsync(instanceCode ?? string.Empty, status, cancellationToken);
        }

        _logger.LogInformation("审批实例事件处理完成: InstanceCode={InstanceCode}, Status={Status}",
            instanceCode, status);
    }

    private async Task UpdateRelatedTaskStatusAsync(
        string instanceId,
        string status,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("更新任务审批状态: InstanceId={InstanceId}, Status={Status}",
            instanceId, status);
        await Task.CompletedTask;
    }
}

/// <summary>
/// 审批历史记录实体
/// </summary>
public class ApprovalHistory
{
    public int Id { get; set; }
    public string InstanceId { get; set; } = string.Empty;
    public string ApprovalCode { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public DateTime ActionTime { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}
