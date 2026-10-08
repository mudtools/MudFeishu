// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.RegularExpressions;

namespace Mud.Feishu.AI.FeishuTools.Tests.ContractGuards;

/// <summary>
/// <b>R5 / B-2</b>：AI 工具面的<b>二进制防线</b> —— 断言<b>没有任何工具能把二进制内容送进 JSON 结果</b>。
/// </summary>
/// <remarks>
/// <para>
/// <b>架构事实（决定了防线该建在哪）</b>：本仓的工具体面是<b>两层、且目录分离</b>的——
/// <list type="bullet">
/// <item><b>工具接口</b>（<c>Mud.Feishu.AI.FeishuTools/Tools/Feishu*ToolInterfaces.cs</c>）声明
/// <c>[FeishuTool]</c> 方法，返回 JSON 可序列化类型；</item>
/// <item><b>SDK 接口</b>（<c>Mud.Feishu/Interfaces/**</c>）提供底层能力，<b>从不</b>携带
/// <c>[FeishuTool]</c>（实测：含二进制方法的 13 个接口，工具计数全为 0）。</item>
/// </list>
/// 两层靠 <c>[FeishuTool(Source = …)]</c> 交叉引用（R-1 起为
/// <c>nameof(接口) + "." + nameof(接口.方法)</c> 常量拼接，编译期求值结果与旧字面量逐字节相同）。
/// </para>
/// <para>
/// <b>所以真正的暴露向量只有一个</b>：<b><c>Source</c> 指向了返回二进制的 SDK 方法</b>。
/// 此时工具方法声明的返回类型"看起来是正常 JSON"，但执行器会把 SDK 的
/// <c>byte[]</c> 结果回填进 <c>result.Content</c> ⇒ 撑爆上下文并击穿 SSE 帧。
/// 这就是本守卫锁定的<b>唯一且真实</b>的失效路径。
/// </para>
/// <para>
/// <b>为什么不是"统一 N 处注释"</b>（R5 对原文方案的修订）：原文修法是"统一 15 处
/// <c>Task&lt;byte[]?&gt;</c> 的注释口径"。实测为 <b>16 个方法 / 13 个接口</b>，且注释
/// <b>不改变任何行为</b>；真正的风险是"某天有人把下载方法挂上工具、或写进 <c>Source</c>"，
/// 那一刻<b>没有任何机制会拦住</b>。⇒ 把人工不变量升级为机械约束，成本更低、覆盖更全。
/// </para>
/// <para>
/// <b>三条断言</b>：① 工具的 <c>Source</c> 不得指向二进制返回方法（核心）；② 工具方法返回类型
/// 不得是二进制（第二道，覆盖未走 <c>Source</c> 的手写实现）；③ 反向自证——扫描器必须真的
/// 看得见工具与二进制方法，否则"扫不到 = 假绿"。
/// </para>
/// </remarks>
public class BinaryDownloadToolExposureContractTests
{
    // R5 / F-1：工具声明面已从 Tools/ 迁到 Curation/（载体 C：策展面与基础设施物理分离）。
    private const string ToolInterfacesDirectory = "Mud.Feishu.AI.FeishuTools/Curation";
    private const string SdkInterfacesDirectory = "Mud.Feishu/Interfaces";

    /// <summary>二进制返回类型的<b>正则形态</b>（与 <c>Mud.HttpUtils</c> 的下载分支判定同源）。</summary>
    private const string BinaryReturnPattern =
        @"Task\s*<\s*(?:byte\s*\[\s*\]\s*\?|Stream|ReadOnlyMemory\s*<\s*byte\s*>)\s*>";

