// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Approval;
using Mud.Feishu.DataModels.Bitable;
using Mud.Feishu.DataModels.Messages;

namespace Mud.Feishu.AI.FeishuTools.Tests.Tools;

/// <summary>
/// 写操作预演（AT-F13①，§8.2 #10）与能力出处元工具（AT-F12，§8.2 #9）。
/// </summary>
public class WriteDryRunAndCapabilityLookupTests
{
    private static IReadOnlyDictionary<string, object?> Args(params (string Key, object? Value)[] items)
        => items.ToDictionary(p => p.Key, p => p.Value);

    private static FeishuAgentOptions AgentOptions() => new() { Instructions = "test" };

    // ───────────────────── AT-F13①：dry_run 不触下游 ─────────────────────

    [Fact]
    public async Task ImSendMessage_DryRun_ShouldNotCallDownstream_AndReturnRequestDigest()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1Message>();
        var tools = new MessageWriteTools(client.Object);

        var result = await tools.SendMessageAsync(
            Args(("receive_id", "oc_secret_chat"), ("text", "这是一条不该被发送的绝密正文"), ("dry_run", true)),
            CancellationToken.None);

        var text = result.ToString()!;
        text.Should().StartWith("[dry_run]");
        text.Should().Contain("POST /open-apis/im/v1/messages", "预演必须给出将要下发的 method + path");
        text.Should().NotContain("这是一条不该被发送的绝密正文",
            "摘要**不得回显正文原文**——否则 dry_run 会成为绕过出站净化与内容安全的回显通道");
        text.Should().NotContain("oc_secret_chat", "标识类同上：只回长度");

        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task BitableAddRecord_DryRun_ShouldValidateFields_First()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1BitableRecord>();
        var tools = new BitableWriteTools(client.Object);

        // 非法 fields 在预演阶段也必须被拒——预演的价值就是"能提前发现的问题都提前发现"。
        var bad = await tools.AddRecordAsync(
            Args(("app_token", "bascnX"), ("table_id", "tblX"), ("fields", "[1,2,3]"), ("dry_run", true)),
            CancellationToken.None);

        bad.ToString().Should().Contain("fields 须为 JSON 对象");
        client.VerifyNoOtherCalls();

        var ok = await tools.AddRecordAsync(
            Args(("app_token", "bascnX"), ("table_id", "tblX"), ("fields", "{\"任务名称\":\"写周报\"}"), ("dry_run", true)),
            CancellationToken.None);

        ok.ToString().Should().StartWith("[dry_run]");
        ok.ToString().Should().Contain("POST /open-apis/bitable/v1/apps/{app_token}/tables/{table_id}/records");
        ok.ToString().Should().NotContain("写周报");
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ApprovalCreateInstance_DryRun_ShouldNotCallDownstream()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV4Approval>();
        var tools = new ApprovalWriteTools(client.Object);

        var result = await tools.CreateInstanceAsync(
            Args(("approval_code", "CODE1"), ("form", "[{\"id\":\"w1\",\"type\":\"input\",\"value\":\"请假\"}]"), ("dry_run", true)),
            CancellationToken.None);

