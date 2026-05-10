// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.Services;
using Mud.Feishu.EventCallback;
using Mud.Feishu.EventCallback.Organization;

namespace Mud.Feishu.Webhook.Demo.Handlers.MultiApp;

/// <summary>
/// App1 专用 - 部门删除事件处理器（组织架构应用）
/// </summary>
public class App1DepartmentDeleteEventHandler : DepartmentDeleteEventHandler
{
    public App1DepartmentDeleteEventHandler(IFeishuEventDeduplicator businessDeduplicator, ILogger<App1DepartmentDeleteEventHandler> logger)
        : base(businessDeduplicator, logger)
    {
    }

    protected override async Task ProcessBusinessLogicAsync(EventData eventData, DepartmentDeleteResult? eventEntity, FeishuEventHeader? header, CancellationToken cancellationToken = default)
    {
        if (eventData == null)
            throw new ArgumentNullException(nameof(eventData));

        _logger.LogInformation(">> [App1-部门删除] 开始处理部门删除事件: {EventId}", eventData.EventId);

        try
        {
            // App1特定的部门删除处理逻辑
            await ProcessDepartmentDeleteAsync(eventEntity, cancellationToken);

            _logger.LogInformation(">> [App1-部门删除] 部门删除事件处理完成: 部门ID {DepartmentId}",
                eventEntity?.Object?.DepartmentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ">> [App1-部门删除] 处理部门删除事件失败: {EventId}", eventData.EventId);
            throw;
        }
    }

    private async Task ProcessDepartmentDeleteAsync(DepartmentDeleteResult? departmentData, CancellationToken cancellationToken)
    {
        _logger.LogDebug(">> [App1-部门删除] 开始处理App1部门删除: {DepartmentId}", departmentData?.Object?.DepartmentId);

        await Task.Delay(50, cancellationToken);

        // App1特定的删除逻辑
        if (!string.IsNullOrWhiteSpace(departmentData?.Object?.DepartmentId))
        {
            _logger.LogInformation(">> [App1-部门删除] 清理App1部门数据: {DepartmentId}", departmentData.Object?.DepartmentId);
            // TODO: 实现实际的清理逻辑
        }

        await Task.CompletedTask;
    }
}
