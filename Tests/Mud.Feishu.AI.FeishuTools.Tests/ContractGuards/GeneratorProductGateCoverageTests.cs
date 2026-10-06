// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规和保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.RegularExpressions;

namespace Mud.Feishu.AI.FeishuTools.Tests.ContractGuards;

/// <summary>
/// <b>R5 / B-9</b>：<b>生成器产物门禁覆盖守卫</b> —— 断言<b>每一个发射产物</b>要么被 golden 直接覆盖，
/// 要么有<b>明确登记的"传递性门禁依据"</b>。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么是这条守卫（评审对原文方案的修订）</b>：R5 原文的 B-9 要求"为其余 7 个产物补 golden 幂等门禁"。
/// 实施前核实两点事实：
/// <list type="number">
/// <item><b>仓库不提交任何生成产物</b>（<c>*.g.cs</c> 无一入库，全部编译期发射），
/// 所以"产物漂移"不可能通过提交引入⇒ 不需要"提交态 diff"式门禁；</item>
/// <item>这 7 个产物<b>都是已门禁输入的纯函数</b>，逐个做 golden 会与既有门禁<b>功能重复</b>：
/// 见下方 <see cref="ProductGates"/> 的逐项依据表。</item>
/// </list>
/// ⇒ 真正的风险不是"某个产物漂移"，而是<b>"新增了一个产物却没人给它想门禁"</b>——
/// 这是一个<b>纯人工不变量</b>，且一旦发生就<b>静默</b>。本守卫把"产物 ⇔ 门禁依据"这条映射
/// 变成机械约束，与本仓已多次验证有效的"元守卫"模式一致。
/// </para>
/// <para>
/// <b>判定口径</b>：解析生成器全部 <c>AddSource(...)</c> 的产物名（字面量与HintName 常量），
/// 要求每个都能在 <see cref="ProductGates"/> 里找到"golden覆盖 / 传递性门禁依据"条目，
/// 且<b>依据字段非空</b>（防止写成空字符串占位）。同时断言
/// <see cref="ProductGates"/> 没有<b>陈旧条目</b>（已不再发射的产物）。
/// </para>
/// </remarks>
public class GeneratorProductGateCoverageTests
{
    /// <summary>
    /// 产物 → 门禁依据的<b>登记表</b>（本守卫的单一真相源）。
    /// </summary>
    /// <remarks>
    /// <b>三类依据</b>：
    /// <list type="bullet">
    /// <item><b>golden</b>：<c>FeishuToolSchemas.golden.txt</c> 直接比对产物文本；</item>
    /// <item><b>传递性</b>：产物是已门禁输入的纯函数，自身不必再快照
    /// （再快照就是与既有门禁功能重复）；</item>
    /// <item><b>不可漂移</b>：产物内容由仓库内文件直接嵌入，输入本身即真相源。</item>
    /// </list>
    /// </remarks>
    private static readonly Dictionary<string, string> ProductGates = new(StringComparer.Ordinal)
    {
        ["FeishuToolSchemas.g.cs"] =
            "golden：与 FeishuToolSchemas.golden.txt 逐字节比对，构建期 MUDFT014 断言",

        ["FeishuToolNames.g.cs"] =
            "传递性：工具名表是工具面的纯函数；工具面变动已被 FeishuToolSchemas.golden.txt 捕获"
            + "（golden 内含每个工具的 name 字段），故额外快照无新增信息",

        ["FeishuToolContracts.g.cs"] =
            "传递性：类型化契约表由同一批CapabilityEntry 派生，输入与 FeishuToolSchemas.g.cs 完全同源",

        ["FeishuCapabilityCatalog.g.cs"] =
            "传递性：能力目录是 CapabilityEntry 的聚合视图（同批同 pass），无独立输入",

        ["FeishuToolGuidance.g.cs"] =
            "不可漂移：guidance 正文来自 AdditionalFiles（Guidance/{domain}.md，仓库内文件），"
            + "直接嵌入产物；且域清单由 GuidanceAssetContractGuards 锁定",

        ["ToolArgs/{TypeName}.g.cs"] =
            "传递性：参数解包器由绑定派生；绑定本身由 MUDFT022~025（工具未绑定/绑定不成立/"
            + "签名不符/构造参数不可解析）零容忍门禁覆盖",

        ["{RegistrarHintName}.g.cs"] =
            "传递性：注册器由 ToolHandlerBinding 派生；同上由 MUDFT022~025 覆盖。"
            + "（另：注册器内出现的 FeishuToolNames 常量引用受 ToolExecutorSkeletonGuards 的"
            + "「每方法至多 1 次」约束）",

        ["{CoreFileName}"] =
            "传递性：核心扩展方法由同一批绑定派生，同 MUDFT022~025 覆盖",
    };

    /// <summary>
    /// 核心断言：每个被发射的产物都必须在 <see cref="ProductGates"/> 中有<b>非空</b>依据。
    /// </summary>
    [Fact]
    public void EveryEmittedProduct_ShouldHaveARegisteredGateBasis()
    {
        var emitted = ReadEmittedProducts();
        emitted.Should().NotBeEmpty(
            "未解析到任何 AddSource 产物——解析规则已失效（假绿），请先修守卫");

        // 归一化：把形如 "ToolArgs/{TypeName}.g.cs" / "{RegistrarHintName}.g.cs" 的
        // 动态 hint 名映射到登记表使用的键。
        var unresolved = new List<string>();
        foreach (var product in emitted)
        {
            if (!TryMatchRegisteredKey(product, out var key))
            {
                unresolved.Add(product);
            }
        }

        unresolved.Should().BeEmpty(
            "以下发射产物没有登记门禁依据——新增产物若不登记，其漂移将<b>静默无门禁</b>。"
            + "请在 ProductGates 中补一条并写明依据（golden / 传递性 / 不可漂移）：{0}",
            string.Join(" | ", unresolved));
    }

