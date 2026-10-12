// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

using Mud.Feishu.AI.AgentTools;

namespace Mud.Feishu.AI.Tools.Tests.ContractGuards;

/// <summary>
/// R-3 / F-7：per-tool affordance 的<b>接入契约</b>守卫。
/// </summary>
/// <remarks>
/// <para>
/// 上游引擎（<c>Mud.HttpUtils 3.0.6</c>）已经用 <c>MUDFT028/029/030/031</c> 把
/// "资产写错"变成构建期诊断。本类不重复它，守的是<b>诊断看不到的那一段</b>——
/// 从"资产通过校验"到"模型真的看到它"之间的接线：
/// <list type="number">
/// <item><b>接线三件套一致</b>：剖面槽 <c>AffordanceDirectory</c> ↔ csproj 的 <c>AdditionalFiles</c> ↔
/// 磁盘目录名。任一不一致 ⇒ 引擎收不到资产 ⇒ <c>x-feishu.affordance</c> 整键消失，
/// 而构建<b>全绿</b>（MUDFT 一条都不会报：它压根没看到资产）；</item>
/// <item><b>强制集合</b>：每个 <c>high-risk-write</c> 工具都必须有资产——这条把"哪些工具必须写"
/// 从人的记忆变成机器断言（新增高危工具不同批补资产即报红）；</item>
/// <item><b>模型可见性</b>：资产配了就必须出现在模型必然读到的字段上（工具 <c>Description</c>），
/// 且**未配资产的工具描述逐字节不变**（升级回归锚）。</item>
/// </list>
/// </para>
/// <para>
/// <b>为什么用仓库侧复算而不是只信 golden</b>：golden 只证明"生成器看到了资产"，不证明
/// "资产集合 == 期望集合"（少了资产，golden 只是少一块，不会报错）。
/// </para>
/// </remarks>
public class AffordanceContractGuards
{
    /// <summary>资产目录（相对仓库根）。</summary>
    private const string AssetDirectory = "Mud.Feishu.AI.Tools/Affordance";

    /// <summary>上游契约的四个固定小节标题（大小写不敏感匹配）。</summary>
    private static readonly string[] ContractHeadings = ["WHEN", "Avoid when", "Prerequisites", "Tips"];

    /// <summary>单份资产字符上界（镜像上游 <c>AffordanceAssets.MaxAssetLength</c>）。</summary>
    private const int MaxAssetLength = 1024;

    /// <summary>单个小节字符上界（镜像上游 <c>AffordanceAssets.MaxSectionLength</c>）。</summary>
    private const int MaxSectionLength = 320;

    // ── 规模冻结（R-3/F-7 接入批实测；增减资产必须同批改这里的数字并说明理由）──
    // 字符总数按 LF 归一化口径测量（ReadAssets 统一 \r\n → \n），Windows(CRLF) 与 CI(LF) 同值。
    private const int ExpectedAssetCount = 16;

    private const int ExpectedTotalAssetCharacters = 5797;

    [Fact]
    public void EveryAffordanceAsset_ShouldSatisfyTheUpstreamStructureContract()
    {
        var violations = new List<string>();
        foreach (var (fileName, _, text) in ReadAssets())
        {
            var error = CheckStructure(text);
            if (error.Length > 0)
            {
                violations.Add($"{fileName}: {error}");
            }
        }

        violations.Should().BeEmpty(
            "以下 affordance 资产不符合上游词法契约（四节固定且各节非空；不得出现非契约小节的 '#' 行）：\n{0}",
            string.Join("\n", violations));
    }

    [Fact]
    public void EveryAffordanceAsset_ShouldTargetAnExistingTool()
    {
        var known = FeishuToolNames.All.ToHashSet(StringComparer.Ordinal);
        var orphans = ReadAssets()
            .Where(asset => !known.Contains(asset.ToolName))
            .Select(asset => asset.FileName)
            .ToArray();

        orphans.Should().BeEmpty(
            "以下资产的文件名不命中任何已策展工具名（等价于 MUDFT029，且改工具名时最容易踩）：{0}",
            string.Join(", ", orphans));

        // 扫描面自证：目录里读到 0 份资产时，上面那条会因"空集"而假绿。
        ReadAssets().Should().HaveCount(
            ExpectedAssetCount,
            "资产份数是冻结基线——增减必须同批更新本数字（防“资产被误删后守卫静默变空”）");
    }

