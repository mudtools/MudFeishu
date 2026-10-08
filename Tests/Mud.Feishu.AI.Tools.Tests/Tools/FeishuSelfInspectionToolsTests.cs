// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.Tools.Internal;

namespace Mud.Feishu.AI.Tools.Tests.Tools;

/// <summary>
/// 方法级自省与万能兜底（R6 / S4-S5）：<c>feishu.schema_read</c> 与 <c>feishu.api_call</c>。
/// </summary>
/// <remarks>
/// <para>
/// <b>本文件锁的是"兜底通道的安全边界"</b>，不是 happy path 的字段堆砌。
/// <c>api_call</c> 的价值是补未策展能力，风险是它绕过了每工具一策展的审查 ——
/// 故四条 fail-closed 边界（用户态拒绝 / 高危拒绝 / 路由参数强校验 / 默认预演）
/// 每条都要有对应用例：任何一条被后人改软，测试立刻红。
/// </para>
/// <para>
/// 用例里的方法限定名全部取自编译期目录（<c>FeishuToolMethodCatalog</c>），
/// 若某天目录里没了它们，这些用例会以"方法不在目录中"的形式报错 ——
/// 这本身是有效信号（说明 SDK 接口改名了，需要同步）。
/// </para>
/// </remarks>
public class FeishuSelfInspectionToolsTests
{
    private static FeishuAgentOptions AgentOptions() => new() { Instructions = "test" };

    private static IReadOnlyDictionary<string, object?> Args(params (string Key, object? Value)[] items)
        => items.ToDictionary(p => p.Key, p => p.Value);

    private static SchemaReadTools CreateSchemaReadTools()
        => new(Options.Create(AgentOptions()));

    private static GenericApiTools CreateApiTools(Mud.Feishu.Abstractions.IFeishuAppManager? appManager = null)
        => new(Options.Create(AgentOptions()), appManager);

    // ───────────────────── schema_read：方法级事实 ─────────────────────

    [Fact]
    public async Task SchemaRead_ByQualifiedName_ShouldReturnHttpFacts()
    {
        var result = await CreateSchemaReadTools().SchemaReadAsync(
            Args(("method", "IFeishuTenantV1OkrPeriod.ListPeriodsAsync")), CancellationToken.None);

        using var document = JsonDocument.Parse(result.ToString()!);
        var root = document.RootElement;

        root.GetProperty("matched_count").GetInt32().Should().Be(1);
        var method = root.GetProperty("methods")[0];
        method.GetProperty("qualified_name").GetString().Should().Be("IFeishuTenantV1OkrPeriod.ListPeriodsAsync");
        method.GetProperty("http").GetString().Should().Be("GET");
        method.GetProperty("route").GetString().Should().Be("/open-apis/okr/v1/periods");
        method.GetProperty("token_kind").GetString().Should().Be("tenant");
        method.GetProperty("module").GetString().Should().Be("OkrPeriod",
            "module 取接口名里的资源段（IFeishuTenantV1OkrPeriod → OkrPeriod），不是域前缀");
        method.GetProperty("risk").GetString().Should().Be("read");
        method.GetProperty("curated").GetBoolean().Should().BeTrue(
            "已策展方法必须回填 curated=true（模型据此优先用专用工具）");
        method.GetProperty("curated_tool").GetString().Should().Be("okr.list_periods");
        method.GetProperty("query_params").EnumerateArray()
            .Select(static e => e.GetString()).Should().Contain("page_size");
    }

    [Fact]
    public async Task SchemaRead_WithoutAnyCriterion_ShouldBeRejectedLocally()
    {
        var result = await CreateSchemaReadTools().SchemaReadAsync(Args(), CancellationToken.None);

        result.ToString().Should().Contain("method",
            "无任何限定条件时目录只能给出任意子集——必须本地拒绝并指向三个判据");
    }

    [Fact]
    public async Task SchemaRead_ByModule_ShouldReturnOnlyThatModule()
    {
        var result = await CreateSchemaReadTools().SchemaReadAsync(
            Args(("module", "OkrPeriod"), ("limit", 5)), CancellationToken.None);

        using var document = JsonDocument.Parse(result.ToString()!);
        var methods = document.RootElement.GetProperty("methods");

        methods.GetArrayLength().Should().BeGreaterThan(0).And.BeLessThanOrEqualTo(5);
        foreach (var method in methods.EnumerateArray())
        {
            method.GetProperty("module").GetString().Should().Be("OkrPeriod");
        }
    }

    // ───────────────────── api_call：默认预演 ─────────────────────

