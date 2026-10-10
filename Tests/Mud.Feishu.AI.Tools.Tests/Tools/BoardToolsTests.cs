// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json.Nodes;

using Mud.Feishu.AI.Tools.Internal;
using Mud.Feishu.DataModels.Board;

namespace Mud.Feishu.AI.Tools.Tests.Tools;

/// <summary>
/// Board 画板域工具（R7 / A4）：6 个工具的链路实参、DSL 渲染、幂等键与参数负例。
/// </summary>
/// <remarks>
/// <para>
/// <b>本域的杀手工具是 <c>board.render_dsl</c></b>（PlantUML/Mermaid 源码 → 画板节点），
/// 用例锁定 <c>dsl_type</c> → <c>SyntaxType</c> 的映射（1=PlantUML / 2=Mermaid）——
/// 映射写反不会报错，只会静默画错图。
/// </para>
/// <para>
/// <b>幂等键</b>：底层 <c>CreateWhiteboardNodeAsync</c> 支持 <c>client_token</c>，
/// 故 <c>board.create_nodes</c> 必须暴露 <c>idempotency_key</c> 并原样透传（写工具重试的前提）。
/// </para>
/// </remarks>
public class BoardToolsTests
{
    private static IReadOnlyDictionary<string, object?> Args(params (string Key, object? Value)[] items)
        => items.ToDictionary(p => p.Key, p => p.Value);

    private static BoardTools CreateTools(Mock<Mud.Feishu.IFeishuTenantV1Board>? client = null)
        => new(client?.Object);

    private static JsonObject Payload(FeishuToolResult result)
        => JsonNode.Parse(result.ToString())!.AsObject();

    // ───────────────────── 契约面 ─────────────────────

    [Fact]
    public void BoardTools_Should_HaveExpectedRiskAndIdentity()
    {
        FeishuToolContracts.ByToolName[FeishuToolNames.BoardGetTheme].Risk.Should().Be(FeishuToolRisk.Read);
        FeishuToolContracts.ByToolName[FeishuToolNames.BoardListNodes].Risk.Should().Be(FeishuToolRisk.Read);

        foreach (var writeTool in new[]
                 {
                     FeishuToolNames.BoardUpdateTheme,
                     FeishuToolNames.BoardRenderDsl,
                     FeishuToolNames.BoardCreateNodes,
                 })
        {
            var contract = FeishuToolContracts.ByToolName[writeTool];
            contract.IsWrite.Should().BeTrue();
            contract.Identity.Should().Be("tenant");
            FeishuToolSchemas.SchemaByToolName[writeTool].Should().Contain("dry_run",
                "每个写工具必须可预演（DryRunContractGuards）");
        }

        FeishuToolContracts.ByToolName[FeishuToolNames.BoardDeleteNodes].Risk
            .Should().Be(FeishuToolRisk.HighRiskWrite,
                "批量删除会递归删子节点 ⇒ 危险词分级为 high-risk-write（MCP 侧映射 destructiveHint）");
    }

    [Fact]
    public void CreateNodes_Should_Expose_IdempotencyKey()
    {
        FeishuToolSchemas.SchemaByToolName[FeishuToolNames.BoardCreateNodes]
            .Should().Contain("idempotency_key",
                "底层支持 client_token ⇒ 工具必须暴露幂等键（否则写工具重试只能靠人工判断）");
    }

    // ───────────────────── 只读面 ─────────────────────

    [Fact]
    public async Task GetNodes_Should_Return_Parent_Children_Fields()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1Board>();
        client.Setup(c => c.GetWhiteboardNodesAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<GetWhiteboardNodesResult>
            {
                Code = 0,
                Data = new GetWhiteboardNodesResult
                {
                    Nodes =
                    [
                        new WhiteboardNodeInfo
                        {
                            Id = "n1",
                            Type = "composite_shape",
                            Children = ["n2"],
                        },
                        new WhiteboardNodeInfo { Id = "n2", Type = "text", ParentId = "n1" },
                    ],
                },
            });

        var result = await CreateTools(client).GetWhiteboardNodesAsync(
            Args(("whiteboard_id", "wb1")),
            CancellationToken.None);

