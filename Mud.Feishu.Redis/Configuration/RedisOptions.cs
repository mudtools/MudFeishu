// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.Utilities;

namespace Mud.Feishu.Redis.Configuration;

/// <summary>
/// Redis 配置选项（R4：连接见 <see cref="Connection"/> / <see cref="Advanced"/>，去重键见 RedisOptions 顶层与 FeishuDeduplication）。
/// </summary>
/// <remarks>
/// <para>
/// <b>R5.2/X6/X13 —— 本类型顶层的去重相关字段均为「双读期回落基座」：</b>
/// <see cref="EventKeyPrefix"/>、<see cref="EventCacheExpiration"/>、<see cref="NonceTtl"/>、
/// <see cref="NonceKeyPrefix"/>、<see cref="SeqIdCacheExpiration"/>、<see cref="SeqIdKeyPrefix"/>、
/// <see cref="SeqIdScopeKey"/>、<see cref="AppKey"/>。
/// </para>
/// <para>
/// 统一节 <c>FeishuDeduplication</c> 存在时，其
/// <c>Event</c>/<c>Nonce</c>/<c>SeqId</c> 字段按「字段级优先」覆盖上述旧键；
/// <b>同时配置两者时旧键会被静默忽略</b>（R5.2 起改为输出 Warning 指明覆盖关系）。
/// 新部署请只使用 <c>FeishuDeduplication</c>，见 <c>documents/Configuration/DeduplicationTruthSource.md</c>。
/// </para>
/// <para>
/// <b>为什么不给这些属性加 <c>[Obsolete]</c></b>：它们在统一节缺失时是**真正生效**的配置面，
/// 给 SDK 自身必须读取的属性加 Obsolete 只会产生噪音，而真正的误用入口（<c>appsettings.json</c>）
/// 对 Obsolete 完全免疫。用「运行时精确告警」替代编译期警告。
/// </para>
/// </remarks>
public class RedisOptions
{
    /// <summary>连接嵌套配置</summary>
    public RedisConnectionOptions Connection { get; set; } = new();

    /// <summary>高级连接旋钮（AllowAdmin / ClientName）</summary>
    public RedisAdvancedOptions Advanced { get; set; } = new();

    /// <summary>
    /// Nonce 有效期，默认 600 秒（WHF-03：建议为 Webhook TimestampToleranceSeconds 的 2 倍且至少严格大于）。
    /// <para>WHF-R2/A4：默认从 5 分钟调整为 600 秒（2 × 300s 上限容差），旧默认恰好压线。</para>
    /// </summary>
    public TimeSpan NonceTtl { get; set; } = TimeSpan.FromSeconds(600);

    /// <summary>Nonce 去重键前缀</summary>
    public string NonceKeyPrefix
    {
        get => _nonceKeyPrefix;
        set => _nonceKeyPrefix = string.IsNullOrEmpty(value) ? Consts.DefaultNonceKeyPrefix : value;
    }
    private string _nonceKeyPrefix = Consts.DefaultNonceKeyPrefix;

    /// <summary>SeqID 去重键前缀</summary>
    public string SeqIdKeyPrefix
    {
        get => _seqIdKeyPrefix;
        set => _seqIdKeyPrefix = string.IsNullOrEmpty(value) ? Consts.DefaultSeqIdKeyPrefix : value;
    }
    private string _seqIdKeyPrefix = Consts.DefaultSeqIdKeyPrefix;

    /// <summary>
    /// 事件去重缓存过期时间，默认 48 小时。
    /// <para>双读期回落基座：<c>FeishuDeduplication:Event:Ttl</c> 存在时会被其覆盖（R5.2 起输出 Warning）。</para>
    /// </summary>
    public TimeSpan EventCacheExpiration
    {
        get => _eventCacheExpiration;
        set => _eventCacheExpiration = value >= TimeSpan.FromMinutes(1) ? value : TimeSpan.FromMinutes(1);
    }
    private TimeSpan _eventCacheExpiration = TimeSpan.FromMilliseconds(Consts.DefaultCacheExpirationMs);

    /// <summary>
    /// 事件去重键前缀。
    /// <para>双读期回落基座：<c>FeishuDeduplication:Event:KeyPrefix</c> 存在时会被其覆盖（R5.2 起输出 Warning）。</para>
    /// </summary>
    public string EventKeyPrefix
    {
        get => _eventKeyPrefix;
        set => _eventKeyPrefix = string.IsNullOrEmpty(value) ? Consts.DefaultEventKeyPrefix : value;
    }
    private string _eventKeyPrefix = Consts.DefaultEventKeyPrefix;

