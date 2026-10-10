// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json.Nodes;

using Mud.Feishu.AI.Tools.Internal;
using Mud.Feishu.DataModels.Docx;

namespace Mud.Feishu.AI.Tools.Tests.Tools;

/// <summary>
/// Docx 群公告面工具（R7 / A2）：<c>docx.get_chat_announcement</c> 读面分页 +
/// <c>docx.set_chat_announcement</c> 写面（敏感广播面）的参数纪律。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么公告写面单独测</b>：公告是<b>全员可见</b>的广播面（§2.A1 敏感工具纪律第 1/4 条），
/// 一旦被误启用就产生不可撤回的组织级影响；其入参是"块列表 JSON 字符串"，
/// 形态错误必须在<b>本地</b>被拒绝并附期望形态（否则模型只看到平台的语焉不详错误）。
/// </para>
/// </remarks>
public class DocxAnnouncementToolsTests
{
    private static FeishuAgentOptions AgentOptions() => new() { Instructions = "test" };

    private static IReadOnlyDictionary<string, object?> Args(params (string Key, object? Value)[] items)
        => items.ToDictionary(p => p.Key, p => p.Value);

    private static JsonObject Payload(FeishuToolResult result)
        => JsonNode.Parse(result.ToString())!.AsObject();

    private static DocxDeepTools CreateTools(Mock<Mud.Feishu.IFeishuTenantV1DocxAnnouncement> announcement)
        => new(
            new Mock<Mud.Feishu.IFeishuTenantV1Docx>().Object,
            new Mock<Mud.Feishu.IFeishuTenantV1DocxBlocks>().Object,
            announcement.Object,
            new Mock<Mud.Feishu.IFeishuTenantV1DriveFiles>().Object,
            Options.Create(AgentOptions()));

    // ───────────────────── 契约面：敏感广播面纪律 ─────────────────────

    [Fact]
    public void SetChatAnnouncement_Should_Be_Write_With_ConsequenceAndDryRun()
    {
        var contract = FeishuToolContracts.ByToolName[FeishuToolNames.DocxSetChatAnnouncement];

        contract.IsWrite.Should().BeTrue();
        contract.Description.Should().Contain("全员可见",
            "公告写面必须写明后果（§2.A1 敏感工具纪律第 1 条）");
        FeishuToolSchemas.SchemaByToolName[FeishuToolNames.DocxSetChatAnnouncement]
            .Should().Contain("dry_run", "每个写工具必须可预演（DryRunContractGuards）");
    }

    // ───────────────────── 读面 ─────────────────────

