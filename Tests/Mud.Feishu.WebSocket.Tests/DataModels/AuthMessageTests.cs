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
/// AuthMessage 认证消息测试类
/// </summary>
public class AuthMessageTests
{
    [Fact]
    public void Constructor_ShouldSetTypeToAuth()
    {
        // Arrange & Act
        var message = new AuthMessage();

        // Assert
        message.Type.Should().Be("auth");
    }

    [Fact]
    public void Constructor_ShouldSetTimestampToZero()
    {
        // Arrange & Act
        var message = new AuthMessage();

        // Assert
        message.Timestamp.Should().Be(0);
    }

    [Fact]
    public void Constructor_ShouldSetDataToNull()
    {
        // Arrange & Act
        var message = new AuthMessage();

        // Assert
        message.Data.Should().BeNull();
    }

    [Fact]
    public void Data_ShouldAcceptValidAuthData()
    {
        // Arrange
        var message = new AuthMessage();
        var authData = new AuthData
        {
            AppAccessToken = "test-access-token",
            AppId = "cli_1234567890"
        };

        // Act
        message.Data = authData;

        // Assert
        message.Data.Should().NotBeNull();
        message.Data!.AppAccessToken.Should().Be("test-access-token");
        message.Data.AppId.Should().Be("cli_1234567890");
    }

    [Fact]
    public void Data_ShouldAcceptNull()
    {
        // Arrange
        var message = new AuthMessage();

        // Act
        message.Data = null;

        // Assert
        message.Data.Should().BeNull();
    }

    [Fact]
    public void Serialization_ShouldSerializeToJson()
    {
        // Arrange
        var message = new AuthMessage
        {
            Type = "auth",
            Timestamp = 1234567890,
            Data = new AuthData
            {
                AppAccessToken = "cli_1234567890abcd",
                AppId = "cli_1234567890"
            }
        };

        // Act
        var json = JsonSerializer.Serialize(message, JsonOptions.Default);

        // Assert
        json.Should().Contain("\"type\":\"auth\"");
        json.Should().Contain("\"timestamp\":1234567890");
        json.Should().Contain("\"app_access_token\":\"cli_1234567890abcd\"");
    }

    [Fact]
    public void Deserialization_ShouldDeserializeFromJson()
    {
        // Arrange
        var json = "{\"type\":\"auth\",\"timestamp\":1234567890,\"data\":{\"app_access_token\":\"cli_1234567890abcd\",\"app_id\":\"cli_1234567890\"}}";

        // Act
        var message = JsonSerializer.Deserialize<AuthMessage>(json, JsonOptions.Default);

        // Assert
        message.Should().NotBeNull();
        message!.Type.Should().Be("auth");
        message.Timestamp.Should().Be(1234567890);
        message.Data.Should().NotBeNull();
        message.Data!.AppAccessToken.Should().Be("cli_1234567890abcd");
    }

    [Fact]
    public void Deserialization_ShouldHandleNullData()
    {
        // Arrange
        var json = "{\"type\":\"auth\",\"timestamp\":1234567890,\"data\":null}";

        // Act
        var message = JsonSerializer.Deserialize<AuthMessage>(json, JsonOptions.Default);

        // Assert
        message.Should().NotBeNull();
        message!.Type.Should().Be("auth");
        message.Data.Should().BeNull();
    }

    [Fact]
    public void Deserialization_ShouldHandleMissingData()
    {
        // Arrange
        var json = "{\"type\":\"auth\",\"timestamp\":1234567890}";

        // Act
        var message = JsonSerializer.Deserialize<AuthMessage>(json, JsonOptions.Default);

        // Assert
        message.Should().NotBeNull();
        message!.Type.Should().Be("auth");
        message.Data.Should().BeNull();
    }

    [Fact]
    public void Deserialization_ShouldHandleEmptyData()
    {
        // Arrange
        var json = "{\"type\":\"auth\",\"data\":{},\"timestamp\":1234567890}";

        // Act
        var message = JsonSerializer.Deserialize<AuthMessage>(json, JsonOptions.Default);

        // Assert
        message.Should().NotBeNull();
        message!.Data.Should().NotBeNull();
        message.Data!.AppAccessToken.Should().Be(string.Empty);
    }

    [Fact]
    public void Serialization_ShouldHandleNullData()
    {
        // Arrange
        var message = new AuthMessage
        {
            Type = "auth",
            Timestamp = 1234567890,
            Data = null
        };

        // Act
        var json = JsonSerializer.Serialize(message, JsonOptions.Default);

        // Assert - null values are omitted by default in JSON serialization
        json.Should().Contain("\"type\"");
        json.Should().Contain("\"timestamp\"");
        json.Should().NotContain("\"data\"");
    }

    [Fact]
    public void ShouldPreserveTypeProperty_WhenSetToDifferentValue()
    {
        // Arrange
        var message = new AuthMessage();
        var originalType = message.Type;

        // Act
        message.Type = "custom_type";
        var modifiedType = message.Type;

        // Assert
        originalType.Should().Be("auth");
        modifiedType.Should().Be("custom_type");
    }
}