    /// <summary>
    /// "二进制返回方法"的完整形态：<b>返回类型紧跟方法名</b>（如
    /// <c>Task&lt;byte[]?&gt; DownloadFileAsync(</c>）。
    /// </summary>
    /// <remarks>
    /// <b>为什么必须前向匹配而不是"从返回类型往前回溯"</b>：返回类型上方常压着
    /// <c>[Get("/open-apis/…/{file_token}/download")]</c> 等带括号的特性，前向回溯会命中特性名
    /// （实测<b>一个都匹配不到</b>，防线静默失效）。方法名恒在返回类型<b>之后</b>，故只前向。
    /// </remarks>
    private const string BinaryMethodPattern =
        BinaryReturnPattern + @"\s+(?<name>\w+)\s*\(";

    /// <summary>
    /// <b>核心断言</b>：<c>[FeishuTool]</c> 的 <c>Source</c> 不得指向返回二进制的 SDK 方法。
    /// </summary>
    [Fact]
    public void ToolSources_ShouldNeverPointToBinaryReturningSdkMethods()
    {
        var binaryMethods = CollectBinarySdkMethods();
        binaryMethods.Should().NotBeEmpty(
            "未解析到任何二进制返回的 SDK 方法——防线无从校验（扫描失效），请先修守卫");

        var binaryNames = binaryMethods
            .Select(static entry => entry.Split('|')[0])
            .ToHashSet(StringComparer.Ordinal);

        // R5 / F-10：<b>挣得的豁免</b>。`drive.download_file` 是受控下载出口——
        // 二进制由宿主 stager 落盘，工具结果只含本地路径/大小/Content-Type。
        // 它必须**显式声明**豁免（Description 里含 no-bytes-in-context），否则本守卫照旧报红。
        // 豁免是 opt-in 的：新工具若指向二进制方法而不声明，一律拦下。
        //
        // ⚠️ **声明本身不足以豁免**（R5 / S-24）：`no-bytes-in-context` 只是一个字符串，
        //    若仅凭它放行，任何人都能靠**改一行描述**关掉本零容忍守卫，而工具实际仍可能把字节
        //    回填进结果。故放行的前提是**核验已挣得**：该工具的执行器必须
        //    ① 注入 `IFeishuAttachmentStager`（字节交给宿主落盘边界，而非回填结果）；
        //    ② 调用 `DownloadedContentGuard.ShouldRejectForJsonErrorBody`（错误体不得落盘）。
        //    核验不通过的"豁免"会被当作违规报出（见下方 VerifyExemptions）。
        //
        //    历史：本机制此前**被收集却从未被使用**（`binarySafeTools` 只在第 84 行赋值），
        //    等于"看着存在但完全不生效"——F-10 因此长期处于回滚状态。
        var binarySafeTools = CollectBinarySafeTools();

        var violations = new List<string>();
        foreach (var (toolFile, toolName, source) in CollectToolSourceReferences())
        {
            // Source 形如 "IFeishuTenantV1DriveFiles.BatchQueryMetasAsync"（也允许带命名空间前缀）。
            var referenced = source.Split('.').Last();
            if (binaryNames.Contains(referenced) && !binarySafeTools.Contains(toolName))
            {
                violations.Add($"{toolFile}::{toolName} → Source=\"{source}\"");
            }
        }

        // 豁免必须"挣得"：声明了标记但执行器未落实两道防线的，按违规报出。
        violations.AddRange(VerifyExemptions());

        violations.Should().BeEmpty(
            "以下工具的 [FeishuTool(Source=…)] 指向了返回二进制的 SDK 方法 ⇒ 二进制会被回填进工具 JSON 结果"
            + "（撑爆上下文并击穿 SSE）。修法：改用返回临时 URL 的能力（如 GetDownloadUrlAsync），"
            + "或把下载能力移出工具面：{0}",
            string.Join(" | ", violations));
    }

