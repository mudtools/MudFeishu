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

    // ── 规模冻结（R-3/F-7 接入批实测；增减资产必须同批改这里的数字并说明理由）──
    // 字符总数按 LF 归一化口径测量（ReadAssets 统一 \r\n → \n），Windows(CRLF) 与 CI(LF) 同值。
    private const int ExpectedAssetCount = 22;

    private const int ExpectedTotalAssetCharacters = 9243;

    // B2 瘦身（测试体系治理，见 documents/AIAgent/单元测试体系过度测试分析与解决方案.md §三.5）：
    // 与上游诊断**同一判定**的仓库侧副本已删除——
    //   · 资产结构（四节固定/空节/未知节）= MUDFT028（Error，构建即断）
    //   · 资产文件名命中契约工具名         = MUDFT029（Error，构建即断）
    //   · 资产内反引号引用存在性           = MUDFT030（Error，构建即断）
    //   · 单资产/单小节字符上界            = MUDFT031（Warning，基线恒 0，CI/Standard 档断言）
    // 本类只守 MUDFT 看不到的两段：接线（资产 → 模型可见面）与规模冻结（目录被整体清空时
    // 上述诊断全部静默——没有文件就没有诊断，故冻结计数是唯一防线）。

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
    public void AffordanceBudget_TotalScale_ShouldStayAtTheFrozenMeasurement()
    {
        // 上界本身由上游 MUDFT031 判定（Warning + 基线，CI/Standard 档断言）；
        // 本守卫只冻结**规模测量**：affordance 是模型可见 token 预算的一部分，
        // 总字符数变化必须同批更新本数字（顺带强制评审“这份新增资产值不值这些 token”）。
        // 同时兼任扫描面防呆：目录被清空/改名时，MUDFT029–031 都静默，只有这里的份数会红。
        var assets = ReadAssets();

        assets.Should().HaveCount(
            ExpectedAssetCount,
            "资产份数是冻结基线——增减必须同批更新本数字（防“资产被误删后守卫静默变空”）");

        assets.Sum(asset => asset.Text.Length).Should().Be(
            ExpectedTotalAssetCharacters,
            "affordance 是模型可见 token 预算的一部分——总字符数是**测量记录**，"
            + "变化必须同批更新本数字");
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

    // ── AD-2：affordance 不得复述工具描述 ──────────────────────────────────

    /// <summary>
    /// <b>AD-2（防重复门禁第 2 条）</b>：affordance 的 <c>WHEN</c> 首条不得与工具 <c>description</c>
    /// 逐字重复（对齐官方 <c>content_embed_affordance_test.go</c> 的 "use_when ≠ description"）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>要防的形态</b>：把工具描述原句搬进 affordance 的 <c>WHEN</c> 节。此时资产"看起来写了"，
    /// 但对模型<b>零信息增量</b>——描述已经在 tools/list 里给过一遍，而 affordance 的价值恰恰是
    /// 决策边界（"什么情况下该用 / 什么情况下别用"），不是再来一遍"这个工具做什么"。
    /// </para>
    /// <para>
    /// <b>判据（归一化后子串包含，长度 ≥ 24）</b>：去空白、去 markdown 强调与标点后比较——
    /// 否则"加个反引号/换个标点"就能绕过门禁（那是断言宽容度的漏洞，不是内容差异）。
    /// 长度阈值避免"用"、"调用"这类短词造成假红。
    /// </para>
    /// </remarks>
    [Fact]
    public void AffordanceWhen_ShouldNotRepeatTheToolDescription()
    {
        var violations = new List<string>();

        foreach (var (fileName, toolName, text) in ReadAssets())
        {
            var contract = FeishuToolContracts.ByToolName[toolName];
            var when = ParseSections(text).First(section => section.Heading == "WHEN").Body;
            var probe = Normalize(FirstItem(when) ?? string.Empty);
            var description = Normalize(contract.Description);

            if (probe.Length >= 24 && description.Contains(probe, StringComparison.Ordinal))
            {
                violations.Add($"{fileName}: WHEN 首条与工具 description 逐字重复（{probe.Length} 字符）");
            }
        }

        violations.Should().BeEmpty(
            "以下资产的 WHEN 节复述了工具描述——affordance 必须给「决策边界」而不是「工具做什么」：{0}",
            string.Join(" | ", violations));
    }

    /// <summary>AD-2 判据的自证（合成输入必须能被点名；否则本条是假门禁）。</summary>
    [Fact]
    public void RepetitionScanner_ShouldDetectCondensedDuplicate()
    {
        var description = Normalize("删除一个预约（Reserve）——预约对应的会议号随即失效且不可恢复。");
        var repeated = Normalize("- 删除一个预约( Reserve )——预约对应的会议号随即失效且不可恢复。");

        description.Contains(repeated, StringComparison.Ordinal).Should().BeTrue(
            "归一化必须抹平标点/空白差异，否则加一个空格就能绕过门禁");

        Normalize("讨论冲突日程时先查忙闲").Length.Should().BeGreaterThan(0);
        description.Contains(Normalize("讨论冲突日程时先查忙闲"), StringComparison.Ordinal).Should().BeFalse(
            "真正的决策边界句不得被误判为复述");
    }

    /// <summary>
    /// <b>AD-1</b>：affordance 的 <c>Tips</c> 不得与<b>同域 L1 guidance</b> 大段互含
    /// （归一化后任一 ≥30 字符连续片段不得互含）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>为什么需要这条</b>：affordance 与 L1 guidance 是<b>两条独立的装配路径</b>
    /// （前者随工具描述进上下文，后者经 routing 摘要 / <c>guidance_read</c>）。
    /// 一旦把 L1 的段落抄进 <c>Tips</c>，同一份内容会在上下文里出现两次——
    /// 既白烧预算，又让"两处说法漂移"成为可能（改一处忘一处 ⇒ 模型拿到自相矛盾的指引）。
    /// </para>
    /// <para>
    /// <b>阈值由来</b>：30 字符（方案 §7.3 判据列原文）。低于这个长度时，"都提到 <c>dry_run</c>"
    /// 这类**必然共现的术语**会造成假红；30 字符≈一个完整短句，只有真抄段落才会撞上。
    /// </para>
    /// <para>
    /// <b>未接入的域</b>（无同名 L1 文件）跳过：那些域的"有资产无入口"由
    /// <see cref="GuidanceToolReferenceContractGuards"/> 独立断言，不在这里混判。
    /// </para>
    /// </remarks>
    [Fact]
    public void AffordanceTips_ShouldNotOverlapSameDomainGuidanceL1()
    {
        const int WindowLength = 30;
        var l1ByDomain = ReadDomainL1Assets();
        var violations = new List<string>();
        var measured = new List<string>();

        foreach (var (fileName, toolName, text) in ReadAssets())
        {
            var domain = toolName.Split('.')[0];
            if (!l1ByDomain.TryGetValue(domain, out var l1))
            {
                continue;
            }

            var tips = Normalize(string.Join(
                " ",
                ParseSections(text).Where(static s => s.Heading == "Tips").Select(static s => s.Body)));

            var (length, fragment) = LongestCommonFragment(tips, l1);
            measured.Add($"{fileName}={length}");

            if (length >= WindowLength)
            {
                violations.Add($"{fileName} 与 Guidance/{domain}.md 有 {length} 字符重叠片段：「{fragment}」"
                    + $"（Tips {tips.Length} 字符 / L1 {l1.Length} 字符）");
            }
        }

        // 扫描面自证：若一条都没量到（目录改名 / 域解析写坏），本用例会恒绿。
        measured.Should().NotBeEmpty(
            "扫描面为空 = 判据失效（假绿）：{0} 下必须有资产，且其域名必须能在 Guidance/ 找到同名 L1",
            AssetDirectory);

        violations.Should().BeEmpty(
            "affordance 的 Tips 与同域 L1 guidance 出现了 ≥{0} 字符的重叠——同一内容不得在两条装配路径上重复：{1}",
            WindowLength, string.Join(" | ", violations));
    }

    /// <summary>AD-1 判据的自证（合成输入必须能被点名；阈值两侧都要能区分）。</summary>
    /// <remarks>
    /// 合成数据<b>必须够长</b>：首版手写样例归一化后只有 20 字符重合，于是"整句抄 L1"这条自证先红了
    /// （判据本身没错，是样例太短）——所以这里显式断言合成片段长度 ≥40。
    /// </remarks>
    [Fact]
    public void OverlapScanner_ShouldMeasureLongestCommonFragment()
    {
        var shared = Normalize("群发前先确认接收方；单聊与群聊走同一个工具，接收方类型由 receive_id_type 决定。");
        shared.Length.Should().BeGreaterThanOrEqualTo(40, "合成片段太短则本自证无意义（阈值是 30）");

        var l1 = Normalize("## 发送消息\n- ") + shared;
        var copied = Normalize("- ") + shared[..40] + Normalize(" 然后按需截断");

        LongestCommonFragment(copied, l1).Length.Should().BeGreaterThanOrEqualTo(30, "整句抄自 L1 必须被测出来");

        LongestCommonFragment(Normalize("- 该工具只支持文本消息，富文本请改用 post 类型发送。"), l1).Length
            .Should().BeLessThan(30, "真正的 Tips（决策边界）不得被误判");

        LongestCommonFragment(string.Empty, l1).Length.Should().Be(0, "空输入必须短路（不得抛）");
    }

    /// <summary>归一化：去空白/markdown 强调/常见中英标点（仅用于 AD-2 的重复判据）。</summary>
    private static string Normalize(string text)
        => new([.. text.Where(static ch => !char.IsWhiteSpace(ch) && !IsNoise(ch))]);

    private static bool IsNoise(char ch)
        => ch is '`' or '*' or '_' or '-' or '—' or '～' or '~' or '（' or '）' or '(' or ')'
            or '，' or ',' or '。' or '.' or '：' or ':' or '；' or ';' or '、' or '/' or '｜' or '|'
            or '"' or '\'' or '“' or '”' or '‘' or '’';

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

    /// <summary>读取 <c>Guidance/</c> 下的 L1 域资产（域 → 归一化正文）。</summary>
    /// <remarks>
    /// 只扫<b>顶层</b> <c>*.md</c>：L2 主题在子目录里，且 AD-3 已单独管"L2 必须被 L1 引用"，
    /// 混进来会让"L2 与 affordance 的正常呼应"变成假红。
    /// </remarks>
    private static Dictionary<string, string> ReadDomainL1Assets()
    {
        const string GuidanceDirectory = "Mud.Feishu.AI.Tools/Guidance";
        var directory = Path.Combine(FindRepositoryRoot(), GuidanceDirectory.Replace('/', Path.DirectorySeparatorChar));
        Directory.Exists(directory).Should().BeTrue($"{GuidanceDirectory} 必须存在（否则 AD-1 退化为空扫描）");

        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var path in Directory.EnumerateFiles(directory, "*.md", SearchOption.TopDirectoryOnly))
        {
            // 行尾归一化：字符度量不得随检出平台漂移（与 ReadAssets 同口径）。
            map[Path.GetFileNameWithoutExtension(path)] =
                Normalize(File.ReadAllText(path).Replace("\r\n", "\n"));
        }

        return map;
    }

    /// <summary>两段文本的最长公共<b>连续</b>片段（长度 + 片段本身，用于 AD-1 的度量与报错定位）。</summary>
    /// <remarks>
    /// 经典后缀 DP，滚动数组降到 O(min(n,m)) 空间。选"最长公共连续片段"而不是"最长公共子序列"：
    /// 后者对"抄了但要拆散重排"不敏感，而 AD-1 要抓的正是<b>整句照抄</b>。
    /// </remarks>
    internal static (int Length, string Fragment) LongestCommonFragment(string a, string b)
    {
        if (a.Length == 0 || b.Length == 0)
        {
            return (0, string.Empty);
        }

        var previous = new int[b.Length + 1];
        var current = new int[b.Length + 1];
        var best = 0;
        var bestEndInA = 0;

        for (var i = 1; i <= a.Length; i++)
        {
            for (var j = 1; j <= b.Length; j++)
            {
                current[j] = a[i - 1] == b[j - 1] ? previous[j - 1] + 1 : 0;
                if (current[j] > best)
                {
                    best = current[j];
                    bestEndInA = i;
                }
            }

            (previous, current) = (current, previous);

            // 换过来的数组是上一轮的行，必须清零——否则本轮未赋值的格子会带着陈旧长度参与下一轮。
            Array.Clear(current, 0, current.Length);
        }

        return (best, best == 0 ? string.Empty : a.Substring(bestEndInA - best, best));
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
