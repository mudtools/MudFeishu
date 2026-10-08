// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;
using Mud.Feishu.AI.Tools.Generated;

namespace Mud.Feishu.AI.Tools.Tests.ContractGuards;

/// <summary>
/// <b>R6 / S1</b>：dry-run 能力的<b>契约守卫</b>——把「写工具必须可预演」从
/// 约定升级为可机械判定的门禁。
/// </summary>
/// <remarks>
/// <para>
/// <b>要解决的缺陷形态</b>：dry-run 的实现（<c>ToolDryRun</c>）与 41 个写工具的接入
/// 在 R6 调研时<b>已全部就位</b>，唯一缺口是「<b>新增写工具时忘记接 <c>dry_run</c></b>」——
/// 这类回归<b>没有任何构建期信号</b>：新工具照常编译、照常进注册表、照常可被模型调用，
/// 只是<b>不可预演</b>，而高危写工具不可预演正是最昂贵的失败。
/// </para>
/// <para>
/// <b>为什么判据取 Schema 而不是反射 <c>[ToolParameter]</c></b>：
/// Schema（golden 快照锁定的模型可见契约）与"模型看到什么"同源；
/// 且它同时覆盖了「参数被正确渲染进 JSON Schema」这一必要条件——
/// 只断言特性存在会漏掉"特性在但渲染器没认"的情形。
/// </para>
/// <para>
/// <b>为什么高危工具的文案断言要放宽到"含 dry_run 且含预演/确认语义"</b>：
/// 逐字锁定「建议先 dry_run 预演」会让合法但不逐字相同的描述被判红（脆弱断言），
/// 而本守卫真正要拦的是"完全没提预演"。
/// </para>
/// </remarks>
public class DryRunContractGuards
{
    /// <summary>dry-run 参数名（模型可见契约，全写工具统一）。</summary>
    private const string DryRunParameterName = "dry_run";

    /// <summary>高危风险字面量（Schema 的 <c>x-feishu.risk</c> 取值）。</summary>
    private const string HighRiskWrite = "high-risk-write";

    /// <summary><b>核心断言</b>：每个写工具的 Schema 必须暴露 <c>dry_run</c> 参数。</summary>
    [Fact]
    public void EveryWriteTool_ShouldExposeDryRunParameter()
    {
        var schemaByToolName = FeishuToolSchemas.SchemaByToolName;

        FeishuToolNames.WriteAll.Should().NotBeEmpty(
            "写工具清单为空——契约表语义变了（假绿），请先修守卫");

        var missing = FeishuToolNames.WriteAll
            .Where(name => !HasDryRunParameter(schemaByToolName[name]))
            .OrderBy(static n => n, StringComparer.Ordinal)
            .ToArray();

        missing.Should().BeEmpty(
            "以下写工具没有 dry_run 参数——模型无法预演，只能盲写；"
            + "高危写操作不可预演是最昂贵的失败形态（R6/S1）：{0}",
            string.Join(" | ", missing));
    }

    /// <summary>每个 high-risk-write 工具的描述必须给出 dry-run 指引。</summary>
    [Fact]
    public void EveryHighRiskTool_ShouldHintDryRunInDescription()
    {
        var schemaByToolName = FeishuToolSchemas.SchemaByToolName;

        var highRisk = schemaByToolName
            .Where(static pair => RiskOf(pair.Value) == HighRiskWrite)
            .ToArray();

        highRisk.Should().NotBeEmpty(
            "没有任何 high-risk-write 工具——风险派生链断了（假绿），请先修守卫");

        var missing = highRisk
            .Where(pair => !DescribesDryRunHint(DescriptionOf(pair.Value)))
            .Select(static pair => pair.Key)
            .OrderBy(static n => n, StringComparer.Ordinal)
            .ToArray();

        missing.Should().BeEmpty(
            "以下高危写工具的描述没有 dry-run 指引（应含 dry_run 且含预演/确认语义）：{0}",
            string.Join(" | ", missing));
    }

    /// <summary>
    /// <b>负例自证</b>：同一套扫描器必须对"缺 dry_run 的合成写工具"报红。
    /// </summary>
    /// <remarks>
    /// 一条恒真的守卫与没有守卫等价。本用例用<b>合成 Schema</b>（不进工具面）驱动
    /// <see cref="HasDryRunParameter"/> 与 <see cref="DescribesDryRunHint"/>，
    /// 保证判据真的区分得开"有 / 无"。
    /// </remarks>
    [Fact]
    public void Scanner_ShouldFlagWriteToolWithoutDryRun_WhenSynthetic()
    {
        const string withoutDryRun =
            """{"name":"synthetic.write","description":"合成写工具（无预演）","parameters":{"type":"object","properties":{"id":{"type":"string"}},"required":["id"]},"x-feishu":{"risk":"write"}}""";
        const string withDryRun =
            """{"name":"synthetic.write","description":"合成写工具：建议先 dry_run 预演确认","parameters":{"type":"object","properties":{"id":{"type":"string"},"dry_run":{"type":"boolean"}},"required":["id"]},"x-feishu":{"risk":"write"}}""";

        HasDryRunParameter(withoutDryRun).Should().BeFalse("判据失效：无 dry_run 的合成 Schema 被判为有");
        HasDryRunParameter(withDryRun).Should().BeTrue("判据失效：有 dry_run 的合成 Schema 未被识别");

        DescribesDryRunHint(DescriptionOf(withoutDryRun)).Should().BeFalse(
            "判据失效：描述里完全没提预演的合成工具被判为已提示");
        DescribesDryRunHint(DescriptionOf(withDryRun)).Should().BeTrue(
            "判据失效：含 dry_run 与预演语义的描述未被识别");
    }

    /// <summary>反向自证：真实工具面上至少有一个写工具被识别出 <c>dry_run</c>（防"扫不到 ⇒ 空集合"）。</summary>
    [Fact]
    public void Guard_ShouldActuallySeeDryRunOnKnownWriteTool()
    {
        HasDryRunParameter(FeishuToolSchemas.SchemaByToolName[FeishuToolNames.BitableAddRecord])
            .Should().BeTrue("判据失效：既有写工具 bitable.add_record 未被识别出 dry_run");
    }

    // ────────── 判据（可被负例驱动的纯函数） ──────────

    /// <summary>Schema 的 <c>parameters.properties</c> 里是否存在 <c>dry_run</c>。</summary>
    private static bool HasDryRunParameter(string schemaJson)
    {
        using var document = JsonDocument.Parse(schemaJson);
        return document.RootElement
            .TryGetProperty("parameters", out var parameters)
            && parameters.TryGetProperty("properties", out var properties)
            && properties.TryGetProperty(DryRunParameterName, out _);
    }

    /// <summary>描述是否给出 dry-run 指引（含参数名 + 预演/确认语义）。</summary>
    private static bool DescribesDryRunHint(string description)
        => description.Contains(DryRunParameterName, StringComparison.Ordinal)
           && (description.Contains("预演", StringComparison.Ordinal)
               || description.Contains("确认", StringComparison.Ordinal));

    private static string DescriptionOf(string schemaJson)
    {
        using var document = JsonDocument.Parse(schemaJson);
        return document.RootElement.TryGetProperty("description", out var description)
            ? description.GetString() ?? string.Empty
            : string.Empty;
    }

    private static string? RiskOf(string schemaJson)
    {
        using var document = JsonDocument.Parse(schemaJson);
        return document.RootElement.TryGetProperty("x-feishu", out var extension)
               && extension.TryGetProperty("risk", out var risk)
            ? risk.GetString()
            : null;
    }
}