        result.ToString().Should().StartWith("[dry_run]");
        result.ToString().Should().Contain("POST /open-apis/approval/v4/instances");
        client.VerifyNoOtherCalls();
    }

    /// <summary>AT-F13③：form 是裸 JSON 参数，非数组必须在**下发前**被拒（此前完全静默）。</summary>
    [Fact]
    public async Task ApprovalCreateInstance_InvalidForm_ShouldFailBeforeDownstream()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV4Approval>();
        var tools = new ApprovalWriteTools(client.Object);

        var notJson = await tools.CreateInstanceAsync(Args(("approval_code", "C"), ("form", "not json")), CancellationToken.None);
        notJson.ToString().Should().Contain("form 不是合法 JSON");

        var notArray = await tools.CreateInstanceAsync(Args(("approval_code", "C"), ("form", "{\"id\":\"w1\"}")), CancellationToken.None);
        notArray.ToString().Should().Contain("form 须为 JSON 数组字符串");

        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task WriteTool_WithoutDryRun_ShouldReachDownstream()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1Message>();
        client
            .Setup(c => c.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<MessageDataResult> { Code = 0, Data = new MessageDataResult { MessageId = "om_x" } });

        var tools = new MessageWriteTools(client.Object);
        var result = await tools.SendMessageAsync(
            Args(("receive_id", "oc1"), ("text", "hi")),
            CancellationToken.None);

        result.ToString().Should().Contain("om_x", "dry_run 缺省为 false —— 不得改变既有写行为（安全默认只体现在白名单与授权）");
        client.Verify(c => c.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ───────────────────── AT-F12：能力出处元工具 ─────────────────────

    [Fact]
    public async Task CapabilityLookup_ShouldReturnGroupLevelMetadata_ForMatchedKeyword()
    {
        var tools = new CapabilityLookupTools(Options.Create(AgentOptions()));

        var result = await tools.LookupAsync(Args(("keyword", "Calendar")), CancellationToken.None);

        using var document = JsonDocument.Parse(result.ToString()!);
        var root = document.RootElement;

        root.GetProperty("sdk_method_count").GetInt32().Should().BePositive();
        root.GetProperty("curated_tool_count").GetInt32().Should().Be(FeishuToolSchemas.SchemaByToolName.Count);

        var groups = root.GetProperty("matched_groups");
        groups.GetArrayLength().Should().BePositive("关键字 Calendar 必须命中 SDK 的日历能力分组");

        var first = groups[0];
        first.GetProperty("group").GetString().Should().Contain("Calendar");
        first.GetProperty("methods").GetInt32().Should().BePositive();
        first.TryGetProperty("module_curated", out var curated).Should().BeTrue();
        curated.GetBoolean().Should().BeFalse("日历尚未策展为工具——这正是本工具要如实告知模型的事实");
    }

    /// <summary>D4 边界：只回元数据，**不得**回方法名与请求构造。</summary>
    [Fact]
    public async Task CapabilityLookup_ShouldReturnMetadataOnly_NeverRequestShapes()
    {
        var tools = new CapabilityLookupTools(Options.Create(AgentOptions()));

        var result = await tools.LookupAsync(Args(("keyword", "Bitable")), CancellationToken.None);
        var text = result.ToString()!;

        text.Should().NotContain("\"route\"", "D4：不得返回请求构造（path/route）");
        text.Should().NotContain("\"http\"", "D4：不得返回 HTTP 方法");
        text.Should().NotContain("Async", "D4/C-3：不得返回方法名（生成器产物里本就没有方法名，不得靠猜补上）");

        using var document = JsonDocument.Parse(text);
        document.RootElement.GetProperty("curated_tools").EnumerateArray()
            .Select(e => e.GetString()!)
            .Should().Contain("bitable.list_tables", "已策展模块必须回填真实工具名（模型据此判断‘有没有 / 是否已启用’）");
    }

    [Fact]
    public async Task CapabilityLookup_ShouldRequireKeyword()
    {
        var tools = new CapabilityLookupTools(Options.Create(AgentOptions()));

        var result = await tools.LookupAsync(Args(), CancellationToken.None);

        result.ToString().Should().StartWith("[tool_error] feishu.capability_lookup");
    }

    [Fact]
    public async Task CapabilityLookup_UnmatchedKeyword_ShouldReturnZeroGroups_NotError()
    {
        var tools = new CapabilityLookupTools(Options.Create(AgentOptions()));

        var result = await tools.LookupAsync(Args(("keyword", "NoSuchDomainXyz")), CancellationToken.None);

        using var document = JsonDocument.Parse(result.ToString()!);
        document.RootElement.GetProperty("matched_group_count").GetInt32().Should().Be(0,
            "查不到是**有效答案**（该能力不在 SDK 中），不是错误——模型需要据此放弃而不是重试");
    }
}
