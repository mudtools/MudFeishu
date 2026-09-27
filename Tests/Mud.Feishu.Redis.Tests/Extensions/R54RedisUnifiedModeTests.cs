// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Mud.Feishu.Abstractions.Configuration;
using Mud.Feishu.Abstractions.Services;
using Mud.Feishu.Redis.Extensions;
using StackExchange.Redis;

namespace Mud.Feishu.Redis.Tests.Extensions;

/// <summary>
/// R5.4-R1/F2：Redis 通道消费统一节 Mode（None→Noop / InMemory→fail-fast）防回归。
/// </summary>
public class R54RedisUnifiedModeTests
{
    private readonly Mock<IConnectionMultiplexer> _redisMock = new();
    private readonly Mock<IDatabase> _databaseMock = new();

    public R54RedisUnifiedModeTests()
    {
        _redisMock.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_databaseMock.Object);
    }

    private ServiceProvider Build(FeishuDeduplicationOptions unified)
    {
        var services = new ServiceCollection();
        services.AddLogging(l => l.AddProvider(NullLoggerProvider.Instance));
        services.AddFeishuRedisDeduplicators(_ => { });

        services.RemoveAll<IOptions<FeishuDeduplicationOptions>>();
        services.AddSingleton(Options.Create(unified));

        // 覆盖真实连接工厂，避免测试期真实 Redis 连接
        services.RemoveAll<IConnectionMultiplexer>();
        services.AddSingleton<IConnectionMultiplexer>(_redisMock.Object);

        return services.BuildServiceProvider();
    }

    [Fact]
    public void RedisEventDeduplicator_ShouldBeNoop_WhenUnifiedModeNone()
    {
        var provider = Build(new FeishuDeduplicationOptions
        {
            IsConfiguredFromConfiguration = true,
            Mode = FeishuDeduplicationOptions.ModeNone
        });

        var dedup = provider.GetRequiredService<IFeishuEventDeduplicator>();

        dedup.Should().BeOfType<NoopFeishuEventDeduplicator>(
            "FeishuDeduplication:Mode=None 须显式关闭事件去重（含 Redis 实现）");
    }

    [Fact]
    public void RedisNonceDeduplicator_ShouldBeNoop_WhenUnifiedModeNone()
    {
        var provider = Build(new FeishuDeduplicationOptions
        {
            IsConfiguredFromConfiguration = true,
            Mode = FeishuDeduplicationOptions.ModeNone
        });

        var nonce = provider.GetRequiredService<IFeishuNonceDistributedDeduplicator>();

        nonce.Should().BeOfType<NoopFeishuNonceDeduplicator>();
    }

    [Fact]
    public void RedisSeqIdDeduplicator_ShouldBeNoop_WhenUnifiedModeNone()
    {
        var provider = Build(new FeishuDeduplicationOptions
        {
            IsConfiguredFromConfiguration = true,
            Mode = FeishuDeduplicationOptions.ModeNone
        });

        var seqId = provider.GetRequiredService<IFeishuSeqIDDeduplicator>();

        seqId.Should().BeOfType<NoopFeishuSeqIdDeduplicator>();
    }

    [Fact]
    public void RedisEventDeduplicator_ShouldThrow_WhenUnifiedModeInMemory()
    {
        var provider = Build(new FeishuDeduplicationOptions
        {
            IsConfiguredFromConfiguration = true,
            Mode = FeishuDeduplicationOptions.ModeInMemory
        });

        var act = () => provider.GetRequiredService<IFeishuEventDeduplicator>();

        act.Should().Throw<InvalidOperationException>(
            "Mode=InMemory 与已注册 Redis 去重器矛盾，须 fail-fast 防止静默双轨");
    }
}