    [Fact]
    public void EveryHighRiskWriteTool_ShouldDeclareAffordance()
    {
        var assets = ReadAssets().Select(asset => asset.ToolName).ToHashSet(StringComparer.Ordinal);

        var missing = FeishuToolContracts.ByToolName.Values
            .Where(contract => contract.Risk == FeishuToolRisk.HighRiskWrite)
            .Where(contract => !assets.Contains(contract.Name))
            .Select(contract => contract.Name)
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();

        missing.Should().BeEmpty(
            "以下 high-risk-write 工具没有 affordance 资产——它们是“误用即不可逆”的面，"
            + "必须显式回答何时用/不要用/前置/提示（新增高危工具时同批补资产）：{0}",
            string.Join(", ", missing));

        // 反向自证：高危集合不得为空（否则本断言恒绿）。
        FeishuToolContracts.ByToolName.Values
            .Count(contract => contract.Risk == FeishuToolRisk.HighRiskWrite)
            .Should().BeGreaterThan(0, "高危写工具集合不得为空（空集会让本守卫恒绿）");
    }

    [Fact]
    public void GoldenSchema_ShouldCarryAffordance_ExactlyForToolsWithAssets()
    {
        var assets = ReadAssets().Select(asset => asset.ToolName).ToHashSet(StringComparer.Ordinal);

        var withAffordance = FeishuToolSchemas.SchemaByToolName
            .Where(pair => pair.Value.Contains("\"affordance\"", StringComparison.Ordinal))
            .Select(pair => pair.Key)
            .ToHashSet(StringComparer.Ordinal);

        withAffordance.Should().BeEquivalentTo(
            assets,
            "编译期 Schema 里带 affordance 的工具集合必须**恰好**等于资产集合——"
            + "少了说明接线断裂（剖面槽/csproj AdditionalFiles/目录名 任一不一致），"
            + "多了说明有资产没进目录或 golden 陈旧");

        // 四键恒出现（上游保证四节非空 ⇒ 四键必在），逐键抽验防"只发射了一部分"。
        foreach (var toolName in assets)
        {
            var schema = FeishuToolSchemas.SchemaByToolName[toolName];
            foreach (var key in new[] { "\"when\"", "\"avoid_when\"", "\"prerequisites\"", "\"tips\"" })
            {
                schema.Should().Contain(key, $"'{toolName}' 的 affordance 必须含 {key} 键（四键恒出现）");
            }
        }
    }

    [Fact]
    public void AffordanceReferences_ShouldExistInTheToolNameContract()
    {
        var known = FeishuToolNames.All.ToHashSet(StringComparer.Ordinal);
        var dangling = new List<string>();

        foreach (var (fileName, _, text) in ReadAssets())
        {
            foreach (var reference in FindDanglingReferences(text, known))
            {
                dangling.Add($"{fileName} → {reference}");
            }
        }

        dangling.Should().BeEmpty(
            "以下 affordance 资产引用了工具名契约表里不存在的工具（等价于 MUDFT030；"
            + "改工具名时最容易漏改资产里的反引号引用）：{0}",
            string.Join(", ", dangling));
    }