    /// <summary>
    /// 工具方法自身的返回类型不得是二进制（第二道防线，覆盖未走 <c>Source</c> 的手写实现）。
    /// </summary>
    [Fact]
    public void ToolMethods_ShouldNeverDeclareBinaryReturnType()
    {
        var violations = new List<string>();

        foreach (var file in EnumerateFiles(ToolInterfacesDirectory))
        {
            foreach (var (toolName, block) in SplitToolMethodBlocks(ReadSourceWithoutComments(file)))
            {
                if (Regex.IsMatch(block, BinaryReturnPattern))
                {
                    violations.Add($"{Path.GetFileName(file)}::{toolName}");
                }
            }
        }

        violations.Should().BeEmpty(
            "以下 [FeishuTool] 方法声明了二进制返回类型——工具结果必须是可序列化 JSON：{0}",
            string.Join(" | ", violations));
    }

    /// <summary>
    /// 反向自证：扫描器必须<b>同时</b>看得见工具 <c>Source</c> 与二进制 SDK 方法。
    /// 本仓已吃过一次"扫描失效 = 假绿"的亏（见 <c>CapabilityEqualityFieldCoverageTests</c> 的表达式体教训）。
    /// </summary>
    [Fact]
    public void Scanner_ShouldSeeBothToolSourcesAndBinaryMethods_OtherwiseGuardsAreFalseGreen()
    {
        CollectToolSourceReferences().Should().NotBeEmpty(
            $"未扫到任何 [FeishuTool(Source=…)]（路径 {ToolInterfacesDirectory} 是否已失效？）");

        CollectBinarySdkMethods().Should().NotBeEmpty(
            $"未扫到任何二进制返回的 SDK 方法（路径 {SdkInterfacesDirectory} 是否已失效？）");
    }

    /// <summary>
    /// <b>R-1+2c 反向自证</b>：扫描器必须读得懂 <c>Source</c> 的<b>现形态</b>
    /// （<c>nameof(接口) + "." + nameof(接口.方法)</c> 常量拼接），且归一结果与旧字面量形态同构。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 为什么必须单列这条：R-1 把 84 处 <c>Source</c> 从字面量改成 <c>nameof</c> 拼接后，
    /// 若扫描器只认 <c>Source = "…"</c>，核心断言（<see cref="ToolSources_ShouldNeverPointToBinaryReturningSdkMethods"/>）
    /// 会退化为"一个 Source 都扫不到" ⇒ <b>防线静默失效</b>（这正是"永远为绿的守卫"形态）。
    /// </para>
    /// <para>
    /// 判据用精确基线 84（与 <c>ToolSourceConsistencyContractTests</c> 同源），
    /// 且要求每个 Source 都含 <c>.</c>（证明接口名没有被 <c>nameof</c> 的"末段标识符"语义吃掉）。
    /// </para>
    /// </remarks>
    [Fact]
    public void Scanner_ShouldReadTheNameofConcatenationForm_AndNormalizeToInterfaceDotMethod()
    {
        var references = CollectToolSourceReferences().ToArray();

        references.Should().HaveCount(
            84,
            "84 个工具声明了 Source（另 3 个元工具无 SDK 源）——骤降说明扫描器读不懂现形态（R-1 的 nameof 拼接）");

        references.Should().OnlyContain(
            static reference => reference.Source.Contains('.', StringComparison.Ordinal)
                && reference.Source.IndexOf('.') > 0
                && reference.Source.IndexOf('.') < reference.Source.Length - 1,
            "每个 Source 必须归一为「接口名.方法名」——单段 nameof(接口.方法) 只产出方法名，会被判为无接口");
    }

    /// <summary>
    /// 基线锁定：二进制方法数 / 接口数都是<b>有意登记的存量</b>，变动时必须显式更新期望值——
    /// 避免"新增下载方法"这类有意的接口面扩张静默发生。
    /// </summary>
    [Fact]
    public void BinarySdkMethods_ShouldMatchRegisteredBaseline()
    {
        // 2026-10-06 实测（R5 / B-2）：16 个方法 / 13 个接口。
        const int ExpectedMethodCount = 16;
        const int ExpectedInterfaceCount = 13;

        var methodCount = CollectBinarySdkMethods().Count;
        var interfaceCount = EnumerateFiles(SdkInterfacesDirectory)
            .Count(path => Regex.IsMatch(File.ReadAllText(path), BinaryReturnPattern));

        methodCount.Should().Be(
            ExpectedMethodCount,
            $"二进制返回方法数从 {ExpectedMethodCount} 变为 {methodCount}——"
            + "新增/删除下载方法属有意的接口面扩张，请同步更新本基线与 B-2 文档");

        interfaceCount.Should().Be(
            ExpectedInterfaceCount,
            $"含二进制方法的接口数从 {ExpectedInterfaceCount} 变为 {interfaceCount}——请同步更新本基线");
    }

