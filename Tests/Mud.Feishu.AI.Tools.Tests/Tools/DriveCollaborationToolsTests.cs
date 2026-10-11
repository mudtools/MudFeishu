// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json.Nodes;

using Mud.Feishu.AI.Tools.Internal;
using Mud.Feishu.DataModels.Drive;

namespace Mud.Feishu.AI.Tools.Tests.Tools;

/// <summary>
/// Drive 协作面工具（R7 / A1）：评论 4 + 权限 6 的链路实参、敏感工具纪律与参数负例。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么这一域必须有独立行为用例</b>：权限面是<b>可扩大数据可达范围</b>的工具
/// （`update_permission_public` 能把文档公开到组织外、`transfer_owner` 不可逆），
/// 而它们全部走 <c>WriteAllowList</c> 键控 + 授权门禁——"风险等级/写面归属"是安全事实，
/// 不能只靠读代码相信（<c>drive.get_permission_public</c> 底层是 GET 却因权限域分级算写面，
/// 正是容易被"顺手改成只读"的形态）。
/// </para>
/// <para>
/// 第二组断言是<b>模型零 JSON 串</b>：评论文本走扁平参数，请求体由执行器组装
/// （<c>ReplyList → Replies[0] → Content.Elements[0].TextRun.Text</c>）。
/// </para>
/// </remarks>
public class DriveCollaborationToolsTests
{
    private static FeishuAgentOptions AgentOptions() => new() { Instructions = "test" };

    private static IReadOnlyDictionary<string, object?> Args(params (string Key, object? Value)[] items)
        => items.ToDictionary(p => p.Key, p => p.Value);

    private static DriveCommentTools CreateCommentTools(Mock<Mud.Feishu.IFeishuTenantV1DriveComments> client)
        => new(client.Object, Options.Create(AgentOptions()));

    private static DrivePermissionTools CreatePermissionTools(Mock<Mud.Feishu.IFeishuTenantV1DrivePermissions> client)
        => new(client.Object, Options.Create(AgentOptions()));

    private static JsonObject Payload(FeishuToolResult result)
        => JsonNode.Parse(result.ToString())!.AsObject();

    // ───────────────────── 契约面（安全事实） ─────────────────────

    [Fact]
    public void PermissionTools_Should_All_Be_WriteSide()
    {
        var permissionTools = new[]
        {
            FeishuToolNames.DriveGetPermissionPublic,
            FeishuToolNames.DriveUpdatePermissionPublic,
            FeishuToolNames.DriveGrantPermission,
            FeishuToolNames.DriveUpdatePermissionMember,
            FeishuToolNames.DriveRemovePermission,
            FeishuToolNames.DriveTransferOwner,
        };

        foreach (var toolName in permissionTools)
        {
            var contract = FeishuToolContracts.ByToolName[toolName];

            contract.IsWrite.Should().BeTrue(
                $"{toolName} 属权限域：即使底层是 GET（get_permission_public），也按引擎危险词分级走写面授权门禁（MUDFT017）");
            contract.Risk.Should().Be(FeishuToolRisk.HighRiskWrite,
                $"{toolName} 的 MCP/Agent 侧 destructiveHint 与 MaxToolRisk 判定都读这个值");
            FeishuToolNames.IsWriteTool(toolName).Should().BeTrue();
        }
    }

    [Fact]
    public void SensitiveTools_Should_DeclareConsequence_InDescription()
    {
        // §2.A1 敏感工具纪律第 1 条：可能扩大数据可达范围的工具，描述必须含后果句。
        FeishuToolContracts.ByToolName[FeishuToolNames.DriveUpdatePermissionPublic].Description
            .Should().Contain("组织外", "公开链接权限可能把文档暴露到组织外——描述必须写明后果");
        FeishuToolContracts.ByToolName[FeishuToolNames.DriveTransferOwner].Description
            .Should().Contain("不可逆", "转移所有者不可撤销，描述必须写明后果");
        FeishuToolContracts.ByToolName[FeishuToolNames.DriveGrantPermission].Description
            .Should().Contain("扩大数据可达范围");
    }

    // ───────────────────── 评论面 ─────────────────────

