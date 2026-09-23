// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Mud.Feishu.Abstractions.Authentication;
using Mud.Feishu.Abstractions.Services;
using Mud.Feishu.Redis.Diagnostics;
using Mud.Feishu.Redis.Extensions;
using Mud.Feishu.Redis.Services;
using StackExchange.Redis;

namespace Mud.Feishu.Redis.Tests.ContractGuards;

/// <summary>
/// Redis 键布局与装配不变量守卫（R2 / T-R2-23，E-03）。
/// </summary>
/// <remarks>
/// <para>
/// 沿用 <c>Tests/Mud.Feishu.WebSocket.Tests/ContractGuards/WebSocketContractGuards.cs</c> 的范式：
/// 锁定"必须始终成立"的结构性不变量，防已修项复活。
/// </para>
/// <para>
/// 本轮守卫的核心动机：R2-01（SeqID 裁剪语义）与 R2-02（清理模式与键不同源）在 135 条全绿的单测下存活，
/// 因为"键布局"与"SCAN 模式"从未被任何断言捆绑在一起。本文件用**属性测试**把二者绑死。
/// </para>
/// </remarks>
[Trait("Category", "ContractGuard")]
public class RedisKeyLayoutContractGuards
{
    private const string EventPrefix = "feishu:event:";
    private const string NoncePrefix = "feishu:nonce:";
    private const string SeqIdPrefix = "feishu:seqid:";

    #region 金标准键样例

    /// <summary>
    /// 金标准：四类键的实测形态必须与文档/实现一致。
    /// </summary>
    /// <remarks>
    /// **任何键布局变更都必须显式修改本守卫并在 CHANGELOG 声明**（ADR-11 的锁定手段）。
    /// 注意双冒号的成因：默认前缀自带尾 <c>:</c>，而 <c>Combine</c> 会在段间再插入一级 <c>:</c>。
    /// </remarks>
    [Fact]
    public void GoldenKeys_Should_Match_ActualLayout()
    {
        // 事件（无 appKey / 有 appKey）
        RedisKeyBuilder.Combine(EventPrefix, "evt1").Should().Be("feishu:event::evt1");
        RedisKeyBuilder.Combine(EventPrefix, "cli_a", "evt1").Should().Be("feishu:event::cli_a:evt1");

        // Nonce
        RedisKeyBuilder.Combine(NoncePrefix, "cli_a", "n1").Should().Be("feishu:nonce::cli_a:n1");

        // SeqID：String 键与 Sorted Set 键
        RedisKeyBuilder.Combine(SeqIdPrefix, "app1|host1", "42").Should().Be("feishu:seqid::app1|host1:42");
        RedisKeyBuilder.Combine(SeqIdPrefix, "app1|host1", "set").Should().Be("feishu:seqid::app1|host1:set");

        // SCAN 模式：以分隔符 + '*' 结尾（段级精确，避免相邻 scopeKey 越界命中）
        RedisKeyBuilder.Pattern(SeqIdPrefix, "app1|host1").Should().Be("feishu:seqid::app1|host1:*");
        RedisKeyBuilder.Pattern(EventPrefix).Should().Be("feishu:event::*");

        // 段内分隔符转义（R-20/R-21：跨段碰撞防护）
        RedisKeyBuilder.Combine(EventPrefix, "a:b", "c").Should().Be(@"feishu:event::a\:b:c");
        RedisKeyBuilder.Combine(EventPrefix, "a", "b:c").Should().Be(@"feishu:event::a:b\:c");
        RedisKeyBuilder.Combine(EventPrefix, "a:b", "c")
            .Should().NotBe(RedisKeyBuilder.Combine(EventPrefix, "a", "b:c"),
                "段内 ':' 必须转义，否则 appKey=\"a:b\"+\"c\" 与 appKey=\"a\"+\"b:c\" 会碰撞到同一键");
    }

