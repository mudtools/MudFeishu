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
using Mud.Feishu.Redis.HealthChecks;
using Mud.Feishu.Redis.Services;
using StackExchange.Redis;
using System.Diagnostics.CodeAnalysis;

namespace Mud.Feishu.Redis.Extensions;

/// <summary>
/// 飞书 Redis 分布式去重服务扩展
/// </summary>
public static class RedisFeishuServiceBuilderExtensions
{
    /// <summary>
    /// 注册 Redis 连接服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    private static IServiceCollection AddFeishuRedis(this IServiceCollection services)
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
            logger?.LogInformation("Redis options loaded. Server: {ServerAddress}", SensitiveDataUtils.MaskSensitiveData(options.ServerAddress));
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
                // 原生支持 redis://、rediss://（自动 Ssl）、host:port,password=... 等形态。
                var config = ConfigurationOptions.Parse(options.Connection.ServerAddress);
                config.ConnectTimeout = options.Connection.ConnectTimeout;
                config.SyncTimeout = options.Connection.SyncTimeout;
                config.Ssl = config.Ssl || options.Connection.Ssl;                // rediss:// 已置 Ssl，取或
                config.Password = string.IsNullOrEmpty(options.Connection.Password) ? config.Password : options.Connection.Password;
                config.AllowAdmin = options.Advanced.AllowAdmin;
                config.AbortOnConnectFail = options.Connection.AbortOnConnectFail;
                config.ConnectRetry = options.Connection.ConnectRetry;
                config.DefaultDatabase = options.Connection.DefaultDatabase;
                config.ClientName = options.Advanced.ClientName ?? $"Feishu-Deduplicator-{Environment.MachineName}";

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
                throw new InvalidOperationException($"Failed to initialize Redis connection to {options.ServerAddress}", ex);
            }
        });

        services.AddSingleton<RedisHealthCheck>();
        services.AddHealthChecks()
            .AddCheck<RedisHealthCheck>("feishu-redis", tags: ["redis", "feishu"]);

        // WHF-10：启动期连接预热——解析 IConnectionMultiplexer（触发 Connect）并 PING，
        // 把首个 Webhook 请求承担的连接建立延迟移到宿主启动阶段。
        // AbortOnConnectFail 语义与 RedisOptions 对齐：true 时预热失败终止启动（fail-fast）
        services.AddHostedService(sp => new RedisConnectionWarmupService(
            sp.GetRequiredService<IConnectionMultiplexer>(),
            sp.GetService<ILogger<RedisConnectionWarmupService>>(),
            sp.GetRequiredService<RedisOptions>().AbortOnConnectFail));

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
#pragma warning disable CS0618 // 仅读取仍消费的字段；失效字段见 Warn
            var dedupOptions = sp.GetService<IOptions<DeduplicationOptions>>()?.Value ?? DeduplicationOptions.Default;
