// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.Configuration;

/// <summary>
/// 统一事件去重配置（C1，配置真相源入口）。
/// </summary>
/// <remarks>
/// <para>
/// 双读期优先级：代码 Configure → <c>FeishuDeduplication</c> 新节（存在且已绑定时）→
/// 旧键（RedisOptions.Event*/WebSocket:EventDeduplication 等）→ Consts 默认。
/// </para>
/// <para>
/// 多租户必须为 Event/Nonce/SeqId 配置互异的 KeyPrefix（TMA2-20）。本类型不构造
/// Redis 键或令牌键，键布局仍由 Redis 包 <c>RedisKeyBuilder</c> 与 D8
/// <c>TokenKeyBuilder</c> 负责。
/// </para>
/// <para>
/// Mode 描述事件去重传输语义；Distributed 仅在宿主已注册分布式实现时生效。
/// </para>
/// </remarks>
public class FeishuDeduplicationOptions
{
    /// <summary>配置节名：<c>FeishuDeduplication</c></summary>
    public const string SectionName = "FeishuDeduplication";

    /// <summary>默认 Mode：内存去重</summary>
    public const string DefaultMode = "InMemory";

    /// <summary>None：不进行事件去重</summary>
    public const string ModeNone = "None";

    /// <summary>InMemory：进程内内存去重</summary>
    public const string ModeInMemory = "InMemory";

    /// <summary>Distributed：Redis 等分布式去重</summary>
    public const string ModeDistributed = "Distributed";

    /// <summary>Default Profile</summary>
    public const string ProfileDefault = "Default";

    /// <summary>HighReliability Profile</summary>
    public const string ProfileHighReliability = "HighReliability";

    /// <summary>HighAvailability Profile</summary>
    public const string ProfileHighAvailability = "HighAvailability";

    /// <summary>
    /// 事件去重模式：<c>None | InMemory | Distributed</c>。
    /// </summary>
    public string Mode { get; set; } = DefaultMode;

    /// <summary>
    /// 去重预设：<c>Default | HighReliability | HighAvailability</c>。
    /// 字段级配置优先于 Profile（先选预设再微调）。
    /// </summary>
    public string Profile { get; set; } = ProfileDefault;

    /// <summary>事件去重条目配置</summary>
    public DeduplicationEntryOptions Event { get; set; } = new();

    /// <summary>Nonce 去重配置</summary>
    public NonceDeduplicationOptions Nonce { get; set; } = new();

    /// <summary>SeqID 去重配置</summary>
    public SeqIdDeduplicationOptions SeqId { get; set; } = new();

    /// <summary>
    /// 是否从 IConfiguration 的 FeishuDeduplication 节完成绑定。
    /// 仅当该节存在时由 SDK 置 true；为 true 时新节对旧键优先。
    /// </summary>
    public bool IsConfiguredFromConfiguration { get; set; }

