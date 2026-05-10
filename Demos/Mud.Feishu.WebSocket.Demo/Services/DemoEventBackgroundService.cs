// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions;

namespace Mud.Feishu.WebSocket.Services;

/// <summary>
/// 演示事件后台服务，用于定时生成模拟事件
/// </summary>
public class DemoEventBackgroundService : BackgroundService
{
    private readonly ILogger<DemoEventBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly Random _random = new();

    public DemoEventBackgroundService(
        ILogger<DemoEventBackgroundService> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(">> [后台服务] 演示事件服务已启动");

        var enableMockEvents = _configuration.GetValue<bool>("DemoSettings:EnableMockEvents", false);
        var mockEventInterval = _configuration.GetValue<int>("DemoSettings:MockEventIntervalMs", 10000);

        if (!enableMockEvents)
        {
            _logger.LogInformation(">> [后台服务] 模拟事件功能已禁用");
            return;
        }

        // 等待WebSocket服务启动
        await Task.Delay(5000, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await GenerateAndProcessRandomEvent(stoppingToken);
                await Task.Delay(mockEventInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation(">> [后台服务] 演示事件服务已停止");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ">> [后台服务] 生成模拟事件时发生错误");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }

    private async Task GenerateAndProcessRandomEvent(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var webSocketManager = scope.ServiceProvider.GetRequiredService<IFeishuWebSocketManager>();
        var demoEventService = scope.ServiceProvider.GetRequiredService<DemoEventService>();

        // 检查WebSocket连接状态
        if (!webSocketManager.IsConnected)
        {
            _logger.LogWarning(">> [后台服务] WebSocket未连接，跳过事件生成");
            return;
        }

        // 随机选择事件类型
        var eventType = _random.Next(0, 3);
        EventData mockEvent = eventType switch
        {
            0 => demoEventService.GenerateMockUserEvent(),
            1 => demoEventService.GenerateMockDepartmentEvent(),
            _ => demoEventService.GenerateMockApprovalEvent()
        };

        try
        {
            // 这里应该通过WebSocket发送事件，但在演示中我们直接记录日志
            var eventTypeName = mockEvent.EventType switch
            {
                "contact.user.created_v3" => "用户创建",
                "contact.department.created_v3" => "部门创建",
                "approval.approval.approved_v1" => "审批处理",
                _ => "未知事件"
            };

            //_logger.LogInformation("🎯 [后台服务] 生成{eventType}事件: {EventId}", eventTypeName, mockEvent.EventId);

            // 模拟事件处理
            await Task.Delay(100, stoppingToken);
        }
        catch (Exception ex)
        {
            //_logger.LogError(ex, "❌ [后台服务] 处理模拟事件失败: {EventId}", mockEvent.EventId);
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(">> [后台服务] 正在停止演示事件服务");
        return base.StopAsync(cancellationToken);
    }
}