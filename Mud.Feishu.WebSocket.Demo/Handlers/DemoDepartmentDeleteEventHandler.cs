// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.DataModels.Organization;
using Mud.Feishu.Abstractions.EventHandlers;

namespace Mud.Feishu.WebSocket.Demo.Handlers;

/// <summary>
/// 演示部门删除事件处理器
/// </summary>
public class DemoDepartmentDeleteEventHandler : DepartmentDeleteEventHandler
{

    public DemoDepartmentDeleteEventHandler(ILogger<DepartmentDeleteEventHandler> logger) : base(logger)
    {
    }

    /// <summary>
    /// 处理部门删除事件的业务逻辑
    /// </summary>
    /// <param name="eventData">事件数据</param>
    /// <param name="eventEntity">部门删除事件实体</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>处理任务</returns>
    protected override async Task ProcessBusinessLogicAsync(EventData eventData, DepartmentDeleteResult? eventEntity, CancellationToken cancellationToken = default)
    {
        if (eventData == null)
            throw new ArgumentNullException(nameof(eventData));

        if (eventEntity == null)
        {
            _logger.LogWarning("部门删除事件实体为空，跳过处理");
            return;
        }

        _logger.LogInformation("🗑️ [部门删除事件] 开始处理部门删除事件: EventId={EventId}, AppId={AppId}, TenantKey={TenantKey}",
            eventData.EventId, eventData.AppId, eventData.TenantKey);

        _logger.LogDebug("部门删除事件详情: {@EventEntity}", eventEntity);

        await Task.CompletedTask;
    }
}