    [Fact]
    public async Task ListComments_Should_Pass_PageSize_From_PageSizes_And_PageToken()
    {
        int? seenPageSize = null;
        string? seenPageToken = null;
        bool? seenIsSolved = null;

        var client = new Mock<Mud.Feishu.IFeishuTenantV1DriveComments>();
        client.Setup(c => c.GetCommentsPageListAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<bool?>(),
                It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<int>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback((
                string _,
                string __,
                bool? ___,
                bool? isSolved,
                bool? ____,
                bool? _____,
                int pageSize,
                string? pageToken,
                string? ______,
                CancellationToken _______) =>
            {
                seenPageSize = pageSize;
                seenPageToken = pageToken;
                seenIsSolved = isSolved;
            })
            .ReturnsAsync(new FeishuApiPageListResult<FileComment>
            {
                Code = 0,
                Data = new ApiPageListResult<FileComment>
                {
                    HasMore = true,
                    PageToken = "next-token",
                    Items =
                    [
                        new FileComment
                        {
                            CommentId = "c1",
                            UserId = "ou_1",
                            IsSolved = false,
                            CreateTime = 1700000000,
                            ReplyList = new FileCommentReplyList
                            {
                                Replies = [new FileCommentReply { ReplyId = "r1" }],
                            },
                        },
                    ],
                },
            });

        var result = await CreateCommentTools(client).ListCommentsAsync(
            Args(("file_token", "doxcn1"), ("file_type", "docx"), ("is_solved", true), ("page_token", "prev-token")),
            CancellationToken.None);

        seenPageSize.Should().Be(PageSizes.DriveComments, "页大小是运维参数：只能来自 PageSizes（不进 Schema）");
        seenPageToken.Should().Be("prev-token", "翻页游标必须原样回传");
        seenIsSolved.Should().BeTrue();

        var payload = Payload(result);
        payload["has_more"]!.GetValue<bool>().Should().BeTrue();
        payload["page_token"]!.GetValue<string>().Should().Be("next-token");
        var item = payload["items"]!.AsArray()[0]!.AsObject();
        item["comment_id"]!.GetValue<string>().Should().Be("c1");
        item["reply_count"]!.GetValue<int>().Should().Be(1, "投影给出回复数，模型不必再拉一次回复列表");
    }

    [Fact]
    public async Task ListComments_Should_Aggregate_Pages_When_FetchAll()
    {
        var calls = 0;
        var client = new Mock<Mud.Feishu.IFeishuTenantV1DriveComments>();
        client.Setup(c => c.GetCommentsPageListAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<bool?>(),
                It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<int>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback(() => calls++)
            .ReturnsAsync(() => calls == 1
                ? new FeishuApiPageListResult<FileComment>
                {
                    Code = 0,
                    Data = new ApiPageListResult<FileComment>
                    {
                        HasMore = true,
                        PageToken = "t2",
                        Items = [new FileComment { CommentId = "c1" }],
                    },
                }
                : new FeishuApiPageListResult<FileComment>
                {
                    Code = 0,
                    Data = new ApiPageListResult<FileComment>
                    {
                        HasMore = false,
                        Items = [new FileComment { CommentId = "c2" }],
                    },
                });

        var result = await CreateCommentTools(client).ListCommentsAsync(
            Args(("file_token", "doxcn1"), ("file_type", "docx"), ("fetch_all", true)),
            CancellationToken.None);

        calls.Should().Be(2, "fetch_all=true 时由 ToolPagination 循环翻页直到 has_more=false");
        var payload = Payload(result);
        payload["items"]!.AsArray().Count.Should().Be(2);
        payload["has_more"]!.GetValue<bool>().Should().BeFalse();
    }

    [Fact]
    public async Task AddComment_Should_Build_ReplyList_From_Flat_Text_Parameter()
    {
        CreateFileCommentRequest? captured = null;
        string? capturedFileType = null;

        var client = new Mock<Mud.Feishu.IFeishuTenantV1DriveComments>();
        client.Setup(c => c.CreateFileCommentAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CreateFileCommentRequest>(),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback((string _, string fileType, CreateFileCommentRequest request, string? __, CancellationToken ___) =>
            {
                captured = request;
                capturedFileType = fileType;
            })
            .ReturnsAsync(new FeishuApiResult<CreateFileCommentResult>
            {
                Code = 0,
                Data = new CreateFileCommentResult { CommentId = "c9" },
            });

        var result = await CreateCommentTools(client).AddCommentAsync(
            Args(("file_token", "doxcn1"), ("file_type", "docx"), ("text", "请补充数据来源")),
            CancellationToken.None);

        capturedFileType.Should().Be("docx");
        captured.Should().NotBeNull();
        captured!.ReplyList!.Replies!.Single().Content.Elements!.Single().TextRun!.Text
            .Should().Be("请补充数据来源", "评论文本走扁平参数，请求体由执行器组装（模型零 JSON 串）");

        Payload(result)["comment_id"]!.GetValue<string>().Should().Be("c9");
    }

    [Fact]
    public async Task AddComment_Should_Reject_Empty_Text_Without_DownstreamCall()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1DriveComments>();

        var result = await CreateCommentTools(client).AddCommentAsync(
            Args(("file_token", "doxcn1"), ("file_type", "docx"), ("text", "   ")),
            CancellationToken.None);

        result.Error.Should().NotBeNull();
        result.Error!.Category.Should().Be("validation");
        result.ToString().Should().Contain("text");
        client.Invocations.Should().BeEmpty();
    }

    [Fact]
    public async Task ResolveComment_Should_Reject_Missing_IsSolved()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1DriveComments>();

        var result = await CreateCommentTools(client).ResolveCommentAsync(
            Args(("file_token", "doxcn1"), ("comment_id", "c1"), ("file_type", "docx")),
            CancellationToken.None);

        // is_solved 是必填布尔：引擎解包映射表不支持必填布尔（Schema 层无法标 required），
        // 故执行器兜底拒绝——"Schema 是提示不是安全边界"。
        result.Error.Should().NotBeNull();
        result.Error!.Subtype.Should().Be(ToolErrorSubtype.InvalidArgs);
        result.ToString().Should().Contain("is_solved");
        client.Invocations.Should().BeEmpty();
    }

    // ───────────────────── 权限面 ─────────────────────

    [Fact]
    public async Task GrantPermission_Should_Reject_Illegal_MemberType_With_AllowedValues()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1DrivePermissions>();

        var result = await CreatePermissionTools(client).GrantPermissionAsync(
            Args(
                ("token", "doxcn1"),
                ("type", "docx"),
                ("member_type", "bogus"),
                ("member_id", "ou_1"),
                ("perm", "view")),
            CancellationToken.None);

        var text = result.ToString();
        text.Should().Contain("member_type").And.Contain("openid").And.Contain("email");
        client.Invocations.Should().BeEmpty("非法枚举值必须本地拒绝（附合法值清单），不落到下游");
    }

    [Fact]
    public async Task TransferOwner_Should_Pass_Member_And_Not_Call_Downstream_When_DryRun()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1DrivePermissions>();

        var result = await CreatePermissionTools(client).TransferOwnerAsync(
            Args(
                ("token", "doxcn1"),
                ("type", "docx"),
                ("member_type", "openid"),
                ("member_id", "ou_new"),
                ("dry_run", true)),
            CancellationToken.None);

        var text = result.ToString();
        text.Should().Contain("[dry_run]");
        text.Should().Contain("/open-apis/drive/v1/permissions/{token}/members/transfer_owner",
            "预演只给 method/path（模板字面量，不代入真实 token）");
        client.Invocations.Should().BeEmpty("不可逆操作必须先预演确认");
    }

    [Fact]
    public async Task UpdatePermissionPublic_Should_Expose_Consequence_Warning_And_DryRun()
    {
        var schema = FeishuToolSchemas.SchemaByToolName[FeishuToolNames.DriveUpdatePermissionPublic];

        schema.Should().Contain("组织外", "危险工具的 Schema 描述必须携带后果句（模型看得到）");
        schema.Should().Contain("dry_run", "每个写工具必须可预演（DryRunContractGuards）");
    }
}