    // ────────── 采集实现 ──────────

    /// <summary>
    /// 采集 SDK 侧全部二进制返回方法，返回 "方法名|所在文件名" 集合（带文件名以消解同名歧义）。
    /// </summary>
    private static HashSet<string> CollectBinarySdkMethods()
    {
        var names = new HashSet<string>(StringComparer.Ordinal);

        foreach (var path in EnumerateFiles(SdkInterfacesDirectory))
        {
            var source = File.ReadAllText(path);
            foreach (System.Text.RegularExpressions.Match match in Regex.Matches(source, BinaryMethodPattern))
            {
                names.Add($"{match.Groups["name"].Value}|{Path.GetFileName(path)}");
            }
        }

        return names;
    }

    /// <summary>
    /// 采集工具侧全部 <c>[FeishuTool]</c> 的工具名与 <c>Source</c> 值（归一到 <c>"接口.方法"</c> 形态）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>必须支持两种声明形态</b>（R-1 迁移后只剩后一种，但扫描器要能同时读——否则形态回退时
    /// 扫描器会静默扫不到任何 Source）：
    /// </para>
    /// <list type="bullet">
    /// <item>旧字面量：<c>Source = "IFeishuTenantV1X.MethodAsync"</c>；</item>
    /// <item>现形态（常量拼接）：<c>Source = nameof(IFeishuTenantV1X) + "." + nameof(IFeishuTenantV1X.MethodAsync)</c>。
    /// 注意单段 <c>nameof(接口.方法)</c> 只产出方法名（丢接口名），故形态必须是两段拼接。</item>
    /// </list>
    /// </remarks>
    private static IEnumerable<(string ToolFile, string ToolName, string Source)> CollectToolSourceReferences()
    {
        foreach (var path in EnumerateFiles(ToolInterfacesDirectory))
        {
            var fileName = Path.GetFileName(path);
            foreach (var (toolName, block) in SplitToolMethodBlocks(ReadSourceWithoutComments(path)))
            {
                var literal = Regex.Match(block, @"Source\s*=\s*""(?<value>[^""]+)""");
                if (literal.Success)
                {
                    yield return (fileName, toolName, literal.Groups["value"].Value);
                    continue;
                }

                var nameofForm = Regex.Match(
                    block,
                    @"Source\s*=\s*nameof\(\s*(?<type>[A-Za-z0-9_.]+)\s*\)\s*\+\s*""\.""\s*\+\s*nameof\(\s*(?<member>[A-Za-z0-9_.]+)\s*\)");
                if (nameofForm.Success)
                {
                    var type = nameofForm.Groups["type"].Value.Split('.').Last();
                    var method = nameofForm.Groups["member"].Value.Split('.').Last();
                    yield return (fileName, toolName, type + "." + method);
                }
            }
        }
    }

