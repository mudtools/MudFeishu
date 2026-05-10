// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace Mud.Feishu.Redis.HealthChecks;

/// <summary>
/// Redis 健康检查
/// </summary>
public class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;

    /// <summary>
    /// 初始化 RedisHealthCheck 实例
    /// </summary>
    /// <param name="connectionMultiplexer">Redis 连接多路复用器</param>
    public RedisHealthCheck(IConnectionMultiplexer connectionMultiplexer)
    {
        _connectionMultiplexer = connectionMultiplexer;
    }

    /// <summary>
    /// 执行健康检查
    /// </summary>
    /// <param name="context">健康检查上下文</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>健康检查结果</returns>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var database = _connectionMultiplexer.GetDatabase();
            var pingResult = await database.PingAsync();

            if (pingResult == TimeSpan.Zero || pingResult == TimeSpan.MinValue)
            {
                return HealthCheckResult.Unhealthy("Redis ping returned invalid time");
            }

            var connectionCount = _connectionMultiplexer.GetEndPoints()
                .Select(endpoint => _connectionMultiplexer.GetServer(endpoint))
                .Count(server => server.IsConnected);

            return HealthCheckResult.Healthy(
                description: "Redis is healthy",
                data: new Dictionary<string, object>
                {
                    { "latency", pingResult.TotalMilliseconds },
                    { "connectedEndpoints", connectionCount }
                });
        }
        catch (RedisException ex)
        {
            return HealthCheckResult.Unhealthy(
                description: "Redis connection failed",
                exception: ex,
                data: new Dictionary<string, object>
                {
                    { "message", ex.Message }
                });
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                description: "Redis health check failed",
                exception: ex,
                data: new Dictionary<string, object>
                {
                    { "message", ex.Message }
                });
        }
    }
}
