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
        // Args/Results/Errors/Execution/Catalog/Hosting/Security/Parsers/Constants 九目录，
        // Tools/ 仅余生成器钉死的根命名空间类型（FeishuToolBinding）。本守卫的"认知边界"
        // 必须覆盖全部源码目录——漏掉任一目录即假绿（该目录新出现的形态认知无人拦截）。
        var scanDirs = new[]
        {
            "Args", "Results", "Errors", "Execution", "Catalog", "Hosting",
            "Security", "Parsers", "Constants", "Tools", "Curation",
        }.Select(d => Path.Combine(projDir, d));

        // 白名单（R-2 已收缩为**单文件**）：形态判定原语（IsJsonString/IsJsonArray）住在
        // ToolArgumentNormalizer 内，其余位置（ToolArgs 取值门面、ToolArgsDigester 摘要、
        // ToolPagination/ToolDryRun 取值助手）只能薄委托——"同一入参在不同工具里被判成不同形态"
        // 是 S1 的同类根因，收敛后该形态在源码层不可能再出现第二处。
        var allowedFiles = new HashSet<string>(StringComparer.Ordinal)
        {
            "ToolArgumentNormalizer.cs", // 唯一形态认知中心（R-2 起，含判定原语）
        };

        var offenders = scanDirs
            .SelectMany(static dir => Directory.EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories))
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(p => !allowedFiles.Contains(Path.GetFileName(p)))
            .Where(p => File.ReadAllText(p).Contains("JsonValueKind.String", StringComparison.Ordinal))
            .Select(Path.GetFileName)
            .ToList();

        offenders.Should().BeEmpty(
            "JsonValueKind.String 的形态认知只允许出现在 ToolArgumentNormalizer（唯一认知中心）；"
            + "取值/摘要位置必须薄委托 IsJsonString/IsJsonArray——各自 switch 是 S1 的根因"
            + "（漏一处即静默失效）——违规文件: " + string.Join(", ", offenders));
    }

    // ────────── R5 / F-1（载体 C）：策展面与基础设施的分离 ──────────

    /// <summary>
    /// <b>R5 / F-1（载体 C）</b>：<b>策展面与基础设施的分离守卫</b>（断言一 + 断言二）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>不变量</b>：人工<b>策展</b>的东西（<c>[FeishuTool]</c>/<c>[ToolParameter]</c> 契约，
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

        var mixedIn = Directory
            .EnumerateFiles(curation, "*.cs", SearchOption.TopDirectoryOnly)
            .Where(static path => !Path.GetFileName(path).EndsWith("ToolInterfaces.cs", StringComparison.Ordinal))
            .Select(static path => Path.GetFileName(path))
            .ToArray();

        mixedIn.Should().BeEmpty(
            "Curation/ 只应收纳工具契约声明（*ToolInterfaces.cs）；混入其他文件会让"
            + "人工策展面与框架基础设施的边界失焦：{0}",
            string.Join(" | ", mixedIn));

        // 工具契约声明不得落在任何运行时目录（原守卫只查 Tools/；重组后运行时面扩为九目录，
        // 不扩展即假绿——契约写进 Args/ 无人拦截）。
        var contractFiles = new[]
        {
            "Args", "Results", "Errors", "Execution", "Catalog", "Hosting",
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
    /// <b>R-1+2c 重组守卫</b>：九 个关注面目录与 <c>Tools/</c>（白名单目录）必须声明与目录
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
            ["Errors"] = "Mud.Feishu.AI.Tools.Errors",
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
            .EnumerateFiles(curation, "*.cs", SearchOption.TopDirectoryOnly)
            .Where(path =>
            {
                var match = Regex.Match(
                    File.ReadAllText(path), @"^namespace\s+(?<ns>[\w.]+)", RegexOptions.Multiline);
                return !match.Success || match.Groups["ns"].Value != Expected;
            })
            .Select(static path => Path.GetFileName(path))
            .ToArray();

        offenders.Should().BeEmpty(
            "Curation/ 下的文件必须声明 namespace {0}：{1}", Expected, string.Join(" | ", offenders));
    }

    /// <summary>
    /// <b>R5 / F-1</b>：基线 + <b>反向自证</b> —— <c>Curation/</c> 恰有 21 个声明文件、162 个契约接口
    /// （R7/C3 后 163，R-12 删除 <c>feishu.api_call</c> 后为 162）。
    /// </summary>
    [Fact]
    public void CurationDirectory_ShouldMatchRegisteredBaseline()
    {
        var curation = Path.Combine(FindRepositoryRoot(), "Mud.Feishu.AI.Tools", "Curation");

        var files = Directory
            .EnumerateFiles(curation, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(static path => Path.GetFileName(path))
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();

        files.Should().HaveCount(
            27,
            "Curation/ 的声明文件数从 16 变为 {0}：{1}。新增/拆分域声明文件属**有意的契约变更**，"
            + "请确认新文件只含工具契约并更新本基线",
            files.Length,
            string.Join(" | ", files));

        var interfaces = files
            .SelectMany(path => Regex.Matches(
                File.ReadAllText(Path.Combine(curation, path)),
                @"\bpublic\s+interface\s+(?<name>I\w+Tool)\b")
                .Cast<System.Text.RegularExpressions.Match>()
                .Select(static m => m.Groups["name"].Value))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        interfaces.Should().HaveCount(
            182,
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
