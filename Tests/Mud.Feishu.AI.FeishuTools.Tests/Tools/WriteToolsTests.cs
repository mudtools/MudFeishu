// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;
using Mud.Feishu.AI.FeishuTools.Internal;
using Mud.Feishu.DataModels.Approval;
using Mud.Feishu.DataModels.Bitable;
using Mud.Feishu.DataModels.Messages;

namespace Mud.Feishu.AI.FeishuTools.Tests.Tools;

/// <summary>
/// 写类工具分域映射单测（Phase 2 §3.3）：扁平参数 → 请求体构造 → 解包投影，
/// 以及非法入参的结构化错误回填（Phase 2 §7 测试策略）。
/// </summary>
public class WriteToolsTests
{
    private readonly Mock<Mud.Feishu.IFeishuTenantV1Message> _messageClient = new();
    private readonly Mock<Mud.Feishu.IFeishuTenantV1BitableRecord> _recordClient = new();
    private readonly Mock<Mud.Feishu.IFeishuTenantV4Approval> _approvalClient = new();
    private readonly Mock<Mud.Feishu.IFeishuTenantV4ApprovalQuery> _approvalQueryClient = new();
    private readonly Mock<Mud.Feishu.IFeishuTenantV4ApprovalTask> _approvalTaskClient = new();

    private static FeishuAgentOptions NewOptions() => new() { Instructions = "test" };

    private ApprovalWriteTools CreateApprovalTools()
        => new(_approvalClient.Object, _approvalQueryClient.Object, _approvalTaskClient.Object, null, Options.Create(NewOptions()));

    private static IReadOnlyDictionary<string, object?> Args(params (string Key, object? Value)[] items)
        => items.ToDictionary(p => p.Key, p => p.Value);

    // ───────────────────── im.send_message ─────────────────────

