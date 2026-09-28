// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.FeishuTools;
using Mud.Feishu.AI.FeishuTools.Tools;
using Mud.Feishu.AI.Tools;

namespace Mud.Feishu.AI.FeishuTools.Tests.Tools;

/// <summary>
/// 工具执行审计出口测试（AI-FD-D12 P1D-3b）：允许与<b>拒绝</b>路径均投递、sink 异常不影响执行链、
/// <see cref="ToolExecutionAuditRecord.ArgsDigest"/> 不含敏感字段原文（SDK 侧脱敏）。
/// </summary>
public class FeishuToolBindingAuditTests
{
    private sealed class RecordingSink(List<ToolExecutionAuditRecord> records, Exception? throwOnWrite = null) : IToolExecutionAuditSink
    {
        public Task WriteAsync(ToolExecutionAuditRecord record, CancellationToken cancellationToken = default)
        {
            if (throwOnWrite is not null)
            {
                throw throwOnWrite;
            }

            records.Add(record);
            return Task.CompletedTask;
        }
    }

    private static FeishuToolDefinition CreateTool(string name = "bitable.query_records", bool isWrite = false)
        => new(name, "描述", ["bitable:app:readonly"], isWrite,
            isWrite ? FeishuToolRisk.Write : FeishuToolRisk.Read, "tenant",
            (_, _, _) => Task.FromResult(FeishuToolResult.FromText("结果")));

    private static FeishuToolContext CreateContext()
        => new("app-a", ConversationKey: "feishu:app-a:conversation:chat:oc_1", ChatId: "oc_1", UserId: "ou_1");

    private static FeishuToolBinding CreateBinding(IToolExecutionAuditSink? sink = null, IToolExecutionAuthorizer? authorizer = null)
        => new(
            new FeishuAppContextScopeFactory(new Mock<Mud.Feishu.IFeishuTenantV1Message>().Object),
            Options.Create(new FeishuAgentOptions { Instructions = "test" }),
            authorizer,
            resultShaper: null,
            sink);

    [Fact]
    public async Task ExecuteAsync_ShouldWriteAudit_OnAllowedPath()
    {
        var records = new List<ToolExecutionAuditRecord>();
        var binding = CreateBinding(new RecordingSink(records));
        var tool = CreateTool();

        var result = await binding.ExecuteAsync(
            tool, new Dictionary<string, object?> { ["app_token"] = "bascnXxx" }, CreateContext(),
            _ => Task.FromResult(FeishuToolResult.FromText("结果文本")));

        result.ToString().Should().Be("结果文本");
        records.Should().ContainSingle();
        records[0].Decision.Should().Be("allowed");
        records[0].ToolName.Should().Be("bitable.query_records");
        records[0].AppKey.Should().Be("app-a");
        records[0].RequiredScopes.Should().Contain("bitable:app:readonly");
        records[0].ConversationKey.Should().Be("feishu:app-a:conversation:chat:oc_1");
        records[0].ArgsDigest.Should().Contain("app_token", "摘要保留参数名");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldWriteAudit_OnDeniedPath_WithZeroDownstreamCalls()
    {
        var records = new List<ToolExecutionAuditRecord>();
        var authorizer = new Mock<IToolExecutionAuthorizer>();
        authorizer
            .Setup(a => a.AuthorizeAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<bool>(), It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<FeishuToolContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthorizationResult.Deny("宿主拒绝"));
        var binding = CreateBinding(new RecordingSink(records), authorizer.Object);
        var tool = CreateTool("im.send_message", isWrite: true);

        var result = await binding.ExecuteAsync(
            tool, new Dictionary<string, object?>(), CreateContext(),
            _ => throw new InvalidOperationException("下游不得被调用"));

        result.ToString().Should().Contain("[tool_error]", "拒绝结构化回填");
        records.Should().ContainSingle("拒绝也是审计事件");
        records[0].Decision.Should().Be("denied");
        records[0].Reason.Should().StartWith("authorization_denied:",
            "审计 reason 必须带拒绝来源前缀（AT-B13：与 policy_denied: 可区分）");
        records[0].Reason.Should().Contain("宿主拒绝");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldWriteAudit_OnErrorPath()
    {
        var records = new List<ToolExecutionAuditRecord>();
        var binding = CreateBinding(new RecordingSink(records));
        var tool = CreateTool();

        var result = await binding.ExecuteAsync(
            tool, new Dictionary<string, object?>(), CreateContext(),
            _ => throw new HttpRequestException("网络中断"));

        result.ToString().Should().Contain("(retryable)");
        records.Should().ContainSingle();
        records[0].Decision.Should().Be("error");
        records[0].Reason.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldIsolateSinkFailures()
    {
        var binding = CreateBinding(new RecordingSink([], new InvalidOperationException("sink 宕机")));
        var tool = CreateTool();

        var act = async () => await binding.ExecuteAsync(
            tool, new Dictionary<string, object?>(), CreateContext(),
            _ => Task.FromResult(FeishuToolResult.FromText("结果文本")));

        await act.Should().NotThrowAsync("sink 异常只记日志、绝不影响执行链（异常隔离）");
    }

    [Fact]
    public void ToolArgsDigester_ShouldMaskSensitiveValues_AndKeepShapes()
    {
        var digest = ToolArgsDigester.Digest(new Dictionary<string, object?>
        {
            ["app_token"] = "bascnSecretValue",
            ["access_token"] = "sk-very-secret-raw",
            ["record_ids"] = System.Text.Json.JsonSerializer.SerializeToElement(new[] { "rec1", "rec2", "rec3" }),
        });

        digest.Should().Contain("app_token=", "普通参数保留名字");
        digest.Should().NotContain("bascnSecretValue", "入参值不进摘要（宿主 sink 不接触原始参数）");
        digest.Should().Contain("access_token=***", "敏感键名一律掩码");
        digest.Should().NotContain("sk-very-secret-raw");
        digest.Should().Contain("record_ids=array(3)", "数组保留形态与长度");
    }
}