    /// <summary>
    /// 令牌键前缀：默认形态与 Memory 路径逐字节一致（ADR-13 的 TMF2-05 契约）。
    /// </summary>
    [Fact]
    public void TokenKeyPrefix_Should_Be_ByteIdentical_Between_Memory_And_Redis()
    {
        const string appKey = "cli_a";

        // Redis 侧（工厂静态出口）
        var redisPrefix = PerAppRedisTokenStoreFactory.BuildKeyPrefix(appKey);
        redisPrefix.Should().Be("feishu:cli_a:token");
        PerAppRedisTokenStoreFactory.BuildKeyPrefix(appKey)
            .Should().Be(PerAppRedisTokenStoreFactory.BuildKeyPrefix(appKey, "feishu"),
                "默认前缀与显式传默认值必须等价");

        // Memory 侧：捕获 FeishuTokenStore 实际写入的缓存键
        var capturedKeys = new List<string>();
        var cache = new Mock<IMemoryCache>();
        cache.Setup(c => c.CreateEntry(It.IsAny<object>()))
            .Returns<object>(key =>
            {
                capturedKeys.Add(key.ToString()!);
                return new Mock<ICacheEntry>().Object;
            });

        var memoryStore = new FeishuTokenStore(cache.Object, appKey);
        memoryStore.SetAccessTokenAsync("tenant:cli_a", "token-value", 3600).GetAwaiter().GetResult();

        capturedKeys.Should().ContainSingle();
        capturedKeys[0].Should().Be($"{redisPrefix}:tenant\\:cli_a:access",
            "Memory 与 Redis 的令牌键必须逐字节一致（否则切换后端会读不到令牌）");
    }

    /// <summary>
    /// 环境隔离：<c>TokenKeyPrefix</c> 必须真正改变前缀（R2-04）。
    /// </summary>
    [Fact]
    public void TokenKeyPrefix_Should_Honor_EnvironmentSegment()
    {
        PerAppRedisTokenStoreFactory.BuildKeyPrefix("cli_a", "prod").Should().Be("prod:cli_a:token");
        PerAppRedisTokenStoreFactory.BuildKeyPrefix("cli_a", "").Should().Be("feishu:cli_a:token",
            "空值必须兜底为默认前缀");
        PerAppRedisTokenStoreFactory.BuildKeyPrefix("cli_a", "prod")
            .Should().NotBe(PerAppRedisTokenStoreFactory.BuildKeyPrefix("cli_a", "dev"),
                "不同环境的令牌键空间必须不同（多环境共用 Redis 的隔离手段）");
    }

    #endregion

    #region 模式与键同源（R2-02 根因）

    /// <summary>
    /// 不变量：任何 SCAN 模式都必须由 <see cref="RedisKeyBuilder.Pattern"/> 产出，
    /// 且必然匹配同源构造器产出的键、不匹配相邻隔离维度的键。
    /// </summary>
    /// <remarks>
    /// 生成器只使用 **不含 glob 元字符**（<c>* ? [ ]</c>）的字符集：
    /// <c>RedisKeyBuilder.Escape</c> 仅转义 <c>:</c>，若段内含 <c>*</c>，会把模式变成通配符，
    /// 使"不匹配邻居"的断言不可判定。`:` 与转义场景由 <see cref="GoldenKeys_Should_Match_ActualLayout"/> 覆盖。
    /// </remarks>
    [Property(MaxTest = 300)]
    public Property Pattern_Should_Match_Own_Keys_And_Reject_Neighbours()
        => Prop.ForAll<string, string>(
            Arb.From(SegmentGen),
            Arb.From(SegmentGen),
            (scope, neighbour) => Predicate(scope, neighbour));

