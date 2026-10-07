// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;

using Moq;

using Mud.Feishu.AI.FeishuTools.Internal;
using Mud.Feishu.DataModels;
using Mud.Feishu.DataModels.Wiki;

namespace Mud.Feishu.AI.FeishuTools.Tests.Tools;

/// <summary>
/// R5 / F-11：wiki 写面（<c>create_node</c> / <c>move_node</c> / <c>move_docs_to_space</c>）断言。
/// </summary>
/// <remarks>
/// 重点锁两条<b>"调用成功但结果等于没做"</b>的防线：
/// ① move_node 目标全空 → 平台原地不动却回成功；
/// ② move_docs_to_space 是异步任务 → 立即返回不代表已生效。
/// 这两处若漏掉，模型会拿到"成功"却什么都没发生的结果。
/// </remarks>
public class WikiWriteToolsTests
{
    private static Dictionary<string, object?> Args(params (string Key, object? Value)[] pairs)
        => pairs.ToDictionary(static p => p.Key, static p => p.Value, StringComparer.Ordinal);

    private static (Mock<IFeishuTenantV2WikiNodes> Client, List<string> Calls) CreateClient()
    {
        var calls = new List<string>();
        var client = new Mock<IFeishuTenantV2WikiNodes>();

        client
            .Setup(c => c.CreateSpaceNodeAsync(
                It.IsAny<string>(), It.IsAny<CreateSpaceNodeRequest>(), It.IsAny<CancellationToken>()))
            .Callback<string, CreateSpaceNodeRequest, CancellationToken>((_, _, _) => calls.Add("create"))
            .ReturnsAsync(new FeishuApiResult<SpaceNodeResult>
            {
                Code = 0,
                Data = new SpaceNodeResult
                {
                    Node = new SpaceNodeInfo { NodeToken = "wikcnNew", Title = "新页面", ObjType = "docx" },
                },
            });

        client
            .Setup(c => c.MoveSpaceNodeAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MoveSpaceNodeRequest>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, MoveSpaceNodeRequest, CancellationToken>((_, _, _, _) => calls.Add("move"))
            .ReturnsAsync(new FeishuApiResult<SpaceNodeResult>
            {
                Code = 0,
                Data = new SpaceNodeResult { Node = new SpaceNodeInfo { NodeToken = "wikcnNew" } },
            });

        client
            .Setup(c => c.MoveDocsToWikiSpaceNodeAsync(
                It.IsAny<string>(), It.IsAny<MoveDocsToWikiSpaceNodeRequest>(), It.IsAny<CancellationToken>()))
            .Callback<string, MoveDocsToWikiSpaceNodeRequest, CancellationToken>((_, _, _) => calls.Add("move_docs"))
            .ReturnsAsync(new FeishuApiResult<MoveDocsToWikiSpaceNodeResult>
            {
                Code = 0,
                Data = new MoveDocsToWikiSpaceNodeResult { WikiToken = "wikcnNew", TaskId = "t-1", Applied = false },
            });

        return (client, calls);
    }

    private static WikiTools CreateTools(Mock<IFeishuTenantV2WikiNodes> client)
        => new(
            client.Object,
            Microsoft.Extensions.Options.Options.Create(
                new FeishuAgentOptions { Instructions = "test", MaxToolResultLength = 8000 }));

    [Fact]
    public async Task CreateNode_ShouldReturnNewNodeIdentity()
    {
        var (client, calls) = CreateClient();

        var result = await CreateTools(client).CreateNodeAsync(
            Args(("space_id", "7xxx"), ("title", "新页面")),
            CancellationToken.None);

        calls.Should().Equal(["create"]);
        result.Text.Should().Contain("wikcnNew");
        result.Text.Should().Contain("新页面");
    }

    /// <summary>
    /// <b>目标全空必须提前拒绝</b>：平台会原地不动却回成功，模型会误判"已移动"。
    /// </summary>
    [Fact]
    public async Task MoveNode_ShouldReject_WhenBothTargetsMissing()
    {
        var (client, calls) = CreateClient();

        var result = await CreateTools(client).MoveNodeAsync(
            Args(("space_id", "7xxx"), ("node_token", "wikcnA")),
            CancellationToken.None);

        calls.Should().BeEmpty("非法移动不得下发到平台");
        result.Text.Should().Contain("target_parent_token");
        result.Text.Should().Contain("空操作", "必须点明'会成功但等于没做'，否则模型无从察觉");
    }

    /// <summary>
    /// <b>异步任务不得谎报成功</b>：必须如实回传 <c>applied=false</c> 并给出核验指引。
    /// </summary>
    [Fact]
    public async Task MoveDocsToSpace_ShouldReportAppliedState_NotJustSuccess()
    {
        var (client, calls) = CreateClient();

        var result = await CreateTools(client).MoveDocsToSpaceAsync(
            Args(("space_id", "7xxx"), ("obj_token", "doxcnA")),
            CancellationToken.None);

        calls.Should().Equal(["move_docs"]);

        using var document = JsonDocument.Parse(result.Text);
        var root = document.RootElement;
        root.GetProperty("applied").GetBoolean().Should().BeFalse("平台尚未生效时不得报成功");
        root.GetProperty("task_id").GetString().Should().Be("t-1", "必须回传 task_id 供后续核验");
        root.GetProperty("note").GetString().Should().Contain("wiki.get_node", "应指引如何确认最终结果");
    }

    /// <summary>dry_run 不得触达下游（wiki 写是写操作，预演必须无副作用）。</summary>
    [Fact]
    public async Task WikiWriteTools_DryRun_ShouldNotCallDownstream()
    {
        var (client, calls) = CreateClient();
        var tools = CreateTools(client);

        await tools.CreateNodeAsync(
            Args(("space_id", "7xxx"), ("title", "t"), ("dry_run", true)), CancellationToken.None);
        await tools.MoveDocsToSpaceAsync(
            Args(("space_id", "7xxx"), ("obj_token", "o"), ("dry_run", true)), CancellationToken.None);

        calls.Should().BeEmpty("dry_run 必须完全不触达下游");
    }
}