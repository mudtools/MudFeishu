// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Options;

namespace Mud.Feishu.Abstractions.Authentication;

/// <summary>
/// 飞书应用级令牌管理器基类
/// </summary>
/// <remarks>
/// <para>
/// 提取 TenantTokenManager 和 AppTokenManager 的公共逻辑。
/// 继承 Mud.HttpUtils v2.0 的 <see cref="TokenManagerBase"/>，获得内置并发安全、自动清理、重试等能力。
/// </para>
/// <para>
/// <b>持久化接线（令牌存储桥接）</b>：注入 <see cref="ITokenStore"/> 时，基类缓存改为
/// <see cref="TokenStoreBackedTokenCache{T}"/>（内层经 <see cref="PurgeGateTokenStoreDecorator"/>
/// 承担清库门与读容错），于是：
/// <list type="bullet">
/// <item><b>恢复</b>：管线在刷新前经 <c>IAsyncTokenCache&lt;T&gt;.GetAsync</c> 读穿透直达持久层，
/// 命中即返回（不再进入刷新路径）；存储值缺过期戳或剩余 ≤ <c>TokenRefreshThreshold</c> 一律视为未命中
/// （D9 / TMA-15）。</item>
/// <item><b>写穿</b>：刷新成功后基类 <c>UpdateToken</c> 写缓存即自动写穿持久层，
/// TTL 由值适配器从 <see cref="CredentialToken.Expire"/> 推导（与管线判定同源）。</item>
/// <item><b>失效</b>：基类 <c>InvalidateTokenAsync</c> 经桥接器写穿删除持久层槽位
/// （TMA-01 / D1：内存与持久层双清）。</item>
/// </list>
/// 不注入 store 时使用与基类无参构造同款的进程内缓存，行为与改造前完全一致。
/// </para>
/// </remarks>
internal abstract class FeishuAppTokenManagerBase : TokenManagerBase
{
    private readonly IFeishuAuthentication _authenticationApi;
    private readonly FeishuAppConfig _options;
    private readonly ILogger _logger;
    private readonly string _tokenTypeKey;

    protected IFeishuAuthentication AuthenticationApi => _authenticationApi;
    protected FeishuAppConfig Options => _options;

    protected FeishuAppTokenManagerBase(
        IFeishuAuthentication authenticationApi,
        IOptions<FeishuAppConfig> options,
        ILogger logger,
        ITokenStore? tokenStore,
        string tokenTypeKeyPrefix)
        : base(BuildCache(tokenStore, options, logger, tokenTypeKeyPrefix))
    {
        _authenticationApi = authenticationApi ?? throw new ArgumentNullException(nameof(authenticationApi));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _tokenTypeKey = BuildTokenTypeKey(options, tokenTypeKeyPrefix);
    }

    protected override int ExpireThresholdSeconds => _options.TokenRefreshThreshold;

