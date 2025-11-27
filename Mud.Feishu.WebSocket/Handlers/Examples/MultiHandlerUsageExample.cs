// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mud.Feishu.WebSocket.DataModels;
using Mud.Feishu.WebSocket.Handlers;
using Mud.Feishu.WebSocket.Handlers.Examples;

namespace Mud.Feishu.WebSocket.Examples;

/// <summary>
/// 多处理器使用示例
/// 展示如何在多处理器模式下配置和使用事件处理器
/// </summary>
public static class MultiHandlerUsageExample
{
    /// <summary>
    /// 配置多处理器的服务注册
    /// </summary>
    /// <param name="services">服务集合</param>
    public static void ConfigureServices(IServiceCollection services)
    {
        // 配置飞书WebSocket服务，启用多处理器模式
        services.AddFeishuWebSocketService(
            webSocketOptions =>
            {
                webSocketOptions.EnableMultiHandlerMode = true;        // 启用多处理器模式
                webSocketOptions.ParallelMultiHandlers = true;       // 并行执行处理器
                webSocketOptions.EnableLogging = true;
                webSocketOptions.HeartbeatIntervalMs = 30000;
            });

        // 注册多个相同类型的处理器
        // 1. 业务处理器 - 处理实际业务逻辑
        services.AddSingleton<ReceiveMessageEventHandler>();

        // 2. 分析处理器 - 进行消息分析和统计
        services.AddSingleton<MessageAnalyticsEventHandler>();

        // 3. 审计处理器 - 记录审计日志
        services.AddSingleton<MessageAuditEventHandler>();

        // 4. 缓存处理器 - 缓存消息数据（示例）
        services.AddSingleton<CustomEventHandler>();

        // 5. 默认处理器 - 处理未知事件类型
        services.AddSingleton<DefaultFeishuEventHandlerImpl>();
    }

    /// <summary>
    /// 使用多处理器的示例服务
    /// </summary>
    public class MultiHandlerUsageService
    {
        private readonly ILogger<MultiHandlerUsageService> _logger;
        private readonly IFeishuEventHandlerFactory _eventHandlerFactory;

        public MultiHandlerUsageService(
            ILogger<MultiHandlerUsageService> logger,
            IFeishuEventHandlerFactory eventHandlerFactory)
        {
            _logger = logger;
            _eventHandlerFactory = eventHandlerFactory;
        }

        /// <summary>
        /// 演示手动触发事件处理
        /// </summary>
        public async Task DemoManualEventHandlingAsync()
        {
            var eventData = new EventData
            {
                EventType = FeishuEventTypes.ReceiveMessage,
                AppId = "demo_app",
                TenantKey = "demo_tenant",
                Event = new
                {
                    MessageId = "msg_123",
                    Content = "Hello, World!",
                    Sender = "user_456"
                }
            };

            _logger.LogInformation("🚀 演示手动事件处理: {EventType}", eventData.EventType);

            // 方法1：获取所有处理器并手动调用
            var handlers = _eventHandlerFactory.GetHandlers(eventData.EventType);
            _logger.LogInformation("📊 找到 {Count} 个处理器", handlers.Count);

            foreach (var handler in handlers)
            {
                await handler.HandleAsync(eventData);
            }

            // 方法2：使用工厂的并行处理方法
            await _eventHandlerFactory.HandleEventParallelAsync(eventData.EventType, eventData);
        }

        /// <summary>
        /// 演示处理器管理功能
        /// </summary>
        public void DemoHandlerManagement()
        {
            _logger.LogInformation("🔧 演示处理器管理功能");

            // 查询已注册的处理器
            var eventTypes = _eventHandlerFactory.GetRegisteredEventTypes();
            _logger.LogInformation("📋 已注册的事件类型: {EventTypes}",
                string.Join(", ", eventTypes));

            // 检查特定事件类型的处理器数量
            var messageType = FeishuEventTypes.ReceiveMessage;
            if (_eventHandlerFactory is MultiFeishuEventHandlerFactory multiFactory)
            {
                var handlers = multiFactory.GetHandlers(messageType);
                _logger.LogInformation("📈 {EventType} 类型有 {Count} 个处理器", messageType, handlers.Count);
            }

            // 检查是否已注册
            var isRegistered = _eventHandlerFactory.IsHandlerRegistered(messageType);
            _logger.LogInformation("✅ {EventType} 是否已注册: {IsRegistered}", messageType, isRegistered);
        }

        /// <summary>
        /// 演示运行时动态注册处理器
        /// </summary>
        public void DemoDynamicHandlerRegistration()
        {
            _logger.LogInformation("➕ 演示动态注册处理器");

            // 创建临时处理器
            var tempHandler = new DynamicEventHandler(_logger);

            // 动态注册
            _eventHandlerFactory.RegisterHandler(tempHandler);

            _logger.LogInformation("✅ 临时处理器已注册: {EventType}", tempHandler.SupportedEventType);

            // 这里可以测试处理器功能...

            // 取消注册（可选）
            // _eventHandlerFactory.UnregisterHandler(tempHandler.SupportedEventType);
        }
    }

    /// <summary>
    /// 动态事件处理器示例
    /// </summary>
    public class DynamicEventHandler : DefaultFeishuEventHandler
    {
        public DynamicEventHandler(ILogger logger) : base(logger)
        {
        }

        public override string SupportedEventType => "dynamic.event.example_v1";

        public override async Task ProcessBusinessLogicAsync(EventData eventData, CancellationToken cancellationToken = default)
        {
            if (eventData == null)
                throw new ArgumentNullException(nameof(eventData));

            _logger.LogInformation("🔄 动态处理器处理事件: {EventType}", eventData.EventType);

            // 动态处理逻辑
            await Task.CompletedTask;
        }
    }
}