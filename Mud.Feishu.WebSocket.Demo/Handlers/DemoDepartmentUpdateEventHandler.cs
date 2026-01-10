// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.DataModels.Organization;
using Mud.Feishu.Abstractions.EventHandlers;
using Mud.Feishu.WebSocket.Services;

namespace Mud.Feishu.WebSocket.Demo.Handlers;

/// <summary>
/// 演示部门事件处理器
/// </summary>
public class DemoDepartmentUpdateEventHandler : DepartmentUpdateEventHandler
{
    private readonly DemoEventService _eventService;

    public DemoDepartmentUpdateEventHandler(ILogger<DemoDepartmentUpdateEventHandler> logger, DemoEventService eventService) : base(logger)
    {
        _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
    }

    protected override async Task ProcessBusinessLogicAsync(EventData eventData, DepartmentUpdateResult? eventEntity, CancellationToken cancellationToken = default)
    {
        if (eventData == null)
            throw new ArgumentNullException(nameof(eventData));

        _logger.LogInformation(">> [部门事件] 开始处理部门更新事件: {EventId}", eventData.EventId);

        try
        {

            _logger.LogInformation(">> [部门事件] 部门更新事件处理完成: 部门ID {DepartmentId}, 部门名 {DepartmentName}",
                eventEntity.Object.DepartmentId, eventEntity.Object.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ">> [部门事件] 处理部门更新事件失败: {EventId}", eventData.EventId);
            throw;
        }
    }
}