        var nodes = Payload(result)["nodes"]!.AsArray();
        nodes.Count.Should().Be(2);
        nodes[0]!["node_id"]!.GetValue<string>().Should().Be("n1");
        nodes[0]!["children"]!.AsArray()[0]!.GetValue<string>().Should().Be("n2");
        nodes[1]!["parent_id"]!.GetValue<string>().Should().Be("n1",
            "parent/children 关系是模型把节点拼成画板的唯一依据");
    }

    [Fact]
    public async Task GetTheme_Should_Report_Found_False_For_Empty_Theme()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1Board>();
        client.Setup(c => c.GetWhiteboardThemeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<GetWhiteboardsThemeResult>
            {
                Code = 0,
                Data = new GetWhiteboardsThemeResult(),
            });

        var result = await CreateTools(client).GetWhiteboardThemeAsync(
            Args(("whiteboard_id", "wb1")),
            CancellationToken.None);

        Payload(result)["found"]!.GetValue<bool>().Should().BeFalse(
            "空结果给显式哨兵（found=false + 提示），而不是空对象让模型猜");
    }

    // ───────────────────── 写面（DSL 渲染 / 幂等 / 删除） ─────────────────────

    [Fact]
    public async Task RenderDsl_Should_Map_DslType_To_SyntaxType()
    {
        CreatePlantumlWhiteboardNodeRequest? captured = null;
        var client = new Mock<Mud.Feishu.IFeishuTenantV1Board>();
        client.Setup(c => c.CreatePlantumlWhiteboardNodeAsync(
                It.IsAny<string>(), It.IsAny<CreatePlantumlWhiteboardNodeRequest>(), It.IsAny<CancellationToken>()))
            .Callback((string _, CreatePlantumlWhiteboardNodeRequest request, CancellationToken __) => captured = request)
            .ReturnsAsync(new FeishuNullDataApiResult { Code = 0 });

        var result = await CreateTools(client).CreatePlantumlWhiteboardNodeAsync(
            Args(("whiteboard_id", "wb1"), ("dsl_type", "plantuml"), ("content", "@startuml\nA->B\n@enduml")),
            CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.PlantUmlCode.Should().Be("@startuml\nA->B\n@enduml", "DSL 源码原样下发（渲染由平台完成）");
        captured.SyntaxType.Should().Be(1, "1=PlantUML / 2=Mermaid；写反只会静默画错图");

        Payload(result)["rendered"]!.GetValue<bool>().Should().BeTrue();
    }

    [Fact]
    public async Task RenderDsl_Should_Use_Mermaid_Syntax_Type()
    {
        CreatePlantumlWhiteboardNodeRequest? captured = null;
        var client = new Mock<Mud.Feishu.IFeishuTenantV1Board>();
        client.Setup(c => c.CreatePlantumlWhiteboardNodeAsync(
                It.IsAny<string>(), It.IsAny<CreatePlantumlWhiteboardNodeRequest>(), It.IsAny<CancellationToken>()))
            .Callback((string _, CreatePlantumlWhiteboardNodeRequest request, CancellationToken __) => captured = request)
            .ReturnsAsync(new FeishuNullDataApiResult { Code = 0 });

        await CreateTools(client).CreatePlantumlWhiteboardNodeAsync(
            Args(("whiteboard_id", "wb1"), ("dsl_type", "mermaid"), ("content", "graph TD; A-->B;")),
            CancellationToken.None);

        captured!.SyntaxType.Should().Be(2);
    }

    [Fact]
    public async Task RenderDsl_Should_Reject_Unknown_DslType_Without_DownstreamCall()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1Board>();

        var result = await CreateTools(client).CreatePlantumlWhiteboardNodeAsync(
            Args(("whiteboard_id", "wb1"), ("dsl_type", "graphviz"), ("content", "digraph {}")),
            CancellationToken.None);

        result.Error.Should().NotBeNull();
        result.ToString().Should().Contain("dsl_type").And.Contain("plantuml").And.Contain("mermaid");
        client.Invocations.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateNodes_Should_Pass_IdempotencyKey_As_ClientToken()
    {
        string? seenClientToken = null;
        CreateWhiteboardNodeRequest? captured = null;

        var client = new Mock<Mud.Feishu.IFeishuTenantV1Board>();
        client.Setup(c => c.CreateWhiteboardNodeAsync(
                It.IsAny<string>(), It.IsAny<CreateWhiteboardNodeRequest>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback((
                string _,
                CreateWhiteboardNodeRequest request,
                string? clientToken,
                string? __,
                CancellationToken ___) =>
            {
                captured = request;
                seenClientToken = clientToken;
            })
            .ReturnsAsync(new FeishuApiResult<CreateWhiteboardNodeResult>
            {
                Code = 0,
                Data = new CreateWhiteboardNodeResult { Ids = ["n1"] },
            });

        var result = await CreateTools(client).CreateWhiteboardNodeAsync(
            Args(
                ("whiteboard_id", "wb1"),
                ("nodes_json", """[{"type":"text","id":"n1"}]"""),
                ("idempotency_key", "key-1")),
            CancellationToken.None);

        seenClientToken.Should().Be("key-1", "幂等键必须透传为 client_token（平台侧去重的唯一依据）");
        captured!.Nodes.Should().HaveCount(1);
        captured.Nodes![0].Type.Should().Be("text");

        Payload(result)["created_nodes"]!.AsArray()[0]!["node_id"]!.GetValue<string>().Should().Be("n1");
    }

    [Fact]
    public async Task CreateNodes_Should_Reject_Malformed_NodesJson()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1Board>();

        var result = await CreateTools(client).CreateWhiteboardNodeAsync(
            Args(("whiteboard_id", "wb1"), ("nodes_json", "[{\"type\":")),
            CancellationToken.None);

        result.Error.Should().NotBeNull("坏 JSON 必须在本地拒绝（附期望形态），不让下游报语焉不详的错误");
        result.ToString().Should().Contain("nodes_json");
        client.Invocations.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteNodes_Should_Call_BatchDelete_And_Report_Count()
    {
        BatchDeleteWhiteboardNodeRequest? captured = null;
        var client = new Mock<Mud.Feishu.IFeishuTenantV1Board>();
        client.Setup(c => c.BatchDeleteWhiteboardNodeAsync(
                It.IsAny<string>(), It.IsAny<BatchDeleteWhiteboardNodeRequest>(),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback((string _, BatchDeleteWhiteboardNodeRequest request, string? __, CancellationToken ___) => captured = request)
            .ReturnsAsync(new FeishuApiResult<BatchDeleteWhiteboardNodeResult> { Code = 0, Data = null });

        var result = await CreateTools(client).BatchDeleteWhiteboardNodeAsync(
            Args(("whiteboard_id", "wb1"), ("node_ids", new[] { "n1", "n2" })),
            CancellationToken.None);

        captured!.Ids.Should().BeEquivalentTo(new[] { "n1", "n2" });

        var payload = Payload(result);
        payload["deleted"]!.GetValue<bool>().Should().BeTrue();
        payload["deleted_count"]!.GetValue<int>().Should().Be(2);
    }

    [Fact]
    public async Task DeleteNodes_Should_Reject_Empty_NodeIds()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1Board>();

        var result = await CreateTools(client).BatchDeleteWhiteboardNodeAsync(
            Args(("whiteboard_id", "wb1"), ("node_ids", Array.Empty<string>())),
            CancellationToken.None);

        result.Error.Should().NotBeNull();
        result.ToString().Should().Contain("node_ids");
        client.Invocations.Should().BeEmpty();
    }

    // ───────────────────── 软依赖 ─────────────────────

    [Fact]
    public async Task Tools_Should_Report_Actionable_Error_When_Board_Client_Missing()
    {
        var result = await CreateTools(null).GetWhiteboardNodesAsync(
            Args(("whiteboard_id", "wb1")),
            CancellationToken.None);

        result.Error.Should().NotBeNull();
        result.ToString().Should().Contain("IFeishuTenantV1Board",
            "软缺席时给出的错误必须告诉宿主该启用什么");
    }
}