    [Fact]
    public async Task SendMessage_ShouldMapFlatParamsIntoBody_AndProjectMessageId()
    {
        SendMessageRequest? captured = null;
        string? capturedReceiveIdType = null;
        _messageClient
            .Setup(c => c.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((SendMessageRequest request, string receiveIdType, CancellationToken _)
                =>
            {
                captured = request;
                capturedReceiveIdType = receiveIdType;
            })
            .ReturnsAsync(new FeishuApiResult<MessageDataResult>
            {
                Code = 0,
                Data = new MessageDataResult { MessageId = "om_send_1" },
            });

        var tools = new MessageWriteTools(_messageClient.Object);
        var result = await tools.SendMessageAsync(
            Args(("receive_id", "oc_group1"), ("text", "你好"), ("receive_id_type", "chat_id")),
            CancellationToken.None);

        using var document = JsonDocument.Parse(result.ToString()!);
        document.RootElement.GetProperty("message_id").GetString().Should().Be("om_send_1");
        capturedReceiveIdType.Should().Be("chat_id");
        captured.Should().NotBeNull();
        captured!.ReceiveId.Should().Be("oc_group1");
        captured.MsgType.Should().Be("text", "首版写工具固定文本消息（绑定层注入，模型不可见）");
        using var contentDocument = JsonDocument.Parse(captured.Content!);
        contentDocument.RootElement.GetProperty("text").GetString().Should().Be("你好",
            "content 为飞书 text 消息 JSON（转义由 JsonNode 承担，断言按解析后文本比较）");
    }

    [Fact]
    public async Task SendMessage_ShouldDefaultReceiveIdTypeToChat_AndRejectUnknownType()
    {
        _messageClient
            .Setup(c => c.SendMessageAsync(It.IsAny<SendMessageRequest>(), "chat_id", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<MessageDataResult>
            {
                Code = 0,
                Data = new MessageDataResult { MessageId = "om_ok" },
            });

        var tools = new MessageWriteTools(_messageClient.Object);
        var ok = await tools.SendMessageAsync(Args(("receive_id", "oc_group1"), ("text", "hi")), CancellationToken.None);
        ok.ToString().Should().Contain("om_ok", "缺省 receive_id_type = chat_id");

        var rejected = await tools.SendMessageAsync(
            Args(("receive_id", "oc_x"), ("text", "hi"), ("receive_id_type", "user_id; drop table")),
            CancellationToken.None);
        rejected.ToString().Should().StartWith("[tool_error] im.send_message", "白名单外的 receive_id_type 结构化拒绝（防参数注入）");
    }

    [Fact]
    public async Task SendMessage_ShouldBackfillReadableError_WhenCodeNotZero()
    {
        _messageClient
            .Setup(c => c.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<MessageDataResult> { Code = 230001, Msg = "无发送权限" });

        var tools = new MessageWriteTools(_messageClient.Object);
        var result = await tools.SendMessageAsync(Args(("receive_id", "oc_x"), ("text", "hi")), CancellationToken.None);

        result.ToString().Should().StartWith("[tool_error] im.send_message")
            .And.Contain("230001")
            .And.Contain("无发送权限", "code != 0 转可读文本回填模型（总体设计 §4 不变式）");
    }

    // ───────────────────── bitable.add_record ─────────────────────

    [Fact]
    public async Task AddRecord_ShouldParseFieldsJsonIntoRequest()
    {
        RecordOpsRequest? captured = null;
        _recordClient
            .Setup(c => c.AddRecordAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<RecordOpsRequest>(),
                It.IsAny<string?>(), It.IsAny<bool?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((string _, string __, RecordOpsRequest request, string? ___, bool? ____, string _____, CancellationToken ______)
                => captured = request)
            .ReturnsAsync(new FeishuApiResult<RecordOpsResult>
            {
                Code = 0,
                Data = new RecordOpsResult { Record = new AppTableRecordInfo { RecordId = "rec_new_1" } },
            });

        var tools = new BitableWriteTools(_recordClient.Object);
        var result = await tools.AddRecordAsync(
            Args(("app_token", "bascnXxx"), ("table_id", "tbl001"), ("fields", """{"任务名称":"写周报","优先级":3}""")),
            CancellationToken.None);

        using var document = JsonDocument.Parse(result.ToString()!);
        document.RootElement.GetProperty("record_id").GetString().Should().Be("rec_new_1");
        captured.Should().NotBeNull();
        var fields = captured!.Fields.Should().BeOfType<JsonElement>().Subject;
        fields.ValueKind.Should().Be(JsonValueKind.Object, "Fields 以 JsonElement 承载（STJ 内建转换器可源生成序列化，无反射）");
        fields.GetProperty("任务名称").GetString().Should().Be("写周报");
        fields.GetProperty("优先级").GetInt32().Should().Be(3);
    }

    [Fact]
    public async Task AddRecord_ShouldRejectNonObjectFields_WithStructuredError()
    {
        var tools = new BitableWriteTools(_recordClient.Object);

        var notJson = await tools.AddRecordAsync(
            Args(("app_token", "b"), ("table_id", "t"), ("fields", "not-json{")), CancellationToken.None);
        notJson.ToString().Should().StartWith("[tool_error] bitable.add_record").And.Contain("fields");

        var notObject = await tools.AddRecordAsync(
            Args(("app_token", "b"), ("table_id", "t"), ("fields", "[1,2]")), CancellationToken.None);
        notObject.ToString().Should().StartWith("[tool_error] bitable.add_record").And.Contain("JSON 对象");
    }

    // ───────────────────── approval.create_instance ─────────────────────

    [Fact]
    public async Task CreateInstance_ShouldMapFlatParamsIntoCreateInstanceRequest()
    {
        CreateInstanceRequest? captured = null;
        _approvalClient
            .Setup(c => c.CreateInstanceAsync(It.IsAny<CreateInstanceRequest>(), It.IsAny<CancellationToken>()))
            .Callback((CreateInstanceRequest request, CancellationToken _) => captured = request)
            .ReturnsAsync(new FeishuApiResult<CreateInstancesResult>
            {
                Code = 0,
                Data = new CreateInstancesResult { InstanceCode = "inst_001" },
            });

        var tools = CreateApprovalTools();
        var result = await tools.CreateInstanceAsync(
            Args(("approval_code", "C4C0E8F0"), ("form", """[{"id":"widget1","type":"input","value":"出差申请"}]"""), ("user_id", "ou_u1")),
            CancellationToken.None);

        using var document = JsonDocument.Parse(result.ToString()!);
        document.RootElement.GetProperty("instance_code").GetString().Should().Be("inst_001");
        captured.Should().NotBeNull();
        captured!.ApprovalCode.Should().Be("C4C0E8F0", "approval_code/form 为 CreateInstanceRequest 的 body 字段（Phase 2 §3.3）");
        captured.Form.Should().Contain("出差申请");
        captured.UserId.Should().Be("ou_u1");
    }

    [Fact]
    public async Task CreateInstance_ShouldBackfillReadableError_WhenCodeNotZero()
    {
        _approvalClient
            .Setup(c => c.CreateInstanceAsync(It.IsAny<CreateInstanceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<CreateInstancesResult> { Code = 134003, Msg = "审批定义不存在" });

        var tools = CreateApprovalTools();
        var result = await tools.CreateInstanceAsync(
            Args(("approval_code", "bad"), ("form", "[]")), CancellationToken.None);

        result.ToString().Should().StartWith("[tool_error] approval.create_instance").And.Contain("134003");
    }
}
