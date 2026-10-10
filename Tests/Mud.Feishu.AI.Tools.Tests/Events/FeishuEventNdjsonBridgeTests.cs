// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Mud.Feishu.AI.Tools.Events;
using Mud.Feishu.EventCallback;

namespace Mud.Feishu.AI.Tools.Tests.Events;

/// <summary>
/// R7 / C7：NDJSON 事件外桥——行格式、载荷原样嵌入、非法载荷 fail-fast、正则路由落盘、
/// 以及"与会话式处理器互斥"的显式闸门。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么载荷必须原样嵌入</b>：外桥若把已反序列化的对象再序列化回文本，会同时踩三个坑
/// （字段丢失、AOT 反射序列化破口、本仓 JSON 上下文成为外桥的格式瓶颈）。故本断言锁死"原始文本进、原始文本出"。
/// </para>
/// <para>
/// <b>为什么非法载荷要抛错而不是跳过</b>：外桥丢事件是最难被发现的故障（表现为"某个订阅一直没数据"），
/// 宁可让调用方在接入时就崩。
/// </para>
/// </remarks>
public class FeishuEventNdjsonBridgeTests
{
    private const string Payload = """{"message":{"message_id":"om_1","content":"{\"text\":\"hi\"}"}}""";

    private static FeishuEventEnvelope Envelope(
        string eventKey = "im.message.receive_v1",
        string payload = Payload,
        string? appKey = null,
        string? eventId = null)
        => new(eventKey, payload, appKey, eventId, new DateTimeOffset(2026, 10, 10, 8, 0, 0, TimeSpan.Zero));

    private static List<string> Lines(StringWriter writer)
        => writer.ToString()
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(static line => line.TrimEnd('\r'))
            .ToList();

    [Fact]
    public async Task WriteAsync_Should_EmitOneNdjsonLine_WithRawPayload()
    {
        using var writer = new StringWriter();
        using var bridge = new FeishuEventNdjsonBridge(writer, logger: NullLogger.Instance);

        await bridge.WriteAsync(Envelope(appKey: "cli_app_a", eventId: "evt-1"));

        var lines = Lines(writer);
        lines.Should().ContainSingle("NDJSON：一行一个事件");
        var line = lines[0];
        line.Should().Contain("\"event_key\":\"im.message.receive_v1\"");
        line.Should().Contain("\"app_key\":\"cli_app_a\"");
        line.Should().Contain("\"event_id\":\"evt-1\"");
        line.Should().Contain("\"received_at\":");
        line.Should().Contain(Payload, "payload 必须原样嵌入（再序列化会丢字段并引入 AOT 破口）");
        bridge.WrittenCount.Should().Be(1, "写出计数是可观测性锚点（外桥是否真在出数据）");
    }

    [Fact]
    public async Task WriteAsync_Should_EmitOneLinePerEvent_InOrder()
    {
        using var writer = new StringWriter();
        using var bridge = new FeishuEventNdjsonBridge(writer);

        await bridge.WriteAsync(Envelope(eventKey: "a.v1"));
        await bridge.WriteAsync(Envelope(eventKey: "b.v1"));
        await bridge.WriteAsync(Envelope(eventKey: "c.v1"));

        Lines(writer).Should().Equal(
            "{\"event_key\":\"a.v1\",\"received_at\":\"2026-10-10T08:00:00.0000000+00:00\",\"payload\":" + Payload + "}",
            "{\"event_key\":\"b.v1\",\"received_at\":\"2026-10-10T08:00:00.0000000+00:00\",\"payload\":" + Payload + "}",
            "{\"event_key\":\"c.v1\",\"received_at\":\"2026-10-10T08:00:00.0000000+00:00\",\"payload\":" + Payload + "}");
    }

    [Fact]
    public async Task WriteAsync_Should_EscapeEnvelopeStrings()
    {
        using var writer = new StringWriter();
        using var bridge = new FeishuEventNdjsonBridge(writer);

        await bridge.WriteAsync(Envelope(eventKey: "quote\"and\\slash", payload: "{}"));

        var line = Lines(writer)[0];
        line.Should().Contain("quote\\\"and\\\\slash", "信封字段必须经 JSON 转义（否则整行不可解析）");
        using var document = System.Text.Json.JsonDocument.Parse(line);
        document.RootElement.GetProperty("event_key").GetString().Should().Be("quote\"and\\slash");
    }