    [Fact]
    public void AffordanceBudget_ShouldStayWithinBounds_AndBeMeasured()
    {
        var assets = ReadAssets();

        foreach (var (fileName, _, text) in assets)
        {
            text.Length.Should().BeLessThanOrEqualTo(
                MaxAssetLength, $"'{fileName}' 超单资产上界（上游 MUDFT031 的口径；处置是压缩文案，不是抬预算）");
        }

        assets.Sum(asset => asset.Text.Length).Should().Be(
            ExpectedTotalAssetCharacters,
            "affordance 是模型可见 token 预算的一部分——总字符数是**测量记录**，"
            + "变化必须同批更新本数字（顺带强制评审“这份新增资产值不值这些 token”）");

        foreach (var (fileName, _, text) in assets)
        {
            foreach (var (heading, body) in ParseSections(text))
            {
                body.Length.Should().BeLessThanOrEqualTo(
                    MaxSectionLength, $"'{fileName}' 的 '{heading}' 小节超上界（上游口径：单小节 ≤ {MaxSectionLength}）");
            }
        }
    }

    // ── 模型可见性（"接线"的最后一段）────────────────────────────────────────

    [Fact]
    public void ToolsWithAssets_ShouldExposeAffordanceOnTheModelVisibleDescription()
    {
        foreach (var (_, toolName, text) in ReadAssets())
        {
            var function = CreateFunction(toolName);

            function.Description.Should().Contain(
                "【决策指导】", $"'{toolName}' 配了资产，模型可见描述里必须有决策指导块");

            // 抽验"内容真的来自资产"（而不是只加了个标题）：取 WHEN 节首条的前若干字符。
            var when = ParseSections(text).First(section => section.Heading == "WHEN").Body;
            var probe = FirstItem(when);
            probe.Should().NotBeNullOrWhiteSpace($"'{toolName}' 的 WHEN 节必须有条目（结构守卫已保证，这里做端到端抽验）");
            function.Description.Should().Contain(
                probe![..Math.Min(12, probe.Length)],
                $"'{toolName}' 的决策指导必须真的来自资产内容（防“接了个空标题”）");
        }
    }

    [Fact]
    public void ToolsWithoutAssets_ShouldKeepTheirDescriptionByteIdentical()
    {
        var assets = ReadAssets().Select(asset => asset.ToolName).ToHashSet(StringComparer.Ordinal);

        // 升级回归锚：未配资产的工具（多数）描述必须与 definition.Description 完全一致——
        // affordance 是"加量"而不是"改口径"，任何描述漂移都会改变模型看到的历史行为。
        foreach (var contract in FeishuToolContracts.ByToolName.Values.Where(c => !assets.Contains(c.Name)))
        {
            var function = CreateFunction(contract.Name);
            function.Description.Should().Be(
                contract.Description, $"'{contract.Name}' 没有 affordance 资产 ⇒ 描述必须逐字节不变");
        }
    }

    [Fact]
    public void McpEnvelopeExport_ShouldCarryAffordance_ExactlyForToolsWithAssets()
    {
        var assets = ReadAssets().Select(asset => asset.ToolName).ToHashSet(StringComparer.Ordinal);

        var exported = JsonNode.Parse(new FeishuToolSchemaExporter().Export(ToolSchemaDialect.McpEnvelope))!;
        var tools = exported["tools"]!.AsArray();

        var carried = tools
            .Where(tool => tool!["affordance"] is not null)
            .Select(tool => tool!["title"]!.GetValue<string>())
            .ToHashSet(StringComparer.Ordinal);

        carried.Should().BeEquivalentTo(
            assets,
            "McpEnvelope 方言必须为“配了资产的工具”透传结构化 affordance（进程外消费者按字段机械消费），"
            + "未配资产的工具**不得**出现该字段（否则导出结果对 166 个工具产生无意义 diff）");

        // 抽验结构：透传的是四键对象，而不是被压成字符串。
        var sample = tools.First(tool => tool!["affordance"] is not null)!;
        var affordance = sample["affordance"]!.AsObject();
        affordance.Select(pair => pair.Key).Should().BeEquivalentTo(
            ["when", "avoid_when", "prerequisites", "tips"], "透传的必须是原始四键结构");
    }

