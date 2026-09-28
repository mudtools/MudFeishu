// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.Services;

/// <summary>
/// 空实现的 Nonce 去重器。
/// 当统一去重节 <c>FeishuDeduplication:Mode=None</c> 时使用：不进行 Nonce 去重，
/// 所有 Nonce 均视为首次使用（不拦截重放）。
/// </summary>
/// <remarks>
/// <b>安全提示</b>：关闭 Nonce 去重等于放弃重放防护，生产环境应显式承担该风险
/// （复用 <c>FeishuWebhook:AllowInMemoryNonceDedupInProduction</c> 的确认语义）。
/// </remarks>
public sealed class NoopFeishuNonceDeduplicator : IFeishuNonceDistributedDeduplicator
{
    private readonly ILogger _logger;

    /// <summary>
    /// 初始化 <see cref="NoopFeishuNonceDeduplicator"/> 的新实例。
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <exception cref="ArgumentNullException"><paramref name="logger"/> 为 null</exception>
    public NoopFeishuNonceDeduplicator(ILogger logger)
        => _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public Task<bool> TryMarkAsUsedAsync(string nonce, string? appKey = null, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Nonce 去重已禁用（Mode=None），Nonce {Nonce} 视为首次使用", nonce);
        return Task.FromResult(false);
    }

    /// <inheritdoc />
    public Task<bool> IsUsedAsync(string nonce, string? appKey = null, CancellationToken cancellationToken = default)
        => Task.FromResult(false);

    /// <inheritdoc />
    public Task<int> CleanupExpiredAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(0);

    /// <inheritdoc />
    public ValueTask DisposeAsync() => new();
}