    /// <summary>
    /// 校验 Mode/Profile/三前缀等不变量。
    /// </summary>
    public void Validate()
    {
        var mode = (Mode ?? DefaultMode).Trim();
        if (!string.Equals(mode, ModeNone, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(mode, ModeInMemory, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(mode, ModeDistributed, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"FeishuDeduplication:Mode '{Mode}' 无效，仅支持 None | InMemory | Distributed");
        }

        var profile = (Profile ?? ProfileDefault).Trim();
        if (!string.Equals(profile, ProfileDefault, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(profile, ProfileHighReliability, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(profile, ProfileHighAvailability, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"FeishuDeduplication:Profile '{Profile}' 无效，仅支持 Default | HighReliability | HighAvailability");
        }

        Event?.Validate(nameof(Event));
        Nonce?.Validate(nameof(Nonce));
        SeqId?.Validate(nameof(SeqId));
    }

    /// <summary>
    /// 按 Profile 解析基础去重预设（仅消费当前主路径真正生效的字段）。
    /// </summary>
    public DeduplicationOptions ResolveProfileDeduplicationOptions()
    {
        var profile = (Profile ?? ProfileDefault).Trim();
        if (string.Equals(profile, ProfileHighReliability, StringComparison.OrdinalIgnoreCase))
            return DeduplicationOptions.HighReliability;
        if (string.Equals(profile, ProfileHighAvailability, StringComparison.OrdinalIgnoreCase))
            return DeduplicationOptions.HighAvailability;
        return DeduplicationOptions.Default;
    }

    /// <summary>
    /// 解析事件 TTL：Profile 预设 → 配置字段覆盖。
    /// </summary>
    public TimeSpan ResolveEventTtl()
    {
        var profileTtl = ResolveProfileDeduplicationOptions().CacheExpiration;
        var configured = Event?.Ttl;
        return configured is { } t && t > TimeSpan.Zero ? t : profileTtl;
    }

    /// <summary>
    /// 解析处理中超时：Profile 预设 → 配置字段覆盖。
    /// </summary>
    public TimeSpan ResolveEventProcessingTimeout()
    {
        var profileValue = ResolveProfileDeduplicationOptions().ProcessingTimeout;
        var configured = Event?.ProcessingTimeout;
        return configured is { } t && t > TimeSpan.Zero ? t : profileValue;
    }
}

/// <summary>事件去重条目配置（统一节）</summary>
public class DeduplicationEntryOptions
{
    /// <summary>事件缓存 TTL；null/零表示未覆盖（回退 Profile/旧键/Consts）</summary>
    public TimeSpan? Ttl { get; set; }

    /// <summary>处理中超时；null/零表示未覆盖</summary>
    public TimeSpan? ProcessingTimeout { get; set; }

    /// <summary>内存清理间隔；null/零表示未覆盖</summary>
    public TimeSpan? CleanupInterval { get; set; }

    /// <summary>事件键前缀；null/空表示未覆盖</summary>
    public string? KeyPrefix { get; set; }

    /// <summary>内存最大条目数；null 表示未覆盖</summary>
    public int? MaxCacheSize { get; set; }

    internal void Validate(string sectionName)
    {
        if (Ttl is { } ttl && ttl <= TimeSpan.Zero)
            throw new InvalidOperationException($"{sectionName}:Ttl 必须为正值");
        if (ProcessingTimeout is { } pt && pt <= TimeSpan.Zero)
            throw new InvalidOperationException($"{sectionName}:ProcessingTimeout 必须为正值");
        if (!string.IsNullOrEmpty(KeyPrefix) && KeyPrefix.StartsWith("*"))
            throw new InvalidOperationException($"{sectionName}:KeyPrefix 不能以 '*' 开头（R-01）");
        if (MaxCacheSize is < 0)
            throw new InvalidOperationException($"{sectionName}:MaxCacheSize 不能为负数");
    }
}

/// <summary>Nonce 去重配置（统一节）</summary>
public class NonceDeduplicationOptions
{
    /// <summary>Nonce TTL；null 表示未覆盖</summary>
    public TimeSpan? Ttl { get; set; }

    /// <summary>Nonce 键前缀；null/空表示未覆盖</summary>
    public string? KeyPrefix { get; set; }

    internal void Validate(string sectionName)
    {
        if (Ttl is { } ttl && ttl <= TimeSpan.Zero)
            throw new InvalidOperationException($"{sectionName}:Ttl 必须为正值");
        if (!string.IsNullOrEmpty(KeyPrefix) && KeyPrefix.StartsWith("*"))
            throw new InvalidOperationException($"{sectionName}:KeyPrefix 不能以 '*' 开头（R-01）");
    }
}

/// <summary>SeqID 去重配置（统一节）</summary>
public class SeqIdDeduplicationOptions
{
    /// <summary>SeqID TTL；null 表示未覆盖</summary>
    public TimeSpan? Ttl { get; set; }

    /// <summary>SeqID 键前缀；null/空表示未覆盖</summary>
    public string? KeyPrefix { get; set; }

    /// <summary>SeqID 隔离维度；null 表示未覆盖</summary>
    public string? ScopeKey { get; set; }

    internal void Validate(string sectionName)
    {
        if (Ttl is { } ttl && ttl <= TimeSpan.Zero)
            throw new InvalidOperationException($"{sectionName}:Ttl 必须为正值");
        if (!string.IsNullOrEmpty(KeyPrefix) && KeyPrefix.StartsWith("*"))
            throw new InvalidOperationException($"{sectionName}:KeyPrefix 不能以 '*' 开头（R-01）");
    }
}
