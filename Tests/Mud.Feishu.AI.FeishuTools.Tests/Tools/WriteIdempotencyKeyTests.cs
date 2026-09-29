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
/// T4-1（WP4 / AT-F13② / F-1）：写工具幂等键的真实调用链路用例——
/// <c>idempotency_key</c> → 三个 SDK 落点的实参抓取断言 + 省略负例（不写空 Uuid）+ dry-run 回显。
/// </summary>
/// <remarks>
/// 三落点失败语义不同（模型可见契约，见各参数描述）：
/// Bitable <c>client_token</c> 重复返回原记录；IM <c>Uuid</c> 1 小时窗口去重；Approval <c>Uuid</c> 冲突报 60012。
/// </remarks>
public class WriteIdempotencyKeyTests
{
    private readonly Mock<Mud.Feishu.IFeishuTenantV1Message> _messageClient = new();
    private readonly Mock<Mud.Feishu.IFeishuTenantV1BitableRecord> _recordClient = new();
    private readonly Mock<Mud.Feishu.IFeishuTenantV4Approval> _approvalClient = new();

    private static IReadOnlyDictionary<string, object?> Args(params (string Key, object? Value)[] items)
        => items.ToDictionary(p => p.Key, p => p.Value);

    private static FeishuAgentOptions NewOptions() => new() { Instructions = "test" };

    // ───────────────────── im.send_message → Uuid ─────────────────────

