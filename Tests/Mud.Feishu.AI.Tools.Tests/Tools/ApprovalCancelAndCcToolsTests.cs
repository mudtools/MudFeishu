// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json.Nodes;

using Moq;

using Mud.Feishu.AI.Tools.Internal;
using Mud.Feishu.DataModels;
using Mud.Feishu.DataModels.Approval;

namespace Mud.Feishu.AI.Tools.Tests.Tools;

/// <summary>
/// 审批实例撤回（<c>approval.cancel_instance</c>）与抄送（<c>approval.cc_instance</c>）断言。
/// </summary>
/// <remarks>
/// 两枚工具各自的硬点：
/// ① 撤回走 <c>FeishuNullDataApiResult</c>（平台无响应体），业务错误码若不被显式解包，
///    模型会把「审批定义没勾选允许撤销」这类配置性失败读成成功；
/// ② 抄送是用户身份工具，客户端缺席必须点名 <c>IFeishuUserV4ApprovalInstance</c> 与
///    <c>AllowedIdentities</c>，且空收件人属于「调用会成功但没人收到」的空操作。
/// </remarks>
public class ApprovalCancelAndCcToolsTests
{
    private static IReadOnlyDictionary<string, object?> Args(params (string Key, object? Value)[] items)
        => items.ToDictionary(static p => p.Key, static p => p.Value, StringComparer.Ordinal);

    private static JsonObject Payload(ToolResult result) => JsonNode.Parse(result.ToString())!.AsObject();

    private static Mock<Mud.Feishu.IFeishuTenantV4Approval> CreateApprovalClient(
        int code = 0,
        string? msg = null)
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV4Approval>();
        client.Setup(c => c.CancelInstanceAsync(
                It.IsAny<CancelInstancesRequest>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuNullDataApiResult { Code = code, Msg = msg });
        return client;
    }

    private static Mock<Mud.Feishu.IFeishuUserV4ApprovalInstance> CreateInstanceUserClient(
        int code = 0,
        string? msg = null)
    {
        var client = new Mock<Mud.Feishu.IFeishuUserV4ApprovalInstance>();
        client.Setup(c => c.AddCcInstanceAsync(
                It.IsAny<AddCcInstanceRequest>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuNullDataApiResult { Code = code, Msg = msg });
        return client;
    }

    private static ApprovalWriteTools CreateTools(
        Mock<Mud.Feishu.IFeishuTenantV4Approval>? approval = null,
        Mock<Mud.Feishu.IFeishuUserV4ApprovalInstance>? instanceUser = null)
        => new(
            (approval ?? CreateApprovalClient()).Object,
            null,
            null,
            instanceUser?.Object,
            Microsoft.Extensions.Options.Options.Create(
                new FeishuAgentOptions { Instructions = "test", MaxToolResultLength = 8000 }));

    private static Dictionary<string, object?> CancelArgs(string? userIdType = null, bool dryRun = false)
    {
        var args = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["approval_code"] = "AC-1",
            ["instance_code"] = "IC_20261019_001",
            ["user_id"] = "ou_submitter",
        };
        if (dryRun)
        {
            args["dry_run"] = true;
        }

        if (userIdType is not null)
        {
            args["user_id_type"] = userIdType;
        }

        return args;
    }

    // ───────────────────── cancel_instance ─────────────────────