    /// <summary>登记表的每条依据<b>不得为空</b>（防止用空字符串占位骗过守卫）。</summary>
    [Fact]
    public void EveryRegisteredGateBasis_ShouldBeNonEmptyAndExplainWhy()
    {
        var empty = ProductGates
            .Where(static kv => string.IsNullOrWhiteSpace(kv.Value))
            .Select(static kv => kv.Key)
            .ToArray();

        empty.Should().BeEmpty(
            "以下产物登记了空依据——等于没门禁：{0}", string.Join(" | ", empty));
    }

    /// <summary>登记表<b>不得有陈旧条目</b>（产物已不再发射却仍在表里会掩盖真实缺口）。</summary>
    [Fact]
    public void ProductGates_ShouldNotContainStaleEntries()
    {
        var emitted = ReadEmittedProducts();
        var matched = new HashSet<string>(StringComparer.Ordinal);
        foreach (var product in emitted)
        {
            if (TryMatchRegisteredKey(product, out var key))
            {
                matched.Add(key);
            }
        }

        var stale = ProductGates.Keys.Where(key => !matched.Contains(key)).OrderBy(static k => k, StringComparer.Ordinal).ToArray();

        stale.Should().BeEmpty(
            "登记表里以下产物已不再被AddSource 发射（陈旧条目会掩盖真实缺口，请删除）：{0}",
            string.Join(" | ", stale));
    }

    // ────────── 采集实现 ──────────

    /// <summary>匹配 <c>AddSource("X.g.cs", …)</c> 的字面量产物名（含首尾引号）。</summary>
    private const string literalHintNamePattern = "^\"(?<name>[^\"]+)\"$";

    /// <summary>解析生成器全部 <c>AddSource(...)</c> 的产物名（字面量与 HintName/常量/模板三种形式）。</summary>
    private static List<string> ReadEmittedProducts()
    {
        var root = Path.Combine(FindRepositoryRoot(), "Mud.Feishu.AI.Tools");
        var products = new List<string>();

        foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            var source = File.ReadAllText(file);

            // 形如：context.AddSource("X.g.cs", …) / context.AddSource(\n    HintName(plan.TypeName), …)
            foreach (System.Text.RegularExpressions.Match call in Regex.Matches(
                source, @"AddSource\s*\(\s*(?<arg>[^,]+?)\s*,"))
            {
                var arg = call.Groups["arg"].Value.Trim().TrimEnd(',').Trim();

                // 字面量形式。
                var literal = Regex.Match(arg, literalHintNamePattern);
                if (literal.Success)
                {
                    products.Add(literal.Groups["name"].Value);
                    continue;
                }

                // 常量/模板/方法调用形式：归一化到登记表键。
                if (arg.Contains("ToolArgs", StringComparison.Ordinal) || arg.Contains("TypeName", StringComparison.Ordinal))
                {
                    products.Add("ToolArgs/{TypeName}.g.cs");
                }
                else if (arg.Contains("RegistrarHintName", StringComparison.Ordinal))
                {
                    products.Add("{RegistrarHintName}.g.cs");
                }
                else if (arg.Contains("CoreFileName", StringComparison.Ordinal))
                {
                    products.Add("{CoreFileName}");
                }
                else if (arg.Contains("HintName", StringComparison.Ordinal))
                {
                    // GuidanceEmitter 的 HintName 常量（值即 FeishuToolGuidance.g.cs）。
                    var constant = ReadHintNameConstant(source);
                    products.Add(constant ?? "UNKNOWN_HINTNAME");
                }
            }
        }

        return [.. products.Distinct(StringComparer.Ordinal)];
    }

    /// <summary>读取 <c>public const string HintName = "…"</c> 的值（用于 GuidanceEmitter）。</summary>
    private static string? ReadHintNameConstant(string source)
    {
        var match = Regex.Match(source, @"\bHintName\s*=\s*""(?<name>[^""]+)""");
        return match.Success ? match.Groups["name"].Value : null;
    }

    /// <summary>把实际产物名映射到登记表键（允许登记表用模板形式登记动态产物）。</summary>
    private static bool TryMatchRegisteredKey(string product, out string key)
    {
        if (ProductGates.ContainsKey(product))
        {
            key = product;
            return true;
        }

        // 模板匹配：把 {X} 段视为通配。
        foreach (var candidate in ProductGates.Keys)
        {
            if (!candidate.Contains('{'))
            {
                continue;
            }

            var pattern = "^" + Regex.Escape(candidate).Replace(@"\{", "{").Replace(@"\}", "}") + "$";

            // 转义后 \{ 仍是 \{ ，用正则替换还原为捕获组。
            pattern = Regex.Replace(Regex.Escape(candidate), @"\\\{[A-Za-z]+\\\}", ".+");

            if (Regex.IsMatch(product, pattern))
            {
                key = candidate;
                return true;
            }
        }

        key = string.Empty;
        return false;
    }

    private static string FindRepositoryRoot()
    {
        var directory = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(directory) && !File.Exists(Path.Combine(directory, "Mud.Feishu.slnx")))
        {
            directory = Path.GetDirectoryName(directory);
        }

        directory.Should().NotBeNullOrEmpty("测试必须能定位仓库根目录（以 Mud.Feishu.slnx 为锚）");
        return directory!;
    }
}