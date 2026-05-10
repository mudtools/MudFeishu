// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证 位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using System.Text.Json;
using Mud.Feishu.WebSocket.DataModels;

namespace Mud.Feishu.WebSocket.Tests.DataModels;

/// <summary>
/// FeishuWebSocketMessage 消息基类测试
/// </summary>
public class FeishuWebSocketMessageTests
{
    [Fact]
    public void Constructor_ShouldSetDefaultValues()
    {
        // Arrange & Act
        var message = new TestFeishuWebSocketMessage();

        // Assert
        message.Type.Should().Be(string.Empty);
        message.Timestamp.Should().Be(0);
    }

    [Fact]
    public void Properties_ShouldAcceptValidValues()
    {
        // Arrange
        var message = new TestFeishuWebSocketMessage();
        var type = "ping";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Act
        message.Type = type;
        message.Timestamp = timestamp;

        // Assert
        message.Type.Should().Be(type);
        message.Timestamp.Should().Be(timestamp);
    }

    [Fact]
    public void Properties_ShouldAcceptNullType()
    {
        // Arrange
        var message = new TestFeishuWebSocketMessage();

        // Act
        message.Type = null;

        // Assert
        message.Type.Should().BeNull();
    }

    [Fact]
    public void Properties_ShouldAcceptEmptyType()
    {
        // Arrange
        var message = new TestFeishuWebSocketMessage();

        // Act
        message.Type = "";

        // Assert
        message.Type.Should().Be(string.Empty);
    }

    [Fact]
    public void Properties_ShouldAcceptWhitespaceType()
    {
        // Arrange
        var message = new TestFeishuWebSocketMessage();
        var type = "   ";

        // Act
        message.Type = type;

        // Assert
        message.Type.Should().Be(type);
    }

    [Fact]
    public void Properties_ShouldAcceptNegativeTimestamp()
    {
        // Arrange
        var message = new TestFeishuWebSocketMessage();

        // Act
        message.Timestamp = -1;

        // Assert
        message.Timestamp.Should().Be(-1);
    }

    [Fact]
    public void Properties_ShouldAcceptZeroTimestamp()
    {
        // Arrange
        var message = new TestFeishuWebSocketMessage();

        // Act
        message.Timestamp = 0;

        // Assert
        message.Timestamp.Should().Be(0);
    }

    [Fact]
    public void Properties_ShouldAcceptVeryLargeTimestamp()
    {
        // Arrange
        var message = new TestFeishuWebSocketMessage();

        // Act
        message.Timestamp = long.MaxValue;

        // Assert
        message.Timestamp.Should().Be(long.MaxValue);
    }

    [Fact]
    public void Serialization_ShouldSerializeToJson()
    {
        // Arrange
        var message = new TestFeishuWebSocketMessage
        {
            Type = "ping",
            Timestamp = 1234567890
        };

        // Act
        var json = JsonSerializer.Serialize(message, JsonOptions.Default);

        // Assert
        json.Should().Contain("\"type\":\"ping\"");
        json.Should().Contain("\"timestamp\":1234567890");
    }

    [Fact]
    public void Deserialization_ShouldDeserializeFromJson()
    {
        // Arrange
        var json = "{\"type\":\"ping\",\"timestamp\":1234567890}";

        // Act
        var message = JsonSerializer.Deserialize<TestFeishuWebSocketMessage>(json, JsonOptions.Default);

        // Assert
        message.Should().NotBeNull();
        message!.Type.Should().Be("ping");
        message.Timestamp.Should().Be(1234567890);
    }

    [Fact]
    public void Deserialization_ShouldHandleNullType()
    {
        // Arrange
        var json = "{\"type\":null,\"timestamp\":1234567890}";

        // Act
        var message = JsonSerializer.Deserialize<TestFeishuWebSocketMessage>(json, JsonOptions.Default);

        // Assert
        message.Should().NotBeNull();
        message!.Type.Should().BeNull();
        message.Timestamp.Should().Be(1234567890);
    }

    [Fact]
    public void Deserialization_ShouldHandleMissingType()
    {
        // Arrange
        var json = "{\"timestamp\":1234567890}";

        // Act
        var message = JsonSerializer.Deserialize<TestFeishuWebSocketMessage>(json, JsonOptions.Default);

        // Assert
        message.Should().NotBeNull();
        // When type is missing, it defaults to empty string, not null
        message!.Type.Should().BeEmpty();
        message.Timestamp.Should().Be(1234567890);
    }

    [Fact]
    public void Deserialization_ShouldHandleMissingTimestamp()
    {
        // Arrange
        var json = "{\"type\":\"ping\"}";

        // Act
        var message = JsonSerializer.Deserialize<TestFeishuWebSocketMessage>(json, JsonOptions.Default);

        // Assert
        message.Should().NotBeNull();
        message!.Type.Should().Be("ping");
        message.Timestamp.Should().Be(0);
    }

    [Fact]
    public void Deserialization_ShouldHandleEmptyJson()
    {
        // Arrange
        var json = "{}";

        // Act
        var message = JsonSerializer.Deserialize<TestFeishuWebSocketMessage>(json, JsonOptions.Default);

        // Assert
        message.Should().NotBeNull();
        // When type is missing, it defaults to empty string, not null
        message!.Type.Should().BeEmpty();
        message.Timestamp.Should().Be(0);
    }

    // Test class implementing the abstract base class
    private class TestFeishuWebSocketMessage : FeishuWebSocketMessage
    {
    }
}
