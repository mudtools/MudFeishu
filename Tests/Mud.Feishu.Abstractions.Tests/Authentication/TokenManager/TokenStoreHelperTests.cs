// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Authentication;

namespace Mud.Feishu.Abstractions.Tests.Authentication.TokenManager;

/// <summary>
/// C-2 裁决（Mud.HttpUtils 3.0.7）落地后的存储值编解码口径用例：
/// 写入单写相对秒（官方 DefaultTokenStoreCodec 形态）；读取双读兼容旧「绝对毫秒戳」存量值。
/// </summary>
public class TokenStoreHelperTests
{
    /// <summary>旧格式绝对毫秒戳样例（2023-11，量级 ≥ 10^10 阈值）。</summary>
    private const long LegacyExpireMs = 1700000000000;

    [Fact]
    public void EncodeStoredToken_ShouldWriteRelativeSecondsFormat()
    {
        // C-2：Encode 单写相对秒 {expiresInSeconds}|{token}（不再写绝对毫秒戳）。
        var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var result = TokenStoreHelper.EncodeStoredToken("abc123", nowMs + 3_600_000);

        var separatorIndex = result.IndexOf('|');
        Assert.True(separatorIndex > 0, $"应含分隔符: {result}");
        Assert.True(long.TryParse(result.Substring(0, separatorIndex), out var seconds));
        Assert.InRange(seconds, 3599, 3600);   // 剩余 1 小时（±1 秒舍入噪声）
        Assert.Equal("abc123", result.Substring(separatorIndex + 1));
        Assert.True(seconds < FeishuTokenStoreCodec.AbsoluteMsThreshold, "相对秒不得落入绝对毫秒戳量级");
    }

    [Fact]
    public void EncodeStoredToken_ZeroExpire_ShouldWriteNoTtlInfo()
    {
        // 无过期信息（expireMs <= 0）→ 相对秒 0（官方「无 TTL 信息」语义）。
        Assert.Equal("0|abc123", TokenStoreHelper.EncodeStoredToken("abc123", 0));
    }

    [Fact]
    public void DecodeStoredToken_ShouldParseLegacyAbsoluteMs()
    {
        // 旧格式（绝对毫秒戳）原样解析（存量数据兼容，无需回灌）。
        var (token, expireMs) = TokenStoreHelper.DecodeStoredToken("1700000000000|abc123");
        Assert.Equal("abc123", token);
        Assert.Equal(1700000000000, expireMs);
    }

    [Fact]
    public void DecodeStoredToken_ShouldProjectRelativeSecondsToExpireMs()
    {
        // 新格式（相对秒）→ 投影为 nowMs + seconds * 1000（消费方「绝对毫秒戳」契约不变）。
        var before = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var (token, expireMs) = TokenStoreHelper.DecodeStoredToken("7200|abc123");
        var after = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        Assert.Equal("abc123", token);
        Assert.InRange(expireMs, before + 7_200_000, after + 7_200_000);
    }

    [Fact]
    public void DecodeStoredToken_ZeroLeading_ShouldProjectToNoTtlInfo()
    {
        // 前导数 0 = 官方「无 TTL 信息」→ 投影为 0（由调用方决定是否刷新）。
        var (token, expireMs) = TokenStoreHelper.DecodeStoredToken("0|abc123");
        Assert.Equal("abc123", token);
        Assert.Equal(0, expireMs);
    }

    [Fact]
    public void DecodeStoredToken_ShouldReturnOriginalValue_WhenNoSeparator()
    {
        var (token, expireMs) = TokenStoreHelper.DecodeStoredToken("plain_token");
        Assert.Equal("plain_token", token);
        Assert.Equal(0, expireMs);
    }

    [Fact]
    public void DecodeStoredToken_ShouldReturnOriginalValue_WhenSeparatorAtStart()
    {
        var (token, expireMs) = TokenStoreHelper.DecodeStoredToken("|token_value");
        Assert.Equal("|token_value", token);
        Assert.Equal(0, expireMs);
    }

    [Fact]
    public void DecodeStoredToken_ShouldReturnOriginalValue_WhenPrefixIsNotNumeric()
    {
        var (token, expireMs) = TokenStoreHelper.DecodeStoredToken("notanumber|token_value");
        Assert.Equal("notanumber|token_value", token);
        Assert.Equal(0, expireMs);
    }