    [Theory]
    [InlineData("{不是合法JSON")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task WriteAsync_Should_FailFast_OnUnusablePayload(string payload)
    {
        using var writer = new StringWriter();
        using var bridge = new FeishuEventNdjsonBridge(writer);

        var act = async () => await bridge.WriteAsync(Envelope(payload: payload));

        // 空载荷允许（写成 payload:null），非空但非法的必须抛。
        if (string.IsNullOrWhiteSpace(payload))
        {
            await act.Should().NotThrowAsync();
            Lines(writer)[0].Should().Contain("\"payload\":null");
            return;
        }

        await act.Should().ThrowAsync<System.Text.Json.JsonException>(
            "非法的原始载荷必须 fail-fast——静默跳过会表现为「某个订阅一直没数据」这类最难查的故障");
    }

    [Fact]
    public async Task WriteAsync_Should_Reject_EnvelopeWithoutEventKey()
    {
        using var writer = new StringWriter();
        using var bridge = new FeishuEventNdjsonBridge(writer);

        var act = async () => await bridge.WriteAsync(new FeishuEventEnvelope(" ", "{}"));

        await act.Should().ThrowAsync<ArgumentException>("缺事件键 ⇒ 无法路由（写出去也没人能消费）");
    }

    [Fact]
    public async Task WriteAsync_Should_KeepWriterOpen_WhenNotOwned()
    {
        using var writer = new StringWriter();

        using (var bridge = new FeishuEventNdjsonBridge(writer))
        {
            await bridge.WriteAsync(Envelope());
        }

        // 释放桥接不得连带释放宿主的 writer（Console.Out 释放后整进程输出即哑掉）。
        writer.Write("still-usable");
        writer.ToString().Should().Contain("still-usable");
    }

    // ────────── 正则路由 ──────────

    [Fact]
    public async Task Router_Should_WriteLineToMatchingDirectory_Only()
    {
        var root = Path.Combine(Path.GetTempPath(), "feishu-ndjson-" + Guid.NewGuid().ToString("N"));
        try
        {
            using var writer = new StringWriter();
            var router = new RegexEventFileRouter(
                root,
                ("^im\\.", "im"),
                ("^approval\\.", "approval"));

            using var bridge = new FeishuEventNdjsonBridge(writer, router);

            await bridge.WriteAsync(Envelope(eventKey: "im.message.receive_v1"));
            await bridge.WriteAsync(Envelope(eventKey: "approval.task.updated_v1"));
            await bridge.WriteAsync(Envelope(eventKey: "contact.user.created_v3"));

            var imFiles = Directory.GetFiles(Path.Combine(root, "im"), "*.ndjson");
            imFiles.Should().ContainSingle("命中 im 路由的事件必须落盘");
            File.ReadAllText(imFiles[0]).Should().Contain("im.message.receive_v1");

            Directory.GetFiles(Path.Combine(root, "approval"), "*.ndjson").Should().ContainSingle();
            Directory.Exists(Path.Combine(root, "contact")).Should().BeFalse("未命中任何路由 ⇒ 不落盘（不得建空目录）");
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Router_Should_AppendWithoutTruncating()
    {
        var root = Path.Combine(Path.GetTempPath(), "feishu-ndjson-" + Guid.NewGuid().ToString("N"));
        try
        {
            using var writer = new StringWriter();
            var router = new RegexEventFileRouter(root, ("^im\\.", "im"));
            using var bridge = new FeishuEventNdjsonBridge(writer, router);

            await bridge.WriteAsync(Envelope());
            await bridge.WriteAsync(Envelope());

            var files = Directory.GetFiles(Path.Combine(root, "im"), "*.ndjson");
            files.Should().ContainSingle("同一事件键必须落在同一文件（路径稳定，消费方才可 tail）");
            File.ReadAllLines(files[0]).Should().HaveCount(2, "追加语义：不得截断既有内容");
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void Router_Should_Reject_EmptyRule()
    {
        var act = () => new RegexEventFileRouter("root", ("^im\\.", " "));

        act.Should().Throw<ArgumentException>("空目录名的路由规则会让落盘落到不可预期位置");
    }

    // ────────── 互斥闸门 ──────────

    [Fact]
    public void EnsureNotConversational_Should_Throw_WhenConversationalHandlersRegistered()
    {
        var act = () => FeishuEventNdjsonBridge.EnsureNotConversational(1);

        act.Should().Throw<InvalidOperationException>().WithMessage("*双重副作用*");
    }

    [Fact]
    public void EnsureNotConversational_Should_Pass_WhenNoConversationalHandlers()
        => FeishuEventNdjsonBridge.EnsureNotConversational(0);

    [Fact]
    public void AllowConcurrentWithConversational_Should_RequireReason()
    {
        ((Action)(() => FeishuEventNdjsonBridge.AllowConcurrentWithConversational("  ")))
            .Should().Throw<ArgumentException>("放行必须给出互斥依据（否则这就是无理由的副作用开关）");

        FeishuEventNdjsonBridge.AllowConcurrentWithConversational("IM 事件进模型，审批事件外桥");
    }

    // ────────── 事件目录 ──────────

    [Fact]
    public void Catalog_Should_ListSortedDistinctEventKeys()
    {
        var keys = FeishuEventCatalog.ListEventKeys();

        keys.Should().NotBeEmpty("事件目录为空 ⇒ 反射没命中常量（假绿）");
        keys.Should().BeInAscendingOrder();
        keys.Should().OnlyHaveUniqueItems();
        keys.Should().Contain(FeishuEventTypes.ReceiveMessage);
        keys.Should().Contain(FeishuEventTypes.BitableRecordChanged,
            "多维表格记录变更事件必须出现在目录里（外桥路由要按它配正则）");
    }

    [Fact]
    public void Catalog_Should_EmitNdjsonLines()
    {
        var lines = FeishuEventCatalog.ListEventKeysAsNdjson().ToList();

        lines.Should().HaveCount(FeishuEventCatalog.ListEventKeys().Count);
        foreach (var line in lines)
        {
            using var document = System.Text.Json.JsonDocument.Parse(line);
            document.RootElement.GetProperty("event_key").GetString().Should().NotBeNullOrWhiteSpace();
        }
    }

    /// <summary>
    /// 事件键 ↔ 常量名必须<b>一对一</b>（已知历史别名钉在白名单里）。
    /// </summary>
    /// <remarks>
    /// 本轮实测发现：<c>FeishuEventTypes</c> 里 <c>ChatUpdated</c> 与 <c>GroupUpdated</c>
    /// 指向<b>同一个</b>事件键（既有别名，未合并）。这在 API 面表现为"两个名字一个事件"，
    /// 外桥按名字配正则时尤其容易困惑。故把该异常钉在案上：新增别名必须先合并/改名。
    /// </remarks>
    [Fact]
    public void Catalog_Should_MapEachEventKeyToExactlyOneConstant()
    {
        var byKey = typeof(FeishuEventTypes)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(static f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(static f => (Name: f.Name, Key: f.GetRawConstantValue() as string))
            .Where(static pair => !string.IsNullOrWhiteSpace(pair.Key))
            .GroupBy(static pair => pair.Key!, StringComparer.Ordinal)
            .ToDictionary(static g => g.Key, static g => g.Select(static p => p.Name).ToArray(), StringComparer.Ordinal);

        var unexpected = byKey
            .Where(pair => pair.Value.Length > 1 && !KnownAliases.ContainsKey(pair.Key))
            .Select(pair => $"{pair.Key} ⇒ {string.Join(" / ", pair.Value)}")
            .ToArray();

        unexpected.Should().BeEmpty(
            "同一事件键出现多个常量名（外桥按名字配正则时会产生歧义）：{0}", string.Join(" | ", unexpected));

        foreach (var alias in KnownAliases)
        {
            byKey.Should().ContainKey(alias.Key, $"白名单登记的事件键 {alias.Key} 已不存在——请更新白名单");
            byKey[alias.Key].Should().BeEquivalentTo(
                alias.Value.Split('/').Select(static name => name.Trim()),
                "既有别名的常量名有增删——白名单必须同步（合并前先更新理由）");
        }

        FeishuEventCatalog.ListEventKeys().Count.Should().Be(
            byKey.Count,
            "目录必须覆盖全部事件键（去重后 = 键数，不是常量数）");
    }

    /// <summary>
    /// 已知的事件键别名（键 → 常量名清单，以 <c>/</c> 分隔）。
    /// </summary>
    /// <remarks>
    /// <c>im.chat.updated_v1</c>：历史别名——群/会话更新是同一个事件，SDK 未合并两个常量名。
    /// </remarks>
    private static readonly Dictionary<string, string> KnownAliases = new(StringComparer.Ordinal)
    {
        ["im.chat.updated_v1"] = "ChatUpdated / GroupUpdated",
    };
}