    [Fact]
    public void ExtensionKeyConstant_ShouldMatchTheProfileSlotValue()
    {
        // 线格式键在两处出现：剖面槽（编译期事实）与运行期读取常量。二者不一致 ⇒ 读不到 affordance
        // 而**不报任何错**（只是"资产配了却看不见"），故必须机械绑定。
        var attribute = typeof(Mud.Feishu.AI.Tools.SdkProfile.FeishuToolProfile)
            .GetCustomAttributes(typeof(Mud.HttpUtils.Attributes.SdkToolProfileAttribute), inherit: false)
            .Cast<Mud.HttpUtils.Attributes.SdkToolProfileAttribute>()
            .Single();

        ToolAffordanceText.ExtensionKey.Should().Be(
            attribute.SchemaExtensionKey,
            "ToolAffordanceText.ExtensionKey 必须等于剖面槽 SchemaExtensionKey（改一处不改另一处即为静默失效）");

        attribute.AffordanceDirectory.Should().Be(
            "/" + AssetDirectory[(AssetDirectory.LastIndexOf('/') + 1)..] + "/",
            "剖面槽的目录片段必须与磁盘目录名一致（csproj 的 AdditionalFiles 目录亦然）");
    }

    // ── 检查器自证（每条判据都必须能报红，否则是假门禁）──────────────────────

    [Fact]
    public void StructureScanner_ShouldReportUnknownHeading()
    {
        CheckStructure("## WHEN\n- 用\n## Avoid when\n- 别\n## Prerequisites\n- 先\n## Tips\n- 注意\n")
            .Should().BeEmpty("合法资产不得报红（否则会误伤正确资产）");

        CheckStructure("#im.send_message\n## WHEN\n- 用\n## Avoid when\n- 别\n## Prerequisites\n- 先\n## Tips\n- 注意\n")
            .Should().Contain("未知小节", "非契约小节的 '#' 行必须报红（上游把任何 '#' 行当小节标题）");

        CheckStructure("## WHEN\n- 用\n## Avoid when\n- 别\n## Prerequisites\n- 先\n")
            .Should().Contain("缺少小节", "缺节必须报红");

        CheckStructure("## WHEN\n## Avoid when\n- 别\n## Prerequisites\n- 先\n## Tips\n- 注意\n")
            .Should().Contain("为空", "空节必须报红");
    }

    [Fact]
    public void ReferenceScanner_ShouldReportDanglingToolName()
    {
        var known = new HashSet<string>(["im.send_message"], StringComparer.Ordinal);

        FindDanglingReferences("- 改用 `im.send_message` 发送。", known).Should().BeEmpty(
            "存在的工具名不得报红");

        FindDanglingReferences("- 改用 `im.not_a_tool` 发送。", known).Should().ContainSingle().Which.Should().Be(
            "im.not_a_tool", "不存在的工具名必须被点名（MUDFT030 的仓库侧等价判据）");

        FindDanglingReferences("- 参数 `dry_run` 与字段 `link_share_entity` 不参与校验。", known).Should().BeEmpty(
            "非“域.工具”形态的反引号片段（参数名 / 字段名）不参与校验——否则正常散文会变成假红");
    }

    // ── 实现 ──

    private static FeishuToolAIFunction CreateFunction(string toolName)
    {
        var contract = FeishuToolContracts.ByToolName[toolName];
        var definition = new FeishuToolDefinition(
            toolName,
            contract.Description,
            contract.RequiredScopes,
            contract.IsWrite,
            contract.Risk,
            contract.Identity,
            (_, _, _) => Task.FromResult(FeishuToolResult.FromText(string.Empty)));

        return new FeishuToolAIFunction(definition, FeishuToolSchemas.SchemaByToolName[toolName]);
    }

    private static (string FileName, string ToolName, string Text)[] ReadAssets()
    {
        var directory = Path.Combine(FindRepositoryRoot(), AssetDirectory.Replace('/', Path.DirectorySeparatorChar));
        Directory.Exists(directory).Should().BeTrue($"{AssetDirectory} 必须存在（否则本守卫退化为空扫描）");

        return
        [
            .. Directory.EnumerateFiles(directory, "*.md", SearchOption.TopDirectoryOnly)
                .OrderBy(static path => path, StringComparer.Ordinal)
                .Select(path => (
                    Path.GetFileName(path),
                    Path.GetFileNameWithoutExtension(path),
                    // 行尾归一化：字符预算是**内容**度量——Windows 检出 CRLF、CI 检出 LF，
                    // 不归一会让基线随检出平台漂移（差异 = 精确总行数）。
                    File.ReadAllText(path).Replace("\r\n", "\n")))
        ];
    }

