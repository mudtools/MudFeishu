// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
// 任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.RegularExpressions;

namespace Mud.Feishu.AI.Tools.Tests.ContractGuards;

/// <summary>
/// 参数形态认知契约守卫（WP7 / WP1）：断言 <c>JsonValueKind.String</c> 的参数值形态认知
/// 只允许出现在归一化中心（<c>ToolArgumentNormalizer</c>）与既有的摘要/值类型转换文件中——
/// 防止 S1（入站净化被 JsonElement 绕过）的根因（四处各自实现形态认知、漏一处即静默失效）复现。
/// </summary>
/// <remarks>
/// <para>
/// <b>判据（R-2 收缩为单文件白名单）</b>：<c>JsonValueKind.String</c> 只允许出现在
/// <c>ToolArgumentNormalizer</c> 内（形态认知的唯一中心）。此前白名单有 6 项，其代价是
/// 「同一入参在不同位置被判成不同形态」会静默通过（R1.3 评审实测 <c>ToolPagination.TryReadBool</c>
/// / <c>ToolDryRun.IsRequested</c> / <c>ToolSearchTools.TryReadInt</c> 各有第三、四、七份副本）。
/// R-2 把判定收成两条原语（<c>IsJsonString</c>/<c>IsJsonArray</c>），其余位置只能薄委托：
/// <list type="bullet">
/// <item><c>ToolArgs</c>（取值门面）→ 转调原语；</item>
/// <item><c>ToolArgsDigester</c>（审计摘要词汇）→ 转调原语（产出词汇仍由摘要器决定）；</item>
/// <item><c>ToolPagination</c> / <c>ToolDryRun</c> / 执行器 → 转调 <c>ToolArgs</c> 的取值门面。</item>
/// </list>
/// </para>
/// <para>
/// 新增位置若需处理 <c>JsonElement</c> 的文本值，必须经上述原语/门面——否则在本守卫报红。
/// </para>
/// </remarks>
public class ToolArgumentShapeContractGuards
{
    /// <summary>
    /// <c>JsonValueKind.String</c> 的参数值形态认知只允许出现在白名单文件中（S1 根因治理）。
    /// </summary>
    [Fact]
    public void JsonValueKindString_ShouldOnlyAppearInNormalizerAndAllowedFiles()
    {
        var root = FindRepositoryRoot();
        var projDir = Path.Combine(root, "Mud.Feishu.AI.Tools");

        // R5 / F-1：声明面在 Curation/；R-1+2c 后续重组：运行时基础设施按关注面解散为
        // Args/Results/Execution/Catalog/Hosting/Security/Parsers/Constants 八目录
        // （Errors/ 于阶段 A-1 随 B-5 迁出而消亡），Tools/ 仅余生成器钉死的根命名空间类型
        // （FeishuToolBinding）。
        // v2.3 迁移：B 组形态认知相关类型（含唯一认知中心 ToolArgumentNormalizer）随中立化迁出飞书仓，
        // 故本仓扫描面收窄为**产品侧**关注面目录——中立侧（Mud.AI.Agent/Tools）的形态认知约束由
        // 新仓 ToolArgumentShapeContractGuards 的同名守卫持有；两面互补，缺一即覆盖不全。
        var scanDirs = new[]
        {
            "Args", "Results", "Execution", "Catalog", "Hosting",
            "Security", "Parsers", "Constants", "Tools", "Curation",
        }.Select(d => Path.Combine(projDir, d))
         .ToArray();

        // 反缩水（§8.2「扫描面悄悄缩水」）：扫描目录必须真实存在——目录被删/改名会让
        // 上面的枚举静默退化成空集，"无违规"恒真（假绿）。
        foreach (var dir in scanDirs)
        {
            Directory.Exists(dir).Should().BeTrue($"扫描目录必须存在（否则扫描面静态缩水 → 假绿）：{dir}");
        }

        // 白名单（迁移后为**空**）：形态判定原语（IsJsonString/IsJsonArray）的唯一认知中心
        // ToolArgumentNormalizer 已迁入中立包 Mud.AI.Agent/Tools/Args——产品侧任何位置都不得再出现
        // JsonValueKind.String 的形态认知（取值门面 ToolArgs、摘要 ToolArgsDigester、分页/干跑助手
        // 只能薄委托中立原语）。"同一入参在不同工具里被判成不同形态"是 S1 的同类根因，
        // 收敛后该形态在产品源码层不可能再出现。
        var scannedPaths = scanDirs
            .SelectMany(static dir => Directory.EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories))
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .ToList();

