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
using Mud.Feishu.DataModels.Wiki;

namespace Mud.Feishu.AI.Tools.Tests.Tools;

/// <summary>
/// Wiki 空间与成员面（<c>space_list</c> / <c>space_get</c> / <c>member_list</c> /
/// <c>member_add</c> / <c>member_remove</c> / <c>node_copy</c>）断言。
/// </summary>
/// <remarks>
/// 三条本域特有的防线：
/// ① 空间侧客户端是<b>软依赖</b>——缺席时只让空间类工具给可执行提示，节点类工具必须照常工作；
/// ② 成员三元组（member_type/member_id/member_role）是删除定位键，闭集校验必须<b>先于</b> dry-run；
/// ③ 空的空间列表是"身份没被加入"而不是"企业没有知识库"，投影必须把这句话带出去。
/// </remarks>
public class WikiSpaceAndMemberToolsTests
{
    private static IReadOnlyDictionary<string, object?> Args(params (string Key, object? Value)[] items)
        => items.ToDictionary(static p => p.Key, static p => p.Value, StringComparer.Ordinal);

    private static JsonObject Payload(ToolResult result) => JsonNode.Parse(result.ToString())!.AsObject();

    private static Mock<IFeishuTenantV2WikiNodes> CreateNodeClient()
    {
        var client = new Mock<IFeishuTenantV2WikiNodes>();
        client.Setup(c => c.CopySpaceNodeAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CopySpaceNodeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<SpaceNodeResult>
            {
                Code = 0,
                Data = new SpaceNodeResult
                {
                    Node = new SpaceNodeInfo
                    {
                        NodeToken = "wikcnCopy", ObjToken = "doxcnCopy", ObjType = "docx",
                        Title = "副本", SpaceId = "7target", ParentNodeToken = "wikcnParent",
                    },
                },
            });
        return client;
    }

