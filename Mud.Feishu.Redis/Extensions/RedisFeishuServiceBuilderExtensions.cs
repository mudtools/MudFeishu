// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mud.Feishu.Redis.Configuration;
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
    public static IServiceCollection AddFeishuRedis(this IServiceCollection services)
    {
        // 注册 RedisOptions 配置
        services.AddSingleton(sp =>
        {
            var configuration = sp.GetService<IConfiguration>();
            var options = new RedisOptions();

            if (configuration != null)
            {
                configuration.GetSection("Feishu:Redis").Bind(options);
            }

            return options;
        });

        // 注册 IConnectionMultiplexer 单例
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var options = sp.GetRequiredService<RedisOptions>();
            var logger = sp.GetService<ILogger<ConnectionMultiplexer>>();

            logger?.LogInformation("正在初始化 Redis 连接，地址: {ConnectionString}", options.ConnectionString);

            var redis = ConnectionMultiplexer.Connect(new ConfigurationOptions
            {
                EndPoints = { options.ConnectionString },
                ConnectTimeout = options.ConnectTimeout,
                SyncTimeout = options.SyncTimeout,
                Ssl = options.Ssl,
                AllowAdmin = options.AllowAdmin,
                AbortOnConnectFail = true,
                ConnectRetry = 3
            });

            logger?.LogInformation("Redis 连接初始化完成");
            return redis;
        });

        return services;
    }

    /// <summary>
    /// 注册 Redis 分布式事件去重服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddFeishuRedisEventDeduplicator(
        this IServiceCollection services)
    {
        services.AddSingleton<IFeishuEventDistributedDeduplicator>(sp =>
        {
            var redis = sp.GetRequiredService<IConnectionMultiplexer>();
            var options = sp.GetRequiredService<RedisOptions>();
            var logger = sp.GetService<ILogger<RedisFeishuEventDistributedDeduplicator>>();

            return new RedisFeishuEventDistributedDeduplicator(
                redis,
                logger,
                options.EventCacheExpiration,
                options.EventKeyPrefix);
        });

        return services;
    }

    /// <summary>
    /// 注册 Redis 分布式 Nonce 去重服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddFeishuRedisNonceDeduplicator(
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
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddFeishuRedisSeqIDDeduplicator(
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
    /// 注册所有 Redis 分布式去重服务（事件去重、Nonce 去重、SeqID 去重）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddFeishuRedisDeduplicators(
        this IServiceCollection services)
    {
        services.AddFeishuRedisEventDeduplicator();
        services.AddFeishuRedisNonceDeduplicator();
        services.AddFeishuRedisSeqIDDeduplicator();

        return services;
    }
}
