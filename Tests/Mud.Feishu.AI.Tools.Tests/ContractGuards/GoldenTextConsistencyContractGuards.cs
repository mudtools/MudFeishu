// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;
using System.Text.RegularExpressions;

namespace Mud.Feishu.AI.Tools.Tests.ContractGuards;

/// <summary>
/// golden 快照「描述 ↔ 元数据」机械一致性守卫（2026-10-28 审查 T-3/T-5/T-7 回归的防复发）。
/// 审查结论：工具面误解风险的根因不是措辞而是<b>结构性不一致</b>——描述声称与 Schema 声明分叉。
/// 三条规则逐工具机械校验 golden 快照：① 描述中的 scope 字样 ∈ 声明 scope；② 描述声称「必填」的
/// 非布尔参数 ∈ schema required；③ 参数描述中的 ID 前缀示例 ∈ 已知前缀词表。
/// </summary>
/// <remarks>
/// 回归出处：.docs/AI模块功能缺陷与完整性分析报告-2026-10-28.md §4（T-3 scope 口径、T-5 veiw 拼写、
/// 必填声明脱节样本）；修复批次见同日 CHANGELOG。布尔参数的必填声明因引擎限制（解包映射表不支持
/// 必填布尔）被<b>有意豁免</b>——该豁隙待上游 Mud.AI.Generator 提供布尔 required 能力后收窄。
/// </remarks>
public partial class GoldenTextConsistencyContractGuards
{
    /// <summary>词表 = golden 全量提取的合法 ID 前缀（2026-10-28 固化；新前缀须同批补词表）。</summary>
    /// <remarks>
    /// 只收「小写前缀＋大写 X 开头尾巴」的严格示例形态（如 <c>omXxx</c>）；泛化占位
    /// （<c>txxx</c>/<c>xxx@xxx</c>/<c>app_xxx</c>）描述的是形态而非前缀，不入词表也不受检。
    /// </remarks>
    private static readonly string[] KnownIdPrefixes =
    [
        "bascnXxx", "blkXxx", "boxcnXxx", "doxcnXxx", "obcnXxx", "ocXxx", "omXxx", "recXxx", "rolXxx",
        "ShtXxx", "shtcnXxx", "tblXxx", "vewXxx", "wikcnXxx",
    ];

    /// <summary>描述中「需 {scope}」字样的 scope 必须出现在 RequiredScopes 声明里（T-3）。</summary>
    /// <remarks>
    /// scope 候选限定<b>含冒号</b>的字样（飞书 scope 均为 <c>域:能力</c> 形态）——
    /// 「需 approval_code 四要素」这类<b>业务参数</b>措辞不是 scope 声明，不得误报。
    /// </remarks>
    [Fact]
    public void DescriptionScopeText_ShouldMatchDeclaredScopes()
    {
        var violations = new List<string>();

        foreach (var (toolName, schema) in GoldenLines())
        {
            using var doc = JsonDocument.Parse(schema);
            var declared = doc.RootElement
                .GetProperty("x-feishu")
                .GetProperty("required_scopes")
                .EnumerateArray()
                .Select(static s => s.GetString())
                .ToList();

            foreach (var match in ScopeTextRegex().Matches(
                         doc.RootElement.GetProperty("description").GetString() ?? string.Empty)
                     .Cast<System.Text.RegularExpressions.Match>())
            {
                var scope = match.Groups[1].Value;
                if (!declared.Contains(scope, StringComparer.Ordinal))
                {
                    violations.Add($"{toolName}: 描述声称「需 {scope}」但 RequiredScopes={string.Join('/', declared)}");
                }
            }
        }

        violations.Should().BeEmpty(
            "描述里的 scope 字样与 RequiredScopes 声明分叉会引导模型/宿主查错权限条目（T-3 回归）");
    }

    /// <summary>参数描述声称「必填」的非布尔参数必须进 schema required（必填声明脱节）。</summary>
    [Fact]
    public void RequiredClaimInDescription_ShouldAppearInSchemaRequired()
    {
        var violations = new List<string>();

        foreach (var (toolName, schema) in GoldenLines())
        {
            using var doc = JsonDocument.Parse(schema);
            var parameters = doc.RootElement.GetProperty("parameters");
            if (!parameters.TryGetProperty("properties", out var properties))
            {
                continue;
            }

            var required = parameters.TryGetProperty("required", out var requiredArray)
                ? requiredArray.EnumerateArray().Select(static r => r.GetString()).ToList()
                : [];

            foreach (var property in properties.EnumerateObject())
            {
                var description = property.Value.TryGetProperty("description", out var desc)
                    ? desc.GetString() ?? string.Empty
                    : string.Empty;
                var isBoolean = property.Value.TryGetProperty("type", out var type)
                    && type.GetString() == "boolean";

                // 布尔豁免：引擎解包映射表不支持必填布尔（Schema 层无法标 required），
                // 其「必填」声明只能留在描述——上游提供布尔 required 后收窄本豁免。
                // 条件必填豁免：「x 时必填」是 oneOf 语义（如 put_top_notice 的 message_id
                // 仅 action_type=1 时需要），Schema 的 required 数组表达不了。
                // 成对条件（「填 y 时本项必填」）同属 oneOf 语义。
                var isConditional = description.Contains("时必填") || description.Contains("需配") || description.Contains("时本项必填");
                if (!isBoolean && !isConditional && description.Contains("必填") && !required.Contains(property.Name))
                {
                    violations.Add($"{toolName}.{property.Name}: 描述声称必填但不在 required 数组");
                }
            }
        }

        violations.Should().BeEmpty(
            "「Schema 说可选、描述说必填」是模型可合法触发执行层拒绝的陷阱（必填声明脱节回归）");
    }

    /// <summary>参数描述中的 ID 前缀示例必须是真实前缀（T-5：veiwXxx 拼写错误的防复发）。</summary>
    [Fact]
    public void IdPrefixExamplesInDescriptions_ShouldUseKnownPrefixes()
    {
        var violations = new List<string>();

        foreach (var (toolName, schema) in GoldenLines())
        {
            foreach (var match in IdPrefixRegex().Matches(schema).Cast<System.Text.RegularExpressions.Match>())
            {
                var prefix = match.Groups[1].Value;
                if (!KnownIdPrefixes.Contains(prefix, StringComparer.Ordinal))
                {
                    violations.Add($"{toolName}: 未知 ID 前缀示例「{prefix}Xxx」（疑似拼写错误，T-5 回归）");
                }
            }
        }

        violations.Should().BeEmpty(
            "错误的 ID 前缀示例会被模型当作合法形态校验依据（T-5：veiwXxx 实为 vew）");
    }

    private static IEnumerable<(string Name, string Schema)> GoldenLines()
    {
        var goldenPath = Path.Combine(FindRepositoryRoot(), "Mud.Feishu.AI.Tools", "FeishuToolSchemas.golden.txt");
        File.Exists(goldenPath).Should().BeTrue("golden 快照必须存在（缺失 = 门禁静默消失）");

        foreach (var line in File.ReadAllLines(goldenPath))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var separator = line.IndexOf('\t');
            yield return (line[..separator], line[(separator + 1)..]);
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(directory) && !File.Exists(Path.Combine(directory, "Mud.Feishu.slnx")))
        {
            directory = Path.GetDirectoryName(directory);
        }

        return directory!;
    }

    [GeneratedRegex(@"需\s*([a-z0-9_]+:[a-z0-9_.:]+)")]
    private static partial Regex ScopeTextRegex();

    [GeneratedRegex(@"形如\s+([a-z]+Xxx)")]
    private static partial Regex IdPrefixRegex();
}