    /// <summary>
    /// 属性断言体（拆分出来以避免 FsCheck 重载推断歧义；FsCheck 失败时会打印反例实参）。
    /// </summary>
    private static bool Predicate(string scope, string neighbour)
    {
        if (scope == neighbour)
            return true;

        var pattern = RedisKeyBuilder.Pattern(SeqIdPrefix, scope);
        // 模式形如 "{key}:*"——去掉尾部的 ':' 与 '*' 得到"段边界前缀"
        var segmentPrefix = pattern.Substring(0, pattern.Length - 2) + ":";

        var ownSeqKey = RedisKeyBuilder.Combine(SeqIdPrefix, scope, "42");
        var ownSortedSetKey = RedisKeyBuilder.Combine(SeqIdPrefix, scope, "set");
        var neighbourKey = RedisKeyBuilder.Combine(SeqIdPrefix, neighbour, "42");

        return pattern == RedisKeyBuilder.Combine(SeqIdPrefix, scope) + ":*"
            && ownSeqKey.StartsWith(segmentPrefix, StringComparison.Ordinal)
            && ownSortedSetKey.StartsWith(segmentPrefix, StringComparison.Ordinal)
            && !neighbourKey.StartsWith(segmentPrefix, StringComparison.Ordinal);
    }

    /// <summary>
    /// 事件 / Nonce 的无段前缀模式必须匹配其键（现状：<c>Combine(prefix) + "*"</c>）。
    /// </summary>
    [Fact]
    public void Pattern_Should_Match_EventAndNonceKeys()
    {
        var eventPattern = RedisKeyBuilder.Pattern(EventPrefix);
        eventPattern.Should().Be("feishu:event::*");
        RedisKeyBuilder.Combine(EventPrefix, "cli_a", "evt1")
            .StartsWith("feishu:event::", StringComparison.Ordinal).Should().BeTrue();
        RedisKeyBuilder.Combine(EventPrefix, "evt1")
            .StartsWith("feishu:event::", StringComparison.Ordinal).Should().BeTrue();

        var noncePattern = RedisKeyBuilder.Pattern(NoncePrefix);
        noncePattern.Should().Be("feishu:nonce::*");
        RedisKeyBuilder.Combine(NoncePrefix, "cli_a", "n1")
            .StartsWith("feishu:nonce::", StringComparison.Ordinal).Should().BeTrue();

        // 段级精确：相邻前缀（"foo" 与 "foobar"）不得互相命中
        var fooPattern = RedisKeyBuilder.Pattern(NoncePrefix, "foo");
        fooPattern.Should().Be("feishu:nonce::foo:*");
        RedisKeyBuilder.Combine(NoncePrefix, "foobar", "n1")
            .StartsWith(fooPattern.Substring(0, fooPattern.Length - 1), StringComparison.Ordinal)
            .Should().BeFalse("模式必须以分隔符结尾，否则 scope=\"foo\" 的清理会越界删除 scope=\"foobar\" 的键");
    }

    /// <summary>
    /// 键段护栏：超长段抛 <c>FeishuRedisException(InvalidArgument)</c>（R2-18：该枚举值必须真实产生）。
    /// </summary>
    [Fact]
    public void OversizedSegment_Should_Throw_InvalidArgument()
    {
        var oversized = new string('x', 257);

        var act = () => RedisKeyBuilder.Combine(EventPrefix, oversized);

        act.Should().Throw<FeishuRedisException>()
            .Which.FailureKind.Should().Be(FeishuRedisFailureKind.InvalidArgument,
                "调用方传入非法键段（> 256 字符）必须可被消费侧识别为'参数错误、不重试'");
    }

    /// <summary>
    /// 前缀护栏：空前缀 / 通配符前缀必须 fail-fast（R-01），且**不**伪装成 InvalidArgument。
    /// </summary>
    [Fact]
    public void IllegalPrefix_Should_FailFast()
    {
        var emptyAct = () => RedisKeyBuilder.Combine("");
        emptyAct.Should().Throw<InvalidOperationException>()
            .Which.Should().NotBeOfType<FeishuRedisException>("空前缀是配置错误，不是调用参数错误");

        var wildcardAct = () => RedisKeyBuilder.Pattern("*feishu:");
        wildcardAct.Should().Throw<InvalidOperationException>();
    }

