// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Utilities;
using Xunit;

#pragma warning disable CS0618

namespace Mud.Feishu.Abstractions.Tests.Utilities;

/// <summary>
/// TokenUtils 单元测试
/// </summary>
public class TokenUtilsTests
{
    #region FormatBearerToken 测试

    [Fact]
    public void FormatBearerToken_WithNullToken_ShouldReturnBearerPrefix()
    {
        // Arrange
        string? token = null;

        // Act
        var result = TokenUtils.FormatBearerToken(token);

        // Assert
        Assert.Equal("Bearer ", result);
    }

    [Fact]
    public void FormatBearerToken_WithEmptyToken_ShouldReturnBearerPrefix()
    {
        // Arrange
        var token = "";

        // Act
        var result = TokenUtils.FormatBearerToken(token);

        // Assert
        Assert.Equal("Bearer ", result);
    }

    [Fact]
    public void FormatBearerToken_WithRawToken_ShouldAddBearerPrefix()
    {
        // Arrange
        var token = "abc123";

        // Act
        var result = TokenUtils.FormatBearerToken(token);

        // Assert
        Assert.Equal("Bearer abc123", result);
    }

    [Fact]
    public void FormatBearerToken_WithBearerPrefix_ShouldNotDuplicate()
    {
        // Arrange
        var token = "Bearer abc123";

        // Act
        var result = TokenUtils.FormatBearerToken(token);

        // Assert
        Assert.Equal("Bearer abc123", result);
    }

    [Fact]
    public void FormatBearerToken_WithLowerCaseBearer_ShouldNotDuplicate()
    {
        // Arrange
        var token = "bearer abc123";

        // Act
        var result = TokenUtils.FormatBearerToken(token);

        // Assert
        Assert.Equal("bearer abc123", result);
    }

    #endregion

    #region RemoveBearerPrefix 测试

    [Fact]
    public void RemoveBearerPrefix_WithNullToken_ShouldReturnEmpty()
    {
        // Arrange
        string? token = null;

        // Act
        var result = TokenUtils.RemoveBearerPrefix(token);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void RemoveBearerPrefix_WithEmptyToken_ShouldReturnEmpty()
    {
        // Arrange
        var token = "";

        // Act
        var result = TokenUtils.RemoveBearerPrefix(token);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void RemoveBearerPrefix_WithBearerToken_ShouldRemovePrefix()
    {
        // Arrange
        var token = "Bearer abc123";

        // Act
        var result = TokenUtils.RemoveBearerPrefix(token);

        // Assert
        Assert.Equal("abc123", result);
    }

    [Fact]
    public void RemoveBearerPrefix_WithRawToken_ShouldReturnSame()
    {
        // Arrange
        var token = "abc123";

        // Act
        var result = TokenUtils.RemoveBearerPrefix(token);

        // Assert
        Assert.Equal("abc123", result);
    }

    [Fact]
    public void RemoveBearerPrefix_WithLowerCaseBearer_ShouldRemovePrefix()
    {
        // Arrange
        var token = "bearer abc123";

        // Act
        var result = TokenUtils.RemoveBearerPrefix(token);

        // Assert
        Assert.Equal("abc123", result);
    }

    #endregion

    #region 双向转换测试

    [Fact]
    public void FormatAndRemove_ShouldBeReversible()
    {
        // Arrange
        var originalToken = "test-token-123";

        // Act
        var formatted = TokenUtils.FormatBearerToken(originalToken);
        var removed = TokenUtils.RemoveBearerPrefix(formatted);

        // Assert
        Assert.Equal("Bearer test-token-123", formatted);
        Assert.Equal(originalToken, removed);
    }

    #endregion
}
