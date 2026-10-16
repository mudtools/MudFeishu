// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Moq;
using Mud.Feishu.Redis.Services;
using StackExchange.Redis;
using Xunit;

namespace Mud.Feishu.Redis.Tests;

/// <summary>
/// U-7（上游 3.0.10 Batch 3）：<see cref="RedisUserTokenStore"/> 用户维度 CAS（Lua 原子）测试。
/// </summary>
/// <remarks>
/// 断言面：脚本入参（键 / 期望明文 / 新存储值 / TTL）与结果映射（1 ⇒ true，0/其他 ⇒ false）。
/// 明文比较在脚本内按「首个 <c>|</c> 之后」切段，故 v1 相对秒 / 旧绝对毫秒戳 / v2 信封三形态同规则。
/// </remarks>
public class RedisUserTokenStoreCompareExchangeTests
{
    private sealed record CasInvocation(string Script, RedisKey[] Keys, RedisValue[] Values);

    private static (RedisUserTokenStore Store, List<CasInvocation> Invocations) BuildStore(RedisResult? result = null)
    {
        var invocations = new List<CasInvocation>();

        var db = new Mock<IDatabase>();
        db.Setup(d => d.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .Callback<string, RedisKey[], RedisValue[], CommandFlags>((script, keys, values, _) =>
                invocations.Add(new CasInvocation(script, keys, values)))
            .ReturnsAsync(result ?? RedisResult.Create((RedisValue)1));

        var redis = new Mock<IConnectionMultiplexer>();
        redis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(db.Object);

        var inner = (RedisTokenStore)new PerAppRedisTokenStoreFactory(redis.Object).Create("cli_a").TokenStore;
        return (new RedisUserTokenStore(inner, redis.Object, "feishu:token"), invocations);
    }

    [Fact]
    public async Task CompareExchangeRefreshTokenAsync_CompareAndDelete_ShouldPassPlaintextAndEmptyNewValue()
    {
        var (store, invocations) = BuildStore();

        var matched = await store.CompareExchangeRefreshTokenAsync(
            "user1", "UserAccessToken:cli_a", "stored-refresh", newToken: null);

        matched.Should().BeTrue();
        invocations.Should().ContainSingle();
        var call = invocations[0];
        call.Keys[0].ToString().Should().Contain("user1").And.Contain("refresh");
        call.Values[0].ToString().Should().Be("stored-refresh", "期望值必须是令牌明文（脚本按首个 '|' 之后切段比较）");
        call.Values[1].ToString().Should().BeEmpty("compare-and-delete：新存储值为空串 ⇒ 脚本走 DEL 分支");
        ((long)call.Values[2]).Should().Be(0);
    }

    [Fact]
    public async Task CompareExchangeRefreshTokenAsync_Set_ShouldEncodeNewValue_AndFallbackTo30DaysTtl()
    {
        var (store, invocations) = BuildStore();

        await store.CompareExchangeRefreshTokenAsync(
            "user1", "UserAccessToken:cli_a", "old-refresh", newToken: "new-refresh");

        var call = invocations.Single();
        call.Values[1].ToString().Should().Be("0|new-refresh",
            "写侧单写 C-2 官方形态；刷新令牌无 TTL 入参 ⇒ 编码为「无 TTL 信息」（前导 0，与 SetRefreshTokenAsync 无过期戳分支同口径）");
        ((long)call.Values[2]).Should().Be((long)TimeSpan.FromDays(30).TotalMilliseconds);
        call.Script.Should().Contain("redis.call('GET'").And.Contain("string.find");
    }

    [Fact]
    public async Task CompareExchangeAccessTokenAsync_ShouldEncodeRelativeSeconds_AndPassTtlInMilliseconds()
    {
        var (store, invocations) = BuildStore();

        await store.CompareExchangeAccessTokenAsync(
            "user1", "UserAccessToken:cli_a", "old-access", "new-access", expiresInSeconds: 7200);

        var call = invocations.Single();
        call.Keys[0].ToString().Should().Contain("user1").And.Contain("access");
        call.Values[0].ToString().Should().Be("old-access");
        call.Values[1].ToString().Should().Be("7200|new-access");
        ((long)call.Values[2]).Should().Be(7_200_000L);
    }

    [Fact]
    public async Task CompareExchangeAccessTokenAsync_WithoutExpiry_ShouldNotSetKeyTtl()
    {
        var (store, invocations) = BuildStore();

        await store.CompareExchangeAccessTokenAsync(
            "user1", "UserAccessToken:cli_a", "old-access", "new-access", expiresInSeconds: 0);

        var call = invocations.Single();
        call.Values[1].ToString().Should().Be("0|new-access");
        ((long)call.Values[2]).Should().Be(0L, "≤ 0 = 不设键 TTL（宁缺勿滥）");
    }

    [Fact]
    public async Task CompareExchange_ShouldReturnFalse_WhenScriptReportsNoMatch()
    {
        var (store, _) = BuildStore(RedisResult.Create((RedisValue)0));

        var matched = await store.CompareExchangeRefreshTokenAsync(
            "user1", "UserAccessToken:cli_a", "stale-refresh", newToken: null);

        matched.Should().BeFalse();
    }

    [Fact]
    public async Task CompareExchange_ShouldReturnFalse_WithoutRedisCall_WhenExpectedTokenIsEmpty()
    {
        var (store, invocations) = BuildStore();

        var matched = await store.CompareExchangeRefreshTokenAsync(
            "user1", "UserAccessToken:cli_a", expectedToken: "", newToken: null);

        matched.Should().BeFalse();
        invocations.Should().BeEmpty("空期望值无比较意义（且会与「已过期占位」的空明文混淆），契约要求非空");
    }
}
