// -----------------------------------------------------------------------
//  作者:Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Reflection;

namespace Mud.Feishu.AI.Tests.Conversations;

/// <summary>
/// <see cref="SessionStoreEncoding"/> 编解码与 TMA-15 语义（无过期时间戳视为 miss）。
/// </summary>
public class SessionStoreEncodingTests
{
    [Fact]
    public void EncodeDecode_ShouldRoundTrip()
    {
        var payload = "{\"some\":\"session-json\"}";
        var stored = SessionStoreEncoding.Encode(payload, 1234567890123);

        SessionStoreEncoding.TryDecode(stored, out var decoded, out var expireAtMs)
            .Should().BeTrue();
        decoded.Should().Be(payload);
        expireAtMs.Should().Be(1234567890123);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("no-separator-no-timestamp")]
    [InlineData("|payload-without-timestamp")]
    [InlineData("0|zero-timestamp")]
    [InlineData("abc|not-a-number")]
    [InlineData("123456|")]
    public void TryDecode_ShouldTreatAsMiss_WhenNoValidExpireTimestamp(string? storedValue)
    {
        // TMA-15：存储值必须自带过期时间戳；无时间戳/损坏 → miss，调用方重建会话。
        SessionStoreEncoding.TryDecode(storedValue, out var payload, out _)
            .Should().BeFalse();
        payload.Should().BeNull();
    }

    [Fact]
    public void Encode_ShouldRejectInvalidInput()
    {
        var actPayload = () => SessionStoreEncoding.Encode("", 1000);
        actPayload.Should().Throw<ArgumentException>();

        var actTimestamp = () => SessionStoreEncoding.Encode("{}", 0);
        actTimestamp.Should().Throw<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// 格式同族契约守卫（Phase 0 §3.3 评审建议 b；C-2 后判据改为「同族」而非「逐字节相同」）。
    /// <b>必须保留的差异</b>：令牌侧自 C-2 起写相对秒，会话侧仍写<b>绝对毫秒戳</b>——
    /// 相对秒在读侧会被逐次「延寿」，直接摧毁 TMA-15 的读侧有效性权威。
    /// 故本守卫锁的是同一惯例：两侧均为 <c>{数值}|{payload}</c>，且互读得到同一绝对毫秒戳。
    /// </summary>
    [Fact]
    public void SessionStoreEncoding_ShouldStayCompatible_WithTokenStoreHelperFormat()
    {
        var abstractions = typeof(Mud.Feishu.Abstractions.Observability.FeishuActivitySource).Assembly;
        var helperType = abstractions.GetType("Mud.Feishu.Abstractions.Authentication.TokenStoreHelper");
        helperType.Should().NotBeNull("TokenStoreHelper 是格式惯例的来源，若被重命名/移动请同步本守卫");

        var encode = helperType!.GetMethod("EncodeStoredToken", BindingFlags.Public | BindingFlags.Static);
        var decode = helperType.GetMethod("DecodeStoredToken", BindingFlags.Public | BindingFlags.Static);
        encode.Should().NotBeNull();
        decode.Should().NotBeNull();

        var payload = "token-or-session-payload|with-pipe";
        var expireMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 600_000;

        var sessionEncoded = SessionStoreEncoding.Encode(payload, expireMs);
        var tokenEncoded = (string)encode!.Invoke(null, [payload, expireMs])!;

        // ① 令牌侧双读必须能读出会话值：形态（时间戳段在前 + '|' 分隔）与单位（绝对毫秒）一致。
        var (sessionToken, sessionExpireMs) = ((string, long))decode!.Invoke(null, [sessionEncoded])!;
        sessionToken.Should().Be(payload, "会话存储值必须能被令牌侧解码出原载荷");
        sessionExpireMs.Should().Be(expireMs, "会话存储值必须能被令牌侧解出同一绝对过期时刻");

        // ② 反向：令牌侧相对秒值解码后必须投影回同一过期时刻（1 秒舍入口径内）。
        var (tokenToken, tokenExpireMs) = ((string, long))decode!.Invoke(null, [tokenEncoded])!;
        tokenToken.Should().Be(payload);
        Math.Abs(tokenExpireMs - expireMs).Should().BeLessThanOrEqualTo(1000,
            "相对秒经双读投影后必须回到同一绝对过期时刻（±1s 舍入）");
    }
}
