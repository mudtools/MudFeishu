// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Wiki;

namespace Mud.Feishu.AI.Tools.Tests.Tools;

/// <summary>
/// <c>wiki.space_create</c> 的链路用例——wiki 域里唯一<b>只有用户身份形态</b>的写工具。
/// </summary>
/// <remarks>
/// <para>
/// 本工具的风险不在删改，而在<b>身份</b>与<b>幂等</b>：平台的空间创建没有 tenant 端点，也没有幂等键，
/// 重复调用会创建多个同名空间。因此链路断言必须落到执行器的<b>用户侧</b>客户端
/// （<c>IFeishuUserV2Wiki</c>，软依赖），并把「该用返回的 space_id 继续做什么」写进回执。
/// </para>
/// </remarks>
public class WikiSpaceCreateToolsChainTests
{
    private readonly Mock<Mud.Feishu.IFeishuTenantV2WikiNodes> _nodesClient = new();
    private readonly Mock<Mud.Feishu.IFeishuUserV2Wiki> _userSpaceClient = new();

    private static IReadOnlyDictionary<string, object?> Args(params (string Key, object? Value)[] items)
        => items.ToDictionary(p => p.Key, p => p.Value);

    private WikiTools CreateTools()
        => new(
            _nodesClient.Object,
            null,
            Options.Create(new FeishuAgentOptions { Instructions = "test", MaxToolResultLength = 8000 }),
            _userSpaceClient.Object);

    private static FeishuApiResult<SpaceInfoResult> SpaceResponse(string spaceId, string name)
        => new()
        {
            Code = 0,
            Data = new SpaceInfoResult
            {
                Space = new SpaceInfo { SpaceId = spaceId, Name = name, SpaceType = "team", Visibility = "default", OpenSharing = "open" },
            },
        };

    // ───────────────────── 契约事实 ─────────────────────

    [Fact]
    public void Contract_ShouldCarrySdkMethodRouteAndUserIdentity()
    {
        var contract = FeishuToolContracts.ByToolName["wiki.space_create"];

        contract.HttpMethod.Should().Be("POST");
        contract.Route.Should().Be("/open-apis/wiki/v2/spaces");
        contract.Identity.Should().Be("user", "平台没有 tenant 形态的空间创建——身份写错即运行期被拒");
        contract.Risk.Should().Be(FeishuToolRisk.Write);
        contract.IsWrite.Should().BeTrue();
        contract.RequiredScopes.Should().Contain("wiki:wiki");
    }

    // ───────────────────── 真实链路 ─────────────────────

    [Fact]
    public async Task CreateSpace_ShouldPostRequestBodyWhitelist_AndHandTheSpaceIdDownstream()
    {
        CreateSpaceRequest? captured = null;
        _userSpaceClient
            .Setup(c => c.CreateSpaceAsync(It.IsAny<CreateSpaceRequest>(), It.IsAny<CancellationToken>()))
            .Callback((CreateSpaceRequest request, CancellationToken _) => captured = request)
            .ReturnsAsync(SpaceResponse("7600000000000000001", "产品知识库"));

        var text = (await CreateTools().CreateSpaceAsync(
            Args(("name", "产品知识库"), ("description", "面向研发的资料空间"), ("open_sharing", "open")),
            CancellationToken.None)).ToString()!;

        captured!.Name.Should().Be("产品知识库");
        captured.Description.Should().Be("面向研发的资料空间");
        captured.OpenSharing.Should().Be("open", "分享状态原样透传（闭集已在入口校验）");

        using var document = JsonDocument.Parse(text);
        var root = document.RootElement;
        root.GetProperty("space").GetProperty("space_id").GetString().Should().Be("7600000000000000001");
        root.GetProperty("note").GetString().Should().Contain("wiki.create_node",
            "新空间此刻是空节点——回执必须把下一步出口交给模型，否则创建只是半程");
    }

    [Fact]
    public async Task CreateSpace_WhenOpenSharingOmitted_ShouldNotInjectAPlatformDefault()
    {
        CreateSpaceRequest? captured = null;
        _userSpaceClient
            .Setup(c => c.CreateSpaceAsync(It.IsAny<CreateSpaceRequest>(), It.IsAny<CancellationToken>()))
            .Callback((CreateSpaceRequest request, CancellationToken _) => captured = request)
            .ReturnsAsync(SpaceResponse("7600000000000000002", "空分享态空间"));

        await CreateTools().CreateSpaceAsync(Args(("name", "空分享态空间")), CancellationToken.None);

        captured!.OpenSharing.Should().BeNull(
            "不传即不发——绑定层替模型选一个分享态等于擅自改变可见范围（安全默认值不得削弱）");
    }

    [Fact]
    public async Task CreateSpace_WhenOpenSharingNotInClosedSet_ShouldReject_WithoutCallingDownstream()
    {
        var text = (await CreateTools().CreateSpaceAsync(
            Args(("name", "空间"), ("open_sharing", "public")), CancellationToken.None)).ToString()!;

        text.Should().Contain("[tool_error] wiki.space_create");
        text.Should().Contain("open_sharing 仅支持 open/closed");
        _userSpaceClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateSpace_DryRun_ShouldWarnAboutMissingIdempotencyKey_AndNotTouchDownstream()
    {
        var text = (await CreateTools().CreateSpaceAsync(
            Args(("name", "产品知识库"), ("dry_run", true)), CancellationToken.None)).ToString()!;

        text.Should().StartWith("[dry_run]");
        text.Should().Contain("POST /open-apis/wiki/v2/spaces");
        text.Should().Contain("不支持幂等键", "重复调用会创建多个同名空间——这是本工具唯一的真实风险");
        text.Should().NotContain("产品知识库", "预演摘要只回字段长度，不回空间名原文");
        _userSpaceClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateSpace_WhenUserSpaceClientMissing_ShouldTellHostToAllowUserIdentity()
    {
        var tools = new WikiTools(
            _nodesClient.Object,
            null,
            Options.Create(new FeishuAgentOptions { Instructions = "test" }));

        var text = (await tools.CreateSpaceAsync(Args(("name", "空间")), CancellationToken.None)).ToString()!;

        text.Should().Contain("[tool_error] wiki.space_create");
        text.Should().Contain("IFeishuUserV2Wiki");
        text.Should().Contain("AllowedIdentities", "缺席原因是身份没放行，不是客户端没注册——提示必须指到配置项");
        _userSpaceClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateSpace_WhenPlatformFails_ShouldReturnStructuredErrorWithCode()
    {
        _userSpaceClient
            .Setup(c => c.CreateSpaceAsync(It.IsAny<CreateSpaceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<SpaceInfoResult> { Code = 1310201, Msg = "space name duplicated" });

        var text = (await CreateTools().CreateSpaceAsync(Args(("name", "已存在空间")), CancellationToken.None)).ToString()!;

        text.Should().Contain("[tool_error] wiki.space_create");
        text.Should().Contain("1310201", "同名空间被平台拒绝时必须给码——重试无用，只有改名或复用既有空间");
    }
}
