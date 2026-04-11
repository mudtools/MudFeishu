// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;

namespace Mud.Feishu.Abstractions.Tests.EventDataTests;

public class FeishuEventHeaderTests
{
    [Fact]
    public void FeishuEventHeader_DefaultValues_ShouldBeCorrect()
    {
        var header = new FeishuEventHeader();

        header.Schema.Should().BeNull();
        header.EventId.Should().BeEmpty();
        header.Token.Should().BeNull();
        header.CreateTime.Should().BeNull();
        header.EventType.Should().BeEmpty();
        header.TenantKey.Should().BeEmpty();
        header.AppId.Should().BeEmpty();
    }

    [Fact]
    public void FeishuEventHeader_ShouldImplementIEventHeader()
    {
        var header = new FeishuEventHeader();
        header.Should().BeAssignableTo<IEventHeader>();
    }

    [Fact]
    public void FeishuEventHeader_Properties_ShouldBeSettable()
    {
        var header = new FeishuEventHeader
        {
            Schema = "2.0",
            EventId = "evt_123",
            Token = "token_abc",
            CreateTime = "1704067200000",
            EventType = "drive.file.edit_v1",
            TenantKey = "tenant_key_001",
            AppId = "cli_abc123"
        };

        header.Schema.Should().Be("2.0");
        header.EventId.Should().Be("evt_123");
        header.Token.Should().Be("token_abc");
        header.CreateTime.Should().Be("1704067200000");
        header.EventType.Should().Be("drive.file.edit_v1");
        header.TenantKey.Should().Be("tenant_key_001");
        header.AppId.Should().Be("cli_abc123");
    }

    [Fact]
    public void FeishuEventHeader_Serialization_ShouldProduceCorrectJson()
    {
        var header = new FeishuEventHeader
        {
            Schema = "2.0",
            EventId = "evt_123",
            Token = "token_abc",
            CreateTime = "1704067200000",
            EventType = "drive.file.edit_v1",
            TenantKey = "tenant_key_001",
            AppId = "cli_abc123"
        };

        var json = JsonSerializer.Serialize(header);

        json.Should().Contain("\"schema\":\"2.0\"");
        json.Should().Contain("\"event_id\":\"evt_123\"");
        json.Should().Contain("\"token\":\"token_abc\"");
        json.Should().Contain("\"create_time\":\"1704067200000\"");
        json.Should().Contain("\"event_type\":\"drive.file.edit_v1\"");
        json.Should().Contain("\"tenant_key\":\"tenant_key_001\"");
        json.Should().Contain("\"app_id\":\"cli_abc123\"");
    }

    [Fact]
    public void FeishuEventHeader_Deserialization_ShouldParseCorrectly()
    {
        var json = @"{
            ""schema"": ""2.0"",
            ""event_id"": ""evt_456"",
            ""token"": ""token_xyz"",
            ""create_time"": ""1704067200000"",
            ""event_type"": ""contact.user.created_v3"",
            ""tenant_key"": ""tenant_key_002"",
            ""app_id"": ""cli_def456""
        }";

        var header = JsonSerializer.Deserialize<FeishuEventHeader>(json);

        header.Should().NotBeNull();
        header!.Schema.Should().Be("2.0");
        header.EventId.Should().Be("evt_456");
        header.Token.Should().Be("token_xyz");
        header.CreateTime.Should().Be("1704067200000");
        header.EventType.Should().Be("contact.user.created_v3");
        header.TenantKey.Should().Be("tenant_key_002");
        header.AppId.Should().Be("cli_def456");
    }

    [Fact]
    public void FeishuEventHeader_Deserialization_WithMissingOptionalFields_ShouldSucceed()
    {
        var json = @"{
            ""event_id"": ""evt_789"",
            ""event_type"": ""test.event"",
            ""tenant_key"": ""tk"",
            ""app_id"": ""app""
        }";

        var header = JsonSerializer.Deserialize<FeishuEventHeader>(json);

        header.Should().NotBeNull();
        header!.Schema.Should().BeNull();
        header.Token.Should().BeNull();
        header.CreateTime.Should().BeNull();
        header.EventId.Should().Be("evt_789");
    }
}

public class EventDataHeaderTests
{
    [Fact]
    public void EventData_Header_DefaultShouldBeNull()
    {
        var eventData = new EventData();
        eventData.Header.Should().BeNull();
    }

    [Fact]
    public void EventData_Schema_WhenHeaderIsNull_ShouldReturnNull()
    {
        var eventData = new EventData();
        eventData.Schema.Should().BeNull();
    }

    [Fact]
    public void EventData_Schema_WhenHeaderHasSchema_ShouldReturnSchemaValue()
    {
        var eventData = new EventData
        {
            Header = new FeishuEventHeader { Schema = "2.0" }
        };
        eventData.Schema.Should().Be("2.0");
    }

    [Fact]
    public void EventData_Schema_WhenHeaderSchemaIsNull_ShouldReturnNull()
    {
        var eventData = new EventData
        {
            Header = new FeishuEventHeader { Schema = null }
        };
        eventData.Schema.Should().BeNull();
    }

    [Fact]
    public void EventData_ExistingProperties_ShouldNotBeAffectedByHeader()
    {
        var eventData = new EventData
        {
            EventId = "evt_001",
            EventType = "test.event",
            AppId = "app_001",
            TenantKey = "tk_001",
            CreateTime = 1704067200,
            Header = new FeishuEventHeader
            {
                EventId = "evt_002",
                EventType = "header.event",
                AppId = "app_002",
                TenantKey = "tk_002",
                Schema = "2.0"
            }
        };

        eventData.EventId.Should().Be("evt_001");
        eventData.EventType.Should().Be("test.event");
        eventData.AppId.Should().Be("app_001");
        eventData.TenantKey.Should().Be("tk_001");
        eventData.CreateTime.Should().Be(1704067200);
        eventData.Header.EventId.Should().Be("evt_002");
        eventData.Header.EventType.Should().Be("header.event");
    }

    [Fact]
    public void EventData_Header_ShouldBeJsonIgnore()
    {
        var eventData = new EventData
        {
            EventId = "evt_001",
            Header = new FeishuEventHeader { Schema = "2.0", EventId = "evt_001" }
        };

        var json = JsonSerializer.Serialize(eventData);

        json.Should().NotContain("\"Header\"");
        json.Should().NotContain("\"Schema\"");
    }
}

public class IEventHeaderInterfaceTests
{
    [Fact]
    public void IEventHeader_ShouldHaveRequiredProperties()
    {
        var header = new FeishuEventHeader
        {
            Schema = "2.0",
            EventId = "evt_001",
            EventType = "test.event",
            TenantKey = "tk_001",
            AppId = "app_001"
        };

        IEventHeader iface = header;
        iface.Schema.Should().Be("2.0");
        iface.EventId.Should().Be("evt_001");
        iface.EventType.Should().Be("test.event");
        iface.TenantKey.Should().Be("tk_001");
        iface.AppId.Should().Be("app_001");
    }
}
