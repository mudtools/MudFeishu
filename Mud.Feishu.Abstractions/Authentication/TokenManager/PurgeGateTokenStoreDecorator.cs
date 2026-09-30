// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.Authentication;

/// <summary>
/// 令牌存储读边界守卫：把「待清库门（D10）」与「读故障降级」收敛为持久化 SPI 读族的单一拦截点。
/// </summary>
/// <remarks>
/// <para>
/// <b>职责来源（改造前分散在管理器内的两处判定与逐点 try/catch）</b>：
/// <list type="bullet">
/// <item>清库门：改造前为 <c>FeishuAppTokenManagerBase.ShouldSkipStoreRestoreForPendingPurge</c> 与
/// <c>UserTokenManager.ShouldSkipUserStoreAccessForPendingPurge</c>（两处逐行同构）。判据相同 ——
/// <see cref="TokenStorePurgeGate.Query(string?)"/> 为 <see cref="TokenStorePurgeGate.PurgeGateState.Pending"/> 时
/// 短路读取（凭据变更清库完成前不得恢复旧凭据来源的令牌）；<see cref="TokenStorePurgeGate.PurgeGateState.TimedOut"/>
/// 时 fail-open 放行并记一次 Warning（绝不因门而永久禁用恢复）。</item>
/// <item>读故障降级：改造前每个直调点各自 <c>catch (ex is not OperationCanceledException)</c> → Warning → 视为未命中。
/// 该语义<b>必须</b>在装饰器保留：上游桥接器 <c>TokenStoreBackedTokenCache&lt;T&gt;.GetAsync</c> 不捕获
/// store 异常，若不在此降级，存储抖动将从「回落 API 刷新」恶化为「把异常抛给业务调用方」。</item>
/// </list>
/// </para>
/// <para>
/// <b>写族与清库族绝不拦截</b>：残留令牌由「提交后二次清库」覆盖（与改造前的门语义一致）；
/// 且 <c>ClearAllUsersAsync</c> 若被拦，D10 清库本身将静默失效。
/// </para>
/// </remarks>
internal sealed class PurgeGateReadGuard
{
    private readonly string _appKey;
    private readonly ILogger _logger;

    /// <summary>
    /// 初始化读边界守卫。
    /// </summary>
    /// <param name="appKey">应用唯一标识（门的判定键）。</param>
    /// <param name="logger">日志记录器。</param>
    internal PurgeGateReadGuard(string appKey, ILogger logger)
    {
        _appKey = appKey;
        _logger = logger;
    }

    /// <summary>
    /// 门判定：返回 true 表示本次读取必须短路（不触达内层存储）。
    /// </summary>
    /// <param name="memberName">读取成员名（仅用于日志定位）。</param>
    /// <returns>true 表示应跳过本次读取并返回「未命中」。</returns>
    internal bool ShouldShortCircuitForPendingPurge(string memberName)
    {
        switch (TokenStorePurgeGate.Query(_appKey))
        {
            case TokenStorePurgeGate.PurgeGateState.Pending:
                _logger.LogDebug(
                    "凭据变更清库进行中，跳过令牌存储读取以避免恢复旧凭据令牌（D10）。AppKey: {AppKey}, Member: {Member}",
                    _appKey, memberName);
                return true;

            case TokenStorePurgeGate.PurgeGateState.TimedOut:
                _logger.LogWarning(
                    "凭据变更清库门超过 {TimeoutSeconds}s 未撤除，已 fail-open：读取路径重新启用，" +
                    "可能存在旧凭据令牌残留。AppKey: {AppKey}, Member: {Member}",
                    TokenStorePurgeGate.SafetyTimeoutSeconds, _appKey, memberName);
                return false;

            default:
                return false;
        }
    }

    /// <summary>
    /// 读故障降级：记录一次 Warning（与改造前管理器内逐点 catch 的语义与可观测口径一致）。
    /// </summary>
    /// <param name="ex">异常。</param>
    /// <param name="memberName">读取成员名（仅用于日志定位）。</param>
    internal void LogReadFailure(Exception ex, string memberName)
        => _logger.LogWarning(ex,
            "读取令牌存储失败，按未命中处理（回落 API 刷新）。AppKey: {AppKey}, Member: {Member}",
            _appKey, memberName);
}