    [Fact]
    public async Task ApiCall_DryRunByDefault_ShouldNotTouchDownstream()
    {
        var appManager = new Mock<Mud.Feishu.Abstractions.IFeishuAppManager>();

        var result = await CreateApiTools(appManager.Object).ApiCallAsync(
            Args(
                ("method", "IFeishuTenantV1OkrPeriod.ListPeriodsAsync"),
                ("query_params", "{\"page_size\":20}")),
            CancellationToken.None);

        using var document = JsonDocument.Parse(result.ToString()!);
        var root = document.RootElement;

        root.GetProperty("dry_run").GetBoolean().Should().BeTrue("dry_run 缺省必须是 true");
        root.GetProperty("http").GetString().Should().Be("GET");
        root.GetProperty("url").GetString().Should().Be("/open-apis/okr/v1/periods?page_size=20");
        root.GetProperty("curated_tool").GetString().Should().Be("okr.list_periods");

        appManager.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ApiCall_RealCallWithoutAppManager_ShouldFailClosed()
    {
        var result = await CreateApiTools().ApiCallAsync(
            Args(
                ("method", "IFeishuTenantV1OkrPeriod.ListPeriodsAsync"),
                ("dry_run", false)),
            CancellationToken.None);

        var text = result.ToString()!;
        text.Should().StartWith("[tool_error] feishu.api_call");
        text.Should().Contain("IFeishuAppManager",
            "缺少应用管理器时必须给出可操作的错误（并说明 dry_run=true 仍可用）");
    }

    // ───────────────────── api_call：fail-closed 边界 ─────────────────────

    [Fact]
    public async Task ApiCall_UserTokenKindMethod_ShouldBeRefused()
    {
        var result = await CreateApiTools().ApiCallAsync(
            Args(("method", "IFeishuUserV4ApprovalInstance.GetInitiatedInstancePageListAsync")),
            CancellationToken.None);

        var text = result.ToString()!;
        text.Should().Contain("用户态",
            "user 身份方法不得经兜底调用：本工具身份轴派定为 tenant，会静默造成身份错配");
        text.Should().Contain("feishu.schema_read");
    }

    [Fact]
    public async Task ApiCall_HighRiskMethod_ShouldBeRefused()
    {
        var result = await CreateApiTools().ApiCallAsync(
            Args(("method", "IFeishuTenantV4ApprovalTask.TransferApprovalAsync")),
            CancellationToken.None);

        result.ToString().Should().Contain("high-risk-write",
            "高危方法不走兜底——它的授权门禁与 dry_run 摘要属于策展工具");
    }

    [Fact]
    public async Task ApiCall_MissingPathParams_ShouldBeRejectedWithRequiredNames()
    {
        var result = await CreateApiTools().ApiCallAsync(
            Args(("method", "IFeishuTenantV1AcsUser.GetUserAsync")),
            CancellationToken.None);

        var text = result.ToString()!;
        text.Should().Contain("user_id",
            "路由占位符未覆盖时必须本地拒绝（否则会把带 {user_id} 字面量的 URL 发出去）");
    }

    [Fact]
    public async Task ApiCall_UnexpectedPathParams_ShouldBeRejected()
    {
        var result = await CreateApiTools().ApiCallAsync(
            Args(
                ("method", "IFeishuTenantV1AcsUser.GetUserAsync"),
                ("path_params", "{\"user_id\":\"ou_1\",\"typo_id\":\"x\"}")),
            CancellationToken.None);

        result.ToString().Should().Contain("typo_id",
            "多传路径参数说明模型调错方法或写错参数名——比让它变成一个奇怪的 404 好");
    }

    [Fact]
    public async Task ApiCall_InvalidJsonParams_ShouldBeRejectedNotSilentlyIgnored()
    {
        var result = await CreateApiTools().ApiCallAsync(
            Args(("method", "IFeishuTenantV1OkrPeriod.ListPeriodsAsync"), ("query_params", "not json")),
            CancellationToken.None);

        result.ToString().Should().Contain("JSON",
            "非法 JSON 不得静默降级为空对象（那就是'看起来成功、实际调错'）");
    }

    [Fact]
    public async Task ApiCall_UnknownMethod_ShouldPointToSchemaRead()
    {
        var result = await CreateApiTools().ApiCallAsync(
            Args(("method", "IFeishuTenantV9NoSuchMethod.DoAsync")), CancellationToken.None);

        var text = result.ToString()!;
        text.Should().Contain("不在 SDK 方法目录中");
        text.Should().Contain("feishu.schema_read");
    }
}
