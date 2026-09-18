// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

namespace Mud.Feishu.Redis.Services;

/// <summary>
/// Redis 连接启动期预热服务（WHF-10）
/// 在宿主启动阶段解析 <see cref="IConnectionMultiplexer"/>（触发连接建立）并对默认数据库执行 PING，
/// 把首个 Webhook 请求承担的连接建立延迟移到启动期。
/// </summary>
/// <remarks>
/// 失败语义：宿主配置 <c>AbortOnConnectFail=true</c>（或 Connect 抛出异常）时上抛以终止启动（fail-fast）；
/// 连接建立成功但 PING 失败时仅告警不阻断启动（连接层具备自动重连能力）。
/// </remarks>
public class RedisConnectionWarmupService : IHostedService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisConnectionWarmupService>? _logger;
    private readonly bool _abortOnConnectFail;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="redis">Redis 连接多路复用器（解析即触发连接建立）</param>
    /// <param name="logger">日志记录器（可选）</param>
    /// <param name="abortOnConnectFail">与 RedisOptions.AbortOnConnectFail 对齐：true 时 PING 失败上抛终止启动</param>
    public RedisConnectionWarmupService(
        IConnectionMultiplexer redis,
        ILogger<RedisConnectionWarmupService>? logger = null,
        bool abortOnConnectFail = true)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _logger = logger;
        _abortOnConnectFail = abortOnConnectFail;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger?.LogInformation("开始预热 Redis 连接（WHF-10）：Endpoints: {Endpoints}",
            string.Join(",", _redis.GetEndPoints().Select(e => e.ToString())));

        try
        {
            var database = _redis.GetDatabase();
            var latency = await database.PingAsync().ConfigureAwait(false);

            _logger?.LogInformation("Redis 连接预热完成，PING 延迟: {LatencyMs}ms", latency.TotalMilliseconds);
        }
        catch (Exception ex) when (!_abortOnConnectFail)
        {
            // 连接已建立但 PING 失败（或短暂不可达）：不阻断启动，交由连接层自动重连
            _logger?.LogWarning(ex, "Redis 连接预热 PING 失败（AbortOnConnectFail=false，不阻断启动，等待自动重连）");
        }
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
