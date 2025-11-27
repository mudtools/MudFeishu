// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Mud.Feishu.WebSocket.DataModels;

namespace Mud.Feishu.WebSocket.Handlers;

/// <summary>
/// 默认的飞书事件处理器实现（用于处理未注册的事件类型）
/// </summary>
public class DefaultFeishuEventHandlerImpl : DefaultFeishuEventHandler
{
    public DefaultFeishuEventHandlerImpl(ILogger<DefaultFeishuEventHandlerImpl> logger) : base(logger)
    {
    }

    /// <summary>
    /// 处理未知类型事件的业务逻辑
    /// </summary>
    /// <param name="eventData">事件数据</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>处理任务</returns>
    public override async Task ProcessBusinessLogicAsync(EventData eventData, CancellationToken cancellationToken = default)
    {
        if (eventData == null)
            throw new ArgumentNullException(nameof(eventData));

        _logger.LogWarning("收到未处理的事件类型: {EventType}, 应用ID: {AppId}, 租户: {TenantKey}, 事件内容: {Event}",
            eventData.EventType, eventData.AppId, eventData.TenantKey, 
            System.Text.Json.JsonSerializer.Serialize(eventData.Event));

        // 默认处理器只记录日志，不执行具体业务逻辑
        // 可以根据需要实现一些通用处理，比如：
        // 1. 将未知事件保存到数据库
        // 2. 发送告警通知
        // 3. 记录事件统计信息

        await Task.CompletedTask;
    }
}