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

namespace Mud.Feishu.Abstractions.Tests.Utilities;

/// <summary>
/// FeishuJsonContext 单元测试。
/// 验证 P0-2 修复：FeishuEventHeader 已注册到 FeishuJsonContext。
/// </summary>
public class FeishuJsonContextTests
{
    [Fact]
    public void FeishuJsonContext_ShouldRegisterFeishuEventHeader()
    {
        // Arrange
        var headerJson = """{"event_id":"evt_123","event_type":"contact.user.created_v3","create_time":"1700000000","token":"xxx","app_id":"cli_xxx","tenant_key":"xxx"}""";

        // Act - 通过 FeishuJsonContext.Default 强类型路径反序列化（AOT 安全路径）
        var header = JsonSerializer.Deserialize(
            headerJson, FeishuJsonContext.Default.FeishuEventHeader);

        // Assert
        header.Should().NotBeNull();
        header!.EventId.Should().Be("evt_123");
        header.EventType.Should().Be("contact.user.created_v3");
        header.CreateTime.Should().Be("1700000000");
        header.Token.Should().Be("xxx");
        header.AppId.Should().Be("cli_xxx");
        header.TenantKey.Should().Be("xxx");
    }

    [Fact]
    public void FeishuJsonContext_ShouldRegisterEventData()
    {
        // Arrange
        var eventData = new EventData
        {
            EventId = "evt_test",
            EventType = "test.event",
            TenantKey = "tk_test",
            AppId = "app_test",
            CreateTime = 1704067200,
            Event = JsonDocument.Parse("""{"foo":"bar"}""").RootElement.Clone()
        };

        // Act - 通过 FeishuJsonContext.Default 序列化
        var json = JsonSerializer.Serialize(eventData, FeishuJsonContext.Default.EventData);

        // Assert
        json.Should().Contain("\"event_id\":\"evt_test\"");
        json.Should().Contain("\"event_type\":\"test.event\"");
    }

    [Fact]
    public void FeishuJsonContext_FeishuEventHeader_WithMissingOptionalFields_ShouldSucceed()
    {
        // Arrange - 缺少可选字段 schema/token/create_time
        var headerJson = """{"event_id":"evt_456","event_type":"test.event","tenant_key":"tk","app_id":"app"}""";

        // Act
        var header = JsonSerializer.Deserialize(
            headerJson, FeishuJsonContext.Default.FeishuEventHeader);

        // Assert
        header.Should().NotBeNull();
        header!.EventId.Should().Be("evt_456");
        header.Schema.Should().BeNull();
        header.Token.Should().BeNull();
        header.CreateTime.Should().BeNull();
    }
}
#endif
