// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Mud.Feishu.Abstractions.Authentication;

namespace Mud.Feishu.Abstractions.Tests.Authentication;

/// <summary>
/// TMR-P2-9（F9）TokenKeyBuilder 异常类型回归测试。
/// </summary>
/// <remarks>
/// 超长键段是<b>输入校验失败</b>（外部可控 userId 可触发）而非"对象处于无效状态"，
/// 抛 <see cref="ArgumentException"/> 而非 <see cref="InvalidOperationException"/>，
/// 脱离与瞬时白名单（IsTransientInitFailure）的语义纠缠。
/// 同时回归 D8 契约（TMA2-02）：合法段的键布局逐字节不变。
/// </remarks>
public class TokenKeyBuilderTmrTests
{
    [Theory]
    [InlineData(256, false)]  // 上界内（含）合法
    [InlineData(257, true)]   // 超上界抛异常
    public void TenantAccessKey_OverlongSegment_ShouldThrowArgumentException(int segmentLength, bool shouldThrow)
    {
        // Arrange
        var segment = new string('u', segmentLength);

        // Act
        var act = () => TokenKeyBuilder.TenantAccessKey("feishu:cli_a:token", segment);

        // Assert
        if (shouldThrow)
        {
            act.Should().Throw<ArgumentException>().WithParameterName("segment");
            act.Should().Throw<ArgumentException>()
                .And.Message.Should().Contain("超过上限");
        }
        else
        {
            act.Should().NotThrow();
        }
    }

    [Fact]
    public void UserAccessKey_OverlongUserId_ShouldThrowArgumentException_NotInvalidOperation()
    {
        // Arrange：外部可控 userId 触发超长段。
        var longUserId = new string('u', 500);

        // Act
        var act = () => TokenKeyBuilder.UserAccessKey("feishu:cli_a:token", longUserId, "UserAccessToken:a");

        // Assert：修复前抛 InvalidOperationException（会被瞬时白名单误判为可重试）。
        act.Should().Throw<ArgumentException>();
        act.Should().NotThrow<InvalidOperationException>();
    }

    [Fact]
    public void TenantAccessKey_LegalSegment_ShouldProduceStableKeyLayout()
    {
        // Arrange + Act：D8 回归——合法键布局逐字节不变。
        var key1 = TokenKeyBuilder.TenantAccessKey("feishu:cli_a:token", "tenant:cli_a");
        var key2 = TokenKeyBuilder.TenantAccessKey("feishu:cli_a:token", "tenant:cli_a");

        // Assert
        key1.Should().Be(key2);
        key1.Should().Be("feishu:cli_a:token:tenant\\:cli_a:access");
    }

    [Fact]
    public void TenantScanPattern_OverlongPrefixSegment_ShouldThrowArgumentException()
    {
        // Arrange：NormalizePrefix 同样经 NormalizeSegment 校验（前缀段超长）。
        var longPrefix = new string('p', 300);

        // Act + Assert
        var act = () => TokenKeyBuilder.TenantScanPattern(longPrefix);
        act.Should().Throw<ArgumentException>();
    }
}
