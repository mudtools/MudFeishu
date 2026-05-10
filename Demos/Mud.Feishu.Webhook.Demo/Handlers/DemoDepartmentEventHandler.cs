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
using Mud.Feishu.Webhook.Demo.Services;

namespace Mud.Feishu.Webhook.Demo.Handlers;

/// <summary>
/// 演示部门事件处理器
/// </summary>
public class DemoDepartmentEventHandler : DepartmentCreatedEventHandler
{
    private readonly DemoEventService _eventService;

    public DemoDepartmentEventHandler(IFeishuEventDeduplicator businessDeduplicator, ILogger<DemoDepartmentEventHandler> logger, DemoEventService eventService) : base(businessDeduplicator, logger)
    {
        _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
    }

    protected override async Task ProcessBusinessLogicAsync(EventData eventData, DepartmentCreatedResult? eventEntity, FeishuEventHeader? header, CancellationToken cancellationToken = default)
    {
        if (eventData == null)
            throw new ArgumentNullException(nameof(eventData));

        _logger.LogInformation(">> [部门事件] 开始处理部门创建事件: {EventId}", eventData.EventId);

        try
        {

            // 记录事件到服务
            await _eventService.RecordDepartmentEventAsync(eventEntity, cancellationToken);

            // 模拟业务处理
            await ProcessDepartmentEventAsync(eventEntity, cancellationToken);

            _logger.LogInformation(">> [部门事件] 部门创建事件处理完成: 部门ID {DepartmentId}, 部门名 {DepartmentName}",
                eventEntity.DepartmentId, eventEntity.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ">> [部门事件] 处理部门创建事件失败: {EventId}", eventData.EventId);
            throw;
        }
    }


    private async Task ProcessDepartmentEventAsync(DepartmentCreatedResult departmentData, CancellationToken cancellationToken)
    {
        _logger.LogDebug(">> [部门事件] 开始处理部门数据: {DepartmentId}", departmentData.DepartmentId);

        // 模拟异步业务操作
        await Task.Delay(100, cancellationToken);

        // 模拟验证逻辑
        if (string.IsNullOrWhiteSpace(departmentData.DepartmentId))
        {
            throw new ArgumentException("部门ID不能为空");
        }

        // 模拟权限初始化
        _logger.LogInformation(">> [部门事件] 初始化部门权限: {DepartmentName}", departmentData.Name);

        // 模拟通知部门主管
        if (!string.IsNullOrWhiteSpace(departmentData.LeaderUserId))
        {
            _logger.LogInformation(">>[部门事件] 通知部门主管: {LeaderUserId}", departmentData.LeaderUserId);
        }

        // 模拟更新统计信息
        _eventService.IncrementDepartmentCount();

        // 模拟层级关系处理
        if (!string.IsNullOrWhiteSpace(departmentData.ParentDepartmentId))
        {
            _logger.LogInformation(">>[部门事件] 建立层级关系: {DepartmentId} -> {ParentDepartmentId}",
                departmentData.DepartmentId, departmentData.ParentDepartmentId);
        }

        await Task.CompletedTask;
    }
}
