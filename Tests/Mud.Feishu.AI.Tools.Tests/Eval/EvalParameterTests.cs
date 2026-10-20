// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Tests.Eval;

/// <summary>
/// 评测层二「参数生成」：同一份入参分别对着 Schema 面与真实执行链判定，断言两者与声明一致。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么分两层</b>：365 枚工具里只有 2 枚在 Schema 上声明了 <c>enum</c>，其余取值闭集
/// （receive_id_type、member_type、member_role）、JSON 字符串形态（sheets 的 values、bitable 的
/// records/fields）与简化文法（bitable 的 filter）全部由 handler 判定。只测 Schema 面会漏掉
/// 「名字写对、值写错」这一整类真实失败；只测 handler 面则无法区分「参数结构错」与「下游失败」。
/// </para>
/// <para>
/// <b>它拦的是什么</b>：参数契约的静默漂移。必填集被生成器改窄、闭集校验被删、文法上限被放宽，
/// 编译期都不报错，但模型会按旧契约继续产出——评测集把「模型会怎么写参数」钉成资产，
/// 漂移即在报告里显形。
/// </para>
/// </remarks>
public class EvalParameterTests
{
    /// <summary>每层最少用例数（少于此即该层判据覆盖面不足，不构成度量）。</summary>
    private const int MinimumCasesPerLayer = 15;

    /// <summary>至少覆盖多少枚不同工具——防"一域测到底、其余域无参数度量"。</summary>
    private const int MinimumDistinctTools = 15;

    [Fact]
    public void ParameterSet_ShouldBeLayeredAndAnchoredOnRealTools()
    {
        var cases = EvalAssets.Parameters;
        cases.Should().NotBeEmpty("参数层用例为空即评测静默退化为假绿");
        cases.Should().OnlyHaveUniqueItems(static c => c.Id, "用例 id 是报告与失败消息的寻址键");

        var illegalLayer = cases.Where(static c => c.Layer is not ("schema" or "handler")).Select(static c => c.Id).ToArray();
        illegalLayer.Should().BeEmpty("layer 只有 schema / handler 两种取值（拼错即用错判据）");

        var illegalExpect = cases.Where(static c => c.Expect is not ("valid" or "invalid")).Select(static c => c.Id).ToArray();
        illegalExpect.Should().BeEmpty("expect 只有 valid / invalid 两种判据");

        cases.Should().OnlyContain(static c => !string.IsNullOrWhiteSpace(c.Why),
            "why 是这条用例存在的理由：没有它，失败时无人能判断该改用例还是改产品");

        var surface = EvalToolSurface.Instance;
        var ghost = cases.Select(static c => c.Tool).Where(t => !surface.Contains(t)).ToArray();
        ghost.Should().BeEmpty("参数用例引用了不存在的工具名（幻觉工具名）");

        foreach (var layer in new[] { "schema", "handler" })
        {
            var slice = cases.Where(c => c.Layer == layer).ToArray();
            slice.Should().HaveCountGreaterThanOrEqualTo(MinimumCasesPerLayer, $"{layer} 层用例不足");
            slice.Should().Contain(static c => c.Expect == "valid", $"{layer} 层没有正例 ⇒ 判据可能只会拒绝一切");
            slice.Should().Contain(static c => c.Expect == "invalid", $"{layer} 层没有反例 ⇒ 判据可能只会放行一切");
        }

        cases.Select(static c => c.Tool).Distinct().Count().Should().BeGreaterThanOrEqualTo(MinimumDistinctTools,
            "参数度量必须跨域铺开，否则只是少数几枚工具的自我确认");
    }

    [Fact]
    public async Task Parameters_ShouldMatchDeclaredVerdictsOnBothLayers()
    {
        using var runner = new EvalRunner();
        var findings = await runner.RunParametersAsync();

        var drift = findings.Where(static f => !f.Passed)
            .Select(static f => $"  - {f.Id} {f.Tool}（{f.Layer} 层期望 {f.Expect}）：{f.Detail}")
            .ToArray();

        var schema = findings.Count(static f => f.Layer == "schema");
        var handler = findings.Count(static f => f.Layer == "handler");
        drift.Should().BeEmpty(
            $"参数层判定与声明不一致（schema {schema} 条 / handler {handler} 条）——" +
            $"要么产品契约漂移，要么评测集写错了期望；两者都必须显式裁决，不许静默改期望值。\n" +
            $"报告：\n{runner.Render([], findings, [])}");
    }

    [Theory]
    // 判据自证：Schema 面四类标记必须各自可触发，且正例必须零标记（只测一侧等于没测）。
    [InlineData("im.send_message", "{}", "missing_argument:receive_id")]
    [InlineData("im.send_message", """{"receive_id":"oc_a1","text":"你好"}""", "")]
    [InlineData("docx.get_document_blocks", """{"document_id":123}""", "type_mismatch:document_id")]
    [InlineData("feishu.tool_search", """{"keyword":"邮件","limitt":5}""", "unknown_argument:limitt")]
    [InlineData("task.create_task", """{"summary":"上线","assignee_ids":[123]}""", "item_type_mismatch:assignee_ids")]
    [InlineData("docx.append_blocks", """{"document_id":"doxcn1","block_type":"nope","text":"x"}""", "enum_violation:block_type")]
    public void Validator_ShouldEmitDeclaredMarkerClass(string tool, string argumentsJson, string expectedMarker)
    {
        var errors = EvalArgumentValidator.Validate(tool, Args(argumentsJson));

        if (expectedMarker.Length == 0)
        {
            errors.Should().BeEmpty($"{tool} 的合规格入参不应被判错（否则阈值断言只是空转）");
            return;
        }

        errors.Should().Contain(e => e.StartsWith(expectedMarker, StringComparison.Ordinal),
            $"{tool} 未按预期产出 {expectedMarker}——Schema 面判据失效");
    }

    private static Dictionary<string, object?> Args(string argumentsJson)
    {
        using var document = JsonDocument.Parse(argumentsJson);
        return EvalAssets.ToArguments(document.RootElement);
    }
}
