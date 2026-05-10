// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Mud.Feishu;
using Mud.Feishu.Abstractions;

namespace TaskManageDemo.Backend.Services.HealthChecks;

/// <summary>
/// 飞书 API 健康检查
/// </summary>
public class FeishuApiHealthCheck : IHealthCheck
{
    private readonly IFeishuAppManager _feishuAppManager;
    private readonly ILogger<FeishuApiHealthCheck> _logger;

    /// <summary>
    /// 初始化飞书API健康检查
    /// </summary>
    public FeishuApiHealthCheck(
        IFeishuAppManager feishuAppManager,
        ILogger<FeishuApiHealthCheck> logger)
    {
        _feishuAppManager = feishuAppManager;
        _logger = logger;
    }

    /// <summary>
    /// 执行健康检查
    /// </summary>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 检查飞书应用配置是否有效
            var config = _feishuAppManager.DefaultConfig;
            if (string.IsNullOrEmpty(config.AppId) || config.AppId == "cli_xxx")
            {
                return HealthCheckResult.Degraded("飞书应用配置未正确设置");
            }

            // 尝试获取应用访问令牌来验证连通性
            var tokenManager = _feishuAppManager.DefaultAppTokenManager;
            var token = await tokenManager.GetTokenAsync();

            if (string.IsNullOrEmpty(token))
            {
                return HealthCheckResult.Unhealthy("无法获取飞书应用访问令牌");
            }

            return HealthCheckResult.Healthy("飞书 API 连接正常");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "飞书 API 健康检查失败");
            return HealthCheckResult.Unhealthy($"飞书 API 检查异常: {ex.Message}");
        }
    }
}
