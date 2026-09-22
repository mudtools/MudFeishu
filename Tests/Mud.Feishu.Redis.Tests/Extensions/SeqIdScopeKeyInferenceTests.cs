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
using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.Configuration;
using Mud.Feishu.Abstractions.Services;
using Mud.Feishu.Redis.Configuration;
using Mud.Feishu.Redis.Extensions;
using StackExchange.Redis;

namespace Mud.Feishu.Redis.Tests.Extensions;

/// <summary>
/// R5.3.1/X13（G-12）：SeqID scopeKey 的 AppKey 解析期推断。
/// </summary>
/// <remarks>
/// <para>
/// 推断优先级：<c>FeishuDeduplication:SeqId:ScopeKey</c> &gt; <c>RedisOptions.SeqIdScopeKey</c>
/// &gt; <b>FeishuApps 默认应用 AppKey</b>（本批新增）&gt; <c>RedisOptions.AppKey</c> 回落。
/// </para>
/// <para>
/// 可观测点：<see cref="Redis.Services.RedisFeishuSeqIDDeduplicator.GetCacheCount"/> 触发
/// <c>SortedSetLength</c>，传入的 RedisKey 形如 <c>{prefix}{scopeKey}set</c>，据此断言 scopeKey。
/// </para>
/// </remarks>
public class SeqIdScopeKeyInferenceTests
{
    private readonly Mock<IConnectionMultiplexer> _redisMock = new();
    private readonly Mock<IDatabase> _databaseMock = new();

    public SeqIdScopeKeyInferenceTests()
    {
        _redisMock.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_databaseMock.Object);
    }

    private (ServiceProvider Provider, Func<string> ReadScopeKey) Build(
        Action<RedisOptions>? configure = null,
        List<FeishuAppConfig>? appConfigs = null,
        FeishuDeduplicationOptions? unified = null)
    {
        RedisKey captured = default;
        _databaseMock
            .Setup(x => x.SortedSetLength(It.IsAny<RedisKey>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<Exclude>(), It.IsAny<CommandFlags>()))
            .Callback<RedisKey, double, double, Exclude, CommandFlags>((k, _, _, _, _) => captured = k)
            .Returns(0L);

        var services = new ServiceCollection();
        services.AddLogging(l => l.AddProvider(NullLoggerProvider.Instance));

        services.AddFeishuRedisDeduplicators(configure ?? (_ => { }));

        if (appConfigs is not null)
        {
            var monitorMock = new Mock<IOptionsMonitor<List<FeishuAppConfig>>>();
            monitorMock.SetupGet(m => m.CurrentValue).Returns(appConfigs);
            services.RemoveAll<IOptionsMonitor<List<FeishuAppConfig>>>();
            services.AddSingleton<IOptionsMonitor<List<FeishuAppConfig>>>(monitorMock.Object);
        }

        if (unified is not null)
        {
            services.RemoveAll<IOptions<FeishuDeduplicationOptions>>();
            services.AddSingleton(Options.Create(unified));
        }

        // 覆盖 AddFeishuRedis 注册的真实连接工厂（后注册者胜出），避免测试期真实连接
        services.RemoveAll<IConnectionMultiplexer>();
        services.AddSingleton<IConnectionMultiplexer>(_redisMock.Object);

        var provider = services.BuildServiceProvider();
        var deduplicator = provider.GetRequiredService<IFeishuSeqIDDeduplicator>();

        return (provider, () =>
        {
            deduplicator.GetCacheCount();
            return captured.ToString();
        }
        );
    }

    [Fact]
    public void ScopeKey_ShouldInferFromDefaultApp_WhenFeishuAppsConfigured()
    {
        var (_, readScopeKey) = Build(appConfigs: new List<FeishuAppConfig>
        {
            new() { AppKey = "main-app", AppId = "cli_x", AppSecret = "s", IsDefault = true },
            new() { AppKey = "hr-app", AppId = "cli_y", AppSecret = "s" }
        });

        readScopeKey().Should().Contain("main-app|",
            "存在 FeishuApps 默认应用时，scopeKey 的 AppKey 部分应从默认应用推断，而非恒为 \"default\"");
    }

    [Fact]
    public void ScopeKey_ShouldFallBackToRedisOptionsAppKey_WhenNoFeishuApps()
    {
        // 宿主未接多应用（GetService 返回 null）→ 回落 RedisOptions.AppKey（默认 "default"）
        var (_, readScopeKey) = Build();

        readScopeKey().Should().Contain("default|",
            "无 FeishuApps 时保持既有回落，绝不因推断逻辑而退化为空 scopeKey");
    }

    [Fact]
    public void ScopeKey_ExplicitSeqIdScopeKey_ShouldOverrideInference()
    {
        var (_, readScopeKey) = Build(
            configure: o => o.SeqIdScopeKey = "explicit-scope",
            appConfigs: new List<FeishuAppConfig>
            {
                new() { AppKey = "main-app", AppId = "cli_x", AppSecret = "s", IsDefault = true }
            });

        readScopeKey().Should().Contain("explicit-scope",
            "显式配置的 RedisOptions.SeqIdScopeKey 恒优先于默认应用推断");
    }

    [Fact]
    public void ScopeKey_UnifiedScopeKey_ShouldHaveHighestPriority()
    {
        var (_, readScopeKey) = Build(
            configure: o => o.SeqIdScopeKey = "explicit-scope",
            appConfigs: new List<FeishuAppConfig>
            {
                new() { AppKey = "main-app", AppId = "cli_x", AppSecret = "s", IsDefault = true }
            },
            unified: new FeishuDeduplicationOptions
            {
                IsConfiguredFromConfiguration = true,
                SeqId = new SeqIdDeduplicationOptions { ScopeKey = "unified-scope" }
            });

        readScopeKey().Should().Contain("unified-scope",
            "FeishuDeduplication:SeqId:ScopeKey 是最高优先级入口（G-12 优先级链）");
    }
}
