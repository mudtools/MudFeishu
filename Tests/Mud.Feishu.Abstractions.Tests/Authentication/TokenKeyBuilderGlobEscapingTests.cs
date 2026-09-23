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
/// P2-2：TokenKeyBuilder glob 元字符转义与往返测试。
/// 验证含 * ? [ ] 等 glob 元字符的 appKey/userId 在键构造时被正确转义，
/// 防止在 Redis SCAN 模式中注入通配符（TMF2-08）。
/// 同时验证 TryParseTenantTokenType 的往返一致性（D8 契约）。
/// </summary>
public class TokenKeyBuilderGlobEscapingTests
{
    [Theory]
    [InlineData('*', @"\*")]
    [InlineData('?', @"\?")]
    [InlineData('[', @"\[")]
    [InlineData(']', @"\]")]
    public void TenantAccessKey_ShouldEscapeGlobMetacharacterInAppKey(char metachar, string escaped)
    {
        // Arrange：appKey 含 glob 元字符
        var appKey = $"cli_{metachar}_app";
        var keyPrefix = TokenKeyBuilder.BuildKeyPrefix(appKey);
        var tokenType = "tenant:cli_a";

        // Act
        var key = TokenKeyBuilder.TenantAccessKey(keyPrefix, tokenType);

        // Assert：元字符被转义为 \{char}，键中不出现裸元字符段
        key.Should().Contain(escaped);
        key.Should().NotContain($"cli_{metachar}_app");  // 裸元字符段不应出现
    }

    [Fact]
    public void TenantAccessKey_ShouldEscapeAllGlobMetacharacters_WhenAppKeyContainsMultiple()
    {
        // Arrange
        var appKey = "cli_*?[].app";
        var keyPrefix = TokenKeyBuilder.BuildKeyPrefix(appKey);

        // Act
        var key = TokenKeyBuilder.TenantAccessKey(keyPrefix, "tenant:cli_a");

        // Assert：所有 glob 元字符均被转义
        key.Should().Contain(@"\*");
        key.Should().Contain(@"\?");
        key.Should().Contain(@"\[");
        key.Should().Contain(@"\]");
    }

    [Fact]
    public void TenantAccessKey_ShouldEscapeBackslashInAppKey()
    {
        // Arrange
        var appKey = @"cli_\_app";
        var keyPrefix = TokenKeyBuilder.BuildKeyPrefix(appKey);

        // Act
        var key = TokenKeyBuilder.TenantAccessKey(keyPrefix, "tenant:cli_a");

        // Assert：反斜杠自身被转义为 \\
        key.Should().Contain(@"\\");
    }

    [Fact]
    public void TenantAccessKey_ShouldEscapeColonInTokenType()
    {
        // Arrange：tokenType 含冒号（段内分隔符需转义）
        var appKey = "cli_special_app";
        var keyPrefix = TokenKeyBuilder.BuildKeyPrefix(appKey);

        // Act
        var key = TokenKeyBuilder.TenantAccessKey(keyPrefix, "tenant:cli_a");

        // Assert：tokenType 中的冒号被转义为 \:，不与段间分隔符混淆
        key.Should().Contain(@"tenant\:cli_a");
    }

    [Fact]
    public void TryParseTenantTokenType_ShouldRoundTrip_WhenAppKeyContainsGlobMetacharacters()
    {
        // Arrange
        var appKey = "cli_*?[].app";
        var keyPrefix = TokenKeyBuilder.BuildKeyPrefix(appKey);
        var originalTokenType = "tenant:cli_a";

        // Act
        var key = TokenKeyBuilder.TenantAccessKey(keyPrefix, originalTokenType);
        var parsed = TokenKeyBuilder.TryParseTenantTokenType(key, keyPrefix, out var tokenType);

        // Assert：往返一致——解析出的 tokenType 与原始值逐字节相同
        parsed.Should().BeTrue();
        tokenType.Should().Be(originalTokenType);
    }

    [Fact]
    public void TryParseTenantTokenType_ShouldRoundTrip_WhenTokenTypeContainsGlobMetacharacters()
    {
        // Arrange：tokenType 自身含 glob 元字符
        var appKey = "cli_normal_app";
        var keyPrefix = TokenKeyBuilder.BuildKeyPrefix(appKey);
        var originalTokenType = "tenant:cli_*?.special";

        // Act
        var key = TokenKeyBuilder.TenantAccessKey(keyPrefix, originalTokenType);
        var parsed = TokenKeyBuilder.TryParseTenantTokenType(key, keyPrefix, out var tokenType);

        // Assert
        parsed.Should().BeTrue();
        tokenType.Should().Be(originalTokenType);
    }

    [Fact]
    public void BuildKeyPrefix_ShouldProduceDifferentPrefixes_ForDifferentAppKeysWithGlobChars()
    {
        // Arrange：两个含 glob 元字符的不同 appKey
        var appKey1 = "cli_*_app";
        var appKey2 = "cli_?_app";

        // Act
        var prefix1 = TokenKeyBuilder.BuildKeyPrefix(appKey1);
        var prefix2 = TokenKeyBuilder.BuildKeyPrefix(appKey2);

        // Assert：不同 appKey → 不同前缀（无碰撞）
        prefix1.Should().NotBe(prefix2);
    }

    [Fact]
    public void TenantAccessKey_ShouldProduceDifferentKeys_ForDifferentAppKeysWithGlobChars()
    {
        // Arrange
        var keyPrefix1 = TokenKeyBuilder.BuildKeyPrefix("cli_*_app");
        var keyPrefix2 = TokenKeyBuilder.BuildKeyPrefix("cli_?_app");
        var tokenType = "tenant:cli_a";

        // Act
        var key1 = TokenKeyBuilder.TenantAccessKey(keyPrefix1, tokenType);
        var key2 = TokenKeyBuilder.TenantAccessKey(keyPrefix2, tokenType);

        // Assert
        key1.Should().NotBe(key2);
    }

    [Fact]
    public void TenantScanPattern_ShouldEscapeGlobMetacharactersInPrefix()
    {
        // Arrange：appKey 含 glob 元字符
        var appKey = "cli_*_app";
        var keyPrefix = TokenKeyBuilder.BuildKeyPrefix(appKey);

        // Act
        var scanPattern = TokenKeyBuilder.TenantScanPattern(keyPrefix);

        // Assert：前缀中的 * 被转义为 \*，仅尾随通配符 * 是裸的
        scanPattern.Should().Contain(@"\*");
        // 最终的通配符 * 是最后一个字符
        scanPattern.Should().EndWith("*");
        // 转义后的 \* 不应是最后一个字符（区分转义与通配）
        scanPattern.Should().NotEndWith(@"\*");
    }
}
