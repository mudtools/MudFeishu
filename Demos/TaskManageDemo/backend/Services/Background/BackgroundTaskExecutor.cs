// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace TaskManageDemo.Backend.Services.Background;

/// <summary>
/// 后台任务执行器
/// </summary>
public class BackgroundTaskExecutor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BackgroundTaskExecutor> _logger;

    public BackgroundTaskExecutor(
        IServiceProvider serviceProvider,
        ILogger<BackgroundTaskExecutor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("后台任务执行器启动");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var scheduledTaskService = scope.ServiceProvider.GetRequiredService<IScheduledTaskService>();

                // 发送截止提醒
                await scheduledTaskService.SendDueRemindersAsync(stoppingToken);

                // 执行全量同步（每小时一次）
                if (DateTime.Now.Minute < 5)
                {
                    await scheduledTaskService.PerformFullSyncAsync(stoppingToken);
                }

                // 重试失败的事件
                await scheduledTaskService.RetryFailedEventsAsync(stoppingToken);

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "后台任务执行出错");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("后台任务执行器停止");
    }
}