    public override async Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        return await GetOrRefreshTokenAsync(cancellationToken).ConfigureAwait(false);
    }

    protected override async Task<CredentialToken> RefreshTokenCoreAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Refreshing {TokenType} for AppId: {AppId}", _tokenTypeKey, _options.AppId);

        var result = await RefreshTokenFromApiAsync(cancellationToken).ConfigureAwait(false);

        // v1.1 修复 P0：飞书 API 异常响应时 result.AccessToken 可能为 null，
        // 空值属于状态无效而非参数传递错误，抛 InvalidOperationException 并附带上下文信息，便于诊断。
        if (string.IsNullOrEmpty(result.AccessToken))
        {
            throw new InvalidOperationException(
                $"飞书 API 刷新 {_tokenTypeKey} 令牌失败：返回的 AccessToken 为空。AppId: {_options.AppId}");
        }

        // 持久化由基类 UpdateToken → 桥接器写穿承担（不再手工编码落库）。
        return new CredentialToken
        {
            AccessToken = result.AccessToken,
            Expire = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + (result.ExpireSeconds * 1000L)
        };
    }

    protected abstract Task<(string? AccessToken, int ExpireSeconds)> RefreshTokenFromApiAsync(CancellationToken cancellationToken);

    /// <summary>
    /// 构建令牌缓存：有持久化存储时装配桥接器，否则退回与基类无参构造一致的进程内缓存。
    /// </summary>
    /// <remarks>
    /// 本方法在基类构造链上执行，<b>不</b>承担参数校验（构造函数体统一负责，保持既有异常类型与参数名）。
    /// 桥接器与装饰器在此处包装 per-app store，使「直接 new 管理器」的路径同样获得读穿透、写穿与门控。
    /// </remarks>
    private static ITokenCache<CredentialToken> BuildCache(
        ITokenStore? tokenStore,
        IOptions<FeishuAppConfig>? options,
        ILogger logger,
        string tokenTypeKeyPrefix)
    {
        if (tokenStore is null)
            return new ConcurrentDictionaryTokenCache<CredentialToken>();

        var appKey = options?.Value?.AppKey ?? string.Empty;
        var tokenTypeKey = BuildTokenTypeKey(options, tokenTypeKeyPrefix);

        return new TokenStoreBackedTokenCache<CredentialToken>(
            new PurgeGateTokenStoreDecorator(tokenStore, appKey, logger),
            valueAdapter: AdaptCredentialToken,
            valueFactory: CreateCredentialToken,
            // BD-1：键映射为常量 tokenType —— 物理键派生（TokenKeyBuilder）与改造前逐字节一致。
            storeKeyMapper: _ => tokenTypeKey,
            logger: logger);
    }

    /// <summary>
    /// 令牌类型键（store 侧 tokenType / 日志维度），构造口径与键映射保持一致。
    /// </summary>
    private static string BuildTokenTypeKey(IOptions<FeishuAppConfig>? options, string tokenTypeKeyPrefix)
        => $"{tokenTypeKeyPrefix}:{options?.Value?.AppKey}";

    /// <summary>
    /// 写穿方向的值适配：<see cref="CredentialToken"/> → 存储三元组（保持 <c>{expireMs}|{token}</c> 格式）。
    /// </summary>
    /// <remarks>
    /// 租户 / 应用令牌无 refresh；TTL 由 <see cref="CredentialToken.Expire"/> 推导，
    /// 剩余 ≤ 0 时返回 0 使桥接器<b>跳过</b>访问令牌写穿（不产生 1 秒 TTL 的垃圾条目）。
    /// </remarks>
    private static TokenStoreValue? AdaptCredentialToken(CredentialToken? token)
        => token is null
            ? null
            : new TokenStoreValue(
                accessToken: FeishuTokenBridgeCodec.EncodeToken(token.AccessToken, token.Expire),
                refreshToken: null,
                expiresInSeconds: FeishuTokenBridgeCodec.RemainingSeconds(token.Expire));

    /// <summary>
    /// 读穿透方向的值工厂：存储三元组 → <see cref="CredentialToken"/>。
    /// </summary>
    /// <remarks>
    /// 解码失败（TMA-15 旧格式 / 损坏值，无 <c>{expireMs}|</c> 前缀）或令牌为空时返回 null ⇒ 读穿透视为未命中。
    /// <c>IssuedAt = 0</c> 是存储格式不含签发时间所致的保守代价（TMF-06 方案 A）：
    /// 管线有效期判定退化为配置阈值 <c>TokenRefreshThreshold</c>，与改造前的恢复阈值（D9）数值与判定完全一致。
    /// </remarks>
    private static CredentialToken? CreateCredentialToken(TokenStoreValue value)
    {
        var (accessToken, expireTimestampMs) = FeishuTokenBridgeCodec.DecodeToken(value.AccessToken);
        if (string.IsNullOrEmpty(accessToken) || expireTimestampMs <= 0)
            return null;

        return new CredentialToken
        {
            AccessToken = accessToken,
            Expire = expireTimestampMs,
            IssuedAt = 0
        };
    }
}
