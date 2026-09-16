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
using Mud.Feishu.DataModels;
using Mud.Feishu.EventCallback.Drive;
using Mud.Feishu.EventCallback.Extensions;
using Mud.Feishu.EventCallback.IM;

namespace Mud.Feishu.Abstractions.Tests.Extensions;

/// <summary>
/// ConfigureEventCallbackResolver 单元测试。
/// 验证 P1-2 修复：EventCallbackJsonContext 通过模块自治模式注入到 FeishuJsonDefaults resolver 链。
/// </summary>
public class FeishuEventCallbackJsonResolverExtensionsTests
{
    [Fact]
    public void ConfigureEventCallbackResolver_ShouldNotThrow()
    {
        // Act
        Action act = () => FeishuEventCallbackJsonResolverExtensions.ConfigureEventCallbackResolver();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void ConfigureEventCallbackResolver_ShouldEnableMessageReceiveResultDeserialization()
    {
        // Arrange
        FeishuEventCallbackJsonResolverExtensions.ConfigureEventCallbackResolver();
        var json = """{"sender":{"sender_id":{"open_id":"ou_xxx","union_id":"on_xxx","user_id":"u_xxx"},"sender_type":"user"},"message":{"message_id":"om_xxx","chat_id":"oc_xxx","message_type":"text","content":"{\"text\":\"hello\"}"}}""";

        // Act
        var result = JsonSerializer.Deserialize<MessageReceiveResult>(
            json, FeishuJsonDefaults.DeserializerOptions);

        // Assert
        result.Should().NotBeNull();
        result!.Sender.Should().NotBeNull();
        result.Sender!.SenderId.Should().NotBeNull();
        result.Sender.SenderId!.OpenId.Should().Be("ou_xxx");
        result.Sender.SenderId.UnionId.Should().Be("on_xxx");
        result.Sender.SenderId.UserId.Should().Be("u_xxx");
        result.Sender.SenderType.Should().Be("user");
        result.Message.Should().NotBeNull();
        result.Message!.MessageId.Should().Be("om_xxx");
        result.Message.ChatId.Should().Be("oc_xxx");
        result.Message.MessageType.Should().Be("text");
    }

    [Fact]
    public void ConfigureEventCallbackResolver_ShouldEnableDriveFileEventHeaderDeserialization()
    {
        // Arrange - N-03 联动验证：DriveFileEventHeader 子类反序列化
        FeishuEventCallbackJsonResolverExtensions.ConfigureEventCallbackResolver();
        var json = """{"event_id":"evt_456","event_type":"drive.file.edit_v1","resource_id":"file_xxx","user_list":[{"user_id":"u1","open_id":"ou1"}]}""";

        // Act
        var result = JsonSerializer.Deserialize<DriveFileEventHeader>(
            json, FeishuJsonDefaults.DeserializerOptions);

        // Assert
        result.Should().NotBeNull();
        result!.EventId.Should().Be("evt_456");
        result.EventType.Should().Be("drive.file.edit_v1");
        result.ResourceId.Should().Be("file_xxx");
        result.UserList.Should().NotBeNull();
        result.UserList!.Length.Should().Be(1);
        result.UserList[0].UserId.Should().Be("u1");
        result.UserList[0].OpenId.Should().Be("ou1");
    }

    [Fact]
    public void ConfigureEventCallbackResolver_ShouldBeIdempotent()
    {
        // Arrange - 多次调用应不抛异常（累加模式）
        FeishuEventCallbackJsonResolverExtensions.ConfigureEventCallbackResolver();

        // Act
        Action act = () => FeishuEventCallbackJsonResolverExtensions.ConfigureEventCallbackResolver();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void ConfigureEventCallbackResolver_ShouldEnableMessageReceiveResultSerialization()
    {
        // Arrange
        FeishuEventCallbackJsonResolverExtensions.ConfigureEventCallbackResolver();
        var result = new MessageReceiveResult
        {
            Sender = new MessageSender
            {
                SenderId = new UserIdInfo { OpenId = "ou_test", UnionId = "on_test" },
                SenderType = "user"
            },
            Message = new MessageContent
            {
                MessageId = "om_test",
                ChatId = "oc_test",
                MessageType = "text"
            }
        };

        // Act
        var json = JsonSerializer.Serialize(result, FeishuJsonDefaults.SerializerOptions);

        // Assert
        json.Should().Contain("\"open_id\":\"ou_test\"");
        json.Should().Contain("\"union_id\":\"on_test\"");
        json.Should().Contain("\"message_id\":\"om_test\"");
        json.Should().Contain("\"sender_type\":\"user\"");
    }
}
#endif
