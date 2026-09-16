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

            logger?.LogInformation("Redis options loaded. Server: {ServerAddress}", options.ServerAddress);
            return options;
        });

        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var options = sp.GetRequiredService<RedisOptions>();
            var logger = sp.GetService<ILogger<ConnectionMultiplexer>>();

            try
            {
                logger?.LogInformation("Initializing Redis connection to: {ConnectionString}", options.ServerAddress);

                // ADR-7.1：使用 ConfigurationOptions.Parse 替代手工 EndPoints.Add，
                // 原生支持 redis://、rediss://（自动 Ssl）、host:port,password=... 等形态。
                var config = ConfigurationOptions.Parse(options.ServerAddress);
                config.ConnectTimeout = options.ConnectTimeout;
                config.SyncTimeout = options.SyncTimeout;
                config.Ssl = config.Ssl || options.Ssl;                // rediss:// 已置 Ssl，取或
                config.Password = string.IsNullOrEmpty(options.Password) ? config.Password : options.Password;
                config.AllowAdmin = options.AllowAdmin;
                config.AbortOnConnectFail = options.AbortOnConnectFail;
                config.ConnectRetry = options.ConnectRetry;
                config.DefaultDatabase = options.DefaultDatabase;
                config.ClientName = options.ClientName ?? $"Feishu-Deduplicator-{Environment.MachineName}";

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
            var dedupOptions = sp.GetService<IOptions<DeduplicationOptions>>()?.Value ?? DeduplicationOptions.Default;

            // RedisOptions 中的 EventCacheExpiration / EventKeyPrefix 优先（与文档承诺一致）
            // 注意：DeduplicationOptions 的 AllowProcessingOnFallback/MaxRetryCount/InitialRetryDelay/MaxRetryDelay
            // 在 Redis 路径不消费（无降级能力），已从 effectiveOptions 中删除以避免"配置看起来生效"。
            var effectiveOptions = new DeduplicationOptions
            {
                CacheExpiration = redisOptions.EventCacheExpiration,
                ProcessingTimeout = dedupOptions.ProcessingTimeout,
                CleanupInterval = dedupOptions.CleanupInterval,
                KeyPrefix = redisOptions.EventKeyPrefix,
                MaxCacheSize = dedupOptions.MaxCacheSize,
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
                options.NonceTtl,
                options.NonceKeyPrefix);
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
            var scopeKey = !string.IsNullOrWhiteSpace(options.SeqIdScopeKey)
                ? options.SeqIdScopeKey
                : $"{options.AppKey}|{Environment.MachineName}";

            return new RedisFeishuSeqIDDeduplicator(
                redis,
                logger,
                cacheExpiration: options.SeqIdCacheExpiration,
                keyPrefix: options.SeqIdKeyPrefix,
                scopeKey: scopeKey);
        });

        return services;
    }

    /// <summary>
    /// 注册 Redis 分布式令牌存储服务
    /// </summary>
    public static IServiceCollection AddFeishuRedisTokenStore(
        this IServiceCollection services)
    {
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

        // 检测 AddFeishuApp 是否已调用：若已注册 IFeishuAppManager，则 AddFeishuAppBaseServices 已注册默认 FeishuTokenStore，
        // 此时 Redis 实现的 TryAddSingleton<ITokenStore> 会因已存在而跳过，导致 Redis TokenStore 无法生效（静默失败）。
        if (services.Any(s => s.ServiceType == typeof(IFeishuAppManager)))
        {
            throw new InvalidOperationException(
                "AddFeishuRedisDeduplicators 必须在 AddFeishuApp 之前调用。" +
                "当前检测到 AddFeishuApp 已被调用，FeishuTokenStore（Memory 实现）已注册为 ITokenStore，" +
                "Redis TokenStore 将因 TryAddSingleton 语义（已存在则跳过）而无法覆盖，导致 Redis 实现永不生效。" +
                "请调整调用顺序：services.AddFeishuRedisDeduplicators(...); services.AddFeishuApp(...);");
        }

        var section = sectionName ?? "FeishuRedis";
        services.Configure<RedisOptions>(options =>
        {
            configuration.GetSection(section).Bind(options);
        });

        // 绑定 DeduplicationOptions（高级参数：ProcessingTimeout、MaxRetryCount、AllowProcessingOnFallback 等）
        // 注意：CacheExpiration 和 KeyPrefix 由 RedisOptions 中的 EventCacheExpiration / EventKeyPrefix 优先覆盖
        services.Configure<DeduplicationOptions>(options =>
        {
            configuration.GetSection($"{section}:Deduplication").Bind(options);
        });

        return services
            .AddFeishuRedis()
            .AddFeishuRedisEventDeduplicator()
            .AddFeishuRedisNonceDeduplicator()
            .AddFeishuRedisSeqIDDeduplicator()
            .AddFeishuRedisTokenStore();
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

        // 检测 AddFeishuApp 是否已调用（同 IConfiguration 重载）
        if (services.Any(s => s.ServiceType == typeof(IFeishuAppManager)))
        {
            throw new InvalidOperationException(
                "AddFeishuRedisDeduplicators 必须在 AddFeishuApp 之前调用。" +
                "当前检测到 AddFeishuApp 已被调用，FeishuTokenStore（Memory 实现）已注册为 ITokenStore，" +
                "Redis TokenStore 将因 TryAddSingleton 语义（已存在则跳过）而无法覆盖，导致 Redis 实现永不生效。" +
                "请调整调用顺序：services.AddFeishuRedisDeduplicators(...); services.AddFeishuApp(...);");
        }

        services.Configure(configureOptions);

        return services
            .AddFeishuRedis()
            .AddFeishuRedisEventDeduplicator()
            .AddFeishuRedisNonceDeduplicator()
            .AddFeishuRedisSeqIDDeduplicator()
            .AddFeishuRedisTokenStore();
    }
}