    /// <summary>SeqID 去重缓存过期时间，默认 48 小时</summary>
    public TimeSpan SeqIdCacheExpiration
    {
        get => _seqIdCacheExpiration;
        set => _seqIdCacheExpiration = value >= TimeSpan.FromMinutes(1) ? value : TimeSpan.FromMinutes(1);
    }
    private TimeSpan _seqIdCacheExpiration = TimeSpan.FromMilliseconds(Consts.DefaultCacheExpirationMs);

    /// <summary>SeqID 隔离维度键；空则合成 {AppKey}|{MachineName}</summary>
    public string? SeqIdScopeKey { get; set; }

    /// <summary>
    /// SeqID 去重 Sorted Set 的容量窗口上界（成员数），默认 <see cref="Consts.DefaultSeqIdWindowCapacity"/>。
    /// <para>R2-01：Sorted Set 的窗口语义为「容量窗口」——每次写入按排名裁剪，仅保留分数最大的
    /// 该数量个成员；<c>GetCacheCount</c> 返回当前成员数（≤ 本值），<c>GetMaxProcessedSeqId</c>
    /// 返回窗口内真实最大值。</para>
    /// </summary>
    public int SeqIdWindowCapacity
    {
        get => _seqIdWindowCapacity;
        set => _seqIdWindowCapacity = value > 0 ? value : Consts.DefaultSeqIdWindowCapacity;
    }
    private int _seqIdWindowCapacity = Consts.DefaultSeqIdWindowCapacity;

    /// <summary>
    /// 令牌键前缀的环境段（R2-04），默认 <c>feishu</c>，最终键前缀为
    /// <c>{TokenKeyPrefix}:{appKey}:token</c>。
    /// <para>
    /// 事件/Nonce/SeqID 三类键的前缀均可配置（<c>EventKeyPrefix</c> 等）；令牌键此前硬编码 <c>feishu</c>，
    /// 导致多环境共用一个 Redis 时租户/用户令牌跨环境串号。需要环境隔离时，
    /// 各环境配置不同的 <c>TokenKeyPrefix</c>（如 <c>dev</c>/<c>prod</c>）。
    /// </para>
    /// <para>空值兜底为默认值；Memory 路径不使用本项（单进程内无跨环境共享）。</para>
    /// </summary>
    public string TokenKeyPrefix
    {
        get => _tokenKeyPrefix;
        set => _tokenKeyPrefix = string.IsNullOrEmpty(value) ? Consts.DefaultTokenKeyPrefix : value;
    }
    private string _tokenKeyPrefix = Consts.DefaultTokenKeyPrefix;

    /// <summary>飞书应用 AppKey，用于 SeqID scopeKey 合成，默认 "default"</summary>
    public string AppKey { get; set; } = "default";

    /// <summary>验证配置的有效性</summary>
    public void Validate()
    {
        Connection ??= new RedisConnectionOptions();
        Advanced ??= new RedisAdvancedOptions();

        if (string.IsNullOrWhiteSpace(Connection.ServerAddress))
            throw new InvalidOperationException("Connection.ServerAddress 不能为空");

        var server = Connection.ServerAddress;
        bool isValidFormat = server.Contains(':') ||
                            server.StartsWith("redis://", StringComparison.OrdinalIgnoreCase) ||
                            server.StartsWith("rediss://", StringComparison.OrdinalIgnoreCase);
        if (!isValidFormat)
            throw new InvalidOperationException("Connection.ServerAddress 格式无效，应为 'host:port' 或 'redis://host:port' 或 'rediss://host:port'");

        if (Connection.ConnectTimeout < 1000)
            throw new InvalidOperationException("Connection.ConnectTimeout 必须至少为 1000 毫秒");
        if (Connection.SyncTimeout < 1000)
            throw new InvalidOperationException("Connection.SyncTimeout 必须至少为 1000 毫秒");
        if (Connection.ConnectRetry < 0)
            throw new InvalidOperationException("Connection.ConnectRetry 不能为负数");

        if (NonceTtl <= TimeSpan.Zero)
            throw new InvalidOperationException("NonceTtl 必须为正值（建议为 TimestampToleranceSeconds 的 2 倍且至少严格大于）");
        if (SeqIdCacheExpiration <= TimeSpan.Zero)
            throw new InvalidOperationException("SeqIdCacheExpiration 必须为正值");
        if (EventCacheExpiration <= TimeSpan.Zero)
            throw new InvalidOperationException("EventCacheExpiration 必须为正值");
        // R2-01：容量窗口必须为正——非正会让写入时裁剪退化为"每次清空集合"
        if (SeqIdWindowCapacity <= 0)
            throw new InvalidOperationException("SeqIdWindowCapacity 必须为正值（Sorted Set 容量窗口上界）");

        ValidateKeyPrefix(NonceKeyPrefix, nameof(NonceKeyPrefix));
        ValidateKeyPrefix(SeqIdKeyPrefix, nameof(SeqIdKeyPrefix));
        ValidateKeyPrefix(EventKeyPrefix, nameof(EventKeyPrefix));
        // R2-04：令牌键前缀同样受护栏约束（非空、不以通配符开头）
        ValidateKeyPrefix(TokenKeyPrefix, nameof(TokenKeyPrefix));
    }

