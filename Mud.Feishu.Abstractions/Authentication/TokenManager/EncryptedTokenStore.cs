// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Mud.Feishu.Abstractions.Authentication;

/// <summary>
/// 令牌存储加密装饰器（ENH-1），对 <see cref="ITokenStore"/> 的读写做透明加解密。
/// </summary>
/// <remarks>
/// <para>
/// 令牌（<c>tenant_access_token</c> / <c>app_access_token</c> 及其 refresh token）默认以明文写入存储后端。
/// 在 Redis 等**持久化、跨进程共享**的后端上，明文令牌意味着任何能读到键值的人都能直接盗用应用身份。
/// 本装饰器在写入前调用 <see cref="IEncryptionProvider.Encrypt"/>、读取后调用
/// <see cref="IEncryptionProvider.Decrypt"/>，对上层（TokenManager）完全透明。
/// </para>
/// <para>
/// 仅令牌值被加密；<b>键布局不变</b>，因此不改变键隔离语义
/// （<c>feishu:{appKey}:token:{tokenType}:access</c>）。
/// </para>
/// <para>
/// 由 <see cref="FeishuAppOptions.EnableTokenEncryption"/> 控制，默认 <c>false</c>。
/// 启用后<b>已存在的明文令牌无法自动迁移</b>：解密旧明文会失败，此时按「缓存未命中」处理并回退为重新获取，
/// 属预期行为（本项目未发布，无存量数据）。
/// </para>
/// </remarks>
public sealed class EncryptedTokenStore : ITokenStore
{
    private readonly ITokenStore _inner;
    private readonly IEncryptionProvider _encryption;

    /// <summary>
    /// 初始化 <see cref="EncryptedTokenStore"/> 实例。
    /// </summary>
    /// <param name="inner">被装饰的底层令牌存储。</param>
    /// <param name="encryption">加密提供程序。</param>
    public EncryptedTokenStore(ITokenStore inner, IEncryptionProvider encryption)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _encryption = encryption ?? throw new ArgumentNullException(nameof(encryption));
    }

    /// <inheritdoc />
    public async Task<string?> GetAccessTokenAsync(string tokenType, CancellationToken cancellationToken = default)
        => Decrypt(await _inner.GetAccessTokenAsync(tokenType, cancellationToken).ConfigureAwait(false));

    /// <inheritdoc />
    public Task SetAccessTokenAsync(string tokenType, string accessToken, long expiresInSeconds, CancellationToken cancellationToken = default)
        => _inner.SetAccessTokenAsync(tokenType, _encryption.Encrypt(accessToken), expiresInSeconds, cancellationToken);

    /// <inheritdoc />
    public async Task<string?> GetRefreshTokenAsync(string tokenType, CancellationToken cancellationToken = default)
        => Decrypt(await _inner.GetRefreshTokenAsync(tokenType, cancellationToken).ConfigureAwait(false));

    /// <inheritdoc />
    public Task SetRefreshTokenAsync(string tokenType, string refreshToken, CancellationToken cancellationToken = default)
        => _inner.SetRefreshTokenAsync(tokenType, _encryption.Encrypt(refreshToken), cancellationToken);

    /// <inheritdoc />
    public Task RemoveAsync(string tokenType, CancellationToken cancellationToken = default)
        => _inner.RemoveAsync(tokenType, cancellationToken);

    /// <inheritdoc />
    public Task<IEnumerable<string>> GetTokenTypesAsync(CancellationToken cancellationToken = default)
        => _inner.GetTokenTypesAsync(cancellationToken);

    /// <inheritdoc />
    public Task ClearAsync(CancellationToken cancellationToken = default)
        => _inner.ClearAsync(cancellationToken);

    /// <summary>
    /// 解密失败（如旧明文数据、密钥轮换）时按「未命中」返回 null，
    /// 使上层回退到重新获取令牌，而不是因异常导致整个请求链路失败。
    /// </summary>
    private string? Decrypt(string? cipherText)
    {
        if (cipherText == null)
        {
            return null;
        }

        try
        {
            return _encryption.Decrypt(cipherText);
        }
        catch (Exception)
        {
            return null;
        }
    }
}

/// <summary>
/// 用户令牌存储加密装饰器（ENH-1）。
/// </summary>
/// <remarks>
/// <para>
/// 用户维度方法（<c>(userId, tokenType)</c> 重载）走本次装饰的加解密；
/// 继承自 <see cref="ITokenStore"/> 的租户维度方法则委托给**已加密的租户令牌存储**
/// （与 <c>UserTokenStoreBase</c> 把 <see cref="ITokenStore"/> 成员委托给内部租户存储的语义一致），
/// 避免同一存储出现「一处加密、一处明文」的不一致。
/// </para>
/// </remarks>
public sealed class EncryptedUserTokenStore : IUserTokenStore
{
    private readonly IUserTokenStore _inner;
    private readonly EncryptedTokenStore _encryptedTenantStore;
    private readonly IEncryptionProvider _encryption;