        // 反缩水：扫描面不得为空（目录存在但枚举不到源码 = 判据恒真 → 假绿）。
        scannedPaths.Should().NotBeEmpty("扫描面为空说明路径解析坏了（本守卫会恒绿）");

        var offenders = scannedPaths
            .Where(p => File.ReadAllText(p).Contains("JsonValueKind.String", StringComparison.Ordinal))
            .Select(Path.GetFileName)
            .ToList();

        offenders.Should().BeEmpty(
            "JsonValueKind.String 的形态认知只允许出现在中立唯一认知中心 ToolArgumentNormalizer（新仓）；"
            + "产品侧取值/摘要位置必须薄委托 IsJsonString/IsJsonArray——各自 switch 是 S1 的根因"
            + "（漏一处即静默失效）——违规文件: " + string.Join(", ", offenders));
    }

    // ────────── R5 / F-1（载体 C）：策展面与基础设施的分离 ──────────

    /// <summary>
    /// <b>R5 / F-1（载体 C）</b>：<b>策展面与基础设施的分离守卫</b>（断言一 + 断言二）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>不变量</b>：人工<b>策展</b>的东西（<c>[AgentTool]</c>/<c>[ToolParameter]</c> 契约，
    /// 即"我们把什么能力交给模型"）必须与框架<b>提供</b>的东西（参数净化、结果裁剪、错误分类、
    /// DI 装配、Args 生成）在<b>目录层面</b>分开：前者集中在 <c>Curation/</c>，后者按关注面
    /// 分居 Args/Results/Errors/Execution/Catalog/Hosting/Security/Parsers/Constants。
    /// </para>
    /// <para>
    /// <b>Tools/ 的新语义（R-1+2c 重组后）</b>：仅收纳<b>被源生成器钉死在根命名空间</b>的类型——
    /// 当前恰为 <c>FeishuToolBinding.cs</c>（上游 <c>ToolRegistrarEmitter</c> 以
    /// <c>ContractNamespace + BindingTypeName</c> 全限定名引用，全限定名不经 using 解析，
    /// 迁往子命名空间即使生成代码 CS0234）。白名单化后该约束从"隐性魔咒"变为机械锁定。
    /// </para>
    /// <para>
    /// <b>为什么必须机械锁定</b>：纯人工不变量，退化方向<b>单向</b> —— 新增工具时顺手把接口写进
    /// 运行时目录，separation 立刻失效，而<b>没有任何编译期或运行期信号</b>。
    /// </para>
    /// <para>
    /// <b>与 R-3 的关系</b>：R3 评审否定 D-2a 的 JSON manifest（与已否决的 U-2 数据表外置同构、
    /// 丢掉 4 项编译期能力），裁定用<b>纯 C# 声明文件</b>（载体 C）。本守卫是该裁定的<b>执行端</b>。
    /// </para>
    /// </remarks>
    [Fact]
    public void CurationDirectory_ShouldContainOnlyToolContractDeclarations()
    {
        var root = FindRepositoryRoot();
        var projDir = Path.Combine(root, "Mud.Feishu.AI.Tools");
        var curation = Path.Combine(projDir, "Curation");
        var tools = Path.Combine(projDir, "Tools");

        // D-1（2026-10-12）：声明面按关注域分为六个组目录（Im/Docs/Collab/People/Platform/Core），
        // 组内仍是「一个域声明一个 *ToolInterfaces.cs」。枚举必须**递归**——组目录结构下
        // TopDirectoryOnly 会退化成"只看 Curation/ 顶层的 0 个文件"，约束被静默削弱（"漏掉任一
        // 目录即假绿"的老账）。身份改用**相对路径**，否则同名文件跨组移动对报告不可见。
        var mixedIn = Directory
            .EnumerateFiles(curation, "*.cs", SearchOption.AllDirectories)
            .Where(static path => !Path.GetFileName(path).EndsWith("ToolInterfaces.cs", StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(curation, path))
            .ToArray();

        mixedIn.Should().BeEmpty(
            "Curation/ 只应收纳工具契约声明（*ToolInterfaces.cs）；混入其他文件会让"
            + "人工策展面与框架基础设施的边界失焦：{0}",
            string.Join(" | ", mixedIn));

        // 工具契约声明不得落在任何运行时目录（原守卫只查 Tools/；重组后运行时面扩为多个关注面
        // 目录，不扩展即假绿——契约写进 Args/ 无人拦截）。Errors/ 目录于阶段 A-1 迁出后消亡，故移除。
        var contractFiles = new[]
        {
            "Args", "Results", "Execution", "Catalog", "Hosting",
            "Security", "Parsers", "Constants", "Tools",
        }
            .Select(d => Path.Combine(projDir, d))
            .SelectMany(dir => Directory.EnumerateFiles(dir, "*ToolInterfaces.cs", SearchOption.AllDirectories))
            .Select(static path => Path.GetFileName(path))
            .ToArray();

        contractFiles.Should().BeEmpty(
            "工具契约声明必须位于 Curation/（载体 C 的核心不变量）。以下文件散落在运行时目录："
            + "新增工具时请写入 Curation/，不要写进运行时基础设施目录：{0}",
            string.Join(" | ", contractFiles));

        // Tools/ 白名单化：仅允许生成器钉死的根命名空间类型（详见类备注）。
        var pinnedInTools = Directory
            .EnumerateFiles(tools, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(static path => Path.GetFileName(path))
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();

        pinnedInTools.Should().Equal(
            new[] { "FeishuToolBinding.cs" },
            "Tools/ 是「生成器契约钉死的根命名空间类型」白名单目录（FeishuToolBinding 被上游"
            + " ToolRegistrarEmitter 以 ContractNamespace+BindingTypeName 全限定名引用，不可迁子命名空间）。"
            + "新增/迁入其他文件即意味着又回到 44 文件平铺的旧态：{0}",
            string.Join(" | ", pinnedInTools));
    }

    /// <summary>
    /// <b>R-1+2c 重组守卫</b>：各 关注面目录与 <c>Tools/</c>（白名单目录）必须声明与目录
    /// 匹配的命名空间——目录与命名空间不一致会让读者按目录找不到类型（复用 Curation 先例的判据）。
    /// </summary>
    [Fact]
    public void ConcernDirectories_ShouldDeclareMatchingNamespace()
    {
        var projDir = Path.Combine(FindRepositoryRoot(), "Mud.Feishu.AI.Tools");

        var expected = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Args"] = "Mud.Feishu.AI.Tools.Args",
            ["Results"] = "Mud.Feishu.AI.Tools.Results",
            ["Execution"] = "Mud.Feishu.AI.Tools.Execution",
            ["Catalog"] = "Mud.Feishu.AI.Tools.Catalog",
            ["Hosting"] = "Mud.Feishu.AI.Tools.Hosting",
            ["Security"] = "Mud.Feishu.AI.Tools.Security",
            ["Parsers"] = "Mud.Feishu.AI.Tools.Parsers",
            ["Constants"] = "Mud.Feishu.AI.Tools.Constants",
            ["Tools"] = "Mud.Feishu.AI.Tools", // 生成器钉死的根命名空间白名单目录
        };

        var offenders = new List<string>();
        foreach (var (dir, ns) in expected)
        {
            var full = Path.Combine(projDir, dir);
            if (!Directory.Exists(full))
            {
                offenders.Add($"{dir}/ (目录不存在)");
                continue;
            }

            foreach (var path in Directory.EnumerateFiles(full, "*.cs", SearchOption.AllDirectories))
            {
                if (path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                {
                    continue;
                }

                var match = Regex.Match(
                    File.ReadAllText(path), @"^namespace\s+(?<ns>[\w.]+)", RegexOptions.Multiline);
                if (!match.Success || match.Groups["ns"].Value != ns)
                {
                    offenders.Add(Path.GetFileName(path) + " -> " + (match.Success ? match.Groups["ns"].Value : "(无 namespace)"));
                }
            }
        }

        offenders.Should().BeEmpty(
            "目录必须声明匹配的命名空间（Args→...Tools.Args，…，Tools→...Tools 根）：{0}",
            string.Join(" | ", offenders));
    }

    /// <summary>
    /// <b>R5 / F-1</b>：<c>Curation/</c> 内文件必须声明 <c>...FeishuTools.Curation</c> 命名空间
    /// （目录与命名空间不一致会让读者按目录找不到类型）。
    /// </summary>
    [Fact]
    public void CurationFiles_ShouldDeclareTheCurationNamespace()
    {
        const string Expected = "Mud.Feishu.AI.Tools.Curation";

        var curation = Path.Combine(FindRepositoryRoot(), "Mud.Feishu.AI.Tools", "Curation");

        var offenders = Directory
            .EnumerateFiles(curation, "*.cs", SearchOption.AllDirectories)
            .Where(path =>
            {
                var match = Regex.Match(
                    File.ReadAllText(path), @"^namespace\s+(?<ns>[\w.]+)", RegexOptions.Multiline);
                return !match.Success || match.Groups["ns"].Value != Expected;
            })
            .Select(path => Path.GetRelativePath(curation, path))
            .ToArray();

        offenders.Should().BeEmpty(
            "Curation/ 下的文件必须声明 namespace {0}：{1}", Expected, string.Join(" | ", offenders));
    }

    /// <summary>
    /// <b>D-1 伴随守卫</b>：<b>特性与声明不得分家</b> —— 每个声明文件里 <c>[AgentTool]</c>
    /// 特性数必须等于 <c>public interface</c> 数（且不为 0）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>为什么必须机械锁定</b>：本约束是 2026-10-12 声明面重组（原 27 文件平铺 → 六组目录 +
    /// 跨域混装文件按域拆分）过程中<b>实际踩中</b>的故障形态——按"向上回溯连续 <c>//</c> 行"切块时，
    /// 特性与接口签名之间不是注释行（是 <c>Source = …)]</c>），回溯停在签名上，于是每个
    /// <c>[AgentTool]</c> + XML 注释被切给<b>前一个</b>接口，接口与其特性落入不同文件。
    /// </para>
    /// <para>
    /// <b>后果是静默的</b>：特性缺失不是编译错误（生成器只是少出一个工具、少一条 Schema），
    /// golden 与规模守卫按<b>工具名集合</b>计数，丢的是"整个工具"而不是"某个字段"，
    /// 只有本守卫这类<b>同文件内 1:1 计数</b>才能在切块阶段立刻报红。
    /// </para>
    /// </remarks>
    [Fact]
    public void CurationFiles_ShouldKeepEachInterfaceWithItsAttribute()
    {
        var curation = Path.Combine(FindRepositoryRoot(), "Mud.Feishu.AI.Tools", "Curation");

        var offenders = Directory
            .EnumerateFiles(curation, "*.cs", SearchOption.AllDirectories)
            .Select(path => new
            {
                Rel = Path.GetRelativePath(curation, path),
                Text = File.ReadAllText(path),
            })
            .Select(static x => new
            {
                x.Rel,
                Interfaces = Regex.Matches(x.Text, @"\bpublic\s+interface\s+I\w+Tool\b").Count,
                // v2.3 P1-E：声明特性硬改名 [FeishuTool] → [AgentTool]（锚点随改名同批改）。
                Attributes = Regex.Matches(x.Text, @"\[AgentTool\s*\(").Count,
            })
            .Where(static x => x.Interfaces != x.Attributes || x.Interfaces == 0)
            .Select(static x => $"{x.Rel} (interface={x.Interfaces}, [AgentTool]={x.Attributes})")
            .ToArray();

        offenders.Should().BeEmpty(
            "每个声明文件里 [AgentTool] 特性数必须与 public interface 数相等且不为 0——不相等即意味着"
            + "某个工具的特性与声明被切到了不同文件（生成器静默少出工具，编译仍通过）：{0}",
            string.Join(" | ", offenders));
    }

    /// <summary>
    /// <b>R5 / F-1</b>：基线 + <b>反向自证</b> —— <c>Curation/</c> 恰有 41 个声明文件、246 个契约接口
    /// （R7/C3 后 163，R-12 删除 <c>feishu.api_call</c> 后为 162；六组目录切分后各域补域累计至 242，
    /// T-1 读取闭环补 4 个至 246）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>D-1（2026-10-12）修订</b>：声明面从「27 文件平铺」改为「六组目录 + 域声明文件」——
    /// 4 个<b>跨域混装</b>文件按域拆分（<c>FeishuToolInterfaces.cs</c> 一个文件里混了
    /// Bitable/Docx/Wiki/Search/IM/Sheets 六域；<c>FeishuWriteToolInterfaces.cs</c> 混了
    /// IM 写与 Approval；<c>FeishuDocxSheetsDriveToolInterfaces.cs</c> 混了 Docx/Sheets/Drive 写；
    /// <c>FeishuMailToolInterfaces.cs</c> 混了 Mail/Contact 部门轴/Approval 实例详情），
    /// 归位为「组目录 = 关注域、组内一域一文件」；补域后现为 41 文件 / 246 接口。
    /// </para>
    /// <para>
    /// <b>身份改用相对路径</b>：文件名相同、组不同的情形（<c>Docs/…</c> vs <c>Collab/…</c>）
    /// 用文件名做身份会让"文件跨组挪动"这类结构变更对基线不可见。
    /// </para>
    /// <para>
    /// <b>组目录白名单 + 恰好一层</b>：顶层平铺（旧态）与更深嵌套都被机械拦截——六组结构本身
    /// 是被守卫锁住的不变量，而不是一条口头约定。
    /// </para>
    /// </remarks>
    [Fact]
    public void CurationDirectory_ShouldMatchRegisteredBaseline()
    {
        var curation = Path.Combine(FindRepositoryRoot(), "Mud.Feishu.AI.Tools", "Curation");

        var files = Directory
            .EnumerateFiles(curation, "*.cs", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(curation, path))
            .OrderBy(static path => path, StringComparer.Ordinal)
            .ToArray();

        files.Should().HaveCount(
            41,
            "Curation/ 的声明文件数从 27 变为 {0}：{1}。新增/拆分域声明文件属**有意的契约变更**，"
            + "请确认新文件只含工具契约并更新本基线",
            files.Length,
            string.Join(" | ", files));

        // 结构不变量：Curation/<Group>/<File>.cs —— 恰好一层组目录。
        var wrongDepth = files
            .Where(static path => path.Split(Path.DirectorySeparatorChar).Length != 2)
            .ToArray();

        wrongDepth.Should().BeEmpty(
            "Curation/ 的声明文件必须恰好位于一层组目录内（Curation/<Group>/<File>.cs）——"
            + "顶层平铺（旧态 27 文件散列）或更深嵌套都会让「按域定位」失效：{0}",
            string.Join(" | ", wrongDepth));

        var allowedGroups = new[] { "Collab", "Core", "Docs", "Im", "People", "Platform" };
        var unknownGroups = files
            .Select(static path => path.Split(Path.DirectorySeparatorChar)[0])
            .Distinct(StringComparer.Ordinal)
            .Where(group => !allowedGroups.Contains(group, StringComparer.Ordinal))
            .OrderBy(static group => group, StringComparer.Ordinal)
            .ToArray();

        unknownGroups.Should().BeEmpty(
            "Curation/ 只允许六个关注域组目录（{0}）——新增组属有意的结构变更，"
            + "请同步更新本白名单与该守卫的说明：{1}",
            string.Join("/", allowedGroups),
            string.Join(" | ", unknownGroups));

        var interfaces = files
            .SelectMany(path => Regex.Matches(
                File.ReadAllText(Path.Combine(curation, path)),
                @"\bpublic\s+interface\s+(?<name>I\w+Tool)\b")
                .Cast<System.Text.RegularExpressions.Match>()
                .Select(static m => m.Groups["name"].Value))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        interfaces.Should().HaveCount(
            331,
            "Curation/ 的工具契约接口数从 162 变为 {0}——新增/删除工具属有意的契约变更，请同步更新本基线"
            + "（若同时看到『文件数没变而接口数变了』，说明有文件被塞进了非契约内容）",
            interfaces.Length);
    }

    private static string FindRepositoryRoot()
    {
        var directory = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(directory) && !File.Exists(Path.Combine(directory, "Mud.Feishu.slnx")))
        {
            directory = Path.GetDirectoryName(directory)!;
        }

        directory.Should().NotBeNullOrEmpty("测试必须能定位仓库根目录（以 Mud.Feishu.slnx 为锚）");
        return directory!;
    }
}
