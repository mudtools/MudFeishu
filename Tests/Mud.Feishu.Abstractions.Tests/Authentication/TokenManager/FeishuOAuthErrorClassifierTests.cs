// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Mud.Feishu.Abstractions.Authentication;
using Xunit;

namespace Mud.Feishu.Abstractions.Tests.Authentication.TokenManager;

/// <summary>
/// TMR2-P1-4：OAuth 刷新失败分类器测试（D12 契约）。
/// </summary>
/// <remarks>
/// 分类结果直接驱动<b>销毁性动作</b>（<c>UserTokenManager</c> 删除 store 中的 refresh token，
/// 销毁用户唯一续期路径，必须重新走 OAuth 授权）。修复前关键字含
/// <c>"refresh token"</c> / <c>"scope"</c> 这类宽泛词且在<b>任意错误码下</b>生效，
/// 瞬时故障（如"failed to refresh token, please retry later"）会被误判为不可重试。
/// 判定顺序现为：显式错误码优先 → 收紧后的关键字兜底（仅码不可判定时）。
/// </remarks>
public class FeishuOAuthErrorClassifierTests
{
    // ============================================================
    // 显式可重试码优先于消息关键字（本轮核心修复）
    // ============================================================

    [Theory]
    [InlineData(500)]
    [InlineData(502)]
    [InlineData(503)]
    [InlineData(504)]
    [InlineData(99991400)]
    [InlineData(1061045)]
    public void IsUnretryable_ShouldReturnFalse_WhenExplicitRetryableCode(int errorCode)
    {
        FeishuOAuthErrorClassifier.IsUnretryable(
                errorCode, "failed to refresh token, please retry later")
            .Should().BeFalse(
                $"错误码 {errorCode} 是显式可重试码（瞬时故障），不得被消息中的 'refresh token' 关键字推翻");
    }

    [Fact]
    public void IsUnretryable_ShouldReturnFalse_WhenTransientCodeAndMsgMentionsScope()
        => FeishuOAuthErrorClassifier.IsUnretryable(503, "scope temporarily unavailable")
            .Should().BeFalse();

    // ============================================================
    // 显式不可重试码优先
    // ============================================================

    [Theory]
    [InlineData(40029)]
    [InlineData(99991661)]
    [InlineData(99991663)]
    [InlineData(99991664)]
    [InlineData(99991668)]
    public void IsUnretryable_ShouldReturnTrue_WhenExplicitUnretryableCode(int errorCode)
        => FeishuOAuthErrorClassifier.IsUnretryable(errorCode, null).Should().BeTrue();

    [Fact]
    public void IsUnretryable_ShouldReturnTrue_WhenCodeIsRefreshTokenRevoked()
        => FeishuOAuthErrorClassifier.IsUnretryable(99991664, "refresh token 已被吊销").Should().BeTrue();

    // ============================================================
    // 错误码不可判定时才用（收紧后的）关键字兜底
    // ============================================================

    [Fact]
    public void IsUnretryable_ShouldReturnTrue_WhenCodeUnknownAndMsgHasStrongKeyword()
        => FeishuOAuthErrorClassifier.IsUnretryable(12345, "refresh token 已被吊销")
            .Should().BeTrue("未知错误码下仍须保留强语义关键字的兜底（避免漏判不可重试）");

    [Fact]
    public void IsUnretryable_ShouldReturnFalse_WhenMsgMentionsScopeOnly()
        => FeishuOAuthErrorClassifier.IsUnretryable(null, "scope mismatch, please check")
            .Should().BeFalse("'scope' 已移出关键字集合（歧义过大，任意刷新消息都可能含此词）");

    [Fact]
    public void IsUnretryable_ShouldReturnFalse_WhenCodeIsZeroAndMsgMentionsRefreshToken()
        => FeishuOAuthErrorClassifier.IsUnretryable(0, "refresh token endpoint busy, retry later")
            .Should().BeFalse("错误码为 0（未提供有效码）时不得用 'refresh token' 判定不可重试");

    [Fact]
    public void IsUnretryable_ShouldReturnTrue_WhenCodeIsZeroAndMsgHasStrongKeyword()
        => FeishuOAuthErrorClassifier.IsUnretryable(0, "user not authorized for this app")
            .Should().BeTrue();

    // ============================================================
    // 无信息 → 可重试（保守：不清库）
    // ============================================================

    [Fact]
    public void IsUnretryable_ShouldReturnFalse_WhenAllInputsMissing()
        => FeishuOAuthErrorClassifier.IsUnretryable(null, null).Should().BeFalse();

    [Fact]
    public void IsUnretryable_ShouldReturnFalse_WhenMsgIsWhitespace()
        => FeishuOAuthErrorClassifier.IsUnretryable(null, "   ").Should().BeFalse();

    [Fact]
    public void IsUnretryable_ShouldReturnTrue_WhenMsgIsInvalidGrant()
        => FeishuOAuthErrorClassifier.IsUnretryable(null, "invalid_grant").Should().BeTrue();

    [Fact]
    public void IsUnretryable_ShouldBeCaseInsensitive_ForKeywords()
        => FeishuOAuthErrorClassifier.IsUnretryable(null, "INVALID_GRANT").Should().BeTrue();
}
