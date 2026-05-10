// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.Services;
using Mud.Feishu.EventCallback;
using Mud.Feishu.EventCallback.Approval;

namespace Mud.Feishu.Webhook.Demo.Handlers.MultiApp;

/// <summary>
/// App2 专用 - 审批通过事件处理器（用户和审批应用）
/// </summary>
public class App2ApprovalPassedEventHandler : ApprovalApprovalUpdatedEventHandler
{
    public App2ApprovalPassedEventHandler(IFeishuEventDeduplicator businessDeduplicator, ILogger<App2ApprovalPassedEventHandler> logger)
        : base(businessDeduplicator, logger)
    {
    }

    protected override async Task ProcessBusinessLogicAsync(EventData eventData, ApprovalApprovalUpdatedResult? eventEntity, FeishuEventHeader? header, CancellationToken cancellationToken = default)
    {
        if (eventData == null)
            throw new ArgumentNullException(nameof(eventData));

        _logger.LogInformation(">> [App2-审批通过] 开始处理审批通过事件: {EventId}", eventData.EventId);

        try
        {
            // App2特定的审批通过处理逻辑
            await ProcessApprovalPassedAsync(eventEntity, cancellationToken);

            _logger.LogInformation(">> [App2-审批通过] 审批通过事件处理完成: 审批单号 {ApprovalNumber}",
                eventEntity?.Object.ApprovalCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ">> [App2-审批通过] 处理审批通过事件失败: {EventId}", eventData.EventId);
            throw;
        }
    }


    private async Task ProcessApprovalPassedAsync(ApprovalApprovalUpdatedResult? approvalData, CancellationToken cancellationToken)
    {
        _logger.LogDebug(">> [App2-审批通过] 开始处理App2审批通过: {ApprovalNumber}", approvalData?.Object?.ApprovalCode);

        await Task.Delay(100, cancellationToken);

        if (approvalData?.Object == null || string.IsNullOrWhiteSpace(approvalData.Object.ApprovalCode))
        {
            throw new ArgumentException("App2: 审批数据不能为空");
        }

        // App2特定的审批后处理
        _logger.LogInformation(">> [App2-审批通过] 执行App2审批后业务逻辑: {ApprovalNumber}", approvalData.Object.ApprovalCode);
        // TODO: 实现实际的审批后处理逻辑

        await Task.CompletedTask;
    }
}
