// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证 位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Mud.Feishu.WebSocket.Exceptions;

namespace Mud.Feishu.WebSocket.Tests.Exceptions;

/// <summary>
/// FeishuConnectionException 连接异常测试类
/// </summary>
public class FeishuConnectionExceptionTests
{
    [Fact]
    public void Constructor_ShouldCreateExceptionWithMessage()
    {
        // Arrange
        var message = "Connection failed";

        // Act
        var exception = new FeishuConnectionException(message);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void Constructor_ShouldCreateExceptionWithMessageAndInnerException()
    {
        // Arrange
        var message = "Connection failed";
        var innerException = new TimeoutException("Connection timeout");

        // Act
        var exception = new FeishuConnectionException(message, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().Be(innerException);
    }

    [Fact]
    public void Exception_ShouldBeCaughtAsFeishuWebSocketException()
    {
        // Arrange
        var exception = new FeishuConnectionException("Test");

        // Act & Assert
        Action action = () => throw exception;

        action.Should().Throw<Mud.Feishu.Exceptions.FeishuException>().WithMessage("Test");
    }
}
