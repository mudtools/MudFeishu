// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.Authentication;

/// <summary>
/// 用户级令牌存储抽象基类
/// </summary>
/// <remarks>
/// 提供 IUserTokenStore 的通用实现骨架：
/// ITokenStore 方法委托给内部存储实例，IUserTokenStore 的用户级方法由子类实现。
/// 子类只需关注特定存储后端的用户级令牌读写逻辑。
/// </remarks>
public abstract class UserTokenStoreBase : IUserTokenStore
{
    private readonly ITokenStore _innerStore;

    /// <summary>
    /// 初始化 UserTokenStoreBase 实例
    /// </summary>
    /// <param name="innerStore">内部令牌存储实例，用于 ITokenStore 方法委托</param>
    protected UserTokenStoreBase(ITokenStore innerStore)
    {
        _innerStore = innerStore ?? throw new ArgumentNullException(nameof(innerStore));
    }

    /// <inheritdoc />
    public Task<string?> GetAccessTokenAsync(string tokenType, CancellationToken cancellationToken = default)
        => _innerStore.GetAccessTokenAsync(tokenType, cancellationToken);

    /// <inheritdoc />
    public Task SetAccessTokenAsync(string tokenType, string accessToken, long expiresInSeconds, CancellationToken cancellationToken = default)
        => _innerStore.SetAccessTokenAsync(tokenType, accessToken, expiresInSeconds, cancellationToken);

    /// <inheritdoc />
    public Task<string?> GetRefreshTokenAsync(string tokenType, CancellationToken cancellationToken = default)
        => _innerStore.GetRefreshTokenAsync(tokenType, cancellationToken);

    /// <inheritdoc />
    public Task SetRefreshTokenAsync(string tokenType, string refreshToken, CancellationToken cancellationToken = default)
        => _innerStore.SetRefreshTokenAsync(tokenType, refreshToken, cancellationToken);

    /// <inheritdoc />
    public Task RemoveAsync(string tokenType, CancellationToken cancellationToken = default)
        => _innerStore.RemoveAsync(tokenType, cancellationToken);

    /// <inheritdoc />
    public Task<IEnumerable<string>> GetTokenTypesAsync(CancellationToken cancellationToken = default)
        => _innerStore.GetTokenTypesAsync(cancellationToken);

    /// <inheritdoc />
    public Task ClearAsync(CancellationToken cancellationToken = default)
        => _innerStore.ClearAsync(cancellationToken);

    /// <inheritdoc />
    public abstract Task<string?> GetAccessTokenAsync(string userId, string tokenType, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract Task SetAccessTokenAsync(string userId, string tokenType, string accessToken, long expiresInSeconds, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract Task<string?> GetRefreshTokenAsync(string userId, string tokenType, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract Task SetRefreshTokenAsync(string userId, string tokenType, string refreshToken, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract Task RemoveAsync(string userId, string tokenType, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract Task<IEnumerable<string>> GetTokenTypesAsync(string userId, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract Task ClearUserAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取键前缀，子类可重写以自定义前缀
    /// </summary>
    protected virtual string KeyPrefix => "feishu:token";

    /// <summary>
    /// 构建用户访问令牌的存储键
    /// </summary>
    /// <param name="userId">用户唯一标识符</param>
    /// <param name="tokenType">令牌类型标识符</param>
    /// <returns>存储键字符串</returns>
    protected virtual string BuildUserAccessTokenKey(string userId, string tokenType) => $"{KeyPrefix}:user:{userId}:{tokenType}:access";

    /// <summary>
    /// 构建用户刷新令牌的存储键
    /// </summary>
    /// <param name="userId">用户唯一标识符</param>
    /// <param name="tokenType">令牌类型标识符</param>
    /// <returns>存储键字符串</returns>
    protected virtual string BuildUserRefreshTokenKey(string userId, string tokenType) => $"{KeyPrefix}:user:{userId}:{tokenType}:refresh";
}