    /// <summary>
    /// 初始化 <see cref="EncryptedUserTokenStore"/> 实例。
    /// </summary>
    /// <param name="inner">被装饰的底层用户令牌存储。</param>
    /// <param name="encryptedTenantStore">用于实现继承的 <see cref="ITokenStore"/> 成员的已加密租户存储。</param>
    /// <param name="encryption">加密提供程序。</param>
    public EncryptedUserTokenStore(IUserTokenStore inner, EncryptedTokenStore encryptedTenantStore, IEncryptionProvider encryption)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _encryptedTenantStore = encryptedTenantStore ?? throw new ArgumentNullException(nameof(encryptedTenantStore));
        _encryption = encryption ?? throw new ArgumentNullException(nameof(encryption));
    }

    #region 用户维度（本次装饰）

    /// <inheritdoc />
    public async Task<string?> GetAccessTokenAsync(string userId, string tokenType, CancellationToken cancellationToken = default)
        => Decrypt(await _inner.GetAccessTokenAsync(userId, tokenType, cancellationToken).ConfigureAwait(false));

    /// <inheritdoc />
    public Task SetAccessTokenAsync(string userId, string tokenType, string accessToken, long expiresInSeconds, CancellationToken cancellationToken = default)
        => _inner.SetAccessTokenAsync(userId, tokenType, _encryption.Encrypt(accessToken), expiresInSeconds, cancellationToken);

    /// <inheritdoc />
    public async Task<string?> GetRefreshTokenAsync(string userId, string tokenType, CancellationToken cancellationToken = default)
        => Decrypt(await _inner.GetRefreshTokenAsync(userId, tokenType, cancellationToken).ConfigureAwait(false));

    /// <inheritdoc />
    public Task SetRefreshTokenAsync(string userId, string tokenType, string refreshToken, CancellationToken cancellationToken = default)
        => _inner.SetRefreshTokenAsync(userId, tokenType, _encryption.Encrypt(refreshToken), cancellationToken);

    /// <inheritdoc />
    public Task RemoveAsync(string userId, string tokenType, CancellationToken cancellationToken = default)
        => _inner.RemoveAsync(userId, tokenType, cancellationToken);

    /// <inheritdoc />
    public Task<IEnumerable<string>> GetTokenTypesAsync(string userId, CancellationToken cancellationToken = default)
        => _inner.GetTokenTypesAsync(userId, cancellationToken);

    /// <inheritdoc />
    public Task ClearUserAsync(string userId, CancellationToken cancellationToken = default)
        => _inner.ClearUserAsync(userId, cancellationToken);

    #endregion

    #region 继承自 ITokenStore（委托给已加密的租户存储）

    /// <inheritdoc />
    public Task<string?> GetAccessTokenAsync(string tokenType, CancellationToken cancellationToken = default)
        => _encryptedTenantStore.GetAccessTokenAsync(tokenType, cancellationToken);

    /// <inheritdoc />
    public Task SetAccessTokenAsync(string tokenType, string accessToken, long expiresInSeconds, CancellationToken cancellationToken = default)
        => _encryptedTenantStore.SetAccessTokenAsync(tokenType, accessToken, expiresInSeconds, cancellationToken);

    /// <inheritdoc />
    public Task<string?> GetRefreshTokenAsync(string tokenType, CancellationToken cancellationToken = default)
        => _encryptedTenantStore.GetRefreshTokenAsync(tokenType, cancellationToken);

    /// <inheritdoc />
    public Task SetRefreshTokenAsync(string tokenType, string refreshToken, CancellationToken cancellationToken = default)
        => _encryptedTenantStore.SetRefreshTokenAsync(tokenType, refreshToken, cancellationToken);

    /// <inheritdoc />
    public Task RemoveAsync(string tokenType, CancellationToken cancellationToken = default)
        => _encryptedTenantStore.RemoveAsync(tokenType, cancellationToken);

    /// <inheritdoc />
    public Task<IEnumerable<string>> GetTokenTypesAsync(CancellationToken cancellationToken = default)
        => _encryptedTenantStore.GetTokenTypesAsync(cancellationToken);

    /// <inheritdoc />
    public Task ClearAsync(CancellationToken cancellationToken = default)
        => _encryptedTenantStore.ClearAsync(cancellationToken);

    #endregion

    private string? Decrypt(string? cipherText)
    {
        if (cipherText == null)
        {
            return null;
        }

        try
        {
            return _encryption.Decrypt(cipherText);
        }
        catch (Exception)
        {
            return null;
        }
    }
}

/// <summary>
/// 令牌存储工厂加密装饰器（ENH-1）：把 <see cref="IFeishuTokenStoreFactory"/> 产出的
/// 租户/用户令牌存储统一包上加密。
/// </summary>
public sealed class EncryptedFeishuTokenStoreFactory : IFeishuTokenStoreFactory
{
    private readonly IFeishuTokenStoreFactory _inner;
    private readonly IEncryptionProvider _encryption;
    private readonly ILogger<EncryptedFeishuTokenStoreFactory>? _logger;

    /// <summary>
    /// 初始化 <see cref="EncryptedFeishuTokenStoreFactory"/> 实例。
    /// </summary>
    /// <param name="inner">被装饰的令牌存储工厂。</param>
    /// <param name="encryption">加密提供程序。</param>
    /// <param name="logger">日志记录器（可选）。</param>
    public EncryptedFeishuTokenStoreFactory(
        IFeishuTokenStoreFactory inner,
        IEncryptionProvider encryption,
        ILogger<EncryptedFeishuTokenStoreFactory>? logger = null)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _encryption = encryption ?? throw new ArgumentNullException(nameof(encryption));
        _logger = logger;
    }

    /// <inheritdoc />
    public (ITokenStore TokenStore, IUserTokenStore? UserTokenStore) Create(string appKey)
    {
        var (tokenStore, userTokenStore) = _inner.Create(appKey);
        var encryptedTenant = new EncryptedTokenStore(tokenStore, _encryption);

        if (userTokenStore == null)
        {
            return (encryptedTenant, null);
        }

        _logger?.LogDebug("应用 {AppKey} 的租户/用户令牌存储已启用加密。", appKey);
        return (encryptedTenant, new EncryptedUserTokenStore(userTokenStore, encryptedTenant, _encryption));
    }
}
