// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Utilities;
using Xunit;

namespace Mud.Feishu.Abstractions.Tests.Utilities;

/// <summary>
/// LogSanitizer 单元测试（WHF-06）
/// 验证日志清洗：控制符/换行替换、超长截断、边界输入
/// </summary>
public class LogSanitizerTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Clean_WithNullOrEmpty_ShouldReturnEmpty(string? value)
    {
        Assert.Equal(string.Empty, LogSanitizer.Clean(value));
    }

    [Fact]
    public void Clean_WithSafeCharacters_ShouldReturnUnchanged()
    {
        var value = "abc-XYZ_123.=90";

        Assert.Equal(value, LogSanitizer.Clean(value));
    }

    [Theory]
    [InlineData("nonce\ninjected", "nonce?injected")]
    [InlineData("nonce\r\nlog-forgery", "nonce??log-forgery")]
    [InlineData("nonce\ttab", "nonce?tab")]
    [InlineData("quote\"braces{}", "quote?braces??")]
    [InlineData("控制字符\u0001", "控制字符?")]
    public void Clean_WithUnsafeCharacters_ShouldReplaceThem(string value, string expected)
    {
        Assert.Equal(expected, LogSanitizer.Clean(value));
    }

    [Fact]
    public void Clean_WithOverlongValue_ShouldTruncateWithEllipsis()
    {
        var value = new string('a', 100);

        var cleaned = LogSanitizer.Clean(value, 64);

        Assert.Equal(64 + 1, cleaned.Length); // 64 字符 + 省略号
        Assert.EndsWith("…", cleaned);
        Assert.StartsWith(new string('a', 64), cleaned);
    }

    [Fact]
    public void Clean_WithExactlyMaxLength_ShouldNotTruncate()
    {
        var value = new string('a', 64);

        Assert.Equal(value, LogSanitizer.Clean(value, 64));
    }
}