/// <summary>
/// 租户维度令牌存储装饰器：<see cref="ITokenStore"/> 读族经 <see cref="PurgeGateReadGuard"/> 门控 + 容错，
/// 写族与清库族一律透传。
/// </summary>
/// <remarks>
/// 装配位置：令牌管理器构造函数内包装 per-app store，使「直接 new 管理器」的路径同样获得门控与容错。
/// </remarks>
internal sealed class PurgeGateTokenStoreDecorator : ITokenStore
{
    private readonly ITokenStore _inner;
    private readonly PurgeGateReadGuard _guard;

    /// <summary>
    /// 初始化租户维度装饰器。
    /// </summary>
    /// <param name="inner">内层令牌存储。</param>
    /// <param name="appKey">应用唯一标识（门的判定键）。</param>
    /// <param name="logger">日志记录器。</param>
    internal PurgeGateTokenStoreDecorator(ITokenStore inner, string appKey, ILogger logger)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _guard = new PurgeGateReadGuard(appKey, logger ?? throw new ArgumentNullException(nameof(logger)));
    }

    /// <inheritdoc />
    public async Task<string?> GetAccessTokenAsync(string tokenType, CancellationToken cancellationToken = default)
    {
        if (_guard.ShouldShortCircuitForPendingPurge(nameof(GetAccessTokenAsync)))
            return null;

        try
        {
            return await _inner.GetAccessTokenAsync(tokenType, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _guard.LogReadFailure(ex, nameof(GetAccessTokenAsync));
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<string?> GetRefreshTokenAsync(string tokenType, CancellationToken cancellationToken = default)
    {
        if (_guard.ShouldShortCircuitForPendingPurge(nameof(GetRefreshTokenAsync)))
            return null;

        try
        {
            return await _inner.GetRefreshTokenAsync(tokenType, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _guard.LogReadFailure(ex, nameof(GetRefreshTokenAsync));
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetTokenTypesAsync(CancellationToken cancellationToken = default)
    {
        if (_guard.ShouldShortCircuitForPendingPurge(nameof(GetTokenTypesAsync)))
            return Enumerable.Empty<string>();

        try
        {
            return await _inner.GetTokenTypesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _guard.LogReadFailure(ex, nameof(GetTokenTypesAsync));
            return Enumerable.Empty<string>();
        }
    }

    // ---- 写族：一律透传（清库期间的残留由「提交后二次清库」覆盖，见 BD-4） ----

    /// <inheritdoc />
    public Task SetAccessTokenAsync(string tokenType, string accessToken, long expiresInSeconds, CancellationToken cancellationToken = default)
        => _inner.SetAccessTokenAsync(tokenType, accessToken, expiresInSeconds, cancellationToken);

    /// <inheritdoc />
    public Task SetRefreshTokenAsync(string tokenType, string refreshToken, CancellationToken cancellationToken = default)
        => _inner.SetRefreshTokenAsync(tokenType, refreshToken, cancellationToken);

    /// <inheritdoc />
    public Task RemoveAsync(string tokenType, CancellationToken cancellationToken = default)
        => _inner.RemoveAsync(tokenType, cancellationToken);

    // ---- 清库族：一律透传（清库本体不得被门拦截） ----

    /// <inheritdoc />
    public Task ClearAsync(CancellationToken cancellationToken = default)
        => _inner.ClearAsync(cancellationToken);
}

/// <summary>
/// 用户维度令牌存储装饰器：带 userId 的读族经 <see cref="PurgeGateReadGuard"/> 门控 + 容错，
/// 写族、单用户清库与 <see cref="IFeishuUserTokenStorePurge"/> 全量清库一律透传。
/// </summary>
/// <remarks>
/// <para>
/// 继承 <see cref="UserTokenStoreBase"/> 复用「<see cref="ITokenStore"/> 成员 → 内层」的转发实现
/// （<see cref="IUserTokenStore"/> 继承 <see cref="ITokenStore"/>，其无 userId 成员在用户语义下未定义，
/// 桥接器亦绝不调用）。
/// </para>
/// <para>
/// 消费者除桥接器外还有 <c>UserTokenManager</c> 的直调路径（refresh 候选读取 / D12 过期清理 / CAS），
/// 因此装饰器同时挂在管理器字段上，两条路径共享同一门控与容错口径。
/// </para>
/// </remarks>
internal sealed class PurgeGateUserTokenStoreDecorator : UserTokenStoreBase, IFeishuUserTokenStorePurge
{
    private readonly IUserTokenStore _inner;
    private readonly PurgeGateReadGuard _guard;

    /// <summary>
    /// 初始化用户维度装饰器。
    /// </summary>
    /// <param name="inner">内层用户令牌存储。</param>
    /// <param name="appKey">应用唯一标识（门的判定键）。</param>
    /// <param name="logger">日志记录器。</param>
    internal PurgeGateUserTokenStoreDecorator(IUserTokenStore inner, string appKey, ILogger logger)
        : base(inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _guard = new PurgeGateReadGuard(appKey, logger ?? throw new ArgumentNullException(nameof(logger)));
    }

    /// <inheritdoc />
    public override async Task<string?> GetAccessTokenAsync(string userId, string tokenType, CancellationToken cancellationToken = default)
    {
        if (_guard.ShouldShortCircuitForPendingPurge(nameof(GetAccessTokenAsync)))
            return null;

        try
        {
            return await _inner.GetAccessTokenAsync(userId, tokenType, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _guard.LogReadFailure(ex, nameof(GetAccessTokenAsync));
            return null;
        }
    }

    /// <inheritdoc />
    public override async Task<string?> GetRefreshTokenAsync(string userId, string tokenType, CancellationToken cancellationToken = default)
    {
        if (_guard.ShouldShortCircuitForPendingPurge(nameof(GetRefreshTokenAsync)))
            return null;

        try
        {
            return await _inner.GetRefreshTokenAsync(userId, tokenType, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _guard.LogReadFailure(ex, nameof(GetRefreshTokenAsync));
            return null;
        }
    }

    /// <inheritdoc />
    public override async Task<IEnumerable<string>> GetTokenTypesAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (_guard.ShouldShortCircuitForPendingPurge(nameof(GetTokenTypesAsync)))
            return Enumerable.Empty<string>();

        try
        {
            return await _inner.GetTokenTypesAsync(userId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _guard.LogReadFailure(ex, nameof(GetTokenTypesAsync));
            return Enumerable.Empty<string>();
        }
    }

    // ---- 写族：一律透传 ----

    /// <inheritdoc />
    public override Task SetAccessTokenAsync(string userId, string tokenType, string accessToken, long expiresInSeconds, CancellationToken cancellationToken = default)
        => _inner.SetAccessTokenAsync(userId, tokenType, accessToken, expiresInSeconds, cancellationToken);

    /// <inheritdoc />
    public override Task SetRefreshTokenAsync(string userId, string tokenType, string refreshToken, CancellationToken cancellationToken = default)
        => _inner.SetRefreshTokenAsync(userId, tokenType, refreshToken, cancellationToken);

    /// <inheritdoc />
    public override Task RemoveAsync(string userId, string tokenType, CancellationToken cancellationToken = default)
        => _inner.RemoveAsync(userId, tokenType, cancellationToken);

    // ---- 清库族：一律透传 ----

    /// <inheritdoc />
    public override Task ClearUserAsync(string userId, CancellationToken cancellationToken = default)
        => _inner.ClearUserAsync(userId, cancellationToken);

    /// <summary>
    /// D10 清库本体（跨用户全清）—— 透传，绝不拦截（被拦会使凭据变更清库静默失效）。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    public Task ClearAllUsersAsync(CancellationToken cancellationToken = default)
        => _inner is IFeishuUserTokenStorePurge purgeable
            ? purgeable.ClearAllUsersAsync(cancellationToken)
            : Task.CompletedTask;
}
