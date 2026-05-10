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
/// AuthData 认证数据测试类
/// </summary>
public class AuthDataTests
{
    [Fact]
    public void Constructor_ShouldSetDefaultValues()
    {
        // Arrange & Act
        var authData = new AuthData();

        // Assert
        authData.AppAccessToken.Should().Be(string.Empty);
        authData.AppId.Should().BeNull();
        authData.EventUrl.Should().BeNull();
        authData.SessionId.Should().BeNull();
    }

    [Fact]
    public void AppAccessToken_ShouldAcceptValidValue()
    {
        // Arrange
        var authData = new AuthData();
        var token = "cli_1234567890abcdefghij";

        // Act
        authData.AppAccessToken = token;

        // Assert
        authData.AppAccessToken.Should().Be(token);
    }

    [Fact]
    public void AppAccessToken_ShouldAcceptNull()
    {
        // Arrange
        var authData = new AuthData();

        // Act
        authData.AppAccessToken = null;

        // Assert
        authData.AppAccessToken.Should().BeNull();
    }

    [Fact]
    public void AppAccessToken_ShouldAcceptEmptyString()
    {
        // Arrange
        var authData = new AuthData();

        // Act
        authData.AppAccessToken = "";

        // Assert
        authData.AppAccessToken.Should().Be(string.Empty);
    }

    [Fact]
    public void AppAccessToken_ShouldAcceptWhitespace()
    {
        // Arrange
        var authData = new AuthData();

        // Act
        authData.AppAccessToken = "   ";

        // Assert
        authData.AppAccessToken.Should().Be("   ");
    }

    [Fact]
    public void AppId_ShouldAcceptValidValue()
    {
        // Arrange
        var authData = new AuthData();
        var appId = "cli_1234567890";

        // Act
        authData.AppId = appId;

        // Assert
        authData.AppId.Should().Be(appId);
    }

    [Fact]
    public void EventUrl_ShouldAcceptValidValue()
    {
        // Arrange
        var authData = new AuthData();
        var eventUrl = "https://open.feishu.cn/open-apis/event";

        // Act
        authData.EventUrl = eventUrl;

        // Assert
        authData.EventUrl.Should().Be(eventUrl);
    }

    [Fact]
    public void SessionId_ShouldAcceptValidValue()
    {
        // Arrange
        var authData = new AuthData();
        var sessionId = "sess_1234567890";

        // Act
        authData.SessionId = sessionId;

        // Assert
        authData.SessionId.Should().Be(sessionId);
    }

    [Fact]
    public void Serialization_ShouldSerializeToJson()
    {
        // Arrange
        var authData = new AuthData
        {
            AppAccessToken = "cli_1234567890abcd",
            AppId = "cli_1234567890",
            EventUrl = "https://open.feishu.cn/open-apis/event",
            SessionId = "sess_1234567890"
        };

        // Act
        var json = JsonSerializer.Serialize(authData, JsonOptions.Default);

        // Assert
        json.Should().Contain("\"app_access_token\":\"cli_1234567890abcd\"");
        json.Should().Contain("\"app_id\":\"cli_1234567890\"");
        json.Should().Contain("\"event_url\":\"https://open.feishu.cn/open-apis/event\"");
        json.Should().Contain("\"session_id\":\"sess_1234567890\"");
    }

    [Fact]
    public void Deserialization_ShouldDeserializeFromJson()
    {
        // Arrange
        var json = "{\"app_access_token\":\"cli_1234567890abcd\",\"app_id\":\"cli_1234567890\",\"event_url\":\"https://open.feishu.cn/open-apis/event\",\"session_id\":\"sess_1234567890\"}";

        // Act
        var authData = JsonSerializer.Deserialize<AuthData>(json, JsonOptions.Default);

        // Assert
        authData.Should().NotBeNull();
        authData!.AppAccessToken.Should().Be("cli_1234567890abcd");
        authData.AppId.Should().Be("cli_1234567890");
        authData.EventUrl.Should().Be("https://open.feishu.cn/open-apis/event");
        authData.SessionId.Should().Be("sess_1234567890");
    }

    [Fact]
    public void Deserialization_ShouldHandleNullProperties()
    {
        // Arrange
        var json = "{\"app_access_token\":\"cli_1234567890abcd\",\"app_id\":null,\"event_url\":null,\"session_id\":null}";

        // Act
        var authData = JsonSerializer.Deserialize<AuthData>(json, JsonOptions.Default);

        // Assert
        authData.Should().NotBeNull();
        authData!.AppAccessToken.Should().Be("cli_1234567890abcd");
        authData.AppId.Should().BeNull();
        authData.EventUrl.Should().BeNull();
        authData.SessionId.Should().BeNull();
    }

    [Fact]
    public void Deserialization_ShouldHandleMissingProperties()
    {
        // Arrange
        var json = "{\"app_access_token\":\"cli_1234567890abcd\"}";

        // Act
        var authData = JsonSerializer.Deserialize<AuthData>(json, JsonOptions.Default);

        // Assert
        authData.Should().NotBeNull();
        authData!.AppAccessToken.Should().Be("cli_1234567890abcd");
        authData.AppId.Should().BeNull();
        authData.EventUrl.Should().BeNull();
        authData.SessionId.Should().BeNull();
    }

    [Fact]
    public void Deserialization_ShouldHandleEmptyJson()
    {
        // Arrange
        var json = "{}";

        // Act
        var authData = JsonSerializer.Deserialize<AuthData>(json, JsonOptions.Default);

        // Assert
        authData.Should().NotBeNull();
        authData!.AppAccessToken.Should().Be(string.Empty);
        authData.AppId.Should().BeNull();
        authData.EventUrl.Should().BeNull();
        authData.SessionId.Should().BeNull();
    }

    [Fact]
    public void Serialization_ShouldHandleNullAppAccessToken()
    {
        // Arrange
        var authData = new AuthData
        {
            AppAccessToken = null,
            AppId = "cli_1234567890"
        };

        // Act
        var json = JsonSerializer.Serialize(authData, JsonOptions.Default);

        // Assert - null values are omitted by default in JSON serialization
        json.Should().Contain("\"app_id\"");
        json.Should().NotContain("\"app_access_token\"");
    }

    [Fact]
    public void Serialization_ShouldHandleEmptyAppAccessToken()
    {
        // Arrange
        var authData = new AuthData
        {
            AppAccessToken = "",
            AppId = "cli_1234567890"
        };

        // Act
        var json = JsonSerializer.Serialize(authData, JsonOptions.Default);

        // Assert
        json.Should().Contain("\"app_access_token\":\"\"");
    }

    [Fact]
    public void ShouldHandleRealWorldTokenFormat()
    {
        // Arrange
        var authData = new AuthData
        {
            AppAccessToken = "cli_a1b2c3d4e5f6g7h8i9j0",
            AppId = "cli_a1b2c3d4e5f6g7h8i9j0",
            EventUrl = "https://open.feishu.cn/open-apis/event",
            SessionId = "sess_xxx"
        };

        // Act
        var json = JsonSerializer.Serialize(authData, JsonOptions.Default);
        var deserialized = JsonSerializer.Deserialize<AuthData>(json, JsonOptions.Default);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.AppAccessToken.Should().Be(authData.AppAccessToken);
        deserialized.AppId.Should().Be(authData.AppId);
        deserialized.EventUrl.Should().Be(authData.EventUrl);
        deserialized.SessionId.Should().Be(authData.SessionId);
    }
}
