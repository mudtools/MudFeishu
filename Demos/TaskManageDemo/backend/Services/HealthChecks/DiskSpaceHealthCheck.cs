// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TaskManageDemo.Backend.Services.HealthChecks;

/// <summary>
/// 磁盘空间健康检查
/// </summary>
public class DiskSpaceHealthCheck : IHealthCheck
{
    private readonly ILogger<DiskSpaceHealthCheck> _logger;
    private const long MinimumFreeSpaceMB = 500; // 最小500MB空闲空间

    /// <summary>
    /// 初始化磁盘空间健康检查
    /// </summary>
    public DiskSpaceHealthCheck(ILogger<DiskSpaceHealthCheck> logger)
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
            var driveInfo = new DriveInfo(Path.GetPathRoot(Directory.GetCurrentDirectory()) ?? "C:");

            if (!driveInfo.IsReady)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("磁盘未就绪"));
            }

            var freeSpaceMB = driveInfo.AvailableFreeSpace / (1024 * 1024);
            var totalSpaceMB = driveInfo.TotalSize / (1024 * 1024);
            var usedSpacePercent = (double)(totalSpaceMB - freeSpaceMB) / totalSpaceMB * 100;

            var data = new Dictionary<string, object>
            {
                { "FreeSpaceMB", freeSpaceMB },
                { "TotalSpaceMB", totalSpaceMB },
                { "UsedSpacePercent", Math.Round(usedSpacePercent, 2) },
                { "DriveName", driveInfo.Name }
            };

            if (freeSpaceMB < MinimumFreeSpaceMB)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    $"磁盘空间不足: 仅剩 {freeSpaceMB}MB", data: data));
            }

            if (freeSpaceMB < MinimumFreeSpaceMB * 2)
            {
                return Task.FromResult(HealthCheckResult.Degraded(
                    $"磁盘空间警告: 仅剩 {freeSpaceMB}MB", data: data));
            }

            return Task.FromResult(HealthCheckResult.Healthy(
                $"磁盘空间正常: {freeSpaceMB}MB 可用", data: data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "磁盘空间健康检查失败");
            return Task.FromResult(HealthCheckResult.Unhealthy($"检查异常: {ex.Message}"));
        }
    }
}
