// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Authentication;

namespace Mud.Feishu.Abstractions.Authentication.MultiApp;

/// <summary>
/// TMR-P0-1（F1）：默认应用令牌管理器的转发代理。
/// </summary>
/// <remarks>
/// <para>
/// DI 桥接注册的是「解析委托」而非「实例」：每次成员调用都现取当前默认应用的管理器，
/// 使 <c>SetDefaultApp</c> / 配置热更新 / <c>RemoveApp</c> 的变更对已注入消费者立即生效。
/// 修复前桥接为「实例桥接」（工厂委托仅在首次解析时执行，容器永久缓存该实例）：
/// 热更新重建默认应用后注入方持已释放管理器（ODE），<c>SetDefaultApp</c> 后注入方继续返回
/// 旧应用的令牌（跨应用凭据串号）。详见 .docs/令牌与多应用管理-审查修复与完善方案.md §2.1。
/// </para>
/// <para>
/// 代理本身<b>无状态、不持有令牌、不实现 IDisposable 释放语义</b>（<see cref="IDisposable.Dispose"/> 为显式
/// no-op——<c>ITokenManager : IDisposable</c> 强制要求实现），不进入退休队列生命周期，
/// 不产生泄漏。解析成本为一次委托调用 + 字典查找/属性访问，热路径（每业务请求 1 次）可接受。
/// 刻意不做 volatile 缓存 + 失效钩子优化：会重新引入「钉死」问题。
/// </para>
/// <para>AOT：纯接口转发，无反射/动态代码，<c>IL2026</c>/<c>IL3050</c> 不受影响；全 TFM 可用。</para>
/// </remarks>
internal sealed class ForwardingTenantTokenManager : ITenantTokenManager
{
    private readonly Func<ITenantTokenManager> _resolve;

    public ForwardingTenantTokenManager(Func<ITenantTokenManager> resolve)
        => _resolve = resolve ?? throw new ArgumentNullException(nameof(resolve));

    /// <inheritdoc />
    public Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
        => _resolve().GetTokenAsync(cancellationToken);

    /// <inheritdoc />
    public Task<string> GetTokenAsync(string[]? scopes, CancellationToken cancellationToken = default)
        => _resolve().GetTokenAsync(scopes, cancellationToken);

    /// <inheritdoc />
    public Task<string> GetOrRefreshTokenAsync(CancellationToken cancellationToken = default)
        => _resolve().GetOrRefreshTokenAsync(cancellationToken);

    /// <inheritdoc />
    public Task<string> GetOrRefreshTokenAsync(string[]? scopes, CancellationToken cancellationToken = default)
        => _resolve().GetOrRefreshTokenAsync(scopes, cancellationToken);

    /// <inheritdoc />
    public Task<TokenResult> InvalidateTokenAsync(string[]? scopes = null, CancellationToken cancellationToken = default)
        => _resolve().InvalidateTokenAsync(scopes, cancellationToken);

    /// <inheritdoc />
    public bool SupportsBackgroundRefresh => _resolve().SupportsBackgroundRefresh;

    /// <summary>
    /// 显式 no-op：代理不持有任何资源（无 Timer/锁/缓存），真正的释放由当前解析到的真实管理器
    /// 经退休队列生命周期承担（FeishuAppManager → FeishuAppContextRetirement）。
    /// </summary>
    void IDisposable.Dispose()
    {
    }
}

/// <summary>
/// TMR-P0-1（F1）：默认应用令牌管理器（应用身份）的转发代理，语义同 <see cref="ForwardingTenantTokenManager"/>。
/// </summary>
internal sealed class ForwardingAppTokenManager : IAppTokenManager
{
    private readonly Func<IAppTokenManager> _resolve;

    public ForwardingAppTokenManager(Func<IAppTokenManager> resolve)
        => _resolve = resolve ?? throw new ArgumentNullException(nameof(resolve));

    /// <inheritdoc />
    public Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
        => _resolve().GetTokenAsync(cancellationToken);

    /// <inheritdoc />
    public Task<string> GetTokenAsync(string[]? scopes, CancellationToken cancellationToken = default)
        => _resolve().GetTokenAsync(scopes, cancellationToken);

    /// <inheritdoc />
    public Task<string> GetOrRefreshTokenAsync(CancellationToken cancellationToken = default)
        => _resolve().GetOrRefreshTokenAsync(cancellationToken);

    /// <inheritdoc />
    public Task<string> GetOrRefreshTokenAsync(string[]? scopes, CancellationToken cancellationToken = default)
        => _resolve().GetOrRefreshTokenAsync(scopes, cancellationToken);

    /// <inheritdoc />
    public Task<TokenResult> InvalidateTokenAsync(string[]? scopes = null, CancellationToken cancellationToken = default)
        => _resolve().InvalidateTokenAsync(scopes, cancellationToken);

    /// <inheritdoc />
    public bool SupportsBackgroundRefresh => _resolve().SupportsBackgroundRefresh;