    [Fact]
    public async Task CancelInstance_ShouldSendSubmitterIdentity_AndReturnReceipt()
    {
        CancelInstancesRequest? captured = null;
        var approval = CreateApprovalClient();
        approval.Setup(c => c.CancelInstanceAsync(
                It.IsAny<CancelInstancesRequest>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<CancelInstancesRequest, string?, CancellationToken>((r, _, _) => captured = r)
            .ReturnsAsync(new FeishuNullDataApiResult { Code = 0 });

        var payload = Payload(await CreateTools(approval)
            .CancelInstanceAsync(CancelArgs(), CancellationToken.None));

        captured!.ApprovalCode.Should().Be("AC-1");
        captured.InstanceCode.Should().Be("IC_20261019_001");
        captured.UserId.Should().Be("ou_submitter", "平台按「提交人 + 定义 + 实例」定位可撤销实例，传操作人必然失败");
        payload["cancelled"]!.GetValue<bool>().Should().BeTrue();
        payload["instance_code"]!.GetValue<string>().Should().Be("IC_20261019_001");
    }

    /// <summary>缺省 user_id_type 归一为 open_id（平台默认值），不能把 null 透传给下游。</summary>
    [Fact]
    public async Task CancelInstance_ShouldDefaultUserIdTypeToOpenId()
    {
        string? capturedUserIdType = null;
        var approval = CreateApprovalClient();
        approval.Setup(c => c.CancelInstanceAsync(
                It.IsAny<CancelInstancesRequest>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<CancelInstancesRequest, string?, CancellationToken>((_, t, _) => capturedUserIdType = t)
            .ReturnsAsync(new FeishuNullDataApiResult { Code = 0 });

        await CreateTools(approval).CancelInstanceAsync(CancelArgs(), CancellationToken.None);

        capturedUserIdType.Should().Be("open_id");
    }

    [Theory]
    [InlineData("union_id")]
    [InlineData("user_id")]
    public async Task CancelInstance_ShouldPassThroughClosedUserIdTypes(string userIdType)
    {
        string? capturedUserIdType = null;
        var approval = CreateApprovalClient();
        approval.Setup(c => c.CancelInstanceAsync(
                It.IsAny<CancelInstancesRequest>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<CancelInstancesRequest, string?, CancellationToken>((_, t, _) => capturedUserIdType = t)
            .ReturnsAsync(new FeishuNullDataApiResult { Code = 0 });

        await CreateTools(approval).CancelInstanceAsync(CancelArgs(userIdType), CancellationToken.None);

        capturedUserIdType.Should().Be(userIdType);
    }

    [Fact]
    public async Task CancelInstance_ShouldReject_WhenUserIdTypeOutsideClosedSet()
    {
        var approval = CreateApprovalClient();

        var text = (await CreateTools(approval)
            .CancelInstanceAsync(CancelArgs("openid"), CancellationToken.None)).ToString()!;

        text.Should().Contain("user_id_type");
        approval.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CancelInstance_DryRun_ShouldNotCallDownstream_AndNotEchoCodes()
    {
        var approval = CreateApprovalClient();

        var text = (await CreateTools(approval)
            .CancelInstanceAsync(CancelArgs(dryRun: true), CancellationToken.None)).ToString()!;

        text.Should().StartWith("[dry_run]");
        text.Should().Contain("POST /open-apis/approval/v4/instances/cancel");
        text.Should().Contain("允许撤销", "预演就要点出配置前置，否则实调只会拿到语焉不详的业务错误");
        text.Should().NotContain("IC_20261019_001", "预演摘要只回长度，不回显实例标识原文");
        approval.VerifyNoOtherCalls();
    }

    /// <summary>
    /// <b>无响应体的接口必须显式解包业务码</b>：否则「定义未勾选允许撤销」会被读成撤回成功。
    /// </summary>
    [Fact]
    public async Task CancelInstance_ShouldSurfaceBusinessError_NotSilentSuccess()
    {
        var approval = CreateApprovalClient(code: 1114001, msg: "the approval definition does not allow cancellation");

        var text = (await CreateTools(approval)
            .CancelInstanceAsync(CancelArgs(), CancellationToken.None)).ToString()!;

        text.Should().Contain("1114001");
        text.Should().Contain("does not allow cancellation");
        text.Should().NotContain("cancelled", "失败回执里不得出现成功字段");
    }

    // ───────────────────── cc_instance ─────────────────────

    [Fact]
    public async Task CcInstance_ShouldSendRecipients_AndReportCount()
    {
        AddCcInstanceRequest? captured = null;
        var instanceUser = CreateInstanceUserClient();
        instanceUser.Setup(c => c.AddCcInstanceAsync(
                It.IsAny<AddCcInstanceRequest>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<AddCcInstanceRequest, string?, CancellationToken>((r, _, _) => captured = r)
            .ReturnsAsync(new FeishuNullDataApiResult { Code = 0 });

        var payload = Payload(await CreateTools(instanceUser: instanceUser).CcInstanceAsync(
            Args(("instance_code", "IC_1"), ("cc_user_ids", new[] { "ou_a", "ou_b" }), ("comment", "请周知")),
            CancellationToken.None));

        captured!.CcUserIds.Should().Equal("ou_a", "ou_b");
        captured.Comment.Should().Be("请周知");
        payload["cc_sent"]!.GetValue<bool>().Should().BeTrue();
        payload["cc_user_count"]!.GetValue<int>().Should().Be(2,
            "抄送人数是模型向用户汇报「谁被拉进来看」的唯一依据");
    }

    /// <summary>
    /// 空收件人是「调用会成功但没人收到」的空操作，必须在本地拒绝。
    /// 实际拦截点是解包期（必填数组不接受空值），处理器内的长度判断是第二道闸。
    /// </summary>
    [Fact]
    public async Task CcInstance_ShouldReject_WhenRecipientsEmpty()
    {
        var instanceUser = CreateInstanceUserClient();

        var text = (await CreateTools(instanceUser: instanceUser).CcInstanceAsync(
            Args(("instance_code", "IC_1"), ("cc_user_ids", Array.Empty<string>())),
            CancellationToken.None)).ToString()!;

        text.Should().Contain("cc_user_ids");
        text.Should().Contain("invalid_args");
        instanceUser.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CcInstance_DryRun_ShouldNotCallDownstream()
    {
        var instanceUser = CreateInstanceUserClient();

        var text = (await CreateTools(instanceUser: instanceUser).CcInstanceAsync(
            Args(("instance_code", "IC_1"), ("cc_user_ids", new[] { "ou_a" }), ("dry_run", true)),
            CancellationToken.None)).ToString()!;

        text.Should().StartWith("[dry_run]");
        text.Should().Contain("POST /open-apis/approval/v4/instances/add_cc");
        instanceUser.VerifyNoOtherCalls();
    }

    /// <summary>用户身份客户端缺席时点名接口与放行开关——否则宿主只会看到一句"工具失败"。</summary>
    [Fact]
    public async Task CcInstance_ShouldFailClosed_WhenUserClientAbsent()
    {
        var text = (await CreateTools().CcInstanceAsync(
            Args(("instance_code", "IC_1"), ("cc_user_ids", new[] { "ou_a" })),
            CancellationToken.None)).ToString()!;

        text.Should().Contain("IFeishuUserV4ApprovalInstance");
        text.Should().Contain("AllowedIdentities");
    }

    [Fact]
    public async Task CcInstance_ShouldSurfaceBusinessError_NotSilentSuccess()
    {
        var instanceUser = CreateInstanceUserClient(code: 1116001, msg: "no permission");

        var text = (await CreateTools(instanceUser: instanceUser).CcInstanceAsync(
            Args(("instance_code", "IC_1"), ("cc_user_ids", new[] { "ou_a" })),
            CancellationToken.None)).ToString()!;

        text.Should().Contain("1116001");
        text.Should().NotContain("cc_sent");
    }
}
