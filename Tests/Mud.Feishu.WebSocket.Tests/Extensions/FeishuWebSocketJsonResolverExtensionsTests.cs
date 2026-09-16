// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

#if NET8_0_OR_GREATER
using System.Text.Json;
using FluentAssertions;
using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.WebSocket.DataModels;
using Mud.Feishu.WebSocket.Extensions;

namespace Mud.Feishu.WebSocket.Tests.Extensions;

/// <summary>
/// ConfigureWebSocketResolver 单元测试。
/// 验证 P1-1 修复：WebSocketJsonContext 通过模块自治模式注入到 FeishuJsonDefaults resolver 链。
/// </summary>
public class FeishuWebSocketJsonResolverExtensionsTests
{
    [Fact]
    public void ConfigureWebSocketResolver_ShouldNotThrow()
    {
        // Act
        Action act = () => FeishuWebSocketJsonResolverExtensions.ConfigureWebSocketResolver();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void ConfigureWebSocketResolver_ShouldEnableAuthResponseMessageDeserialization()
    {
        // Arrange - 在 ConfigureWebSocketResolver 调用后，AuthResponseMessage 应能通过 resolver 链反序列化
        FeishuWebSocketJsonResolverExtensions.ConfigureWebSocketResolver();
        var json = """{"code":0,"msg":"success","session_id":"sess_xxx","type":"auth"}""";

        // Act
        var result = JsonSerializer.Deserialize<AuthResponseMessage>(
            json, FeishuJsonDefaults.DeserializerOptions);

        // Assert
        result.Should().NotBeNull();
        result!.Code.Should().Be(0);
        result.Message.Should().Be("success");
        result.SessionId.Should().Be("sess_xxx");
        result.Type.Should().Be("auth");
    }

    [Fact]
    public void ConfigureWebSocketResolver_ShouldEnablePingMessageDeserialization()
    {
        // Arrange
        FeishuWebSocketJsonResolverExtensions.ConfigureWebSocketResolver();
        var json = """{"type":"ping"}""";

        // Act
        var result = JsonSerializer.Deserialize<PingMessage>(
            json, FeishuJsonDefaults.DeserializerOptions);

        // Assert
        result.Should().NotBeNull();
        result!.Type.Should().Be("ping");
    }

    [Fact]
    public void ConfigureWebSocketResolver_ShouldBeIdempotent()
    {
        // Arrange - 多次调用应不抛异常（累加模式）
        FeishuWebSocketJsonResolverExtensions.ConfigureWebSocketResolver();

        // Act
        Action act = () => FeishuWebSocketJsonResolverExtensions.ConfigureWebSocketResolver();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void ConfigureWebSocketResolver_ShouldEnableAuthResponseMessageSerialization()
    {
        // Arrange
        FeishuWebSocketJsonResolverExtensions.ConfigureWebSocketResolver();
        var message = new AuthResponseMessage
        {
            Code = 0,
            Message = "success",
            SessionId = "sess_test"
        };

        // Act
        var json = JsonSerializer.Serialize(message, FeishuJsonDefaults.SerializerOptions);

        // Assert
        json.Should().Contain("\"code\":0");
        json.Should().Contain("\"msg\":\"success\"");
        json.Should().Contain("\"session_id\":\"sess_test\"");
        json.Should().Contain("\"type\":\"auth\"");
    }
}
#endif
