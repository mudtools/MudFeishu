// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Mud.Feishu.WebSocket.DataModels;
using System.Text.Json;

namespace Mud.Feishu.WebSocket.Handlers.Examples;

/// <summary>
/// 消息审计事件处理器示例
/// 演示多处理器模式下的审计功能
/// </summary>
public class MessageAuditEventHandler : DefaultFeishuEventHandler
{
    private readonly ILogger<MessageAuditEventHandler> _logger;

    public MessageAuditEventHandler(ILogger<MessageAuditEventHandler> logger) : base(logger)
    {
    }

    /// <summary>
    /// 支持的事件类型
    /// </summary>
    public override string SupportedEventType => FeishuEventTypes.ReceiveMessage;

    /// <summary>
    /// 处理消息审计的业务逻辑
    /// </summary>
    /// <param name="eventData">事件数据</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>处理任务</returns>
    public override async Task ProcessBusinessLogicAsync(EventData eventData, CancellationToken cancellationToken = default)
    {
        if (eventData == null)
            throw new ArgumentNullException(nameof(eventData));

        _logger.LogInformation("🔍 开始审计消息事件: {EventType}, 应用ID: {AppId}", 
            eventData.EventType, eventData.AppId);

        // 模拟消息审计逻辑
        await AuditMessageAsync(eventData);

        await Task.CompletedTask;
    }

    /// <summary>
    /// 审计消息
    /// </summary>
    private async Task AuditMessageAsync(EventData eventData)
    {
        try
        {
            // 创建审计记录
            var auditRecord = new
            {
                Timestamp = DateTimeOffset.UtcNow,
                EventType = eventData.EventType,
                AppId = eventData.AppId,
                TenantKey = eventData.TenantKey,
                EventData = eventData.Event,
                AuditLevel = "INFO",
                Processor = nameof(MessageAuditEventHandler)
            };

            // 记录审计日志
            _logger.LogInformation("🔍 消息审计记录: {AuditRecord}", 
                JsonSerializer.Serialize(auditRecord, new JsonSerializerOptions { WriteIndented = true }));

            // 这里可以添加具体的审计逻辑：
            // 1. 保存到审计数据库
            // 2. 发送到审计系统
            // 3. 记录敏感操作
            // 4. 合规性检查

            // 模拟异步审计操作
            await Task.Delay(5, cancellationToken: CancellationToken.None);

            _logger.LogDebug("🔍 消息审计完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🔍 消息审计失败");
            
            // 审计失败也要记录
            _logger.LogWarning("🔍 审计失败事件: EventType={EventType}, AppId={AppId}, Error={Error}",
                eventData.EventType, eventData.AppId, ex.Message);
        }
    }
}