    /// <summary>上游词法的仓库侧复算：四节固定、各节非空、无未知/重复小节。</summary>
    private static string CheckStructure(string text)
    {
        var sections = ParseSections(text);

        if (sections.Count == 0)
        {
            return "未识别到任何小节（须为 WHEN / Avoid when / Prerequisites / Tips）";
        }

        foreach (var (heading, _) in sections)
        {
            if (!ContractHeadings.Contains(heading, StringComparer.OrdinalIgnoreCase))
            {
                return $"未知小节 '{heading}'（可用小节：{string.Join(" / ", ContractHeadings)}）";
            }
        }

        foreach (var expected in ContractHeadings)
        {
            var found = sections.FirstOrDefault(section =>
                string.Equals(section.Heading, expected, StringComparison.OrdinalIgnoreCase));

            if (found == default)
            {
                return $"缺少小节 '{expected}'（四节均为必填）";
            }

            if (string.IsNullOrWhiteSpace(found.Body))
            {
                return $"小节 '{expected}' 为空（四节均须至少一个条目）";
            }
        }

        return string.Empty;
    }

    /// <summary>取全部小节（标题 + 体），并恢复"重复小节"这一错误形态。</summary>
    private static List<(string Heading, string Body)> ParseSections(string text)
    {
        var sections = new List<(string Heading, string Body)>();
        string? heading = null;
        var body = new System.Text.StringBuilder();

        foreach (var rawLine in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
        {
            var trimmed = rawLine.TrimStart();
            if (trimmed.StartsWith('#'))
            {
                if (heading is not null)
                {
                    sections.Add((heading, body.ToString()));
                }

                heading = trimmed.TrimStart('#').Trim();
                body.Clear();
                continue;
            }

            if (heading is not null)
            {
                body.Append(rawLine.Trim()).Append('\n');
            }
        }

        if (heading is not null)
        {
            sections.Add((heading, body.ToString()));
        }

        return sections;
    }

    /// <summary>取小节里第一个条目（跳过空行；条目 = 去掉项目符号的一行）。</summary>
    private static string? FirstItem(string sectionBody)
        => sectionBody
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(static line => line.Trim().TrimStart('-', '*', '+').Trim())
            .FirstOrDefault(static line => line.Length > 0);

    /// <summary>反引号包裹且形如 <c>域.工具</c>、但不在工具名契约表内的引用。</summary>
    internal static IReadOnlyList<string> FindDanglingReferences(string text, IReadOnlyCollection<string> known)
    {
        var dangling = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var index = 0;

        while (index < text.Length)
        {
            var open = text.IndexOf('`', index);
            if (open < 0)
            {
                break;
            }

            var close = text.IndexOf('`', open + 1);
            if (close < 0)
            {
                break;
            }

            var span = text.Substring(open + 1, close - open - 1).Trim();
            if (LooksLikeToolName(span) && !known.Contains(span) && seen.Add(span))
            {
                dangling.Add(span);
            }

            index = close + 1;
        }

        return dangling;
    }

    private static bool LooksLikeToolName(string value)
    {
        var dot = value.IndexOf('.');
        if (dot <= 0 || dot == value.Length - 1 || value.IndexOf('.', dot + 1) >= 0)
        {
            return false;
        }

        return IsIdentifier(value[..dot]) && IsIdentifier(value[(dot + 1)..]);
    }

    private static bool IsIdentifier(string value)
        => value.Length > 0
            && value[0] >= 'a' && value[0] <= 'z'
            && value.All(static ch => (ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9') || ch == '_');

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
