// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.Configuration;
using Mud.Feishu.Abstractions.Extensions;
using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.Redis.Configuration;
using Mud.Feishu.Redis.Diagnostics;
using Mud.Feishu.Redis.HealthChecks;
using Mud.Feishu.Redis.Services;
using StackExchange.Redis;
using System.Diagnostics.CodeAnalysis;


#pragma warning disable CS0618 // R5/X6: Obsolete dual-read fallback base — intentionally references DeduplicationOptions/EventDeduplicationOptions
namespace Mud.Feishu.Redis.Extensions;

/// <summary>
/// 飞书 Redis 分布式去重服务扩展
/// </summary>
public static class RedisFeishuServiceBuilderExtensions
{
    /// <summary>
    /// 注册 Redis 连接服务（连接多路复器、选项、健康检查、启动期预热）。
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="registerHealthCheck">
    /// 是否把 <see cref="RedisHealthCheck"/> 注册到宿主健康检查系统（R2-12）。
    /// <c>true</c>（默认）时库内会调用 <c>AddHealthChecks()</c>——对未使用健康检查的宿主产生隐式注册；
    /// 传 <c>false</c> 则仅注册 <see cref="RedisHealthCheck"/> 类型，由宿主自行 <c>AddCheck</c>。
    /// </param>
    /// <returns>服务集合</returns>
    private static IServiceCollection AddFeishuRedis(this IServiceCollection services, bool registerHealthCheck = true)
    {
        services.AddSingleton<IValidateOptions<RedisOptions>, RedisOptionsValidator>();

        // T-M3-4：验证时机前移——net6+ 使用 ValidateOnStart，在宿主启动期即触发校验
        // netstandard2.0 不支持 ValidateOnStart，由 IValidateOptions 在首次解析时触发
#if NET6_0_OR_GREATER
        services.AddOptions<RedisOptions>().ValidateOnStart();
#endif

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<RedisOptions>>().Value;
            var logger = sp.GetService<ILogger<RedisOptions>>();

            // TMA2-19 / P2-10：连接串含口令时脱敏后再记录。
            logger?.LogInformation("Redis options loaded. Server: {ServerAddress}", SensitiveDataUtils.MaskSensitiveData(options.Connection.ServerAddress));
            return options;
        });

        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var options = sp.GetRequiredService<RedisOptions>();
            var logger = sp.GetService<ILogger<ConnectionMultiplexer>>();

            try
            {
                // TMA2-19 / P2-10：连接串含口令时脱敏后再记录。
                // C7/R3：优先嵌套 Connection/Advanced，旧扁平键经 Obsolete 垫片写入同一存储
                logger?.LogInformation("Initializing Redis connection to: {ConnectionString}", SensitiveDataUtils.MaskSensitiveData(options.Connection.ServerAddress));

                // ADR-7.1：使用 ConfigurationOptions.Parse 替代手工 EndPoints.Add，
                // 原生支持 redis://、rediss://、host:port,password=... 等形态。
                // R2-26：连接选项装配抽到 RedisConnectionFactory.Build（可单测），
                // 并显式推导 rediss:// → Ssl（实测 Parse 不会因 scheme 自动启用 TLS）。
                var config = RedisConnectionFactory.Build(options);

                var redis = ConnectionMultiplexer.Connect(config);

                redis.ConnectionFailed += (sender, args) =>
                {
                    logger?.LogWarning(args.Exception, "Redis connection failed: {FailureType}", args.FailureType);
                };

                redis.ConnectionRestored += (sender, args) =>
                {
                    logger?.LogInformation("Redis connection restored");
                };

                logger?.LogInformation("Redis connection initialized successfully");
                return redis;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to initialize Redis connection");
                throw new InvalidOperationException($"Failed to initialize Redis connection to {options.Connection.ServerAddress}", ex);
            }
        });

        // R2-12：健康检查注册可选——库内无条件 AddHealthChecks() 会向未使用健康检查的宿主
        // 隐式注册整套 HealthCheck 基础设施（HealthCheckService 等）。传 false 时仅注册类型，
        // 由宿主自行 AddCheck<RedisHealthCheck>。
        services.AddSingleton<RedisHealthCheck>();
        if (registerHealthCheck)
        {
            services.AddHealthChecks()
                .AddCheck<RedisHealthCheck>("feishu-redis", tags: ["redis", "feishu"]);
        }

        // WHF-10：启动期连接预热——解析 IConnectionMultiplexer（触发 Connect）并 PING，
        // 把首个 Webhook 请求承担的连接建立延迟移到宿主启动阶段。
        // AbortOnConnectFail 语义与 RedisOptions 对齐：true 时预热失败终止启动（fail-fast）
        services.AddHostedService(sp => new RedisConnectionWarmupService(
            sp.GetRequiredService<IConnectionMultiplexer>(),
            sp.GetService<ILogger<RedisConnectionWarmupService>>(),
            sp.GetRequiredService<RedisOptions>().Connection.AbortOnConnectFail));

        return services;
    }

    /// <summary>
    /// 注册 Redis 分布式事件去重服务
    /// </summary>
    private static IServiceCollection AddFeishuRedisEventDeduplicator(
        this IServiceCollection services)
    {
        services.AddSingleton<IFeishuEventDeduplicator>(sp =>
        {
            var redis = sp.GetRequiredService<IConnectionMultiplexer>();
            var redisOptions = sp.GetRequiredService<RedisOptions>();
            var logger = sp.GetService<ILogger<RedisFeishuEventDistributedDeduplicator>>();

            // 从 DI 解析 DeduplicationOptions（可通过 FeishuRedis:Deduplication 节点配置高级参数）
            // R5/X6: 文件级 #pragma warning disable CS0618 已覆盖 — 本类型为双读回落基座
            var dedupOptions = sp.GetService<IOptions<DeduplicationOptions>>()?.Value ?? DeduplicationOptions.Default;
            var unified = sp.GetService<IOptions<FeishuDeduplicationOptions>>()?.Value;
            var unifiedActive = unified is { IsConfiguredFromConfiguration: true };
            var profileBase = unified?.ResolveProfileDeduplicationOptions() ?? DeduplicationOptions.Default;

            // C1 双读：FeishuDeduplication 新节存在时字段级优先，否则保持 RedisOptions 覆盖 DeduplicationOptions
            var effectiveEventTtl = (unifiedActive && unified!.Event?.Ttl is { } uTtl && uTtl > TimeSpan.Zero)
                ? uTtl
                : redisOptions.EventCacheExpiration;
            var effectiveEventPrefix = (unifiedActive && !string.IsNullOrEmpty(unified!.Event?.KeyPrefix))
                ? unified.Event!.KeyPrefix!
                : redisOptions.EventKeyPrefix;
            var effectiveProcessing = (unifiedActive && unified!.Event?.ProcessingTimeout is { } uPt && uPt > TimeSpan.Zero)
                ? uPt
                : dedupOptions.ProcessingTimeout > TimeSpan.Zero
                    ? dedupOptions.ProcessingTimeout
                    : profileBase.ProcessingTimeout;
            var effectiveCleanup = (unifiedActive && unified!.Event?.CleanupInterval is { } uCl && uCl > TimeSpan.Zero)
                ? uCl
                : dedupOptions.CleanupInterval > TimeSpan.Zero
                    ? dedupOptions.CleanupInterval
                    : profileBase.CleanupInterval;
            var effectiveMaxCache = (unifiedActive && unified!.Event?.MaxCacheSize is { } uMs)
                ? uMs
                : dedupOptions.MaxCacheSize > 0 ? dedupOptions.MaxCacheSize : profileBase.MaxCacheSize;

            WarnIfDeduplicationKeysAreIneffective(logger, redisOptions, dedupOptions, unified);

            var effectiveOptions = new DeduplicationOptions
            {
                CacheExpiration = effectiveEventTtl,
                ProcessingTimeout = effectiveProcessing,
                CleanupInterval = effectiveCleanup,
                KeyPrefix = effectiveEventPrefix,
                MaxCacheSize = effectiveMaxCache
            };

            return new RedisFeishuEventDistributedDeduplicator(
                redis,
                effectiveOptions,
                logger);
        });

        return services;
    }

    /// <summary>
    /// 双读期「配了但无效」的告警（R5.2/X6 扩展为两个方向）。
    /// </summary>
    /// <param name="logger">日志；为 null 时不输出。</param>
    /// <param name="redisOptions">Redis 配置（旧键回落基座）。</param>
    /// <param name="dedupOptions">旧的高级去重配置（<c>FeishuRedis:Deduplication</c>）。</param>
    /// <param name="unified">统一节配置；<c>IsConfiguredFromConfiguration=true</c> 时视为已生效。</param>
    /// <remarks>
    /// <para><b>情形 A（统一节未生效）</b>：<see cref="DeduplicationOptions"/> 的 TTL/前缀会被
    /// <see cref="RedisOptions"/> 覆盖 → 告警并指向应改用的旧键。</para>
    /// <para><b>情形 B（统一节已生效，R5.2 新增）</b>：显式配置的旧键被 <c>FeishuDeduplication</c>
    /// **字段级覆盖**。此前这种情况完全静默——这是「配了但无效」最典型的形态，也正是 X6/X13 的核心风险。</para>
    /// <para>
    /// <b>为什么只告警而不给这些属性加 <c>[Obsolete]</c></b>：它们是双读期**合法的**回落基座
    /// （<c>FeishuDeduplication</c> 未配置时真正生效），给 SDK 自身必须读取的属性加 Obsolete 只会
    /// 产生大量噪音；而真正的误用入口是 <c>appsettings.json</c>（Obsolete 对它完全无效）。
    /// 运行时告警能精确指出「哪个键被哪个键覆盖」，比编译期警告更贴合该风险。
    /// </para>
    /// </remarks>
    internal static void WarnIfDeduplicationKeysAreIneffective(
        ILogger? logger,
        RedisOptions redisOptions,
        DeduplicationOptions dedupOptions,
        FeishuDeduplicationOptions? unified = null)
    {
        if (logger is null)
            return;

        var unifiedActive = unified is { IsConfiguredFromConfiguration: true };

        if (!unifiedActive)
        {
            // 情形 A：仅当 DeduplicationOptions 使用了非默认（与 RedisOptions 不一致）的值时告警，
            // 避免默认对齐场景产生噪音。
            if (dedupOptions.CacheExpiration != redisOptions.EventCacheExpiration)
            {
                logger.LogWarning(
                    "DeduplicationOptions.CacheExpiration({DedupTtl}) 在 Redis 路径不生效，请改用 FeishuRedis:EventCacheExpiration({RedisTtl})。",
                    dedupOptions.CacheExpiration,
                    redisOptions.EventCacheExpiration);
            }

            if (!string.Equals(dedupOptions.KeyPrefix, redisOptions.EventKeyPrefix, StringComparison.Ordinal))
            {
                logger.LogWarning(
                    "DeduplicationOptions.KeyPrefix('{DedupPrefix}') 在 Redis 路径不生效，请改用 FeishuRedis:EventKeyPrefix('{RedisPrefix}')。",
                    dedupOptions.KeyPrefix,
                    redisOptions.EventKeyPrefix);
            }

            return;
        }

        // 情形 B：统一节已生效。仅当「旧键被显式配置为非默认」且「统一节对应字段确实非空」时才告警——
        // 若统一节未提供该字段，旧键仍然是实际生效值（不算「无效」），此时告警会误导。
        var defaultEventTtl = TimeSpan.FromMilliseconds(Consts.DefaultCacheExpirationMs);

        if (unified!.Event?.Ttl is { } unifiedTtl && unifiedTtl > TimeSpan.Zero
            && redisOptions.EventCacheExpiration != defaultEventTtl)
        {
            logger.LogWarning(
                "FeishuRedis:EventCacheExpiration({LegacyTtl}) 已被 FeishuDeduplication:Event:Ttl({EffectiveTtl}) 覆盖，" +
                "该旧键不生效。请移除旧键并统一使用 FeishuDeduplication。",
                redisOptions.EventCacheExpiration,
                unifiedTtl);
        }

        if (!string.IsNullOrEmpty(unified.Event?.KeyPrefix)
            && !string.Equals(redisOptions.EventKeyPrefix, Consts.DefaultEventKeyPrefix, StringComparison.Ordinal))
        {
            logger.LogWarning(
                "FeishuRedis:EventKeyPrefix('{LegacyPrefix}') 已被 FeishuDeduplication:Event:KeyPrefix('{EffectivePrefix}') 覆盖，" +
                "该旧键不生效。请移除旧键并统一使用 FeishuDeduplication。",
                redisOptions.EventKeyPrefix,
                unified.Event!.KeyPrefix);
        }
    }

    /// <summary>
    /// 注册 Redis 分布式 Nonce 去重服务
    /// </summary>
    private static IServiceCollection AddFeishuRedisNonceDeduplicator(
        this IServiceCollection services)
    {
        services.AddSingleton<IFeishuNonceDistributedDeduplicator>(sp =>
        {
            var redis = sp.GetRequiredService<IConnectionMultiplexer>();
            var options = sp.GetRequiredService<RedisOptions>();
            var logger = sp.GetService<ILogger<RedisFeishuNonceDistributedDeduplicator>>();

            return new RedisFeishuNonceDistributedDeduplicator(
                redis,
                logger,
                ResolveUnifiedNonceTtl(sp, options),
                ResolveUnifiedNonceKeyPrefix(sp, options));
        });

        return services;
    }

    /// <summary>
    /// 注册 Redis 分布式 SeqID 去重服务
    /// </summary>
    private static IServiceCollection AddFeishuRedisSeqIDDeduplicator(
        this IServiceCollection services)
    {
        services.AddSingleton<IFeishuSeqIDDeduplicator>(sp =>
        {
            var redis = sp.GetRequiredService<IConnectionMultiplexer>();
            var options = sp.GetRequiredService<RedisOptions>();
            var logger = sp.GetService<ILogger<RedisFeishuSeqIDDeduplicator>>();

            // ADR-3（T-M2-4）：合成 scopeKey 以实现多实例/多应用隔离。
            // R5.3.1/X13（G-12）：scopeKey 的 AppKey 部分在**解析期**推断（见 ResolveUnifiedSeqIdScopeKey），
            // 不再固定取 RedisOptions.AppKey（默认 "default"）。
            // scopeKey 为空会在构造函数中抛 ArgumentException（fail-fast，防止退化为全局共享键）。
            var scopeKey = ResolveUnifiedSeqIdScopeKey(sp, options);

            return new RedisFeishuSeqIDDeduplicator(
                redis,
                logger,
                cacheExpiration: ResolveUnifiedSeqIdTtl(sp, options),
                keyPrefix: ResolveUnifiedSeqIdKeyPrefix(sp, options),
                scopeKey: scopeKey,
                // ADR-10（R2-01）：容量窗口来自配置（默认 100000），非正值已在 Validate 期拦截
                windowCapacity: options.SeqIdWindowCapacity);
        });

        return services;
    }

    /// <summary>
    /// 注册 Redis 分布式令牌存储服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="registerHealthCheck">
    /// 是否把 Redis 健康检查注册到宿主健康检查系统（R2-12）；仅在本次调用顺带初始化
    /// <see cref="IConnectionMultiplexer"/> 注册时生效（幂等：已注册则忽略）。
    /// </param>
    /// <remarks>
    /// ADR-14（R2-09）：<see cref="RedisTokenStore"/>/<see cref="RedisUserTokenStore"/> 具体类型仍注册，
    /// 但其键前缀已与 per-app 工厂对齐——<c>{TokenKeyPrefix}:{默认应用AppKey}:token</c>，
    /// 避免"按类型解析"写入一套令牌管理器永远读不到的键空间。
    /// <para>
    /// 该兼容面**不参与**令牌管理器读取路径（管理器只经 <see cref="IFeishuTokenStoreFactory.Create"/>），
    /// 也**不参与**加密装饰器（装饰器只包裹 <see cref="IFeishuTokenStoreFactory"/>）。
    /// </para>
    /// </remarks>
    public static IServiceCollection AddFeishuRedisTokenStore(
        this IServiceCollection services,
        bool registerHealthCheck = true)
    {
        // TMA2-17 / P2-7：检测 AddFeishuApp 是否已调用（与 AddFeishuRedisDeduplicators 一致的抛异常语义）。
        // 若已注册 IFeishuAppManager，则 AddFeishuAppBaseServices 已注册默认 Memory TokenStoreFactory，
        // 此时 Redis 的 TryAddSingleton<IFeishuTokenStoreFactory> 会因已存在而跳过，
        // 导致 Redis 实现永不生效（静默退化为内存存储）。
        EnsureFeishuAppNotRegistered(services, nameof(AddFeishuRedisTokenStore));

        // T-M3-2：确保 AddFeishuRedis() 已调用（幂等化——重复调用不会重复注册）
        if (!services.Any(s => s.ServiceType == typeof(IConnectionMultiplexer)))
        {
            services.AddFeishuRedis(registerHealthCheck);
        }

        // ADR-5（T-M2-5）：删除 ITokenStore/IUserTokenStore 的 DI 单例注册。
        // 原注册使用 feishu:token:* 键空间，与工厂路径 feishu:{appKey}:token:* 不一致（R-09）。
        // 仓库内零消费方，用户应改用 IFeishuTokenStoreFactory.Create(appKey)。
        // ADR-14（R2-09）：具体类型改为「默认应用 + TokenKeyPrefix」前缀，与工厂同键空间。
        // 注意：**不得**经 IFeishuTokenStoreFactory 解析——AddFeishuAppBaseServices 末尾会对工厂叠加
        // EncryptedFeishuTokenStoreFactory 装饰器，工厂返回的可能是 EncryptedTokenStore，
        // 强制转换回 RedisTokenStore 会抛 InvalidCastException。
        services.AddSingleton<RedisTokenStore>(sp =>
        {
            var redis = sp.GetRequiredService<IConnectionMultiplexer>();
            var options = sp.GetRequiredService<RedisOptions>();
            // R-25：logger 兜底 NullLogger，与 PerAppRedisTokenStoreFactory 一致
            var logger = sp.GetService<ILogger<RedisTokenStore>>() ?? NullLogger<RedisTokenStore>.Instance;
            var keyPrefix = TokenKeyBuilder.BuildKeyPrefix(
                RedisDefaultAppKeyResolver.Resolve(sp, options),
                options.TokenKeyPrefix);
            return new RedisTokenStore(redis, logger, keyPrefix);
        });

        services.AddSingleton<RedisUserTokenStore>(sp =>
        {
            var innerStore = sp.GetRequiredService<RedisTokenStore>();
            var redis = sp.GetRequiredService<IConnectionMultiplexer>();
            var options = sp.GetRequiredService<RedisOptions>();
            var keyPrefix = TokenKeyBuilder.BuildKeyPrefix(
                RedisDefaultAppKeyResolver.Resolve(sp, options),
                options.TokenKeyPrefix);
            return new RedisUserTokenStore(innerStore, redis, keyPrefix);
        });

        // TOK-1 修复：注册 PerAppRedisTokenStoreFactory，使每个应用拥有独立的 Redis 键空间
        // （feishu:{appKey}:token），与 Memory 路径 FeishuTokenStore / FeishuUserTokenStore 的键布局一致。
        // 原 SingletonFeishuTokenStoreFactory 忽略 appKey 并返回共享单例，多应用下令牌会互相覆盖。
        // 所有 per-app 实例共享同一 IConnectionMultiplexer，不会造成连接池膨胀。
        // 必须在 AddFeishuApp 之前调用，由 TryAdd 语义保证覆盖默认的 PerAppFeishuTokenStoreFactory。
        // R2-04：携带 TokenKeyPrefix 环境段，使多环境共用 Redis 时令牌键空间隔离。
        services.TryAddSingleton<IFeishuTokenStoreFactory>(sp => new PerAppRedisTokenStoreFactory(
            sp.GetRequiredService<IConnectionMultiplexer>(),
            sp.GetService<ILoggerFactory>(),
            sp.GetRequiredService<RedisOptions>().TokenKeyPrefix));

        // E-02（R2-22）：运维诊断门面（聚合三类键空间计数/最大 SeqID/服务端时间）
        services.TryAddSingleton<IRedisDeduplicationDiagnostics, RedisDeduplicationDiagnostics>();

        return services;
    }

    /// <summary>
    /// 注册所有 Redis 分布式去重服务（事件去重、Nonce 去重、SeqID 去重）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <param name="sectionName">配置节名称</param>
    /// <param name="registerHealthCheck">
    /// 是否把 Redis 健康检查注册到宿主健康检查系统（R2-12，默认 <c>true</c>）。
    /// 传 <c>false</c> 时不调用 <c>AddHealthChecks()</c>（避免对未使用健康检查的宿主产生隐式注册），
    /// <see cref="RedisHealthCheck"/> 类型仍注册，可由宿主自行 <c>AddCheck</c>。
    /// </param>
    /// <returns>服务集合</returns>
    /// <exception cref="InvalidOperationException">
    /// 当 <see cref="IFeishuAppManager"/> 已注册时抛出。<br/>
    /// 调用顺序约束：必须先 <c>AddFeishuRedisDeduplicators</c>，再 <c>AddFeishuApp</c>。
    /// 颠倒顺序会导致 Redis TokenStore 因 TryAddSingleton 语义而无法覆盖默认 Memory 实现，
    /// 且不会有任何错误抛出（静默失败）。
    /// </exception>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("反射式配置绑定（Configure<TOptions>）在裁剪下无法静态分析配置类型成员")]