    private static void ValidateKeyPrefix(string prefix, string name)
    {
        if (string.IsNullOrEmpty(prefix))
            throw new InvalidOperationException($"{name} 不能为空——空前缀会导致 SCAN 退化为全库匹配（R-01 护栏）");
        if (prefix.StartsWith("*"))
            throw new InvalidOperationException($"{name} 不能以 '*' 开头——通配符前缀会导致 SCAN 匹配所有键（R-01 护栏）");
    }

    /// <summary>
    /// 从配置节回填 R3 前的扁平连接键（仅配置 JSON 兼容；代码 API 使用嵌套属性）。
    /// </summary>
    /// <param name="section">FeishuRedis 配置节</param>
    public void ApplyLegacyFlatConnectionKeys(IConfigurationSection section)
    {
        if (section is null)
            return;

        Connection ??= new RedisConnectionOptions();
        Advanced ??= new RedisAdvancedOptions();

        static string? Raw(IConfigurationSection s, string key) => s[key];

        var addr = Raw(section, "ServerAddress");
        if (!string.IsNullOrEmpty(addr) && Connection.ServerAddress == "localhost:6379")
            Connection.ServerAddress = addr!;

        var password = Raw(section, "Password");
        if (password is not null && Connection.Password.Length == 0)
            Connection.Password = password;

        if (int.TryParse(Raw(section, "ConnectTimeout"), out var ct) && Connection.ConnectTimeout == 5000)
            Connection.ConnectTimeout = ct;
        if (int.TryParse(Raw(section, "SyncTimeout"), out var st) && Connection.SyncTimeout == 5000)
            Connection.SyncTimeout = st;
        if (int.TryParse(Raw(section, "ConnectRetry"), out var cr) && Connection.ConnectRetry == Consts.DefaultRedisConnectRetry)
            Connection.ConnectRetry = cr;
        if (bool.TryParse(Raw(section, "Ssl"), out var ssl) && !Connection.Ssl)
            Connection.Ssl = ssl;
        if (bool.TryParse(Raw(section, "AllowAdmin"), out var admin) && !Advanced.AllowAdmin)
            Advanced.AllowAdmin = admin;
        if (bool.TryParse(Raw(section, "AbortOnConnectFail"), out var abort) && Connection.AbortOnConnectFail)
            Connection.AbortOnConnectFail = abort;
        if (int.TryParse(Raw(section, "DefaultDatabase"), out var db) && Connection.DefaultDatabase is null)
            Connection.DefaultDatabase = db;
        var clientName = Raw(section, "ClientName");
        if (!string.IsNullOrEmpty(clientName) && Advanced.ClientName is null)
            Advanced.ClientName = clientName;
    }

    /// <summary>
    /// 输出配置摘要（敏感字段经 <see cref="SensitiveDataUtils.MaskSensitiveData"/> 掩码）。
    /// </summary>
    /// <returns>用于日志的配置摘要字符串。</returns>
    public override string ToString()
    {
        Connection ??= new RedisConnectionOptions();
        Advanced ??= new RedisAdvancedOptions();
        var maskedAddress = SensitiveDataUtils.MaskSensitiveData(Connection.ServerAddress);
        var maskedClientName = Advanced.ClientName != null ? SensitiveDataUtils.MaskSensitiveData(Advanced.ClientName) : "null";
        return $"RedisOptions {{ Connection.ServerAddress: {maskedAddress}, Password: {SensitiveDataUtils.MaskSensitiveData(Connection.Password)}, DefaultDatabase: {Connection.DefaultDatabase?.ToString() ?? "默认"}, ConnectTimeout: {Connection.ConnectTimeout}ms, SyncTimeout: {Connection.SyncTimeout}ms, Ssl: {Connection.Ssl}, EventCacheExpiration: {EventCacheExpiration}, SeqIdCacheExpiration: {SeqIdCacheExpiration}, SeqIdWindowCapacity: {SeqIdWindowCapacity}, EventKeyPrefix: {EventKeyPrefix}, NonceKeyPrefix: {NonceKeyPrefix}, SeqIdKeyPrefix: {SeqIdKeyPrefix}, TokenKeyPrefix: {TokenKeyPrefix}, ClientName: {maskedClientName} }}";
    }
}
