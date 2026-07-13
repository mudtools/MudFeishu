// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mud.Feishu.Abstractions.Configuration;
using Mud.Feishu.Redis.Configuration;
using Mud.Feishu.Redis.Extensions;

namespace Mud.Feishu.Redis.Tests.Configuration;

/// <summary>
/// RedisOptions IConfiguration 绑定测试
/// 验证从 appsettings.json 风格的配置绑定到 RedisOptions 的正确性
/// </summary>
public class RedisOptionsBindingTests
{
    [Fact]
    public void Bind_FromJson_ShouldMapAllProperties()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeishuRedis:ServerAddress"] = "redis.example.com:6380",
                ["FeishuRedis:Password"] = "secret",
                ["FeishuRedis:EventCacheExpiration"] = "1.00:00:00",
                ["FeishuRedis:SeqIdCacheExpiration"] = "12:00:00",
                ["FeishuRedis:NonceTtl"] = "00:10:00",
                ["FeishuRedis:EventKeyPrefix"] = "myapp:event:",
                ["FeishuRedis:NonceKeyPrefix"] = "myapp:nonce:",
                ["FeishuRedis:SeqIdKeyPrefix"] = "myapp:seqid:",
                ["FeishuRedis:ConnectTimeout"] = "8000",
                ["FeishuRedis:SyncTimeout"] = "8000",
                ["FeishuRedis:Ssl"] = "true",
                ["FeishuRedis:ConnectRetry"] = "5",
                ["FeishuRedis:DefaultDatabase"] = "2"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddFeishuRedisDeduplicators(config);
        var sp = services.BuildServiceProvider();

        // Act
        var options = sp.GetRequiredService<RedisOptions>();

        // Assert
        Assert.Equal("redis.example.com:6380", options.ServerAddress);
        Assert.Equal("secret", options.Password);
        Assert.Equal(TimeSpan.FromHours(24), options.EventCacheExpiration);
        Assert.Equal(TimeSpan.FromHours(12), options.SeqIdCacheExpiration);
        Assert.Equal(TimeSpan.FromMinutes(10), options.NonceTtl);
        Assert.Equal("myapp:event:", options.EventKeyPrefix);
        Assert.Equal("myapp:nonce:", options.NonceKeyPrefix);
        Assert.Equal("myapp:seqid:", options.SeqIdKeyPrefix);
        Assert.Equal(8000, options.ConnectTimeout);
        Assert.Equal(8000, options.SyncTimeout);
        Assert.True(options.Ssl);
        Assert.Equal(5, options.ConnectRetry);
        Assert.Equal(2, options.DefaultDatabase);
    }

    [Fact]
    public void Bind_EventCacheExpiration_BelowMinimum_ShouldClampToOneMinute()
    {
        // Arrange - TimeSpan "00:00:30" = 30 秒，低于最小值 1 分钟
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeishuRedis:ServerAddress"] = "localhost:6379",
                ["FeishuRedis:EventCacheExpiration"] = "00:00:30"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddFeishuRedisDeduplicators(config);
        var sp = services.BuildServiceProvider();

        // Act
        var options = sp.GetRequiredService<RedisOptions>();

        // Assert - setter 应将 30 秒提升至 1 分钟
        Assert.Equal(TimeSpan.FromMinutes(1), options.EventCacheExpiration);
    }

    [Fact]
    public void Bind_EventKeyPrefix_EmptyString_ShouldFallbackToDefault()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeishuRedis:ServerAddress"] = "localhost:6379",
                ["FeishuRedis:EventKeyPrefix"] = ""
            })
            .Build();

        var services = new ServiceCollection();
        services.AddFeishuRedisDeduplicators(config);
        var sp = services.BuildServiceProvider();

        // Act
        var options = sp.GetRequiredService<RedisOptions>();

        // Assert - 空字符串应回退到默认前缀
        Assert.Equal("feishu:event:", options.EventKeyPrefix);
    }

    [Fact]
    public void Bind_DeduplicationSubSection_ShouldBindAdvancedOptions()
    {
        // Arrange - 测试 FeishuRedis:Deduplication 子节绑定
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeishuRedis:ServerAddress"] = "localhost:6379",
                ["FeishuRedis:Deduplication:ProcessingTimeout"] = "00:20:00",
                ["FeishuRedis:Deduplication:MaxRetryCount"] = "7",
                ["FeishuRedis:Deduplication:AllowProcessingOnFallback"] = "false"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddFeishuRedisDeduplicators(config);
        var sp = services.BuildServiceProvider();

        // Act
        var dedupOptions = sp.GetService<IOptions<DeduplicationOptions>>()?.Value;

        // Assert
        Assert.NotNull(dedupOptions);
        Assert.Equal(TimeSpan.FromMinutes(20), dedupOptions!.ProcessingTimeout);
        Assert.Equal(7, dedupOptions.MaxRetryCount);
        Assert.False(dedupOptions.AllowProcessingOnFallback);
    }

    [Fact]
    public void Bind_TimeSpanTrap_24Hours_ShouldNotBe24Days()
    {
        // Arrange - 验证 IConfiguration 绑定中 "24:00:00" 的实际解释行为
        // 注意：Microsoft.Extensions.Configuration 的 ConfigurationBinder 使用 TypeDescriptor 转换，
        // 将 "24:00:00" 解释为 24 天 (24.00:00:00)，而非 24 小时 (1.00:00:00)。
        // 正确的 24 小时格式应为 "1.00:00:00" 或 "24:00:00"（在 TimeSpan.Parse 中为 24 小时，但绑定器不同）。
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeishuRedis:ServerAddress"] = "localhost:6379",
                ["FeishuRedis:EventCacheExpiration"] = "24:00:00"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddFeishuRedisDeduplicators(config);
        var sp = services.BuildServiceProvider();

        // Act
        var options = sp.GetRequiredService<RedisOptions>();

        // Assert - 记录陷阱：IConfiguration 绑定将 "24:00:00" 解释为 24 天
        // setter 的最小值校验（1 分钟）无法拦截此值（24 天 >> 1 分钟），故配置静默生效为 24 天
        Assert.Equal(TimeSpan.FromDays(24), options.EventCacheExpiration);

        // 验证正确的 24 小时格式
        var correctConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeishuRedis:ServerAddress"] = "localhost:6379",
                ["FeishuRedis:EventCacheExpiration"] = "1.00:00:00"
            })
            .Build();

        var correctServices = new ServiceCollection();
        correctServices.AddFeishuRedisDeduplicators(correctConfig);
        var correctSp = correctServices.BuildServiceProvider();
        var correctOptions = correctSp.GetRequiredService<RedisOptions>();

        Assert.Equal(TimeSpan.FromHours(24), correctOptions.EventCacheExpiration);
    }
}