    #endregion

    #region 装配与 CI 覆盖率

    /// <summary>
    /// 集成测试工程必须在解决方案内（R2-03 回归守卫）。
    /// </summary>
    /// <remarks>
    /// 该项目此前不在 <c>Mud.Feishu.slnx</c>，导致本机与 CI 都不构建/不运行它——
    /// 这是本轮 4 个 P1 中两个（R2-01/R2-02）能长期存活的根因。
    /// </remarks>
    [Fact]
    public void IntegrationTestsProject_Should_Be_Listed_In_Solution()
    {
        var slnx = Path.Combine(GetRepositoryRoot(), "Mud.Feishu.slnx");
        File.Exists(slnx).Should().BeTrue($"解决方案文件必须存在：{slnx}");

        var content = File.ReadAllText(slnx);
        content.Should().Contain("Tests/Mud.Feishu.Redis.IntegrationTests/Mud.Feishu.Redis.IntegrationTests.csproj",
            "集成测试工程必须在解决方案内，否则门禁的逐工程 TRX 断言不会覆盖它（R2-03）");
    }

    /// <summary>
    /// 无日志宿主的解析不得抛异常（R-25 回归）并暴露诊断门面（E-02）。
    /// </summary>
    /// <remarks>
    /// 用 Moq 覆盖 <see cref="IConnectionMultiplexer"/> 注册，避免解析期真实连接。
    /// </remarks>
    [Fact]
    public void Deduplicators_And_Diagnostics_Should_Resolve_Without_Logging()
    {
        var services = new ServiceCollection();
        services.AddFeishuRedisDeduplicators(_ => { });

        // 覆盖真实连接工厂（后注册者胜出），避免测试期真实连接 Redis
        services.RemoveAll<IConnectionMultiplexer>();
        services.AddSingleton(new Mock<IConnectionMultiplexer>().Object);

        using var provider = services.BuildServiceProvider();

        var diagnostics = provider.GetRequiredService<IRedisDeduplicationDiagnostics>();
        diagnostics.Should().NotBeNull();

        provider.GetRequiredService<IFeishuEventDeduplicator>().Should().BeOfType<RedisFeishuEventDistributedDeduplicator>();
        provider.GetRequiredService<IFeishuNonceDistributedDeduplicator>().Should().BeOfType<RedisFeishuNonceDistributedDeduplicator>();
        provider.GetRequiredService<IFeishuSeqIDDeduplicator>().Should().BeOfType<RedisFeishuSeqIDDeduplicator>();
    }

