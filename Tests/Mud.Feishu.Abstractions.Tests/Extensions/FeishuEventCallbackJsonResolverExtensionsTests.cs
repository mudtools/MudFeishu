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
using Mud.Feishu.EventCallback.Bitable;
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

    /// <summary>
    /// R7：多维表格记录变更事件（<c>drive.file.bitable_record_changed_v1</c>）载荷必须能经
    /// resolver 链完整反序列化——<b>这是 C2 事件上下文装配的上游依赖</b>：
    /// 载荷一旦反序列化不出来，AI 侧的 <c>BitableRecordChangedConversationalEventHandler</c>
    /// 只会看到空 DTO（表现为"事件到了但什么都不说"），而这条链路此前<b>没有任何用例</b>。
    /// </summary>
    [Fact]
    public void ConfigureEventCallbackResolver_ShouldEnableBitableRecordChangedDeserialization()
    {
        // Arrange：官方载荷形态（含 before/after 字段值，均为 JSON 序列化后的字符串）
        FeishuEventCallbackJsonResolverExtensions.ConfigureEventCallbackResolver();
        var json = """
        {
          "file_type": "bitable",
          "file_token": "bascnTbl123",
          "table_id": "tblABC",
          "revision": 12,
          "operator_id": { "open_id": "ou_op", "union_id": "on_op", "user_id": "u_op" },
          "action_list": [
            {
              "record_id": "rec1",
              "action": "record_edited",
              "before_value": [ { "field_id": "fldA", "field_value": "\"旧\"" } ],
              "after_value": [ { "field_id": "fldA", "field_value": "\"新\"" } ]
            }
          ],
          "subscriber_id_list": [ { "open_id": "ou_sub" } ],
          "update_time": 1759990000
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<BitableRecordChangedResult>(
            json, FeishuJsonDefaults.DeserializerOptions);

        // Assert
        result.Should().NotBeNull();
        result!.FileType.Should().Be("bitable");
        result.FileToken.Should().Be("bascnTbl123");
        result.TableId.Should().Be("tblABC");
        result.Revision.Should().Be(12);
        result.OperatorId.Should().NotBeNull();
        result.OperatorId!.OpenId.Should().Be("ou_op", "操作人是事件投递目标（无 chat_id 的行级事件）");
        result.ActionList.Should().NotBeNull();
        result.ActionList!.Length.Should().Be(1);
        result.ActionList[0].RecordId.Should().Be("rec1");
        result.ActionList[0].Action.Should().Be("record_edited");
        result.ActionList[0].BeforeValue![0].FieldId.Should().Be("fldA");
        result.ActionList[0].BeforeValue![0].FieldValue.Should().Be("\"旧\"", "字段值是 JSON 序列化后的字符串（不二次解析）");
        result.ActionList[0].AfterValue![0].FieldValue.Should().Be("\"新\"");
        result.SubscriberIdList!.Length.Should().Be(1);
        result.UpdateTime.Should().Be(1759990000);
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