    /// <summary>
    /// 读取源文件并<b>丢弃整行注释</b>（行首为 <c>//</c> / <c>///</c>）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>为什么必须丢</b>：本守卫按 <c>[FeishuTool(</c> 切块，而块边界是"到下一个 <c>[FeishuTool(</c>"，
    /// 因此<b>两个工具之间的注释会落进前一个工具的块里</b>。于是注释里只要提到被禁的形态
    /// （标记名、<c>Task&lt;byte[]?&gt;</c>），前一个工具就会被<b>误判</b>。
    /// 实测撞到过两次：一次是说明注释提到豁免标记 ⇒ 前一个工具被当成"未挣得的豁免"；
    /// 一次是注释里写了 <c>Task&lt;byte[]?&gt;</c> ⇒ 前一个工具被当成"声明了二进制返回类型"。
    /// </para>
    /// <para>
    /// <b>为什么只丢"整行"注释</b>：行内 <c>//</c> 可能出现在字符串字面量里（如描述中的
    /// <c>https://…</c> 链接），按 <c>//</c> 粗暴截断会把链接后半段（甚至其后的标记）一并切掉。
    /// 只处理行首即注释的行，既能消灭上述误判，又不会碰字符串内容；即便漏判，
    /// 方向也是"豁免不成立 ⇒ 报违规"，属 fail-safe。
    /// </para>
    /// </remarks>
    private static string ReadSourceWithoutComments(string path)
        => StripFullLineComments(File.ReadAllText(path));

    /// <summary>同一判定逻辑的可测重载（单测须测<b>实现本身</b>，而不是它的副本）。</summary>
    internal static string StripFullLineComments(string source)
        => string.Join(
            "\n",
            source.Split('\n').Where(static line =>
                !line.TrimStart().StartsWith("//", StringComparison.Ordinal)));

