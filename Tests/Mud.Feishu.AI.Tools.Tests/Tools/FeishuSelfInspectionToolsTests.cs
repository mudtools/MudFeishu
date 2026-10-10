// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.Tools.Internal;

namespace Mud.Feishu.AI.Tools.Tests.Tools;

/// <summary>
/// 方法级自省（R6 / S4）：<c>feishu.schema_read</c>。
/// </summary>
/// <remarks>
/// <para>
/// <b>R-12（2026-10-10 评审决策）</b>：本文件曾同时覆盖万能兜底通道 <c>feishu.api_call</c>
/// 的四条 fail-closed 边界。该工具已<b>整条删除</b>（双轨调用路径 + 全工具面最高风险面 + 零外部消费），
/// 故其用例同批删除——保留"已删工具的测试"会让后来者以为该工具仍存在。
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

    /// <summary>
    /// R-12 回归：schema_read 的模型可见文案<b>不得</b>再指引任何兜底调用通道。
    /// </summary>
    /// <remarks>
    /// 这是"删了工具却留下指引"这一缺陷形态的<b>行为级</b>断言（<c>R-12</c> 的
    /// "Guidance 工具名引用"守卫只覆盖 <c>Guidance/**/*.md</c>，不覆盖运行期产出的 note 文案）。
    /// 指引残留会让模型持续臆造一个不存在的工具名。
    /// </remarks>
    [Fact]
    public async Task SchemaRead_Note_ShouldNotReferenceRemovedTool()
    {
        var result = await CreateSchemaReadTools().SchemaReadAsync(
            Args(("method", "IFeishuTenantV1OkrPeriod.ListPeriodsAsync")), CancellationToken.None);

        result.ToString().Should().NotContain("api_call",
            "R-12：万能兜底通道已删除，模型可见文案不得再引用它（否则模型会持续臆造调用）");
    }
}
