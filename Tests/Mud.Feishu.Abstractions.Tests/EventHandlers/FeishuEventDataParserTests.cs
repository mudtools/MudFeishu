// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Mud.Feishu.Abstractions.Tests.EventHandlers;

/// <summary>
/// FeishuEventDataParser 单元测试（R-E1：v1.0/v2.0/data 包裹形态单一真源）
/// </summary>
public class FeishuEventDataParserTests
{
    private static (bool Ok, EventData? Data, FeishuEventDataParseFailure Failure) Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var ok = FeishuEventDataParser.TryParse(doc.RootElement, out var data, out var failure);
        return (ok, data, failure);
    }

    // ===== 官方 v1.0 样例（对照官方《事件概述》JSON 原文）=====

    [Fact]
    public void TryParse_WithOfficialV1Sample_ShouldMapAllFields()
    {
        const string json = """
            {
              "ts": "1502199207.7171419",
              "uuid": "bc447199585340d1f3728d26b1c0297a",
              "token": "41a9425ea7df4536a7623e38fa321bae",
              "type": "event_callback",
              "event": {
                "app_id": "cli_9c8609450f78d102",
                "tenant_key": "736588c9260f175c",
                "type": "p2p_chat_create"
              }
            }
            """;

        var (ok, data, failure) = Parse(json);

        ok.Should().BeTrue();
        failure.Should().Be(FeishuEventDataParseFailure.None);
        data!.EventId.Should().Be("bc447199585340d1f3728d26b1c0297a");
        data.EventType.Should().Be("p2p_chat_create");
        data.AppId.Should().Be("cli_9c8609450f78d102");
        data.TenantKey.Should().Be("736588c9260f175c");

        // 合成 Header（AD-2）：Schema 恒为 null，字段来自根级 uuid/token 与 event.*
        data.Header.Should().NotBeNull("v1 事件应携带合成 Header（E-P1-1）");
        data.Header!.Schema.Should().BeNull("v1 合成 Header 的 Schema 契约为 null");
        data.Header.EventId.Should().Be(data.EventId);
        data.Header.EventType.Should().Be(data.EventType);
        data.Header.Token.Should().Be("41a9425ea7df4536a7623e38fa321bae");
        data.Header.AppId.Should().Be(data.AppId);
        data.Header.TenantKey.Should().Be(data.TenantKey);
        data.Schema.Should().BeNull();
    }

    [Fact]
    public void TryParse_WithOfficialV1Sample_ShouldStoreEventAsRawJsonString()
    {
        const string json = """
            {
              "ts": "1502199207.7171419",
              "uuid": "u1",
              "token": "t1",
              "type": "event_callback",
              "event": { "type": "p2p_chat_create", "chat_id": "oc_1" }
            }
            """;

        var (_, data, _) = Parse(json);

        // AD-3：Event 恒为 JSON 原文字符串
        data!.Event.Should().BeOfType<string>();
        var raw = data.GetEventRawJson();
        raw.Should().NotBeNullOrEmpty();
        raw!.Should().Contain("p2p_chat_create").And.Contain("oc_1");
    }

    [Fact]
    public void TryParse_WithOfficialV1Sample_ShouldConvertFloatSecondTsToMilliseconds()
    {
        const string json = """
            { "ts": "1502199207.7171419", "uuid": "u1", "token": "t1",
              "type": "event_callback", "event": { "type": "p2p_chat_create" } }
            """;

        var (_, data, _) = Parse(json);

        // E-P2-2：浮点秒 → 毫秒
        data!.CreateTime.Should().Be(1502199207717);
        data.Header!.CreateTime.Should().Be("1502199207.7171419", "Header.CreateTime 保留原始字符串");
    }

    [Fact]
    public void TryParse_WithV1IntegerSecondTs_ShouldConvertToMilliseconds()
    {
        const string json = """
            { "ts": "1502199207", "uuid": "u1", "token": "t1",
              "type": "event_callback", "event": { "type": "p2p_chat_create" } }
            """;

        var (_, data, _) = Parse(json);

        data!.CreateTime.Should().Be(1502199207000);
    }

    // ===== v2.0 回归 =====

    [Fact]
    public void TryParse_WithV2Sample_ShouldMapAllFields()
    {
        const string json = """
            {
              "schema": "2.0",
              "header": {
                "event_id": "e2", "event_type": "contact.user.created_v3",
                "create_time": "1704067200000", "token": "tk2",
                "tenant_key": "tn2", "app_id": "cli_2"
              },
              "event": { "user_id": "ou_1" }
            }
            """;

        var (ok, data, _) = Parse(json);

        ok.Should().BeTrue();
        data!.EventId.Should().Be("e2");
        data.EventType.Should().Be("contact.user.created_v3");
        data.TenantKey.Should().Be("tn2");
        data.AppId.Should().Be("cli_2");
        data.Header!.Schema.Should().Be("2.0");
        data.Header.Token.Should().Be("tk2");
        data.Header.CreateTime.Should().Be("1704067200000");
        data.CreateTime.Should().Be(1704067200000, "13 位毫秒原值保留（E-P2-2 毫秒口径）");
        data.Event.Should().BeOfType<string>();
    }

    // ===== data 包裹形态（AD-4）=====

    [Fact]
    public void TryParse_WithDataWrappedV1Frame_ShouldDrillIntoData()
    {
        const string json = """
            {
              "type": "event",
              "data": {
                "ts": "1502199207.7171419",
                "uuid": "w1",
                "token": "wt1",
                "type": "event_callback",
                "event": { "type": "p2p_chat_create", "app_id": "cli_w", "tenant_key": "tn_w" }
              }
            }
            """;

        var (ok, data, _) = Parse(json);

        ok.Should().BeTrue("网关包裹帧必须可解析（兼容回退）");
        data!.EventId.Should().Be("w1");
        data.EventType.Should().Be("p2p_chat_create");
        data.AppId.Should().Be("cli_w");
        data.Header!.Token.Should().Be("wt1");
    }

    [Fact]
    public void TryParse_WithDataWrappedV2Frame_ShouldDrillIntoData()
    {
        const string json = """
            {
              "type": "event",
              "data": {
                "schema": "2.0",
                "header": { "event_id": "wv2", "event_type": "test.event", "token": "tok" },
                "event": {}
              }
            }
            """;

        var (ok, data, _) = Parse(json);

        ok.Should().BeTrue();
        data!.EventId.Should().Be("wv2");
        data.EventType.Should().Be("test.event");
        data.Header!.Schema.Should().Be("2.0");
    }

    // ===== 失败语义 =====

    [Theory]
    [InlineData("""{"uuid":"u1","type":"event_callback","event":{"app_id":"cli"}}""")]
    [InlineData("""{"uuid":"u1"}""")]
    [InlineData("""{"type":"url_verification","challenge":"c"}""")]
    public void TryParse_WithoutDeterminableEventType_ShouldFailWithNoEventType(string json)
    {
        var (ok, data, failure) = Parse(json);

        ok.Should().BeFalse();
        data.Should().BeNull();
        failure.Should().Be(FeishuEventDataParseFailure.NoEventType);
    }

    [Fact]
    public void TryParse_WithNonObjectRoot_ShouldFailWithUnsupportedShape()
    {
        var (ok, data, failure) = Parse("[1,2,3]");

        ok.Should().BeFalse();
        data.Should().BeNull();
        failure.Should().Be(FeishuEventDataParseFailure.UnsupportedShape);
    }

    // ===== create_time 启发式（§3.5 四组用例）=====

    [Theory]
    [InlineData("\"1704067200000\"", 1704067200000)]     // 13 位毫秒原值
    [InlineData("\"1704067200000000\"", 1704067200000)]  // 16 位微秒 /1000
    [InlineData("\"1502199207\"", 1502199207000)]        // 10 位秒 ×1000
    [InlineData("\"1502199207.7171419\"", 1502199207717)]// 浮点秒
    [InlineData("1704067200000", 1704067200000)]         // 数字毫秒
    [InlineData("1502199207", 1502199207000)]            // 数字秒
    public void TryParse_CreateTimeHeuristic_ShouldNormalizeToMilliseconds(string raw, long expected)
    {
        var json = "{\"schema\":\"2.0\",\"header\":{\"event_id\":\"e\",\"event_type\":\"t\",\"create_time\":" + raw + "}}";

        var (_, data, _) = Parse(json);

        data!.CreateTime.Should().Be(expected);
    }

    [Fact]
    public void TryParse_WithNonNumericCreateTime_ShouldReturnZero()
    {
        const string json = """
            {"schema":"2.0","header":{"event_id":"e","event_type":"t","create_time":"not-a-number"}}
            """;

        var (_, data, _) = Parse(json);

        data!.CreateTime.Should().Be(0, "无法解析的 create_time 静默为 0");
        data.Header!.CreateTime.Should().Be("not-a-number", "Header 保留原始字符串");
    }

    // ===== 历史变体兼容 =====

    [Fact]
    public void TryParse_WithLegacyRootEventType_ShouldRemainParseable()
    {
        // 旧实现只认根级 event_id/event_type——该形态修复后仍须可解析（零破坏）
        const string json = """
            {"event_id":"legacy1","event_type":"legacy.event","tenant_key":"tk","app_id":"cli","event":{"k":"v"}}
            """;

        var (ok, data, _) = Parse(json);

        ok.Should().BeTrue();
        data!.EventId.Should().Be("legacy1");
        data.EventType.Should().Be("legacy.event");
        data.TenantKey.Should().Be("tk");
        data.AppId.Should().Be("cli");
    }

    [Fact]
    public void TryParse_WithV1RootTypeNonReserved_ShouldUseRootTypeAsEventType()
    {
        // 兼容网关直接在根级携带真实事件类型的变体
        const string json = """
            {"uuid":"u1","type":"custom.event","event":{"app_id":"cli"}}
            """;

        var (ok, data, _) = Parse(json);

        ok.Should().BeTrue();
        data!.EventType.Should().Be("custom.event");
    }
}