    [Fact]
    public void DecodeStoredToken_ShouldHandleTokenContainingSeparator()
    {
        var (token, expireMs) = TokenStoreHelper.DecodeStoredToken("1700000000000|token|with|pipes");
        Assert.Equal("token|with|pipes", token);
        Assert.Equal(1700000000000, expireMs);
    }

    [Fact]
    public void EncodeThenDecode_ShouldRoundTrip()
    {
        // 相对秒格式的往返：解码投影回绝对毫秒戳，允许 ±2 秒舍入/时钟推进噪声。
        const string originalToken = "test_access_token_12345";
        var originalExpire = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 3_600_000;

        var encoded = TokenStoreHelper.EncodeStoredToken(originalToken, originalExpire);
        var (token, expireMs) = TokenStoreHelper.DecodeStoredToken(encoded);

        Assert.Equal(originalToken, token);
        Assert.InRange(expireMs, originalExpire - 2_000, originalExpire + 2_000);
    }

    [Fact]
    public void TryDecodeExpiry_LegacyPastMs_ShouldReturnTrueWithPastValue()
    {
        // 旧格式已过期毫秒戳 → true + 原值（Redis 侧据此删键，行为不变）。
        var ok = TokenStoreHelper.TryDecodeExpiry("1700000000000|abc123", out var expireMs);
        Assert.True(ok);
        Assert.Equal(1700000000000, expireMs);
    }

    [Fact]
    public void TryDecodeExpiry_RelativeSeconds_ShouldProjectToExpireMs()
    {
        var before = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var ok = TokenStoreHelper.TryDecodeExpiry("86400|abc123", out var expireMs);
        var after = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        Assert.True(ok);
        Assert.InRange(expireMs, before + 86_400_000, after + 86_400_000);
    }

    [Fact]
    public void TryDecodeExpiry_ZeroLeading_ShouldReturnFalse_NoTtlInfo()
    {
        // C-2 行为修正：前导数 0 = 「无 TTL 信息」→ false（Redis 侧回落 30 天），
        // 不再误判为「已过期」（expireMs=0 <= now）删键。
        var ok = TokenStoreHelper.TryDecodeExpiry("0|abc123", out var expireMs);
        Assert.False(ok);
        Assert.Equal(0, expireMs);
    }

    [Fact]
    public void TryDecodeExpiry_InvalidFormats_ShouldReturnFalse()
    {
        Assert.False(TokenStoreHelper.TryDecodeExpiry("plain_token", out _));
        Assert.False(TokenStoreHelper.TryDecodeExpiry("|abc123", out _));
        Assert.False(TokenStoreHelper.TryDecodeExpiry("notanumber|abc123", out _));
        Assert.False(TokenStoreHelper.TryDecodeExpiry(string.Empty, out _));
    }

    // ──────────────────────────────────────────────
    //  FeishuTokenStoreCodec（官方 ITokenStoreCodec 适配 / 互操作锚点）
    // ──────────────────────────────────────────────

    [Fact]
    public void Codec_Encode_ShouldMatchOfficialDefaultCodec()
    {
        // C-2：Encode 委派官方实现（逐字节一致）。
        Assert.Equal(
            DefaultTokenStoreCodec.Instance.Encode("tok", 3600),
            FeishuTokenStoreCodec.Instance.Encode("tok", 3600));
        Assert.Null(FeishuTokenStoreCodec.Instance.Encode(null, 3600));      // 墓碑
        Assert.Null(FeishuTokenStoreCodec.Instance.Encode(string.Empty, 3600));
    }

    [Fact]
    public void Codec_Decode_RelativeSeconds_ShouldPassThrough()
    {
        var (token, seconds) = FeishuTokenStoreCodec.Instance.Decode("7200|tok")!.Value;
        Assert.Equal("tok", token);
        Assert.Equal(7200, seconds);
    }

    [Fact]
    public void Codec_Decode_LegacyFutureMs_ShouldConvertToRemainingSeconds()
    {
        // 旧格式未过期毫秒戳 → 换算剩余秒（正数）。
        var futureMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 3_600_000;
        var (token, seconds) = FeishuTokenStoreCodec.Instance.Decode($"{futureMs}|tok")!.Value;
        Assert.Equal("tok", token);
        Assert.InRange(seconds, 3598, 3600);
    }

