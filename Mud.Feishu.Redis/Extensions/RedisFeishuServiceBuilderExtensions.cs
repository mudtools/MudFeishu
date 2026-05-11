// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mud.Feishu.Redis.Configuration;
using Mud.Feishu.Redis.HealthChecks;
using Mud.Feishu.Redis.Services;
using StackExchange.Redis;

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

                var config = new ConfigurationOptions
                {
                    EndPoints = { options.ServerAddress },
                    ConnectTimeout = options.ConnectTimeout,
                    SyncTimeout = options.SyncTimeout,
                    Ssl = options.Ssl,
                    Password = options.Password,
                    AllowAdmin = options.AllowAdmin,
                    AbortOnConnectFail = options.AbortOnConnectFail,
                    ConnectRetry = options.ConnectRetry,
                    DefaultDatabase = options.DefaultDatabase,
                    ClientName = options.ClientName ?? $"Feishu-Deduplicator-{Environment.MachineName}"
                };

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
            var options = sp.GetRequiredService<RedisOptions>();
            var logger = sp.GetService<ILogger<RedisFeishuEventDistributedDeduplicator>>();

            return new RedisFeishuEventDistributedDeduplicator(
                redis,
                logger,
                options.EventCacheExpiration,
                processingTimeout: null,
                keyPrefix: options.EventKeyPrefix);
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

            return new RedisFeishuSeqIDDeduplicator(
                redis,
                logger,
                options.SeqIdCacheExpiration,
                options.SeqIdKeyPrefix);
        });

        return services;
    }

    /// <summary>
    /// 注册 Redis 分布式令牌存储服务
    /// </summary>
    public static IServiceCollection AddFeishuRedisTokenStore(
        this IServiceCollection services)
    {
        services.AddSingleton<RedisTokenStore>(sp =>
        {
            var redis = sp.GetRequiredService<IConnectionMultiplexer>();
            var logger = sp.GetService<ILogger<RedisTokenStore>>();
            return new RedisTokenStore(redis, logger!);
        });

        if (!services.Any(s => s.ServiceType == typeof(ITokenStore)))
        {
            services.AddSingleton<ITokenStore>(sp => sp.GetRequiredService<RedisTokenStore>());
        }

        if (!services.Any(s => s.ServiceType == typeof(IUserTokenStore)))
        {
            services.AddSingleton<IUserTokenStore>(sp =>
            {
                var innerStore = sp.GetRequiredService<RedisTokenStore>();
                var redis = sp.GetRequiredService<IConnectionMultiplexer>();
                var logger = sp.GetService<ILogger<RedisUserTokenStore>>();
                return new RedisUserTokenStore(innerStore, redis, logger!);
            });
        }

        return services;
    }

    /// <summary>
    /// 注册所有 Redis 分布式去重服务（事件去重、Nonce 去重、SeqID 去重）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <param name="sectionName">配置节名称</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddFeishuRedisDeduplicators(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = "FeishuRedis")
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        var section = sectionName ?? "FeishuRedis";
        services.Configure<RedisOptions>(options =>
        {
            configuration.GetSection(section).Bind(options);
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
    public static IServiceCollection AddFeishuRedisDeduplicators(
        this IServiceCollection services,
        Action<RedisOptions> configureOptions)
    {
        if (configureOptions == null)
            throw new ArgumentNullException(nameof(configureOptions));

        services.Configure(configureOptions);

        return services
            .AddFeishuRedis()
            .AddFeishuRedisEventDeduplicator()
            .AddFeishuRedisNonceDeduplicator()
            .AddFeishuRedisSeqIDDeduplicator()
            .AddFeishuRedisTokenStore();
    }
}
