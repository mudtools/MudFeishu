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
    /// 格式等价契约守卫（Phase 0 §3.3 评审建议 b）：
    /// <c>SessionStoreEncoding</c> 与 Abstractions 的 internal <c>TokenStoreHelper</c>
    /// 必须对同一输入产出逐字节一致的编码——防止两侧格式漂移。
    /// </summary>
    [Fact]
    public void SessionStoreEncoding_ShouldStayByteEquivalent_WithTokenStoreHelperFormat()
    {
        var abstractions = typeof(Mud.Feishu.Abstractions.Observability.FeishuActivitySource).Assembly;
        var helperType = abstractions.GetType("Mud.Feishu.Abstractions.Authentication.TokenStoreHelper");
        helperType.Should().NotBeNull("TokenStoreHelper 是格式惯例的来源，若被重命名/移动请同步本守卫");

        var encode = helperType!.GetMethod("EncodeStoredToken", BindingFlags.Public | BindingFlags.Static);
        encode.Should().NotBeNull();

        var payload = "token-or-session-payload|with-pipe";
        var expireMs = 1727136000000L;

        var tokenEncoded = (string)encode!.Invoke(null, [payload, expireMs])!;
        var sessionEncoded = SessionStoreEncoding.Encode(payload, expireMs);

        tokenEncoded.Should().Be(sessionEncoded, "{expireTimestampMs}|{payload} 格式必须在令牌与会话两条存储路径间保持逐字节一致");
    }
}
