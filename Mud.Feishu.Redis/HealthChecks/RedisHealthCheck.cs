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
        _connectionMultiplexer = connectionMultiplexer ?? throw new ArgumentNullException(nameof(connectionMultiplexer));
    }

    /// <summary>
    /// 执行健康检查
    /// </summary>
    /// <param name="context">健康检查上下文</param>
    /// <param name="cancellationToken">取消令牌（命令之间生效；<c>PingAsync</c> 本身不接收令牌）</param>
    /// <returns>健康检查结果</returns>
    /// <remarks>
    /// R2-12：判据收敛为「PING 命令是否成功」，端点连通数仅作为 <c>data</c> 呈现——
    /// <list type="bullet">
    /// <item>此前 <c>pingResult == TimeSpan.Zero</c> 判 Unhealthy：毫秒精度下 <c>0</c> 可能是"极低延迟"的正常值；</item>
    /// <item>此前对每个端点直接调 <c>GetServer(endpoint).IsConnected</c>：副本/集群/Sentinel 端点异常会
    /// 冒泡到外层 catch，使主节点健康的实例被整体判为 Unhealthy。现改为端点级 <c>try/catch</c>，只影响计数。</item>
    /// </list>
    /// </remarks>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var database = _connectionMultiplexer.GetDatabase();
            var pingResult = await database.PingAsync().ConfigureAwait(false);

            var (connectedEndpoints, totalEndpoints) = CountConnectedEndpoints();

            return HealthCheckResult.Healthy(
                description: "Redis is healthy",
                data: new Dictionary<string, object>
                {
                    { "latency", pingResult.TotalMilliseconds },
                    { "connectedEndpoints", connectedEndpoints },
                    { "totalEndpoints", totalEndpoints }
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

    /// <summary>
    /// 统计已连接端点（R2-12）。
    /// </summary>
    /// <remarks>
    /// 端点级失败（副本不可达、集群拓扑变化、<c>GetServer</c> 抛异常）只影响该端点的计数，
    /// **不改变整体健康判定**——整体健康由 PING 决定。
    /// </remarks>
    private (int Connected, int Total) CountConnectedEndpoints()
    {
        var connected = 0;
        var total = 0;

        foreach (var endpoint in _connectionMultiplexer.GetEndPoints())
        {
            total++;

            try
            {
                if (_connectionMultiplexer.GetServer(endpoint).IsConnected)
                {
                    connected++;
                }
            }
            catch (Exception)
            {
                // 见 remarks：端点级异常不参与健康判定
            }
        }

        return (connected, total);
    }
}