    private static IEnumerable<string> EnumerateFiles(string relativeDirectory)
    {
        var root = Path.Combine(FindRepositoryRoot(), relativeDirectory.Replace('/', Path.DirectorySeparatorChar));
        if (!Directory.Exists(root))
        {
            yield break;
        }

        foreach (var path in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            if (path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            yield return path;
        }
    }

    /// <summary>
    /// 把源码按 <c>[FeishuTool(</c> 切块，每块含"特性 + 其后方法签名"，返回（方法名, 块文本）。
    /// </summary>
    private static IEnumerable<(string ToolName, string Block)> SplitToolMethodBlocks(string source)
    {
        var starts = Regex.Matches(source, @"\[FeishuTool\s*\(");
        for (var i = 0; i < starts.Count; i++)
        {
            var from = starts[i].Index;
            var to = i + 1 < starts.Count ? starts[i + 1].Index : source.Length;
            var block = source[from..to];

            var name = Regex.Match(block, @"\b(?<name>\w+)\s*\(");
            yield return (name.Success ? name.Groups["name"].Value : "?", block);
        }
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
    /// <summary>
    /// 收集<b>显式声明</b>了「字节不进上下文」豁免的工具名。
    /// </summary>
    /// <remarks>
    /// 判据：<c>[FeishuTool("name", … Description = "…no-bytes-in-context…")]</c>。
    /// 用「声明块内是否出现该标记」而非「文件内是否出现」，避免一个文件里多个工具时互相冒名顶替。
    /// </remarks>
    /// <summary>
    /// <b>R5 / S-24</b>：核验每一处 <c>no-bytes-in-context</c> 豁免是否<b>已挣得</b>。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 判据放在<b>实现</b>而非<b>声明</b>上：光在描述里写一句标记不构成豁免资格，
    /// 还必须能在执行器源码里看到两道防线真的落地：
    /// ① 执行器类注入 <c>IFeishuAttachmentStager</c>（<b>非可空</b> ⇒ 与既有上传工具同款软缺席语义：
    ///    宿主没配落盘器时该工具<b>不进注册表</b>，而不是运行期才失败）；
    /// ② 绑定方法体内调用 <c>ShouldRejectForJsonErrorBody</c>（B-2 的错误体防线）。
    /// </para>
    /// <para>
    /// <b>核到"文件级 + 类级"的粒度</b>：绑定与构造写在同一个执行器类里，
    /// 故按「包含该绑定的 <c>internal sealed class</c> 段」判定 —— 比全文件匹配更紧，
    /// 又不必解析语法树（本仓契约守卫一贯用源码扫描，避免引入 Roslyn 依赖）。
    /// </para>
    /// </remarks>
    private static IReadOnlyList<string> VerifyExemptions()
    {
        var root = FindRepositoryRoot();
        var curation = Directory
            .GetFiles(Path.Combine(root, "Mud.Feishu.AI.FeishuTools", "Curation"), "*.cs", SearchOption.AllDirectories)
            .Select(static path => (Path.GetFileName(path), ReadSourceWithoutComments(path)))
            .ToArray();
        var executors = Directory
            .GetFiles(Path.Combine(root, "Mud.Feishu.AI.FeishuTools", "Internal"), "*.cs", SearchOption.AllDirectories)
            .Select(static path => (Path.GetFileName(path), ReadSourceWithoutComments(path)))
            .ToArray();

        return FindUnearnedExemptions(curation, executors);
    }

    /// <summary>
    /// 核验内核（<b>纯函数，便于单测反向自证</b>）：返回"未挣得豁免"的原因列表。
    /// </summary>
    /// <param name="curationFiles">声明面文件（文件名, 源码）。</param>
    /// <param name="executorFiles">执行器文件（文件名, 源码）。</param>
    internal static IReadOnlyList<string> FindUnearnedExemptions(
        IEnumerable<(string FileName, string Source)> curationFiles,
        IEnumerable<(string FileName, string Source)> executorFiles)
    {
        const string marker = "no-bytes-in-context";
        var reasons = new List<string>();
        var executorList = executorFiles.ToArray();

        foreach (var (fileName, source) in curationFiles)
        {
            // 按 [FeishuTool( 分块：块 = 该工具的属性块 + 其后的接口声明。
            var starts = Regex.Matches(source, @"\[FeishuTool\s*\(");
            for (var i = 0; i < starts.Count; i++)
            {
                var from = starts[i].Index;
                var to = i + 1 < starts.Count ? starts[i + 1].Index : source.Length;
                var block = source[from..to];

                // ⚠️ 标记只在**属性块内**（`[FeishuTool(…)]` 到配对的 `)]`）才算数。
                //    若按"到下一个 [FeishuTool( 为止"判定，则**两个工具之间的注释**会被算进
                //    前一个工具的块里 —— 实测撞到过：在新工具的说明注释里提到标记，
                //    结果前一个工具被误判为"声明了豁免却未挣得"。属性块边界是唯一的判据。
                var attributeEnd = block.IndexOf(")]", StringComparison.Ordinal);
                var attributeBlock = attributeEnd > 0 ? block[..attributeEnd] : block;

                if (!attributeBlock.Contains(marker, StringComparison.Ordinal))
                {
                    continue;
                }

                var toolName = Regex.Match(block, @"\[FeishuTool\s*\(\s*""(?<name>[^""]+)""").Groups["name"].Value;
                var interfaceName = Regex.Match(block, @"public\s+interface\s+(?<name>\w+)").Groups["name"].Value;

                if (toolName.Length == 0 || interfaceName.Length == 0)
                {
                    reasons.Add($"{fileName}: 声明了 {marker} 但无法解析工具名/接口名（工具名='{toolName}'）");
                    continue;
                }

                var binding = $"FeishuToolHandler(typeof({interfaceName}))";
                var host = executorList.FirstOrDefault(entry => entry.Source.Contains(binding, StringComparison.Ordinal));
                if (host.Source is null)
                {
                    reasons.Add(
                        $"'{toolName}': 声明了 {marker} 豁免，但全仓找不到绑定该接口（{interfaceName}）的执行器 —— "
                        + "没有任何实现可核验，豁免不成立");
                    continue;
                }

                var classSegment = ExtractContainingClass(host.Source, binding);

                // ① 注入落盘器（非可空 ⇒ 软缺席语义）。
                if (!Regex.IsMatch(classSegment, @"(?<!\?)IFeishuAttachmentStager\s+\w+\s*[,)]"))
                {
                    reasons.Add(
                        $"'{toolName}'（{host.FileName}）: 声明豁免，但执行器未注入**非可空**的 IFeishuAttachmentStager —— "
                        + "无法保证字节交给宿主落盘边界（且软缺席语义缺失：宿主未配落盘器时该工具应不进注册表）");
                }

                // ② 调用错误体防线（B-2 的 DoD：错误 JSON 不得落盘）。
                if (!host.Source.Contains("ShouldRejectForJsonErrorBody", StringComparison.Ordinal))
                {
                    reasons.Add(
                        $"'{toolName}'（{host.FileName}）: 声明豁免，但执行器未调用 "
                        + "DownloadedContentGuard.ShouldRejectForJsonErrorBody —— 平台错误体会被当文件落盘");
                }
            }
        }

        return reasons;
    }

    /// <summary>取出包含给定锚点的 <c>internal sealed class</c> 段（到下一个类声明为止）。</summary>
    private static string ExtractContainingClass(string source, string anchor)
    {
        var anchorIndex = source.IndexOf(anchor, StringComparison.Ordinal);
        if (anchorIndex < 0)
        {
            return string.Empty;
        }

        var lastClass = source.LastIndexOf("internal sealed class", anchorIndex, StringComparison.Ordinal);
        if (lastClass < 0)
        {
            return source[..anchorIndex];
        }

        var nextClass = source.IndexOf("internal sealed class", anchorIndex, StringComparison.Ordinal);
        return nextClass < 0 ? source[lastClass..] : source[lastClass..nextClass];
    }

    /// <summary>
    /// <b>核验器自证</b>：用合成源码证明它<b>会失败</b>——否则"豁免全部合规"可能只是核验恒真。
    /// </summary>
    [Fact]
    public void BinarySafeExemption_ShouldDetectUnearnedClaim()
    {
        const string curation = """
            [FeishuTool("fake.download",
                Description = "受控下载出口（no-bytes-in-context）。")]
            public interface IFakeDownloadTool
            {
                Task<string> DownloadAsync();
            }
            """;

        // ① 只有声明、执行器什么都没做 ⇒ 必须报出（这正是"改一行描述即可关掉守卫"的路径）。
        var nothingDone = FindUnearnedExemptions(
            [("Curation.cs", curation)],
            [("Internal.cs", "[FeishuToolHandler(typeof(IFakeDownloadTool))] public Task<X> DownloadAsync() { }")]);

        nothingDone.Should().HaveCount(2,
            "既未注入 stager、也未调用错误体防线 ⇒ 两条理由都要报出，且**不得**放过");

        // ② 只注入了落盘器、没做错误体自检 ⇒ 仍必须报出（少一道防线即不合格）。
        var noGuardCall = FindUnearnedExemptions(
            [("Curation.cs", curation)],
            [("Internal.cs", """
                internal sealed class FakeTools(IFeishuAttachmentStager stager)
                {
                    [FeishuToolHandler(typeof(IFakeDownloadTool))]
                    public Task<X> DownloadAsync() => throw new System.NotSupportedException();
                }
                """)]);

        noGuardCall.Should().ContainSingle(
            reason => reason.Contains("ShouldRejectForJsonErrorBody", StringComparison.Ordinal),
            "注入了落盘器但缺错误体自检 ⇒ 必须仍被拦下");

        // ③ 两道防线齐备 ⇒ 放行（否则豁免机制形同不存在，F-10 永远落不了地）。
        var earned = FindUnearnedExemptions(
            [("Curation.cs", curation)],
            [("Internal.cs", """
                internal sealed class FakeTools(IFeishuAttachmentStager stager)
                {
                    [FeishuToolHandler(typeof(IFakeDownloadTool))]
                    public Task<X> DownloadAsync()
                    {
                        _ = DownloadedContentGuard.ShouldRejectForJsonErrorBody(default, null, out _);
                        throw new System.NotSupportedException();
                    }
                }
                """)]);

        earned.Should().BeEmpty("两道防线齐备的豁免必须放行");
    }

    /// <summary>
    /// <b>注释不得参与判定</b>：两个工具之间的注释会落进前一个工具的块里，
    /// 若注释里的"违禁形态"字面量被当真，前一个工具会被误判。
    /// </summary>
    /// <remarks>
    /// 实测撞到过两次（同一根因）：① 说明注释提到豁免标记 ⇒ 前一个工具被当成"未挣得的豁免"；
    /// ② 注释里写了 <c>Task&lt;byte[]?&gt;</c> ⇒ 前一个工具被当成"声明了二进制返回类型"。
    /// 本用例把"注释被忽略"变成机械断言，避免第三人再踩。
    /// </remarks>
    [Fact]
    public void Comments_ShouldNotParticipateInScanning()
    {
        // 第一块是正常工具；紧随其后的**整行注释**里刻意写出两处违禁形态字面量。
        const string source = """
            [FeishuTool("fake.normal", Description = "普通工具。")]
            public interface IFakeNormalTool
            {
                Task<string> DoAsync();
            }

            // 下面这段注释提到 no-bytes-in-context 标记，并写出 Task<byte[]?> 形态——
            // 它们必须被扫描忽略，否则上面的 fake.normal 会被误判。
            [FeishuTool("fake.next", Description = "下一个工具。")]
            public interface IFakeNextTool
            {
                Task<string> DoAsync();
            }
            """;

        // 断言**实现本身**（不是逻辑副本）。
        var kept = StripFullLineComments(source);

        kept.Should().NotContain("no-bytes-in-context", "整行注释必须被丢弃");
        kept.Should().NotContain("Task<byte[]?>", "注释里的二进制形态字面量必须被丢弃");
        kept.Should().Contain("fake.normal", "代码行必须原样保留（剥离只针对注释）");
        kept.Should().Contain("fake.next", "代码行必须原样保留（剥离只针对注释）");

        // 反向自证：未剥离的原文里这些字面量确实存在 ⇒ 若不剥离就会误判（本断言因此不是多余的）。
        source.Should().Contain("no-bytes-in-context");
        source.Should().Contain("Task<byte[]?>");
    }

    /// <summary>
    /// 反向自证：若把 <c>no-bytes-in-context</c> 换成别的词，核验必须<b>不再触发</b>
    /// （证明判据确实锚在标记上，而不是恒真）。
    /// </summary>
    [Fact]
    public void BinarySafeExemption_ShouldNotTriggerWithoutMarker()
    {
        const string withoutMarker = """
            [FeishuTool("fake.download", Description = "普通下载工具。")]
            public interface IFakeDownloadTool
            {
                Task<string> DownloadAsync();
            }
            """;

        FindUnearnedExemptions(
            [("Curation.cs", withoutMarker)],
            [("Internal.cs", "[FeishuToolHandler(typeof(IFakeDownloadTool))] public Task<X> DownloadAsync() { }")])
            .Should().BeEmpty("未声明标记就不属于豁免路径（由核心断言按违规处理）");
    }

    private static HashSet<string> CollectBinarySafeTools()
    {
        const string marker = "no-bytes-in-context";
        var result = new HashSet<string>(StringComparer.Ordinal);

        var root = Path.Combine(
            FindRepositoryRoot(), "Mud.Feishu.AI.FeishuTools", "Curation");
        foreach (var file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            var text = ReadSourceWithoutComments(file);

            // 以 [FeishuTool( 为分隔，逐块判定：块内出现标记才算该工具声明了豁免。
            var chunks = text.Split("\"[FeishuTool(\"", StringSplitOptions.None);
            for (var i = 1; i < chunks.Length; i++)
            {
                var chunk = chunks[i];
                var quote = chunk.IndexOf('"');
                if (quote <= 0)
                {
                    continue;
                }

                var name = chunk[..quote];

                // 只看本工具的属性块（到下一个顶层声明为止），避免跨工具串味。
                var end = chunk.IndexOf(")]", StringComparison.Ordinal);
                var block = end > 0 ? chunk[..end] : chunk;
                if (block.Contains(marker, StringComparison.Ordinal))
                {
                    result.Add(name);
                }
            }
        }

        return result;
    }
}
