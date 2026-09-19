// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.Configuration;
using Mud.Feishu.Redis.Configuration;
using Mud.Feishu.Redis.Extensions;

namespace Mud.Feishu.Redis.Tests.Configuration;


/// <summary>
/// B2/R1.3：Redis 路径下无效 Deduplication 键的诊断日志（方法级单测，无需真实 Redis）。
/// </summary>
public class RedisDeduplicationIneffectiveKeysWarnTests
{
    private sealed class CapturingLogger : ILogger
    {
        public List<string> Messages { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }
    }

    [Fact]
    public void Warn_ShouldFire_WhenDeduplicationCacheDiffersFromRedisOptions()
    {
        var logger = new CapturingLogger();
        var redisOptions = new RedisOptions
        {
            EventCacheExpiration = TimeSpan.FromHours(48)
        };
        var dedupOptions = new DeduplicationOptions
        {
            CacheExpiration = TimeSpan.FromHours(2)
        };

        RedisFeishuServiceBuilderExtensions.WarnIfDeduplicationKeysAreIneffective(logger, redisOptions, dedupOptions);

        logger.Messages.Should().Contain(m => m.Contains("DeduplicationOptions.CacheExpiration"));
    }

    [Fact]
    public void Warn_ShouldFire_WhenDeduplicationKeyPrefixDiffersFromRedisOptions()
    {
        var logger = new CapturingLogger();
        var redisOptions = new RedisOptions
        {
            EventKeyPrefix = "t-a:feishu:event:"
        };
        var dedupOptions = new DeduplicationOptions
        {
            KeyPrefix = "feishu:event:"
        };

        RedisFeishuServiceBuilderExtensions.WarnIfDeduplicationKeysAreIneffective(logger, redisOptions, dedupOptions);

        logger.Messages.Should().Contain(m => m.Contains("DeduplicationOptions.KeyPrefix"));
    }


    [Fact]
    public void Warn_ShouldNotFire_WhenDeduplicationKeysAlignWithRedisDefaults()
    {
        var logger = new CapturingLogger();
        var redisOptions = new RedisOptions();
        var dedupOptions = new DeduplicationOptions(); // 默认值与 Consts/RedisOptions 对齐

        RedisFeishuServiceBuilderExtensions.WarnIfDeduplicationKeysAreIneffective(logger, redisOptions, dedupOptions);

        logger.Messages.Should().BeEmpty();
    }
}