    [Fact]
    public async Task GetChatAnnouncement_Should_Pass_PageSize_And_PageToken()
    {
        int seenPageSize = 0;
        string? seenPageToken = null;

        var announcement = new Mock<Mud.Feishu.IFeishuTenantV1DocxAnnouncement>();
        announcement.Setup(c => c.GetChatAnnouncementBlocksPageListAsync(
                It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int>(), It.IsAny<string?>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((string _, int? __, int pageSize, string? pageToken, string ___, CancellationToken ____) =>
            {
                seenPageSize = pageSize;
                seenPageToken = pageToken;
            })
            .ReturnsAsync(new FeishuApiPageListResult<Block>
            {
                Code = 0,
                Data = new ApiPageListResult<Block>
                {
                    HasMore = true,
                    PageToken = "next",
                    Items = [new Block { BlockId = "blk1", BlockType = 2 }],
                },
            });

        var result = await CreateTools(announcement).GetChatAnnouncementBlocksPageListAsync(
            Args(("chat_id", "oc_1"), ("page_token", "prev")),
            CancellationToken.None);

        seenPageSize.Should().Be(PageSizes.DocxBlocks, "页大小来自 PageSizes（运维参数不进 Schema）");
        seenPageToken.Should().Be("prev");

        var payload = Payload(result);
        payload["has_more"]!.GetValue<bool>().Should().BeTrue();
        payload["page_token"]!.GetValue<string>().Should().Be("next");
        payload["items"]!.AsArray()[0]!["block_id"]!.GetValue<string>().Should().Be("blk1");
    }

    // ───────────────────── 写面 ─────────────────────

    [Fact]
    public async Task SetChatAnnouncement_Should_Build_UpdateRequests_From_BlocksJson()
    {
        BatchUpdateBlocksRequest? captured = null;
        string? seenClientToken = null;

        var announcement = new Mock<Mud.Feishu.IFeishuTenantV1DocxAnnouncement>();
        announcement.Setup(c => c.BatchUpdateChatAnnouncementBlocksAsync(
                It.IsAny<string>(), It.IsAny<BatchUpdateBlocksRequest>(), It.IsAny<int?>(),
                It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((
                string _,
                BatchUpdateBlocksRequest request,
                int? __,
                string? clientToken,
                string ___,
                CancellationToken ____) =>
            {
                captured = request;
                seenClientToken = clientToken;
            })
            // 解包器口径：Code==0 但 Data 为 null 会被判为"飞书接口返回空数据"（拒绝空载荷），
            // 故成功响应必须带一个非空 Data 实例。
            .ReturnsAsync(new FeishuApiResult<BatchUpdateChatAnnouncementBlocksResult>
            {
                Code = 0,
                Data = new BatchUpdateChatAnnouncementBlocksResult(),
            });

        var result = await CreateTools(announcement).BatchUpdateChatAnnouncementBlocksAsync(
            Args(
                ("chat_id", "oc_1"),
                ("blocks", """[{"block_id":"blk1","text":"本周公告：周五团建"}]"""),
                ("idempotency_key", "key-1")),
            CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Requests.Should().HaveCount(1);
        captured.Requests![0].BlockId.Should().Be("blk1");
        captured.Requests[0].UpdateTextElements!.Elements![0].TextRun!.Content.Should().Be("本周公告：周五团建");
        seenClientToken.Should().Be("key-1", "幂等键透传为 client_token（平台侧 24 小时去重）");

        Payload(result)["updated"]!.GetValue<int>().Should().Be(1);
    }

    [Fact]
    public async Task SetChatAnnouncement_Should_Reject_Non_Array_Blocks()
    {
        var announcement = new Mock<Mud.Feishu.IFeishuTenantV1DocxAnnouncement>();

        var result = await CreateTools(announcement).BatchUpdateChatAnnouncementBlocksAsync(
            Args(("chat_id", "oc_1"), ("blocks", """{"block_id":"blk1","text":"x"}""")),
            CancellationToken.None);

        result.Error.Should().NotBeNull();
        result.Error!.Category.Should().Be("validation");
        result.ToString().Should().Contain("blocks").And.Contain("JSON 数组");
        announcement.Invocations.Should().BeEmpty();
    }

    [Fact]
    public async Task SetChatAnnouncement_Should_Reject_Item_Without_BlockId()
    {
        var announcement = new Mock<Mud.Feishu.IFeishuTenantV1DocxAnnouncement>();

        var result = await CreateTools(announcement).BatchUpdateChatAnnouncementBlocksAsync(
            Args(("chat_id", "oc_1"), ("blocks", """[{"text":"缺少 block_id"}]""")),
            CancellationToken.None);

        result.Error.Should().NotBeNull();
        result.ToString().Should().Contain("block_id");
        announcement.Invocations.Should().BeEmpty();
    }

    [Fact]
    public async Task SetChatAnnouncement_Should_Not_Call_Downstream_When_DryRun()
    {
        var announcement = new Mock<Mud.Feishu.IFeishuTenantV1DocxAnnouncement>();

        var result = await CreateTools(announcement).BatchUpdateChatAnnouncementBlocksAsync(
            Args(
                ("chat_id", "oc_1"),
                ("blocks", """[{"block_id":"blk1","text":"预演"}]"""),
                ("dry_run", true)),
            CancellationToken.None);

        var text = result.ToString();
        text.Should().Contain("[dry_run]");
        text.Should().Contain("announcement/blocks/batch_update");
        announcement.Invocations.Should().BeEmpty();
    }
}
