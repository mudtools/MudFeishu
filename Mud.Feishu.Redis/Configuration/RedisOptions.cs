// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.Utilities;

namespace Mud.Feishu.Redis.Configuration;

/// <summary>
/// Redis 配置选项
/// </summary>
public class RedisOptions
{
    // Nonce 和 SeqID 键前缀已统一至 Consts.DefaultNonceKeyPrefix 和 Consts.DefaultSeqIdKeyPrefix

    /// <summary>
/// Redis 连接字符串
/// <para>示例: "localhost:6379", "127.0.0.1:6379", "rediss://secure.redis.com:6380"</para>
/// </summary>
[Obsolete("请使用 Connection.ServerAddress")]
public string ServerAddress
{
    get => Connection.ServerAddress;
    set => Connection.ServerAddress = value;
}

/// <summary>
/// Redis 密码
/// </summary>
[Obsolete("请使用 Connection.Password")]
public string Password
{
    get => Connection.Password;
    set => Connection.Password = value;
}

/// <summary>连接嵌套配置（C7/R3）</summary>
public RedisConnectionOptions Connection { get; set; } = new();

/// <summary>高级连接旋钮（C7/R3）</summary>
public RedisAdvancedOptions Advanced { get; set; } = new();

    /// <summary>
    /// Nonce 有效期，默认 5 分钟
    /// </summary>
    /// <remarks>
    /// 重放窗口不变量（WHF-03）：<b>本值必须 ≥ FeishuWebhookOptions.TimestampToleranceSeconds</b>——
    /// 否则在 Nonce 过期后、时间戳容差窗口结束前的区间内重放攻击可行。
    /// 默认组合（300s / 容差上限 300s）天然满足；跨工程 Options 无法在单一库内联断言，
    /// 由两侧 XML 文档共同声明该约束。
    /// </remarks>
    public TimeSpan NonceTtl { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Nonce 去重键前缀（空值时回退默认值，Validator 保证非空）
    /// </summary>
    public string NonceKeyPrefix
    {
        get => _nonceKeyPrefix;
        set => _nonceKeyPrefix = string.IsNullOrEmpty(value) ? Mud.Feishu.Abstractions.Consts.DefaultNonceKeyPrefix : value;
    }
    private string _nonceKeyPrefix = Mud.Feishu.Abstractions.Consts.DefaultNonceKeyPrefix;

    /// <summary>
    /// SeqID 去重键前缀（空值时回退默认值，Validator 保证非空）
    /// </summary>
    public string SeqIdKeyPrefix
    {
        get => _seqIdKeyPrefix;
        set => _seqIdKeyPrefix = string.IsNullOrEmpty(value) ? Mud.Feishu.Abstractions.Consts.DefaultSeqIdKeyPrefix : value;
    }
    private string _seqIdKeyPrefix = Mud.Feishu.Abstractions.Consts.DefaultSeqIdKeyPrefix;

    /// <summary>
    /// 事件去重缓存过期时间，默认 48 小时
    /// <para>应用于 RedisFeishuEventDistributedDeduplicator 的默认 TTL。</para>
    /// </summary>
    public TimeSpan EventCacheExpiration
    {
        get => _eventCacheExpiration;
        set => _eventCacheExpiration = value >= TimeSpan.FromMinutes(1) ? value : TimeSpan.FromMinutes(1);
    }
    private TimeSpan _eventCacheExpiration = TimeSpan.FromMilliseconds(Mud.Feishu.Abstractions.Consts.DefaultCacheExpirationMs);

    /// <summary>
    /// 事件去重键前缀，默认 "feishu:event:"
    /// </summary>
    public string EventKeyPrefix
    {
        get => _eventKeyPrefix;
        set => _eventKeyPrefix = string.IsNullOrEmpty(value) ? Mud.Feishu.Abstractions.Consts.DefaultEventKeyPrefix : value;
    }
    private string _eventKeyPrefix = Mud.Feishu.Abstractions.Consts.DefaultEventKeyPrefix;

    /// <summary>
    /// SeqID 去重缓存过期时间，默认 48 小时
    /// <para>应用于 RedisFeishuSeqIDDeduplicator 的缓存过期时间。</para>
    /// </summary>
    public TimeSpan SeqIdCacheExpiration
    {
        get => _seqIdCacheExpiration;
        set => _seqIdCacheExpiration = value >= TimeSpan.FromMinutes(1) ? value : TimeSpan.FromMinutes(1);
    }
    private TimeSpan _seqIdCacheExpiration = TimeSpan.FromMilliseconds(Mud.Feishu.Abstractions.Consts.DefaultCacheExpirationMs);

    /// <summary>
    /// SeqID 隔离维度键（ADR-3/T-M2-4）。
    /// <para>非空时直接使用；为空时由 DI 合成 <c>{AppKey}|{MachineName}</c>。</para>
    /// <para>多实例共享 Redis 时，scopeKey 必须包含实例维度，否则跨实例会互相判重。</para>
    /// </summary>
    public string? SeqIdScopeKey { get; set; }

    /// <summary>
    /// 飞书应用 AppKey，用于 SeqID scopeKey 合成。
    /// <para>仅在 <see cref="SeqIdScopeKey"/> 为空时参与合成，默认 "default"。</para>
    /// </summary>
    public string AppKey { get; set; } = "default";

    /// <summary>连接超时时间，默认 5000 毫秒</summary>
    [Obsolete("请使用 Connection.ConnectTimeout")]
    public int ConnectTimeout
    {
        get => Connection.ConnectTimeout;
        set => Connection.ConnectTimeout = value;
    }

    /// <summary>同步超时时间，默认 5000 毫秒</summary>
    [Obsolete("请使用 Connection.SyncTimeout")]
    public int SyncTimeout
    {
        get => Connection.SyncTimeout;
        set => Connection.SyncTimeout = value;
    }

    /// <summary>是否启用 TLS/SSL，默认 false</summary>
    [Obsolete("请使用 Connection.Ssl")]
    public bool Ssl
    {
        get => Connection.Ssl;
        set => Connection.Ssl = value;
    }

    /// <summary>是否允许管理员操作，默认 false</summary>
    [Obsolete("请使用 Advanced.AllowAdmin")]
    public bool AllowAdmin
    {
        get => Advanced.AllowAdmin;
        set => Advanced.AllowAdmin = value;
    }

    /// <summary>是否在连接失败时中止，默认 true</summary>
    [Obsolete("请使用 Connection.AbortOnConnectFail")]
    public bool AbortOnConnectFail
    {
        get => Connection.AbortOnConnectFail;
        set => Connection.AbortOnConnectFail = value;
    }

    /// <summary>连接重试次数，默认 3 次</summary>
    [Obsolete("请使用 Connection.ConnectRetry")]
    public int ConnectRetry
    {
        get => Connection.ConnectRetry;
        set => Connection.ConnectRetry = value;
    }

    /// <summary>默认数据库索引</summary>
    [Obsolete("请使用 Connection.DefaultDatabase")]
    public int? DefaultDatabase
    {
        get => Connection.DefaultDatabase;
        set => Connection.DefaultDatabase = value;
    }

    /// <summary>客户端名称</summary>
    [Obsolete("请使用 Advanced.ClientName")]
    public string? ClientName
    {
        get => Advanced.ClientName;
        set => Advanced.ClientName = value;
    }

    /// <summary>
    /// 验证配置的有效性
    /// </summary>
    public void Validate()
    {
#pragma warning disable CS0618
        if (string.IsNullOrWhiteSpace(ServerAddress))
            throw new InvalidOperationException("ServerAddress 不能为空");

        bool isValidFormat = ServerAddress.Contains(':') ||
                            ServerAddress.StartsWith("redis://", StringComparison.OrdinalIgnoreCase) ||
                            ServerAddress.StartsWith("rediss://", StringComparison.OrdinalIgnoreCase);

        if (!isValidFormat)
            throw new InvalidOperationException("ServerAddress 格式无效，应为 'host:port' 或 'redis://host:port' 或 'rediss://host:port'");

        if (ConnectTimeout < 1000)
            throw new InvalidOperationException("ConnectTimeout 必须至少为 1000 毫秒");

        if (SyncTimeout < 1000)
            throw new InvalidOperationException("SyncTimeout 必须至少为 1000 毫秒");

        if (ConnectRetry < 0)
            throw new InvalidOperationException("ConnectRetry 不能为负数");
#pragma warning restore CS0618

        // R-12/R-21 护栏：TTL 非正、前缀为空或以 * 开头 → 启动失败
        if (NonceTtl <= TimeSpan.Zero)
            throw new InvalidOperationException("NonceTtl 必须为正值（建议 5 分钟以上）");

        if (SeqIdCacheExpiration <= TimeSpan.Zero)
            throw new InvalidOperationException("SeqIdCacheExpiration 必须为正值");

        if (EventCacheExpiration <= TimeSpan.Zero)
            throw new InvalidOperationException("EventCacheExpiration 必须为正值");

        ValidateKeyPrefix(NonceKeyPrefix, nameof(NonceKeyPrefix));
        ValidateKeyPrefix(SeqIdKeyPrefix, nameof(SeqIdKeyPrefix));
        ValidateKeyPrefix(EventKeyPrefix, nameof(EventKeyPrefix));
    }

    /// <summary>
    /// 校验键前缀：非空且不以 * 开头。
    /// </summary>
    private static void ValidateKeyPrefix(string prefix, string name)
    {
        if (string.IsNullOrEmpty(prefix))
            throw new InvalidOperationException($"{name} 不能为空——空前缀会导致 SCAN 退化为全库匹配（R-01 护栏）");

        if (prefix.StartsWith("*"))
            throw new InvalidOperationException($"{name} 不能以 '*' 开头——通配符前缀会导致 SCAN 匹配所有键（R-01 护栏）");
    }

    /// <summary>
    /// 返回配置的字符串表示（敏感信息已掩码）
    /// </summary>
    public override string ToString()
    {
        // T-M3-8：ServerAddress 可能内联凭据（如 host:port,password=...），剥离后掩码
        var maskedAddress = SensitiveDataUtils.MaskSensitiveData(Connection.ServerAddress);
        var maskedClientName = Advanced.ClientName != null ? SensitiveDataUtils.MaskSensitiveData(Advanced.ClientName) : "null";
        return $"RedisOptions {{ ServerAddress: {maskedAddress}, Password: {SensitiveDataUtils.MaskSensitiveData(Connection.Password)}, DefaultDatabase: {Connection.DefaultDatabase?.ToString() ?? "默认"}, ConnectTimeout: {Connection.ConnectTimeout}ms, SyncTimeout: {Connection.SyncTimeout}ms, Ssl: {Connection.Ssl}, EventCacheExpiration: {EventCacheExpiration}, SeqIdCacheExpiration: {SeqIdCacheExpiration}, EventKeyPrefix: {EventKeyPrefix}, NonceKeyPrefix: {NonceKeyPrefix}, SeqIdKeyPrefix: {SeqIdKeyPrefix}, ClientName: {maskedClientName} }}";
    }
}