#endif
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("反射式配置绑定（Configure<TOptions>）在 AOT/动态代码生成环境下不可用")]
#endif
    public static IServiceCollection AddFeishuRedisDeduplicators(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = "FeishuRedis",
        bool registerHealthCheck = true)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        // TMA2-17：复用统一守卫。
        EnsureFeishuAppNotRegistered(services, nameof(AddFeishuRedisDeduplicators));

        var section = sectionName ?? "FeishuRedis";
        services.Configure<RedisOptions>(options =>
        {
            configuration.GetSection(section).Bind(options);
            // R4：公共扁平连接属性已删除；配置 JSON 旧键回填嵌套 Connection/Advanced
            options.ApplyLegacyFlatConnectionKeys(configuration.GetSection(section));
        });

        // 绑定 DeduplicationOptions（高级参数：ProcessingTimeout 等）
        services.Configure<DeduplicationOptions>(options =>
        {
            configuration.GetSection($"{section}:Deduplication").Bind(options);
        });

        // C1：统一去重节（存在时对旧键字段级优先）
        services.AddFeishuDeduplicationOptions(configuration);

        return services
            .AddFeishuRedis(registerHealthCheck)
            .AddFeishuRedisEventDeduplicator()
            .AddFeishuRedisNonceDeduplicator()
            .AddFeishuRedisSeqIDDeduplicator()
            .AddFeishuRedisTokenStore(registerHealthCheck);
    }

    private static FeishuDeduplicationOptions? GetUnifiedDeduplication(IServiceProvider sp) =>
        sp.GetService<IOptions<FeishuDeduplicationOptions>>()?.Value is { IsConfiguredFromConfiguration: true } u ? u : null;

    private static TimeSpan ResolveUnifiedNonceTtl(IServiceProvider sp, RedisOptions options)
    {
        var unified = GetUnifiedDeduplication(sp);
        return unified?.Nonce?.Ttl is { } ttl && ttl > TimeSpan.Zero ? ttl : options.NonceTtl;
    }

    private static string ResolveUnifiedNonceKeyPrefix(IServiceProvider sp, RedisOptions options)
    {
        var unified = GetUnifiedDeduplication(sp);
        return !string.IsNullOrEmpty(unified?.Nonce?.KeyPrefix) ? unified!.Nonce!.KeyPrefix! : options.NonceKeyPrefix;
    }

    private static TimeSpan ResolveUnifiedSeqIdTtl(IServiceProvider sp, RedisOptions options)
    {
        var unified = GetUnifiedDeduplication(sp);
        return unified?.SeqId?.Ttl is { } ttl && ttl > TimeSpan.Zero ? ttl : options.SeqIdCacheExpiration;
    }

    private static string ResolveUnifiedSeqIdKeyPrefix(IServiceProvider sp, RedisOptions options)
    {
        var unified = GetUnifiedDeduplication(sp);
        return !string.IsNullOrEmpty(unified?.SeqId?.KeyPrefix) ? unified!.SeqId!.KeyPrefix! : options.SeqIdKeyPrefix;
    }

    private static string ResolveUnifiedSeqIdScopeKey(IServiceProvider sp, RedisOptions options)
    {
        var unified = GetUnifiedDeduplication(sp);
        if (!string.IsNullOrWhiteSpace(unified?.SeqId?.ScopeKey))
            return unified!.SeqId!.ScopeKey!;

        if (!string.IsNullOrWhiteSpace(options.SeqIdScopeKey))
            return options.SeqIdScopeKey!;

        // R5.3.1/X13（G-12）+ R2-04/R2-09：AppKey 解析收敛到 RedisDefaultAppKeyResolver
        // （与令牌具体类型注册共用同一解析链）：FeishuApps 默认应用 > 首个应用 > RedisOptions.AppKey。
        // 显式配置的 FeishuDeduplication:SeqId:ScopeKey / RedisOptions.SeqIdScopeKey 恒优先于推断。
        return $"{RedisDefaultAppKeyResolver.Resolve(sp, options)}|{Environment.MachineName}";
    }

    /// <summary>
    /// 注册所有 Redis 分布式去重服务（使用预配置的选项）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configureOptions">配置选项的回调</param>
    /// <param name="registerHealthCheck">
    /// 是否把 Redis 健康检查注册到宿主健康检查系统（R2-12，默认 <c>true</c>）；语义同
    /// <see cref="AddFeishuRedisDeduplicators(IServiceCollection, IConfiguration, string, bool)"/>。
    /// </param>
    /// <returns>服务集合</returns>
    /// <exception cref="InvalidOperationException">
    /// 当 <see cref="IFeishuAppManager"/> 已注册时抛出。详见 <see cref="AddFeishuRedisDeduplicators(IServiceCollection, IConfiguration, string, bool)"/>。
    /// </exception>
    public static IServiceCollection AddFeishuRedisDeduplicators(
        this IServiceCollection services,
        Action<RedisOptions> configureOptions,
        bool registerHealthCheck = true)
    {
        if (configureOptions == null)
            throw new ArgumentNullException(nameof(configureOptions));

        // TMA2-17：复用统一守卫（同 IConfiguration 重载）。
        EnsureFeishuAppNotRegistered(services, nameof(AddFeishuRedisDeduplicators));

        services.Configure(configureOptions);

        // 代码路径也注册统一节 Options/Validator（无 IConfiguration 时不绑定）
        services.AddFeishuDeduplicationOptions();

        return services
            .AddFeishuRedis(registerHealthCheck)
            .AddFeishuRedisEventDeduplicator()
            .AddFeishuRedisNonceDeduplicator()
            .AddFeishuRedisSeqIDDeduplicator()
            .AddFeishuRedisTokenStore(registerHealthCheck);
    }

    /// <summary>
    /// TMA2-17 / P2-7：检测 AddFeishuApp 是否已调用。
    /// 若已注册 IFeishuAppManager，则 AddFeishuAppBaseServices 已注册默认 Memory TokenStoreFactory，
    /// 此时 Redis 的 TryAddSingleton 会因已存在而跳过，导致 Redis 实现永不生效（静默失败）。
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="callerName">调用方方法名（用于错误消息）</param>
    /// <exception cref="InvalidOperationException">当 IFeishuAppManager 已注册时抛出</exception>
    private static void EnsureFeishuAppNotRegistered(IServiceCollection services, string callerName)
    {
        if (services.Any(s => s.ServiceType == typeof(IFeishuAppManager)))
        {
            throw new InvalidOperationException(
                $"{callerName} 必须在 AddFeishuApp 之前调用。" +
                "当前检测到 AddFeishuApp 已被调用，Memory TokenStoreFactory 已注册为 IFeishuTokenStoreFactory，" +
                "Redis TokenStore 将因 TryAddSingleton 语义（已存在则跳过）而无法覆盖，导致 Redis 实现永不生效。" +
                "请调整调用顺序：services.AddFeishuRedisTokenStore(); services.AddFeishuApp(...);");
        }
    }
}
