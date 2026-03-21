// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TaskManageDemo.Backend.Services.HealthChecks;

/// <summary>
/// 内存健康检查
/// </summary>
public class MemoryHealthCheck : IHealthCheck
{
    private readonly ILogger<MemoryHealthCheck> _logger;
    private const long MemoryThresholdMB = 1024; // 1GB阈值

    /// <summary>
    /// 初始化内存健康检查
    /// </summary>
    public MemoryHealthCheck(ILogger<MemoryHealthCheck> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 执行健康检查
    /// </summary>
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var process = System.Diagnostics.Process.GetCurrentProcess();
            var workingSetMB = process.WorkingSet64 / (1024 * 1024);
            var gcMemoryMB = GC.GetTotalMemory(false) / (1024 * 1024);

            // 获取系统总内存和可用内存（近似值）
            var totalMemoryMB = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / (1024 * 1024);

            var data = new Dictionary<string, object>
            {
                { "WorkingSetMB", workingSetMB },
                { "GCMemoryMB", gcMemoryMB },
                { "TotalMemoryMB", totalMemoryMB },
                { "ProcessName", process.ProcessName },
                { "ThreadCount", process.Threads.Count }
            };

            if (workingSetMB > MemoryThresholdMB * 2)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    $"内存使用过高: {workingSetMB}MB", data: data));
            }

            if (workingSetMB > MemoryThresholdMB)
            {
                return Task.FromResult(HealthCheckResult.Degraded(
                    $"内存使用警告: {workingSetMB}MB", data: data));
            }

            return Task.FromResult(HealthCheckResult.Healthy(
                $"内存使用正常: {workingSetMB}MB", data: data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "内存健康检查失败");
            return Task.FromResult(HealthCheckResult.Unhealthy($"检查异常: {ex.Message}"));
        }
    }
}
