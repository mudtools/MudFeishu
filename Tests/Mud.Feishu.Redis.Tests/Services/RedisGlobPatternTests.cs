// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Mud.Feishu.Redis.Services;
using Xunit;

namespace Mud.Feishu.Redis.Tests.Services;

/// <summary>
/// TMR2-P1-2：<see cref="RedisGlobPattern"/> 直断言（字面量转义语义）。
/// </summary>
public class RedisGlobPatternTests
{
    [Theory]
    [InlineData(@"a\b", @"a\\b")]
    [InlineData("a*b", @"a\*b")]
    [InlineData("a?b", @"a\?b")]
    [InlineData("a[0]b", @"a\[0\]b")]
    [InlineData("plain:value", "plain:value")]
    [InlineData("", "")]
    public void EscapeLiteral_ShouldEscapeGlobMetacharacters(string literal, string expected)
    {
        RedisGlobPattern.EscapeLiteral(literal).Should().Be(expected);
    }

    [Fact]
    public void FromLiteralPrefix_ShouldAppendWildcard_AfterEscaping()
    {
        // 规范化前缀（TokenKeyBuilder 产出）中 '\' 已用于转义 ':'，故必须再转义一次
        RedisGlobPattern.FromLiteralPrefix(@"feishu:app\:with\:colon:token:")
            .Should().Be(@"feishu:app\\:with\\:colon:token:*");
    }

    [Fact]
    public void FromLiteralPrefix_ShouldReturnBareWildcard_ForEmptyPrefix()
        => RedisGlobPattern.FromLiteralPrefix(string.Empty).Should().Be("*");

    [Fact]
    public void EscapeLiteral_ShouldEscapeAllMetacharacters_InMixedInput()
    {
        RedisGlobPattern.EscapeLiteral(@"a*b?c[d]e\f").Should().Be(@"a\*b\?c\[d\]e\\f");
    }
}
