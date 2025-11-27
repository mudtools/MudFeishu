// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Mud.Feishu.WebSocket.DataModels;
using System.Diagnostics;

namespace Mud.Feishu.WebSocket.Handlers.Examples;

/// <summary>
/// 消息分析事件处理器示例
/// 演示多处理器模式下的数据分析功能
/// </summary>
public class MessageAnalyticsEventHandler : DefaultFeishuEventHandler
{
    private readonly ILogger<MessageAnalyticsEventHandler> _logger;
    private static readonly Dictionary<string, long> MessageCounters = new();
    private static readonly object _lock = new object();

    public MessageAnalyticsEventHandler(ILogger<MessageAnalyticsEventHandler> logger) : base(logger)
    {
    }

    /// <summary>
    /// 支持的事件类型
    /// </summary>
    public override string SupportedEventType => FeishuEventTypes.ReceiveMessage;

    /// <summary>
    /// 处理消息分析的业务逻辑
    /// </summary>
    /// <param name="eventData">事件数据</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>处理任务</returns>
    public override async Task ProcessBusinessLogicAsync(EventData eventData, CancellationToken cancellationToken = default)
    {
        if (eventData == null)
            throw new ArgumentNullException(nameof(eventData));

        _logger.LogInformation("📊 开始分析消息事件: {EventType}, 应用ID: {AppId}", 
            eventData.EventType, eventData.AppId);

        // 模拟消息分析逻辑
        await AnalyzeMessageAsync(eventData);

        // 统计消息数量
        await IncrementMessageCounterAsync(eventData);

        await Task.CompletedTask;
    }

    /// <summary>
    /// 分析消息内容
    /// </summary>
    private async Task AnalyzeMessageAsync(EventData eventData)
    {
        try
        {
            // 模拟消息分析
            var stopwatch = Stopwatch.StartNew();

            // 这里可以添加具体的分析逻辑：
            // 1. 消息情感分析
            // 2. 关键词提取
            // 3. 消息分类
            // 4. 用户行为分析

            await Task.Delay(10, cancellationToken: CancellationToken.None); // 模拟处理时间

            stopwatch.Stop();
            _logger.LogDebug("📊 消息分析完成，耗时: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "📊 消息分析失败");
        }
    }

    /// <summary>
    /// 增加消息计数器
    /// </summary>
    private async Task IncrementMessageCounterAsync(EventData eventData)
    {
        await Task.Run(() =>
        {
            lock (_lock)
            {
                var key = $"{eventData.AppId}_{eventData.TenantKey}";
                MessageCounters[key] = MessageCounters.GetValueOrDefault(key, 0) + 1;

                if (MessageCounters[key] % 100 == 0)
                {
                    _logger.LogInformation("📈 应用 {AppId} 租户 {TenantKey} 已处理 {Count} 条消息",
                        eventData.AppId, eventData.TenantKey, MessageCounters[key]);
                }
            }
        });
    }

    /// <summary>
    /// 获取消息统计信息
    /// </summary>
    public static Dictionary<string, long> GetMessageStatistics()
    {
        lock (_lock)
        {
            return new Dictionary<string, long>(MessageCounters);
        }
    }
}