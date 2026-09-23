// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Diagnostics;
using Mud.Feishu.Abstractions.Metrics;
using StackExchange.Redis;

namespace Mud.Feishu.Redis.Services;

/// <summary>
/// Redis 指标上报的单一出口（R2-21 / E-01）。
/// </summary>
/// <remarks>
/// <para>
/// 复用 <c>Mud.Feishu.Abstractions.Metrics.FeishuMetrics</c> 的单一 Meter（<c>Mud.Feishu</c>），
/// 宿主无需额外 <c>AddMeter</c>；无监听者时 OpenTelemetry 语义下 instrument 为 no-op。
/// </para>
/// <para>
/// <b>热路径成本控制</b>：使用 <see cref="Stopwatch.GetTimestamp"/> + <see cref="TagList"/>（结构体），
/// 不分配计时器对象；标签值全部取自受控枚举（<c>FeishuMetrics.RedisCommands</c>/<c>RedisOutcomes</c>/
/// <c>DedupTypes</c>），无高基数风险。
/// </para>
/// </remarks>
internal static class RedisMetricsHelper
{
    /// <summary>开始计时（返回时间戳，无分配）。</summary>
    /// <returns>起始时间戳；与 <see cref="Record(string, string, string, long)"/> 配对使用</returns>
    public static long Begin() => Stopwatch.GetTimestamp();

    /// <summary>
    /// 记录一次 Redis 操作（计数 + 可选耗时）。
    /// </summary>
    /// <param name="command">命令名，取 <c>FeishuMetrics.RedisCommands</c> 常量</param>
    /// <param name="dedupType">去重类型，取 <c>FeishuMetrics.DedupTypes</c> 常量</param>
    /// <param name="outcome">结果，取 <c>FeishuMetrics.RedisOutcomes</c> 常量</param>
    /// <param name="startTimestamp"><see cref="Begin"/> 的返回值；为 0 时不上报耗时</param>
    public static void Record(string command, string dedupType, string outcome, long startTimestamp = 0)
    {
        var tags = new TagList
        {
            { FeishuMetrics.Tags.RedisCommand, command },
            { FeishuMetrics.Tags.DedupType, dedupType },
            { FeishuMetrics.Tags.Outcome, outcome },
        };

        FeishuMetrics.RedisOperationCount.Add(1, tags);

        if (startTimestamp != 0)
        {
            var elapsedMs = (Stopwatch.GetTimestamp() - startTimestamp) * 1000.0 / Stopwatch.Frequency;
            FeishuMetrics.RedisOperationDuration.Record(elapsedMs, tags);
        }
    }

    /// <summary>
    /// 记录 SCAN 类 API 的键数（扫描数 / 实际删除数）。
    /// </summary>
    /// <param name="command">命令名（如 <c>FeishuMetrics.RedisCommands.ClearCache</c>）</param>
    /// <param name="count">键数</param>
    /// <param name="deleted"><c>true</c> 记为 deleted，<c>false</c> 记为 scanned</param>
    public static void RecordScan(string command, long count, bool deleted)
    {
        if (count <= 0)
            return;

        var tags = new TagList
        {
            { FeishuMetrics.Tags.RedisCommand, command },
            { FeishuMetrics.Tags.Outcome, deleted ? FeishuMetrics.RedisOutcomes.Deleted : FeishuMetrics.RedisOutcomes.Scanned },
        };

        FeishuMetrics.RedisScanKeysCount.Add(count, tags);
    }

    /// <summary>
    /// 把 <see cref="FeishuRedisFailureKind"/> 映射为 outcome 标签值。
    /// </summary>
    /// <param name="kind">失败分类</param>
    /// <returns>outcome 取值（连接/超时/非法参数/服务端）</returns>
    public static string FromFailureKind(FeishuRedisFailureKind kind) => kind switch
    {
        FeishuRedisFailureKind.Connection => FeishuMetrics.RedisOutcomes.Connection,
        FeishuRedisFailureKind.Timeout => FeishuMetrics.RedisOutcomes.Timeout,
        FeishuRedisFailureKind.InvalidArgument => FeishuMetrics.RedisOutcomes.InvalidArgument,
        _ => FeishuMetrics.RedisOutcomes.Server,
    };

    /// <summary>
    /// 把原始异常映射为 outcome 标签值（用于不包装异常、直接透传的令牌存储路径）。
    /// </summary>
    /// <param name="exception">原始异常</param>
    /// <returns>outcome 取值</returns>
    public static string FromException(Exception exception) => exception switch
    {
        RedisConnectionException => FeishuMetrics.RedisOutcomes.Connection,
        RedisTimeoutException => FeishuMetrics.RedisOutcomes.Timeout,
        _ => FeishuMetrics.RedisOutcomes.Server,
    };
}