    [Fact]
    public void Codec_Decode_LegacyPastMs_ShouldReturnNonPositiveSeconds()
    {
        // 旧格式已过期毫秒戳 → 剩余秒为负（由调用方判过期）。
        var pastMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - 60_000;
        var (token, seconds) = FeishuTokenStoreCodec.Instance.Decode($"{pastMs}|tok")!.Value;
        Assert.Equal("tok", token);
        Assert.True(seconds < 0);
    }

    [Fact]
    public void Codec_Decode_Invalid_ShouldReturnNull()
    {
        Assert.Null(FeishuTokenStoreCodec.Instance.Decode(null));
        Assert.Null(FeishuTokenStoreCodec.Instance.Decode(string.Empty));
        Assert.Null(FeishuTokenStoreCodec.Instance.Decode("|tok"));
        Assert.Null(FeishuTokenStoreCodec.Instance.Decode("plain"));
        Assert.Null(FeishuTokenStoreCodec.Instance.Decode("nan|tok"));
    }

    [Theory]
    [InlineData(FeishuTokenStoreCodec.AbsoluteMsThreshold - 1)]    // 阈值之下 → 相对秒
    [InlineData(FeishuTokenStoreCodec.AbsoluteMsThreshold)]        // 阈值之上 → 绝对毫秒戳
    public void Codec_Decode_ThresholdBoundary_ShouldSwitchBranches(long leading)
    {
        var (token, seconds) = FeishuTokenStoreCodec.Instance.Decode($"{leading}|tok")!.Value;
        Assert.Equal("tok", token);
        if (leading >= FeishuTokenStoreCodec.AbsoluteMsThreshold)
        {
            // 绝对毫秒戳分支：10^10 ms 早于 1970-04 → 剩余秒为巨大负数。
            Assert.True(seconds < 0);
        }
        else
        {
            Assert.Equal(leading, seconds);
        }
    }

    // ──────────────────────────────────────────────
    //  U-1 回收守卫（上游 3.0.10 Batch 1）：薄包装必须与官方 MigrationTokenStoreCodec 同源
    // ──────────────────────────────────────────────

    [Fact]
    public void Codec_Decode_Should_Use_Feishu_Threshold_Explicitly()
    {
        // 薄包装显式传 Feishu 阈值（10^10），不依赖官方默认值。
        // 注意：官方 MigrationTokenStoreCodec.DefaultLegacyAbsoluteMillisecondsThreshold 实为 10^11
        // （源码常量 100_000_000_000L，而其 XML 注释写「默认 10^10」——上游文档笔误，已反馈）。
        // 两阈值在两真实形态下等价（相对秒 < 10^10，毫秒戳 > 10^12，中间空档无真实形态），故只断言行为同源。
        var officialWithFeishuThreshold = new MigrationTokenStoreCodec(FeishuTokenStoreCodec.AbsoluteMsThreshold);
        var futureMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 3_600_000;

        var feishu = FeishuTokenStoreCodec.Instance.Decode($"{futureMs}|tok")!.Value;
        var official = officialWithFeishuThreshold.Decode($"{futureMs}|tok")!.Value;

        Assert.Equal(feishu, official);
        Assert.NotEqual(MigrationTokenStoreCodec.DefaultLegacyAbsoluteMillisecondsThreshold, FeishuTokenStoreCodec.AbsoluteMsThreshold);
    }

    [Fact]
    public void Codec_Decode_Should_Delegate_To_Official_MigrationCodec()
    {
        // 双读结果必须与官方迁移 codec 一致（旧格式 floor 投影 / 新格式直读）。
        var futureMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 3_600_000;

        var feishu = FeishuTokenStoreCodec.Instance.Decode($"{futureMs}|tok")!.Value;
        var official = MigrationTokenStoreCodec.Instance.Decode($"{futureMs}|tok")!.Value;

        Assert.Equal(feishu.Token, official.Token);
        Assert.InRange(Math.Abs(feishu.ExpiresInSeconds - official.ExpiresInSeconds), 0, 1);

        var relative = FeishuTokenStoreCodec.Instance.Decode("7200|tok")!.Value;
        var officialRelative = MigrationTokenStoreCodec.Instance.Decode("7200|tok")!.Value;
        Assert.Equal(officialRelative, relative);
    }
}