    /// <summary>显式 no-op，理由同 <see cref="ForwardingTenantTokenManager"/>。</summary>
    void IDisposable.Dispose()
    {
    }
}

/// <summary>
/// TMR-P0-1（F1）：默认应用用户令牌管理器的转发代理，语义同 <see cref="ForwardingTenantTokenManager"/>。
/// 实现 <see cref="IFeishuUserTokenManager"/> 全员（含 <see cref="IUserTokenManager"/> 按用户成员与
/// <see cref="IFeishuUserTokenManager.StoreUserTokenAsync"/>）逐一现取转发。
/// </summary>
internal sealed class ForwardingUserTokenManager : IFeishuUserTokenManager
{
    private readonly Func<IFeishuUserTokenManager> _resolve;

    public ForwardingUserTokenManager(Func<IFeishuUserTokenManager> resolve)
        => _resolve = resolve ?? throw new ArgumentNullException(nameof(resolve));

    // === ITokenManager 成员（现取转发） ===

    /// <inheritdoc />
    public Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
        => _resolve().GetTokenAsync(cancellationToken);

    /// <inheritdoc />
    public Task<string> GetTokenAsync(string[]? scopes, CancellationToken cancellationToken = default)
        => _resolve().GetTokenAsync(scopes, cancellationToken);

    /// <inheritdoc />
    public Task<string> GetOrRefreshTokenAsync(CancellationToken cancellationToken = default)
        => _resolve().GetOrRefreshTokenAsync(cancellationToken);

    /// <inheritdoc />
    public Task<string> GetOrRefreshTokenAsync(string[]? scopes, CancellationToken cancellationToken = default)
        => _resolve().GetOrRefreshTokenAsync(scopes, cancellationToken);

    /// <inheritdoc />
    public Task<TokenResult> InvalidateTokenAsync(string[]? scopes = null, CancellationToken cancellationToken = default)
        => _resolve().InvalidateTokenAsync(scopes, cancellationToken);

    /// <inheritdoc />
    public bool SupportsBackgroundRefresh => _resolve().SupportsBackgroundRefresh;

    /// <summary>显式 no-op，理由同 <see cref="ForwardingTenantTokenManager"/>。</summary>
    void IDisposable.Dispose()
    {
    }

    // === IUserTokenManager 成员（按 userId，现取转发） ===

    /// <inheritdoc />
    public Task<string?> GetTokenAsync(string? userId, CancellationToken cancellationToken = default)
        => _resolve().GetTokenAsync(userId, cancellationToken);

    /// <inheritdoc />
    public Task<string?> GetTokenAsync(string? userId, string[]? scopes, CancellationToken cancellationToken = default)
        => _resolve().GetTokenAsync(userId, scopes, cancellationToken);

    /// <inheritdoc />
    public Task<UserTokenInfo?> GetTokenInfoAsync(string userId, CancellationToken cancellationToken = default)
        => _resolve().GetTokenInfoAsync(userId, cancellationToken);

    /// <inheritdoc />
    public Task<UserTokenInfo?> GetUserTokenWithCodeAsync(
        string code, string redirectUri, CancellationToken cancellationToken = default)
        => _resolve().GetUserTokenWithCodeAsync(code, redirectUri, cancellationToken);

    /// <inheritdoc />
    public Task<UserTokenInfo?> RefreshUserTokenAsync(string userId, CancellationToken cancellationToken = default)
        => _resolve().RefreshUserTokenAsync(userId, cancellationToken);

    /// <inheritdoc />
    public Task<bool> RemoveTokenAsync(string userId, CancellationToken cancellationToken = default)
        => _resolve().RemoveTokenAsync(userId, cancellationToken);

    /// <inheritdoc />
    public Task<bool> HasValidTokenAsync(string userId, CancellationToken cancellationToken = default)
        => _resolve().HasValidTokenAsync(userId, cancellationToken);

    /// <inheritdoc />
    public Task<bool> CanRefreshTokenAsync(string userId, CancellationToken cancellationToken = default)
        => _resolve().CanRefreshTokenAsync(userId, cancellationToken);

    /// <inheritdoc />
    public Task<string?> GetOrRefreshTokenAsync(string? userId, CancellationToken cancellationToken = default)
        => _resolve().GetOrRefreshTokenAsync(userId, cancellationToken);

    /// <inheritdoc />
    public Task<string?> GetOrRefreshTokenAsync(string? userId, string[]? scopes, CancellationToken cancellationToken = default)
        => _resolve().GetOrRefreshTokenAsync(userId, scopes, cancellationToken);

    // === IFeishuUserTokenManager 成员 ===

    /// <inheritdoc />
    public Task StoreUserTokenAsync(string userId, UserTokenInfo tokenInfo, CancellationToken cancellationToken = default)
        => _resolve().StoreUserTokenAsync(userId, tokenInfo, cancellationToken);
}
