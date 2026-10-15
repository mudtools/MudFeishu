// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Reflection;
using Mud.Feishu.Redis.Services;

namespace Mud.Feishu.Redis.Tests.Services;

/// <summary>
/// C-2 裁决（Mud.HttpUtils 3.0.7）落地后的 refresh TTL 推导用例：
/// 存储值双读（相对秒新格式 / 绝对毫秒戳旧格式）驱动 refresh 键 TTL，行为分界：
/// ① 相对秒 → TTL = 投影过期时刻 − now；② 旧格式未过期毫秒戳 → TTL = 原值 − now；
/// ③ 旧格式已过期毫秒戳 → 删键不写入；④ 无 TTL 信息（前导数 ≤ 0）/ 无法解析 → 回落 30 天。
/// </summary>
/// <remarks>
/// SE.Redis 3.x 将 <c>StringSetAsync</c> 的 TTL 参数从 <c>TimeSpan?</c> 改为 <see cref="Expiration"/>
/// 结构（B-5 破坏点之一）。TTL 数值经 Invocations 记录 + 反射读内部字段提取，
/// 与 3.x 内部表示解耦；提取失败时断言消息携带字段转储以便定位。
/// </remarks>
public class RedisTokenStoreCodecTtlTests
{
    private readonly Mock<IConnectionMultiplexer> _connectionMultiplexerMock;
    private readonly Mock<IDatabase> _databaseMock;
    private readonly Mock<ILogger<RedisTokenStore>> _loggerMock;

    public RedisTokenStoreCodecTtlTests()
    {
        _connectionMultiplexerMock = new Mock<IConnectionMultiplexer>();
        _databaseMock = new Mock<IDatabase>();
        _loggerMock = new Mock<ILogger<RedisTokenStore>>();

        _connectionMultiplexerMock
            .Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_databaseMock.Object);

        _databaseMock
            .Setup(x => x.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);
    }

    private RedisTokenStore CreateStore() => new(_connectionMultiplexerMock.Object, _loggerMock.Object);

    private bool WasKeyDeleted() =>
        _databaseMock.Invocations.Any(i => i.Method.Name == "KeyDeleteAsync");

    /// <summary>从 StringSetAsync 调用记录的第三参数（Expiration）提取 TTL 数值。</summary>
    private TimeSpan? GetWrittenTtl()
    {
        var invocation = _databaseMock.Invocations.FirstOrDefault(i => i.Method.Name == "StringSetAsync");
        if (invocation is null || invocation.Arguments.Count < 3 || invocation.Arguments[2] is null)
            return null;

        var expiration = invocation.Arguments[2]!;
        var fields = expiration.GetType().GetFields(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        long? rawValue = null;
        var flagsText = string.Empty;
        foreach (var field in fields)
        {
            var value = field.GetValue(expiration);
            if (value is null)
                continue;
            if (value is TimeSpan ts)
                return ts;
            if (value is long l && field.Name == "_value")
                rawValue = l;
            if (field.Name == "_flags")
                flagsText = value.ToString() ?? string.Empty;
        }

        // SE.Redis 3.3.0 Expiration 内部布局（实测转储）：_value 数值 + _flags 标志；
        // _flags 含 IsMillis 时 _value 为毫秒，否则为秒（如 30 天 = 2,592,000）。
        return rawValue.HasValue && flagsText.Contains("IsMillis")
            ? TimeSpan.FromMilliseconds(rawValue.Value)
            : rawValue.HasValue ? TimeSpan.FromSeconds(rawValue.Value) : null;

        return TimeSpan.MinValue;   // 哨兵：字段布局变化（断言消息可见）
    }

    [Fact]
    public async Task SetRefreshTokenAsync_RelativeSecondsValue_ShouldDeriveTtlFromProjectedExpiry()
    {
        // C-2 新格式（相对秒）：TTL ≈ 剩余秒。
        await CreateStore().SetRefreshTokenAsync("app", "7200|refresh-token");

        Assert.False(WasKeyDeleted());
        var ttl = GetWrittenTtl();
        Assert.True(
            ttl.HasValue && ttl != TimeSpan.MinValue,
            "Expiration 字段布局变化: " + DumpExpiration());
        Assert.InRange(ttl!.Value.TotalSeconds, 7199, 7201);
    }

    [Fact]
    public async Task SetRefreshTokenAsync_LegacyFutureMs_ShouldDeriveTtlFromAbsoluteValue()
    {
        // 旧格式（绝对毫秒戳，未过期）：TTL = 原值 − now（存量数据兼容，无需回灌）。
        var futureMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 3_600_000;
        await CreateStore().SetRefreshTokenAsync("app", $"{futureMs}|refresh-token");

        Assert.False(WasKeyDeleted());
        var ttl = GetWrittenTtl();
        Assert.True(
            ttl.HasValue && ttl != TimeSpan.MinValue,
            "Expiration 字段布局变化: " + DumpExpiration());
        Assert.InRange(ttl!.Value.TotalMinutes, 59, 60);
    }

    [Fact]
    public async Task SetRefreshTokenAsync_LegacyPastMs_ShouldDeleteInsteadOfWriting()
    {
        // 旧格式已过期毫秒戳 → 删键（TMA2-15：Redis TTL=0 等于永不过期，不得写）。
        var pastMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - 60_000;
        await CreateStore().SetRefreshTokenAsync("app", $"{pastMs}|refresh-token");

        Assert.True(WasKeyDeleted());
        Assert.Null(GetWrittenTtl());
    }

    [Fact]
    public async Task SetRefreshTokenAsync_NoTtlInfo_ShouldFallbackTo30Days()
    {
        // C-2 行为修正：前导数 0 = 「无 TTL 信息」→ 回落 30 天（不再误判为已过期删键）。
        await CreateStore().SetRefreshTokenAsync("app", "0|refresh-token");

        Assert.False(WasKeyDeleted());
        var ttl = GetWrittenTtl();
        Assert.True(
            ttl == TimeSpan.FromDays(30),
            "ttl=" + ttl + " dump=" + DumpExpiration());
    }

    [Fact]
    public async Task SetRefreshTokenAsync_UnparseableValue_ShouldFallbackTo30Days()
    {
        await CreateStore().SetRefreshTokenAsync("app", "plain-refresh-token");

        Assert.False(WasKeyDeleted());
        Assert.Equal(TimeSpan.FromDays(30), GetWrittenTtl());
    }

    private string DumpExpiration()
    {
        var invocation = _databaseMock.Invocations.FirstOrDefault(i => i.Method.Name == "StringSetAsync");
        if (invocation is null)
            return "<no StringSetAsync invocation>";
        var expiration = invocation.Arguments.Count > 2 ? invocation.Arguments[2] : null;
        if (expiration is null)
            return "<null expiration>";
        return expiration.GetType().GetFields(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Select(f => $"{f.Name}={f.GetValue(expiration)}")
            .Aggregate((a, b) => a + ", " + b);
    }
}