    /// <summary>
    /// <c>rediss://</c> 必须启用 TLS（R-13/R2-26 回归）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 实测（StackExchange.Redis 3.3.0）：<c>ConfigurationOptions.Parse("rediss://…").Ssl == false</c>——
    /// 该 API **不因 scheme 自动启用 TLS**。因此仅依赖 <c>Parse</c> 的实现在文档推荐的
    /// <c>"ServerAddress": "rediss://…"</c> 配置下会以明文连接 TLS 端口（必然失败或明文传输）。
    /// </para>
    /// <para>
    /// 守卫分两段：① 记录上游行为（防止有人误以为 Parse 会自动开 TLS 而删掉推导逻辑）；
    /// ② 断言本组件的装配出口 <see cref="RedisConnectionFactory.Build"/> 确实置位 <c>Ssl</c>。
    /// </para>
    /// </remarks>
    [Fact]
    public void RedissUri_Should_Enable_Tls_In_Assembled_Options()
    {
        // ① 上游真相：Parse 不推导 TLS
        ConfigurationOptions.Parse("rediss://secure.redis.com:6380").Ssl
            .Should().BeFalse("StackExchange.Redis 的 Parse 不因 rediss:// scheme 自动置 Ssl（实测 3.3.0）");

        // ② 本组件的装配出口必须推导
        RedisConnectionFactory.RequiresSsl("rediss://secure.redis.com:6380").Should().BeTrue();
        RedisConnectionFactory.RequiresSsl("REDISS://secure.redis.com:6380").Should().BeTrue("scheme 判定必须大小写不敏感");
        RedisConnectionFactory.RequiresSsl("redis://plain.redis.com:6379").Should().BeFalse();
        RedisConnectionFactory.RequiresSsl("localhost:6379").Should().BeFalse();

        var redissOptions = new Mud.Feishu.Redis.Configuration.RedisOptions();
        redissOptions.Connection.ServerAddress = "rediss://secure.redis.com:6380";
        var redissConfig = RedisConnectionFactory.Build(redissOptions);
        redissConfig.Ssl.Should().BeTrue("rediss:// 必须启用 TLS，否则文档推荐的配置会明文连接 TLS 端口");
        redissConfig.EndPoints.Should().ContainSingle().Which.ToString().Should().Contain("6380");

        var plainOptions = new Mud.Feishu.Redis.Configuration.RedisOptions();
        plainOptions.Connection.ServerAddress = "plain.redis.com:6379";
        RedisConnectionFactory.Build(plainOptions).Ssl.Should().BeFalse();

        // 显式 Ssl=true 也必须生效（与 scheme 取或）
        var explicitSsl = new Mud.Feishu.Redis.Configuration.RedisOptions();
        explicitSsl.Connection.ServerAddress = "plain.redis.com:6380";
        explicitSsl.Connection.Ssl = true;
        RedisConnectionFactory.Build(explicitSsl).Ssl.Should().BeTrue();
    }

    /// <summary>
    /// 连接选项装配：显式配置项必须覆盖连接串，超时/口令/ClientName 全部映射。
    /// </summary>
    [Fact]
    public void ConnectionOptions_Should_Map_All_Knobs()
    {
        var options = new Mud.Feishu.Redis.Configuration.RedisOptions();
        options.Connection.ServerAddress = "redis.example.com:6380,password=inline-secret";
        options.Connection.Password = "explicit-secret";
        options.Connection.ConnectTimeout = 7000;
        options.Connection.SyncTimeout = 8000;
        options.Connection.ConnectRetry = 5;
        options.Connection.AbortOnConnectFail = false;
        options.Connection.DefaultDatabase = 3;
        options.Advanced.AllowAdmin = true;
        options.Advanced.ClientName = "custom-client";

        var config = RedisConnectionFactory.Build(options);

        config.Password.Should().Be("explicit-secret", "显式口令必须覆盖连接串内联口令");
        config.ConnectTimeout.Should().Be(7000);
        config.SyncTimeout.Should().Be(8000);
        config.ConnectRetry.Should().Be(5);
        config.AbortOnConnectFail.Should().BeFalse();
        config.DefaultDatabase.Should().Be(3);
        config.AllowAdmin.Should().BeTrue();
        config.ClientName.Should().Be("custom-client");
    }

    #endregion

    #region 辅助

    /// <summary>
    /// 键段生成器：1–8 个字符，字符集覆盖字母/数字/<c>|</c>/<c>-</c>/<c>_</c>/<c>:</c>，
    /// **排除** glob 元字符（见属性测试 remarks）。
    /// </summary>
    private static readonly Gen<string> SegmentGen =
        from length in Gen.Choose(1, 8)
        from chars in Gen.ArrayOf(length, Gen.Elements("abcXYZ019|-_ :".ToCharArray()))
        select new string(chars);

    /// <summary>
    /// 从 <see cref="AppContext.BaseDirectory"/> 向上定位仓库根（含 <c>Mud.Feishu.slnx</c>）。
    /// </summary>
    /// <remarks>与 <c>WebSocketContractGuards.GetRepositoryRoot</c> 同法：宁可失败也不要假绿。</remarks>
    private static string GetRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Mud.Feishu.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            $"未能从 {AppContext.BaseDirectory} 向上定位仓库根目录（Mud.Feishu.slnx）——守卫无法工作");
    }

    #endregion
}