#pragma warning restore CS0618
            var unified = sp.GetService<IOptions<FeishuDeduplicationOptions>>()?.Value;
            var unifiedActive = unified is { IsConfiguredFromConfiguration: true };
            var profileBase = unified?.ResolveProfileDeduplicationOptions() ?? DeduplicationOptions.Default;

            // C1 双读：FeishuDeduplication 新节存在时字段级优先，否则保持 RedisOptions 覆盖 DeduplicationOptions
            var effectiveEventTtl = (unifiedActive && unified!.Event?.Ttl is { } uTtl && uTtl > TimeSpan.Zero)
                ? uTtl
                : redisOptions.EventCacheExpiration;
            var effectiveEventPrefix = (unifiedActive && !string.IsNullOrEmpty(unified.Event?.KeyPrefix))
                ? unified!.Event!.KeyPrefix!
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

            WarnIfDeduplicationKeysAreIneffective(logger, redisOptions, dedupOptions);

            var effectiveOptions = new DeduplicationOptions
            {
                CacheExpiration = effectiveEventTtl,
                ProcessingTimeout = effectiveProcessing,
                CleanupInterval = effectiveCleanup,
                KeyPrefix = effectiveEventPrefix,
                MaxCacheSize = effectiveMaxCache,
                EnableVerboseLogging = dedupOptions.EnableVerboseLogging
            };

            return new RedisFeishuEventDistributedDeduplicator(
                redis,
                effectiveOptions,
                logger);
        });

        return services;
    }

    /// <summary>
    /// Redis 路径下 DeduplicationOptions 的 CacheExpiration/KeyPrefix 会被 RedisOptions 覆盖。
    /// 两侧值不一致时输出 Warn，避免「文档写了可配、运行时无效」。
    /// </summary>
    internal static void WarnIfDeduplicationKeysAreIneffective(
        ILogger? logger,
        RedisOptions redisOptions,
        DeduplicationOptions dedupOptions)
    {
        if (logger is null)
            return;

        // 仅当 DeduplicationOptions 使用了非默认（与 RedisOptions 不一致）的值时告警，
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

        // B3：分布式降级/重试字段在 Redis 主路径不消费——用户显式改过非默认值时提示
#pragma warning disable CS0618
        if (!dedupOptions.AllowProcessingOnFallback
            || dedupOptions.MaxRetryCount != Consts.DefaultDeduplicationRetryCount)
#pragma warning restore CS0618
        {
            logger.LogWarning(
                "DeduplicationOptions.AllowProcessingOnFallback/MaxRetryCount 等字段在当前 Redis 主路径不消费（已从 effectiveOptions 剔除）。" +
                "请勿依赖这些键改变分布式失败语义；详见 documents/Configuration/DeduplicationTruthSource.md。");
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
            // 默认策略：AppKey + MachineName（可配置 RedisOptions.SeqIdScopeKey 覆盖）。
            // scopeKey 为空会在构造函数中抛 ArgumentException（fail-fast，防止退化为全局共享键）。
            var scopeKey = ResolveUnifiedSeqIdScopeKey(sp, options);

            return new RedisFeishuSeqIDDeduplicator(
                redis,
                logger,
                cacheExpiration: ResolveUnifiedSeqIdTtl(sp, options),
                keyPrefix: ResolveUnifiedSeqIdKeyPrefix(sp, options),
                scopeKey: ResolveUnifiedSeqIdScopeKey(sp, options));
        });

        return services;
    }

    /// <summary>
    /// 注册 Redis 分布式令牌存储服务
    /// </summary>
    public static IServiceCollection AddFeishuRedisTokenStore(
        this IServiceCollection services)
    {
        // TMA2-17 / P2-7：检测 AddFeishuApp 是否已调用（与 AddFeishuRedisDeduplicators 一致的抛异常语义）。
        // 若已注册 IFeishuAppManager，则 AddFeishuAppBaseServices 已注册默认 Memory TokenStoreFactory，
        // 此时 Redis 的 TryAddSingleton<IFeishuTokenStoreFactory> 会因已存在而跳过，
        // 导致 Redis 实现永不生效（静默退化为内存存储）。
        EnsureFeishuAppNotRegistered(services, nameof(AddFeishuRedisTokenStore));

        // T-M3-2：确保 AddFeishuRedis() 已调用（幂等化——重复调用不会重复注册）
        if (!services.Any(s => s.ServiceType == typeof(IConnectionMultiplexer)))
        {
            services.AddFeishuRedis();
        }

        // ADR-5（T-M2-5）：删除 ITokenStore/IUserTokenStore 的 DI 单例注册。
        // 原注册使用 feishu:token:* 键空间，与工厂路径 feishu:{appKey}:token:* 不一致（R-09）。
        // 仓库内零消费方，用户应改用 IFeishuTokenStoreFactory.Create(appKey)。
        // RedisTokenStore/RedisUserTokenStore 具体类型仍注册，供按类型解析。
        services.AddSingleton<RedisTokenStore>(sp =>
        {
            var redis = sp.GetRequiredService<IConnectionMultiplexer>();
            // R-25：logger 兜底 NullLogger，与 PerAppRedisTokenStoreFactory 一致
            var logger = sp.GetService<ILogger<RedisTokenStore>>() ?? NullLogger<RedisTokenStore>.Instance;
            return new RedisTokenStore(redis, logger);
        });

        services.AddSingleton<RedisUserTokenStore>(sp =>
        {
            var innerStore = sp.GetRequiredService<RedisTokenStore>();
            var redis = sp.GetRequiredService<IConnectionMultiplexer>();
            return new RedisUserTokenStore(innerStore, redis);
        });

        // TOK-1 修复：注册 PerAppRedisTokenStoreFactory，使每个应用拥有独立的 Redis 键空间
        // （feishu:{appKey}:token），与 Memory 路径 FeishuTokenStore / FeishuUserTokenStore 的键布局一致。
        // 原 SingletonFeishuTokenStoreFactory 忽略 appKey 并返回共享单例，多应用下令牌会互相覆盖。
        // 所有 per-app 实例共享同一 IConnectionMultiplexer，不会造成连接池膨胀。
        // 必须在 AddFeishuApp 之前调用，由 TryAdd 语义保证覆盖默认的 PerAppFeishuTokenStoreFactory。
        services.TryAddSingleton<IFeishuTokenStoreFactory>(sp => new PerAppRedisTokenStoreFactory(
            sp.GetRequiredService<IConnectionMultiplexer>(),
            sp.GetService<ILoggerFactory>()));

        return services;
    }

    /// <summary>
    /// 注册所有 Redis 分布式去重服务（事件去重、Nonce 去重、SeqID 去重）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <param name="sectionName">配置节名称</param>
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
        string sectionName = "FeishuRedis")
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        // TMA2-17：复用统一守卫。
        EnsureFeishuAppNotRegistered(services, nameof(AddFeishuRedisDeduplicators));

        var section = sectionName ?? "FeishuRedis";
        services.Configure<RedisOptions>(options =>
        {
            configuration.GetSection(section).Bind(options);
        });

        // 绑定 DeduplicationOptions（高级参数：ProcessingTimeout 等）
        services.Configure<DeduplicationOptions>(options =>
        {
            configuration.GetSection($"{section}:Deduplication").Bind(options);
        });

        // C1：统一去重节（存在时对旧键字段级优先）
        services.AddFeishuDeduplicationOptions(configuration);

        return services
            .AddFeishuRedis()
            .AddFeishuRedisEventDeduplicator()
            .AddFeishuRedisNonceDeduplicator()
            .AddFeishuRedisSeqIDDeduplicator()
            .AddFeishuRedisTokenStore();
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
        return !string.IsNullOrWhiteSpace(options.SeqIdScopeKey)
            ? options.SeqIdScopeKey
            : $"{options.AppKey}|{Environment.MachineName}";
    }

    /// <summary>
    /// 注册所有 Redis 分布式去重服务（使用预配置的选项）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configureOptions">配置选项的回调</param>
    /// <returns>服务集合</returns>
    /// <exception cref="InvalidOperationException">
    /// 当 <see cref="IFeishuAppManager"/> 已注册时抛出。详见 <see cref="AddFeishuRedisDeduplicators(IServiceCollection, IConfiguration, string)"/>。
    /// </exception>
    public static IServiceCollection AddFeishuRedisDeduplicators(
        this IServiceCollection services,
        Action<RedisOptions> configureOptions)
    {
        if (configureOptions == null)
            throw new ArgumentNullException(nameof(configureOptions));

        // TMA2-17：复用统一守卫（同 IConfiguration 重载）。
        EnsureFeishuAppNotRegistered(services, nameof(AddFeishuRedisDeduplicators));

        services.Configure(configureOptions);

        // 代码路径也注册统一节 Options/Validator（无 IConfiguration 时不绑定）
        services.AddFeishuDeduplicationOptions();

        return services
            .AddFeishuRedis()
            .AddFeishuRedisEventDeduplicator()
            .AddFeishuRedisNonceDeduplicator()
            .AddFeishuRedisSeqIDDeduplicator()
            .AddFeishuRedisTokenStore();
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
