// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using TaskManageDemo.Backend.Data;
using TaskManageDemo.Backend.Models.Entities;

namespace TaskManageDemo.Backend.EventHandlers;

/// <summary>
/// 审批实例事件处理器
/// 处理飞书审批实例的回调事件
/// </summary>
public class ApprovalInstanceEventHandler
{
    private readonly TaskManageDbContext _dbContext;
    private readonly ILogger<ApprovalInstanceEventHandler> _logger;
    private readonly IEventProcessService _eventProcessService;

    public ApprovalInstanceEventHandler(
        TaskManageDbContext dbContext,
        ILogger<ApprovalInstanceEventHandler> logger,
        IEventProcessService eventProcessService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _eventProcessService = eventProcessService;
    }

    /// <summary>
    /// 处理审批实例开始事件
    /// </summary>
    public async Task HandleApprovalStartedAsync(
        string instanceId,
        string approvalCode,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var eventId = $"approval_started_{instanceId}";

        // 检查幂等性
        if (await _eventProcessService.IsProcessedAsync(eventId, cancellationToken))
        {
            _logger.LogWarning("审批开始事件已处理: {InstanceId}", instanceId);
            return;
        }

        var record = await _eventProcessService.StartProcessAsync(eventId, "ApprovalStarted", cancellationToken);

        try
        {
            _logger.LogInformation("处理审批开始事件: InstanceId={InstanceId}, ApprovalCode={ApprovalCode}", 
                instanceId, approvalCode);

            // 记录审批历史
            var history = new ApprovalHistory
            {
                InstanceId = instanceId,
                ApprovalCode = approvalCode,
                UserId = userId,
                Action = "started",
                ActionTime = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Add(history);
            await _dbContext.SaveChangesAsync(cancellationToken);

            await _eventProcessService.MarkSuccessAsync(record.Id, cancellationToken);
            _logger.LogInformation("审批开始事件处理完成: {InstanceId}", instanceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理审批开始事件失败: {InstanceId}", instanceId);
            await _eventProcessService.MarkFailedAsync(record.Id, ex.Message, cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// 处理审批实例通过事件
    /// </summary>
    public async Task HandleApprovalPassedAsync(
        string instanceId,
        string approvalCode,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var eventId = $"approval_passed_{instanceId}";

        if (await _eventProcessService.IsProcessedAsync(eventId, cancellationToken))
        {
            _logger.LogWarning("审批通过事件已处理: {InstanceId}", instanceId);
            return;
        }

        var record = await _eventProcessService.StartProcessAsync(eventId, "ApprovalPassed", cancellationToken);

        try
        {
            _logger.LogInformation("处理审批通过事件: InstanceId={InstanceId}", instanceId);

            var history = new ApprovalHistory
            {
                InstanceId = instanceId,
                ApprovalCode = approvalCode,
                UserId = userId,
                Action = "passed",
                ActionTime = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Add(history);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // 更新关联任务状态（如果有）
            await UpdateRelatedTaskStatusAsync(instanceId, "approved", cancellationToken);

            await _eventProcessService.MarkSuccessAsync(record.Id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理审批通过事件失败: {InstanceId}", instanceId);
            await _eventProcessService.MarkFailedAsync(record.Id, ex.Message, cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// 处理审批实例拒绝事件
    /// </summary>
    public async Task HandleApprovalRejectedAsync(
        string instanceId,
        string approvalCode,
        string userId,
        string? rejectReason,
        CancellationToken cancellationToken = default)
    {
        var eventId = $"approval_rejected_{instanceId}";

        if (await _eventProcessService.IsProcessedAsync(eventId, cancellationToken))
        {
            _logger.LogWarning("审批拒绝事件已处理: {InstanceId}", instanceId);
            return;
        }

        var record = await _eventProcessService.StartProcessAsync(eventId, "ApprovalRejected", cancellationToken);

        try
        {
            _logger.LogInformation("处理审批拒绝事件: InstanceId={InstanceId}", instanceId);

            var history = new ApprovalHistory
            {
                InstanceId = instanceId,
                ApprovalCode = approvalCode,
                UserId = userId,
                Action = "rejected",
                ActionTime = DateTime.UtcNow,
                Comment = rejectReason,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Add(history);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // 更新关联任务状态
            await UpdateRelatedTaskStatusAsync(instanceId, "rejected", cancellationToken);

            await _eventProcessService.MarkSuccessAsync(record.Id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理审批拒绝事件失败: {InstanceId}", instanceId);
            await _eventProcessService.MarkFailedAsync(record.Id, ex.Message, cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// 更新关联任务状态
    /// </summary>
    private async Task UpdateRelatedTaskStatusAsync(
        string instanceId,
        string status,
        CancellationToken cancellationToken)
    {
        // TODO: 实现任务-审批关联逻辑
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