    [Fact]
    public async Task SendMessage_ShouldPassIdempotencyKeyIntoUuid_WhenProvided()
    {
        SendMessageRequest? captured = null;
        _messageClient
            .Setup(c => c.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((SendMessageRequest request, string _, CancellationToken __) => captured = request)
            .ReturnsAsync(new FeishuApiResult<MessageDataResult>
            {
                Code = 0,
                Data = new MessageDataResult { MessageId = "om_idem_1" },
            });

        var tools = new MessageWriteTools(_messageClient.Object);
        await tools.SendMessageAsync(
            Args(("receive_id", "oc_group1"), ("text", "你好"), ("idempotency_key", "bill-42-send")),
            CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Uuid.Should().Be("bill-42-send",
            "idempotency_key 必须落到 SendMessageRequest.Uuid（平台 1 小时窗口去重）");
    }

    [Fact]
    public async Task SendMessage_ShouldNotFillUuid_WhenIdempotencyKeyOmitted()
    {
        SendMessageRequest? captured = null;
        _messageClient
            .Setup(c => c.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((SendMessageRequest request, string _, CancellationToken __) => captured = request)
            .ReturnsAsync(new FeishuApiResult<MessageDataResult>
            {
                Code = 0,
                Data = new MessageDataResult { MessageId = "om_idem_2" },
            });

        var tools = new MessageWriteTools(_messageClient.Object);
        await tools.SendMessageAsync(
            Args(("receive_id", "oc_group1"), ("text", "你好")),
            CancellationToken.None);

        captured!.Uuid.Should().BeNullOrEmpty("省略幂等键时不得写入空 Uuid（空串可能引发平台 400）");
    }

    // ───────────────────── bitable.add_record → client_token ─────────────────────

    [Fact]
    public async Task AddRecord_ShouldPassIdempotencyKeyIntoClientToken_WhenProvided()
    {
        string? capturedClientToken = null;
        _recordClient
            .Setup(c => c.AddRecordAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<RecordOpsRequest>(),
                It.IsAny<string?>(), It.IsAny<bool?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((string _, string __, RecordOpsRequest ___, string? clientToken, bool? ____, string _____, CancellationToken ______)
                => capturedClientToken = clientToken)
            .ReturnsAsync(new FeishuApiResult<RecordOpsResult>
            {
                Code = 0,
                Data = new RecordOpsResult { Record = new AppTableRecordInfo { RecordId = "rec_idem_1" } },
            });

        var tools = new BitableWriteTools(_recordClient.Object);
        await tools.AddRecordAsync(
            Args(("app_token", "bascnX"), ("table_id", "tblY"), ("fields", "{\"任务\":\"写周报\"}"), ("idempotency_key", "bill-42-add")),
            CancellationToken.None);

        capturedClientToken.Should().Be("bill-42-add",
            "idempotency_key 必须透传为 client_token 查询参数（重复请求返回原记录）");
    }

    [Fact]
    public async Task AddRecord_ShouldNotFillClientToken_WhenIdempotencyKeyOmitted()
    {
        string? capturedClientToken = "sentinel";
        _recordClient
            .Setup(c => c.AddRecordAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<RecordOpsRequest>(),
                It.IsAny<string?>(), It.IsAny<bool?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((string _, string __, RecordOpsRequest ___, string? clientToken, bool? ____, string _____, CancellationToken ______)
                => capturedClientToken = clientToken)
            .ReturnsAsync(new FeishuApiResult<RecordOpsResult>
            {
                Code = 0,
                Data = new RecordOpsResult { Record = new AppTableRecordInfo { RecordId = "rec_idem_2" } },
            });

        var tools = new BitableWriteTools(_recordClient.Object);
        await tools.AddRecordAsync(
            Args(("app_token", "bascnX"), ("table_id", "tblY"), ("fields", "{\"任务\":\"写周报\"}")),
            CancellationToken.None);

        capturedClientToken.Should().BeNull("省略幂等键时不得下发空 client_token");
    }

    // ───────────────────── approval.create_instance → Uuid ─────────────────────

    [Fact]
    public async Task CreateInstance_ShouldPassIdempotencyKeyIntoUuid_WhenProvided()
    {
        CreateInstanceRequest? captured = null;
        _approvalClient
            .Setup(c => c.CreateInstanceAsync(It.IsAny<CreateInstanceRequest>(), It.IsAny<CancellationToken>()))
            .Callback((CreateInstanceRequest request, CancellationToken _) => captured = request)
            .ReturnsAsync(new FeishuApiResult<CreateInstancesResult>
            {
                Code = 0,
                Data = new CreateInstancesResult { InstanceCode = "ic_idem_1" },
            });

        var tools = new ApprovalWriteTools(_approvalClient.Object, null, null, Options.Create(NewOptions()));
        await tools.CreateInstanceAsync(
            Args(("approval_code", "AC-1"), ("form", "[{\"id\":\"w1\",\"type\":\"input\",\"value\":\"内容\"}]"), ("idempotency_key", "7C468A54-8745-2245-9675-08B7C63E7A87")),
            CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Uuid.Should().Be("7C468A54-8745-2245-9675-08B7C63E7A87",
            "idempotency_key 必须落到 CreateInstanceRequest.Uuid（冲突返回 60012）");
    }

    [Fact]
    public async Task CreateInstance_ShouldNotFillUuid_WhenIdempotencyKeyOmitted()
    {
        CreateInstanceRequest? captured = null;
        _approvalClient
            .Setup(c => c.CreateInstanceAsync(It.IsAny<CreateInstanceRequest>(), It.IsAny<CancellationToken>()))
            .Callback((CreateInstanceRequest request, CancellationToken _) => captured = request)
            .ReturnsAsync(new FeishuApiResult<CreateInstancesResult>
            {
                Code = 0,
                Data = new CreateInstancesResult { InstanceCode = "ic_idem_2" },
            });

        var tools = new ApprovalWriteTools(_approvalClient.Object, null, null, Options.Create(NewOptions()));
        await tools.CreateInstanceAsync(
            Args(("approval_code", "AC-1"), ("form", "[{\"id\":\"w1\",\"type\":\"input\",\"value\":\"内容\"}]")),
            CancellationToken.None);

        captured!.Uuid.Should().BeNullOrEmpty("省略幂等键时不得写入空 Uuid");
    }

    // ───────────────────── dry-run 幂等键回显（预演不占坑） ─────────────────────

    [Fact]
    public async Task DryRun_ShouldEchoIdempotencyKeyState_ForAllThreeWriteTools()
    {
        var messageTools = new MessageWriteTools(_messageClient.Object);
        var bitableTools = new BitableWriteTools(_recordClient.Object);
        var approvalTools = new ApprovalWriteTools(_approvalClient.Object, null, null, Options.Create(NewOptions()));

        var provided = (await messageTools.SendMessageAsync(
            Args(("receive_id", "oc_g"), ("text", "hi"), ("idempotency_key", "k1"), ("dry_run", true)),
            CancellationToken.None)).ToString()!;
        provided.Should().Contain("幂等键：provided", "预演必须显式回显幂等键状态（T4-1：预演不占坑）");
        provided.Should().NotContain("k1", "摘要不得回显幂等键原文（与 text/fields/form 同一不回原文纪律）");

        var omitted = (await bitableTools.AddRecordAsync(
            Args(("app_token", "bascnX"), ("table_id", "tblY"), ("fields", "{\"任务\":\"写周报\"}"), ("dry_run", true)),
            CancellationToken.None)).ToString()!;
        omitted.Should().Contain("幂等键：omitted", "省略幂等键时预演必须显式说明「不保证幂等」");

        var approvalProvided = (await approvalTools.CreateInstanceAsync(
            Args(("approval_code", "AC-1"), ("form", "[]"), ("idempotency_key", "k3"), ("dry_run", true)),
            CancellationToken.None)).ToString()!;
        approvalProvided.Should().Contain("幂等键：provided");
    }
}