    private static Mock<IFeishuTenantV2Wiki> CreateSpaceClient(
        bool emptySpaces = false,
        bool hasMoreOnFirstPage = false)
    {
        var client = new Mock<IFeishuTenantV2Wiki>();

        client.Setup(c => c.GetSpacesPageListAsync(
                It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int _, string? pageToken, CancellationToken __) => pageToken is null && hasMoreOnFirstPage
                ? new FeishuApiPageListResult<SpaceInfo>
                {
                    Code = 0,
                    Data = new ApiPageListResult<SpaceInfo>
                    {
                        Items = { new SpaceInfo { SpaceId = "7a", Name = "团队知识库 A", SpaceType = "team", Visibility = "private" } },
                        HasMore = true,
                        PageToken = "sp2",
                    },
                }
                : new FeishuApiPageListResult<SpaceInfo>
                {
                    Code = 0,
                    Data = new ApiPageListResult<SpaceInfo>
                    {
                        Items =
                        {
                            new SpaceInfo
                            {
                                SpaceId = "7b", Name = "团队知识库 B", Description = "研发规范",
                                SpaceType = "team", Visibility = "public", OpenSharing = "open",
                            },
                        },
                        HasMore = false,
                    },
                });

        client.Setup(c => c.GetSpaceInfoAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<SpaceInfoResult>
            {
                Code = 0,
                Data = new SpaceInfoResult
                {
                    Space = new SpaceInfo
                    {
                        SpaceId = "7b", Name = "团队知识库 B", SpaceType = "team",
                        Visibility = "public", OpenSharing = "close",
                    },
                },
            });

        if (emptySpaces)
        {
            client.Setup(c => c.GetSpacesPageListAsync(
                    It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FeishuApiPageListResult<SpaceInfo>
                {
                    Code = 0,
                    Data = new ApiPageListResult<SpaceInfo> { HasMore = false },
                });
        }

        client.Setup(c => c.GetSpaceMemberPageListAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<SpaceMemberResult>
            {
                Code = 0,
                Data = new SpaceMemberResult
                {
                    Members =
                    [
                        new SpaceMemberInfo { MemberType = "openid", MemberId = "ou_admin", MemberRole = "admin" },
                        new SpaceMemberInfo { MemberType = "openid", MemberId = "ou_dev", MemberRole = "member" },
                    ],
                    HasMore = false,
                },
            });

        client.Setup(c => c.CreateSpaceMemberAsync(
                It.IsAny<string>(), It.IsAny<CreateSpaceMemberRequest>(), It.IsAny<bool?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<SpaceInfoResult>
            {
                Code = 0,
                Data = new SpaceInfoResult { Space = new SpaceInfo { SpaceId = "7b", Name = "团队知识库 B" } },
            });

        client.Setup(c => c.DeleteSpaceMemberAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DeleteSpaceMemberRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<DeleteSpaceMemberResult>
            {
                Code = 0,
                Data = new DeleteSpaceMemberResult
                {
                    Member = new SpaceMemberInfo { MemberType = "openid", MemberId = "ou_dev", MemberRole = "member" },
                },
            });

        return client;
    }

    private static WikiTools CreateTools(
        Mock<IFeishuTenantV2WikiNodes>? nodes = null,
        Mock<IFeishuTenantV2Wiki>? space = null)
        => new(
            (nodes ?? CreateNodeClient()).Object,
            space?.Object,
            Microsoft.Extensions.Options.Options.Create(
                new FeishuAgentOptions { Instructions = "test", MaxToolResultLength = 8000 }));

    // ───────────────────── 只读：space_list / space_get / member_list ─────────────────────

    /// <summary>fetch_all 必须真翻页；投影只给空间白名单六项。</summary>
    [Fact]
    public async Task ListSpaces_FetchAll_ShouldAggregateAndProjectWhitelist()
    {
        var client = CreateSpaceClient(hasMoreOnFirstPage: true);

        var payload = Payload(await CreateTools(space: client)
            .ListSpacesAsync(Args(("fetch_all", true)), CancellationToken.None));

        var items = payload["items"]!.AsArray();
        items.Count.Should().Be(2, "has_more=true 时聚合路径必须续取下一页");
        items[0]!["space_id"]!.GetValue<string>().Should().Be("7a");
        items[1]!["visibility"]!.GetValue<string>().Should().Be("public");
        items[1]!["open_sharing"]!.GetValue<string>().Should().Be("open");
        payload["pages_fetched"]!.GetValue<int>().Should().Be(2);
    }

    /// <summary>
    /// <b>空列表不得让模型读成"企业没有知识库"</b>：平台按可见性过滤，空列表只说明当前身份没被加入。
    /// </summary>
    [Fact]
    public async Task ListSpaces_Empty_ShouldExplainVisibilityFilter()
    {
        var client = CreateSpaceClient(emptySpaces: true);

        var payload = Payload(await CreateTools(space: client)
            .ListSpacesAsync(Args(), CancellationToken.None));

        payload["items"]!.AsArray().Should().BeEmpty();
        payload["note"]!.GetValue<string>().Should()
            .Contain("当前调用身份没有被加入任何知识空间")
            .And.Contain("wiki.member_add", "空结果要把「怎么被加入」的下一步指出来");
    }

    /// <summary>space_get 的 visibility/space_type 是 member_add 的前置判据，必须原样投出。</summary>
    [Fact]
    public async Task GetSpace_ShouldProjectVisibilityAndSpaceType()
    {
        var client = CreateSpaceClient();

        var payload = Payload(await CreateTools(space: client)
            .GetSpaceAsync(Args(("space_id", "7b")), CancellationToken.None));

        var space = payload["space"]!.AsObject();
        space["visibility"]!.GetValue<string>().Should().Be("public");
        space["space_type"]!.GetValue<string>().Should().Be("team");
        space.Should().NotBeNull();
        space.ContainsKey("is_trash").Should().BeFalse("投影走白名单，未列入的存量字段不得搭车出站");
    }

    /// <summary>member_list 投出的是删除定位所需的三元组，缺一即无法安全调用 member_remove。</summary>
    [Fact]
    public async Task ListMembers_ShouldProjectIdentityTriple()
    {
        var client = CreateSpaceClient();

        var payload = Payload(await CreateTools(space: client)
            .ListMembersAsync(Args(("space_id", "7b")), CancellationToken.None));

        var items = payload["items"]!.AsArray();
        items.Should().HaveCount(2);
        items[0]!["member_type"]!.GetValue<string>().Should().Be("openid");
        items[0]!["member_id"]!.GetValue<string>().Should().Be("ou_admin");
        items[0]!["member_role"]!.GetValue<string>().Should().Be("admin");
    }

    // ───────────────────── 写面：闭集校验 / dry-run / 投影 ─────────────────────

    /// <summary>
    /// <b>校验先于预演</b>：闭集外的 member_type/member_role 即便 dry_run 也必须被拒——
    /// 预演"看起来会成功"等于把失败推到真实调用那一侧。
    /// </summary>
    [Theory]
    [InlineData("department", "admin", "member_type")]
    [InlineData("openid", "owner", "member_role")]
    public async Task AddMember_ShouldRejectClosedSetViolation_EvenInDryRun(
        string memberType, string memberRole, string expectedField)
    {
        var client = CreateSpaceClient();

        var result = await CreateTools(space: client).AddMemberAsync(
            Args(
                ("space_id", "7b"),
                ("member_type", memberType),
                ("member_id", "ou_dev"),
                ("member_role", memberRole),
                ("dry_run", true)),
            CancellationToken.None);

        result.ToString()!.Should().Contain(expectedField);
        client.VerifyNoOtherCalls();
    }

    /// <summary>
    /// 本工具固定走 tenant access token，平台在该身份下拒绝 <c>opendepartmentid</c>——
    /// 它是闭集内的合法值，因此需要单独一条拒绝路径，并把替代取值说清楚。
    /// </summary>
    [Fact]
    public async Task AddMember_ShouldRejectOpenDepartmentId_UnderTenantIdentity()
    {
        var client = CreateSpaceClient();

        var result = await CreateTools(space: client).AddMemberAsync(
            Args(
                ("space_id", "7b"),
                ("member_type", "opendepartmentid"),
                ("member_id", "od_dept"),
                ("member_role", "member")),
            CancellationToken.None);

        var text = result.ToString()!;
        text.Should().Contain("tenant access token");
        text.Should().Contain("openid");
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AddMember_DryRun_ShouldDescribeRequestWithoutCallingDownstream()
    {
        var client = CreateSpaceClient();

        var text = (await CreateTools(space: client).AddMemberAsync(
            Args(
                ("space_id", "7b"),
                ("member_type", "openid"),
                ("member_id", "ou_dev"),
                ("member_role", "admin"),
                ("dry_run", true)),
            CancellationToken.None)).ToString()!;

        text.Should().StartWith("[dry_run]");
        text.Should().Contain("POST /open-apis/wiki/v2/spaces/7b/members");
        text.Should().NotContain("admin", "请求体字段只回长度，不回显取值");
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AddMember_ShouldReturnSpaceAndVerificationNote()
    {
        var client = CreateSpaceClient();

        var payload = Payload(await CreateTools(space: client).AddMemberAsync(
            Args(
                ("space_id", "7b"),
                ("member_type", "openid"),
                ("member_id", "ou_dev"),
                ("member_role", "member")),
            CancellationToken.None));

        payload["space"]!["space_id"]!.GetValue<string>().Should().Be("7b");
        payload["note"]!.GetValue<string>().Should().Contain("wiki.member_list",
            "平台可能不回填新成员，须给出复核路径");
    }

    [Fact]
    public async Task RemoveMember_DryRun_ShouldDescribeDeleteWithoutCallingDownstream()
    {
        var client = CreateSpaceClient();

        var text = (await CreateTools(space: client).RemoveMemberAsync(
            Args(
                ("space_id", "7b"),
                ("member_id", "ou_dev"),
                ("member_type", "openid"),
                ("member_role", "member"),
                ("dry_run", true)),
            CancellationToken.None)).ToString()!;

        text.Should().Contain("DELETE /open-apis/wiki/v2/spaces/7b/members/ou_dev");
        client.VerifyNoOtherCalls();
    }

    /// <summary>回执必须回显定位三元组：角色填错时平台仍可能返回成功，只有三项可比对。</summary>
    [Fact]
    public async Task RemoveMember_ShouldEchoLocatedTriple()
    {
        var client = CreateSpaceClient();

        var payload = Payload(await CreateTools(space: client).RemoveMemberAsync(
            Args(
                ("space_id", "7b"),
                ("member_id", "ou_dev"),
                ("member_type", "openid"),
                ("member_role", "member")),
            CancellationToken.None));

        payload["removed"]!.GetValue<bool>().Should().BeTrue();
        payload["member_id"]!.GetValue<string>().Should().Be("ou_dev");
        payload["member_role"]!.GetValue<string>().Should().Be("member");
    }

    /// <summary>与 move_node 同款空操作防线：目标全空时平台不会复制，但调用仍会"成功"。</summary>
    [Fact]
    public async Task CopyNode_ShouldReject_WhenBothTargetsMissing()
    {
        var nodes = CreateNodeClient();

        var result = await CreateTools(nodes).CopyNodeAsync(
            Args(("space_id", "7a"), ("node_token", "wikcnA")), CancellationToken.None);

        var text = result.ToString()!;
        text.Should().Contain("target_parent_token");
        text.Should().Contain("空操作");
        nodes.VerifyNoOtherCalls();
    }

    /// <summary>副本必须回新 node_token / obj_token——与源节点不同才说明复制真的发生。</summary>
    [Fact]
    public async Task CopyNode_ShouldReturnNewTokens()
    {
        var nodes = CreateNodeClient();

        var payload = Payload(await CreateTools(nodes).CopyNodeAsync(
            Args(("space_id", "7a"), ("node_token", "wikcnA"), ("target_space_id", "7target")),
            CancellationToken.None));

        payload["ok"]!.GetValue<bool>().Should().BeTrue();
        payload["node_token"]!.GetValue<string>().Should().Be("wikcnCopy");
        payload["obj_token"]!.GetValue<string>().Should().Be("doxcnCopy",
            "副本的底层文档也是新对象，沿用源 obj_token 会让后续编辑打到原文档");
    }

    // ───────────────────── 软缺席：空间侧客户端缺席 ─────────────────────

    /// <summary>
    /// 空间侧客户端缺席时给<b>可执行</b>提示（点名接口与装配入口），而不是空引用。
    /// </summary>
    [Theory]
    [InlineData("space_list")]
    [InlineData("space_get")]
    [InlineData("member_list")]
    [InlineData("member_add")]
    [InlineData("member_remove")]
    public async Task SpaceTools_ShouldFailClosed_WhenSpaceClientAbsent(string which)
    {
        var tools = CreateTools(space: null);
        var args = which switch
        {
            "space_list" => Args(),
            "space_get" => Args(("space_id", "7b")),
            "member_list" => Args(("space_id", "7b")),
            "member_add" => Args(("space_id", "7b"), ("member_type", "openid"), ("member_id", "ou"), ("member_role", "member")),
            _ => Args(("space_id", "7b"), ("member_id", "ou"), ("member_type", "openid"), ("member_role", "member")),
        };

        var text = which switch
        {
            "space_list" => (await tools.ListSpacesAsync(args, CancellationToken.None)).ToString()!,
            "space_get" => (await tools.GetSpaceAsync(args, CancellationToken.None)).ToString()!,
            "member_list" => (await tools.ListMembersAsync(args, CancellationToken.None)).ToString()!,
            "member_add" => (await tools.AddMemberAsync(args, CancellationToken.None)).ToString()!,
            _ => (await tools.RemoveMemberAsync(args, CancellationToken.None)).ToString()!,
        };

        text.Should().Contain("IFeishuTenantV2Wiki");
        text.Should().Contain("AddWikiApi");
    }

    /// <summary>节点侧不依赖空间客户端：缺席时必须照常工作（软缺席只圈空间类工具）。</summary>
    [Fact]
    public async Task NodeCopy_ShouldStillWork_WhenSpaceClientAbsent()
    {
        var payload = Payload(await CreateTools(nodes: CreateNodeClient(), space: null).CopyNodeAsync(
            Args(("space_id", "7a"), ("node_token", "wikcnA"), ("target_parent_token", "wikcnParent")),
            CancellationToken.None));

        payload["ok"]!.GetValue<bool>().Should().BeTrue();
    }
}
