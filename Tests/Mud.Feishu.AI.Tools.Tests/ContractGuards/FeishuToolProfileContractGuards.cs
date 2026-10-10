// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Mud.Feishu.AI.Tools.SdkProfile;
using Mud.HttpUtils;
using Mud.HttpUtils.Attributes;

namespace Mud.Feishu.AI.Tools.Tests.ContractGuards;

/// <summary>
/// <b>R-1+2c 迁移契约守卫</b>：把「飞书工具生成剖面」与「上游引擎事实」钉成机械约束。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么需要它</b>：迁移把工具面生成的真相源从<b>本仓源码</b>（一个已删除的本地生成器工程，
/// 11 个文件，含全部飞书命名硬编码）搬到了<b>组件侧引擎 + 一份剖面声明</b>
/// （<c>SdkProfile/FeishuToolProfile.cs</c>）。搬移的正确性只能靠两条机械约束守住：
/// </para>
/// <list type="number">
/// <item><b>剖面逐槽冻结</b>：剖面的每个槽都直接进 golden、生成代码（命名空间/类型名）
/// 或诊断 ID——写错不会编译失败，只会表现为 golden 漂移、<c>CS0246/CS0103</c> 或诊断口径分叉。
/// 故此处把 31 个槽逐一冻结（见 <see cref="ExpectedSlots"/>），任一槽被改即红，必须显式评审。</item>
/// <item><b>上游槽位镜像</b>：零容忍诊断集、严重级别、门禁三集合的真相源已上移到组件侧
/// <c>ToolSurfaceDiagnostics.Slots</c>。本仓无法符号引用该 internal 类型，故镜像为
/// <see cref="UpstreamSlotMirror"/> 并与 <c>scripts/diagnostics-gate.ps1</c> 逐集合比对；
/// <b>镜像与门禁脚本任一侧漂移即红</b>——这替代了原 <c>GeneratorDiagnosticsContractGuards</c>
/// 中依赖已删除的 <c>Diagnostics.cs</c> 的"定义集 == 上报点集"守卫。</item>
/// </list>
/// <para>
/// 另附两条迁移台账守卫（<see cref="RetiredLocalEngine_ShouldNotReappearAsALocalGenerator"/>、
/// <see cref="ToolSourceDeclarations_ShouldUseNameofForm"/>）：前者防"删了代码忘了删条目"导致
/// 两套引擎并存的窗口被重新打开；后者锁定 R-1 的交付物形态（84 处 <c>Source</c> 全部 nameof 化）。
/// </para>
/// </remarks>
public class FeishuToolProfileContractGuards
{
    private const string ProfileNamespacePrefix = "Mud.Feishu.AI.Tools.SdkProfile.";
    private const string SourceDirectory = "Mud.Feishu.AI.Tools/Curation";

    /// <summary>
    /// 本守卫自身所在文件名——判据 ①② 要扫描的字面量就写在本文件里，必须把自己排除出扫描面。
    /// </summary>
    private static readonly string SelfFileName = Path.GetFileName(CallerPath());

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string CallerPath([CallerFilePath] string path = "") => path;

    // ────────────────────────────────────────────────────────────────────
    // 守卫 1：剖面逐槽冻结（"旧引擎 11 个文件的硬编码收拢"这一主张的可验证形态）
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 飞书剖面的<b>冻结契约</b>：槽名 → 期望值，逐条注明来源（迁移前该事实所在的位置）。
    /// </summary>
    /// <remarks>
    /// 改动本表 = 改动工具面的**编译期命名事实**（产物名/命名空间/诊断 ID/解包表），
    /// 必须与 golden 重新固化、PublicAPI 同步、CHANGELOG 同批进行。
    /// </remarks>
    private static readonly Dictionary<string, string> ExpectedSlots = new(StringComparer.Ordinal)
    {
        // 剖面标识
        ["Name"] = "Feishu",
        // 1. 特性识别（旧 Extractors.GetFeishuToolAttribute / ToolHandlerScanner.*）
        ["ToolAttributeName"] = "FeishuTool",
        ["ToolAttributeNamespace"] = "Mud.Feishu.AI.Tools",
        ["ToolHandlerAttributeName"] = "FeishuToolHandler",
        // 本工程更名后根命名空间与 [FeishuTool] 所在的 Mud.Feishu.AI.Tools 同名，故 handler 特性
        // 独占 .Handlers 子命名空间——两槽塌陷为同值会让引擎失去「声明面 vs 执行器绑定面」的可寻址性。
        ["ToolHandlerAttributeNamespace"] = "Mud.Feishu.AI.Tools.Handlers",
        ["ParameterAttributeName"] = "ToolParameter",
        // 2. 源解析（旧 Extractors.ResolveSourceMember 的 "Mud.Feishu." 前缀）
        ["SdkNamespaceRoot"] = "Mud.Feishu",
        // 3. 接口范式（旧 Extractors.SdkInterfaceNameRegex，逐字符一致）
        ["InterfaceNameRegex"] = @"^IFeishu(?:Tenant|User)?V\d+(?<domain>[A-Z][a-zA-Z0-9]*)(?<resource>[A-Z][a-zA-Z0-9]*)?$",
        // 4. 令牌身份（旧 CuratedToolScanner.DeriveIdentityFromSource 的 StartsWith 语义）
        ["TokenKindStrategy"] = nameof(TokenKindDerivationStrategy.NamePrefix),
        ["TokenKindMarkers"] = "IFeishuTenant=Tenant;IFeishuUser=User",
        // 5-7. 产物前缀 / 诊断 / 危险词（旧各 Emitter 前缀、Diagnostics.cs、Extractors.DangerWords）
        ["ProductPrefix"] = "FeishuTool",
        ["ProductPluralPrefix"] = "FeishuTools",
        ["DiagnosticPrefix"] = "MUDFT",
        ["DiagnosticCategory"] = "MudFeishu.AI",
        ["ToolingDiagnosticCategory"] = "MudFeishu.Tooling",
        ["WriteVerbKeywords"] = "delete|remove|transfer|cancel|revoke|resign|permission|reset_secret|password|dismiss|purge|wipe",
        // 8-11. 描述符与契约产物事实（旧 SchemaWriter 扩展键、SchemaEmitter.ToolNamesOwnerAssembly、
        //        各 Emitter 命名空间、SchemaEmitter 的风险枚举全名）
        ["SchemaExtensionKey"] = "x-feishu",
        ["OwnerAssembly"] = "Mud.Feishu.AI.Tools",
        ["GeneratedNamespace"] = "Mud.Feishu.AI.Tools.Generated",
        ["ContractNamespace"] = "Mud.Feishu.AI.Tools",
        ["RegistrationNamespace"] = "Mud.Feishu.AI.Tools.Registration",
        ["RiskEnumFullName"] = "Mud.Feishu.AI.Tools.FeishuToolRisk",
        // 12-17. 执行器 / 输出 / 聚合事实（旧 ToolHandlerScanner 返回类型名、ToolRegistrarEmitter
        //        绑定类型名、TypeSchemaResolver 解包表、CapabilityCatalogEmitter 接口前缀）
        ["ResultTypeName"] = "FeishuToolResult",
        ["BindingTypeName"] = "FeishuToolBinding",
        ["OutputWrapperNamespace"] = "Mud.Feishu.DataModels",
        ["OutputWrapperTypeNames"] = "FeishuApiResult|FeishuApiPageListResult|FeishuApiListResult|FeishuApiPageListTotalResult|FeishuNullDataApiResult",
        ["SdkInterfacePrefix"] = "IFeishu",
        // 开关与资产
        ["CapabilityCatalogPropertyName"] = "FeishuToolCatalog",
        ["GoldenFileName"] = "FeishuToolSchemas.golden.txt",
        ["GoldenUpdatePropertyName"] = "FeishuToolGoldenUpdate",
        ["GuidanceDirectory"] = "/Guidance/",
    };

    /// <summary>剖面的每个槽必须逐字节等于冻结契约（改槽即改工具面的编译期命名事实）。</summary>
    [Fact]
    public void FeishuToolProfile_ShouldDeclareTheFrozenFeishuContract()
    {
        var attribute = ReadProfileAttribute();

        var drift = new List<string>();
        foreach (var (slot, expected) in ExpectedSlots)
        {
            var property = typeof(SdkToolProfileAttribute).GetProperty(slot, BindingFlags.Public | BindingFlags.Instance);
            property.Should().NotBeNull($"SdkToolProfileAttribute 必须仍有 {slot} 槽（上游契约变更须显式评审本守卫）");

            var actual = property!.GetValue(attribute)?.ToString() ?? string.Empty;
            if (!string.Equals(actual, expected, StringComparison.Ordinal))
            {
                drift.Add($"{slot}: 期望 '{expected}'，实际 '{actual}'");
            }
        }

        drift.Should().BeEmpty(
            "飞书剖面槽位与冻结契约不一致——这些值直接进 golden / 生成代码 / 诊断 ID，"
            + "改动必须与 golden 重新固化 + PublicAPI 同步 + CHANGELOG 同批：\n{0}",
            string.Join("\n", drift));
    }

    /// <summary>
    /// 剖面必须<b>成对</b>声明（实现 <c>ISdkToolProfile</c> + 标注 <c>[SdkToolProfile]</c>）——
    /// 这是 <c>SDKT001</c> 的编译期约束，此处再锁一次"确实存在的那个剖面类没被写坏"。
    /// </summary>
    [Fact]
    public void FeishuToolProfile_ShouldBePairedWithTheMarkerInterface()
    {
        typeof(ISdkToolProfile).IsAssignableFrom(typeof(FeishuToolProfile)).Should().BeTrue(
            "剖面类必须实现 ISdkToolProfile（缺了它，引擎的剖面发现会把它当普通类型忽略 ⇒ 工具面静默消失）");

        ReadProfileAttribute().Should().NotBeNull("剖面类必须同时标注 [SdkToolProfile]（SDKT001 成对性）");
    }

    /// <summary>剖面的类型名/命名空间是"每编译单元携带剖面"这条接线纪律的锚点。</summary>
    [Fact]
    public void FeishuToolProfile_ShouldLiveInTheOwningAssemblyAndNamespace()
    {
        var type = typeof(FeishuToolProfile);

        type.FullName.Should().Be(
            ProfileNamespacePrefix + nameof(FeishuToolProfile),
            "剖面类的全名是接线锚点（owner 程序集 + 命名空间），改名须同步测试工程的 Compile Include 链接");

        type.Assembly.GetName().Name.Should().Be(
            "Mud.Feishu.AI.Tools",
            "剖面必须编译进工具面宿主程序集（owner 门槛 = 该程序集名）");
    }

    // ────────────────────────────────────────────────────────────────────
    // 守卫 2：上游槽位镜像 ↔ 门禁脚本 ↔ CI（替代已删除的 Diagnostics.cs 口径守卫）
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 组件侧引擎 <c>ToolSurfaceDiagnostics.Slots</c> 的<b>镜像</b>
    /// （槽位号 → 标题 → 严重级别 → 是否零容忍）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 来源：<c>Mud.HttpUtils.Generator/ToolSurface/ToolSurfaceDiagnostics.cs</c>（组件侧 3.0.3）。
    /// 本仓无法符号引用该 internal 表，故镜像冻结于此；<b>引擎升级若改动槽位/级别/零容忍列，
    /// 必须显式评审并同步本表与 <c>scripts/diagnostics-gate.ps1</c></b>。
    /// </para>
    /// <para>
    /// 已删除的槽位（007/012/013）<b>不得复活</b>：它们是"指向不存在机制的僵尸定义"，
    /// 由 <see cref="UpstreamSlotMirror_ShouldNotResurrectRetiredSlots"/> 锁定。
    /// </para>
    /// </remarks>
    private static readonly (string Slot, string Severity, bool ZeroTolerance)[] UpstreamSlotMirror =
    [
        ("001", "Error", true),    // 工具缺少名称
        ("002", "Error", true),    // 接口命名不符合 SDK 范式
        ("003", "Error", true),    // 工具名冲突
        ("004", "Error", true),    // 返回类型不可映射
        ("005", "Warning", false), // XML summary 缺失（恒 0 Warning 集）
        ("006", "Warning", false), // 参数缺 param 说明（恒 0 Warning 集）
        ("008", "Error", true),    // 上传参数无法映射 binary
        ("009", "Warning", false), // OutputSchema 截断（聚合单条，基线集）
        ("010", "Error", true),    // 查询参数展开失败
        ("011", "Error", true),    // AnyOf 引用不存在的参数
        ("014", "Error", true),    // golden 漂移
        ("015", "Error", true),    // Schema 内部不一致
        ("016", "Error", true),    // 身份与接口令牌类型不一致
        ("017", "Error", true),    // 读写分类与 SDK 事实脱钩
        ("018", "Info", false),    // 能力覆盖报告（恒 0，Info 级）
        ("019", "Error", true),    // SDK 源无法解析
        ("020", "Error", true),    // 参数类型无解包映射
        ("021", "Warning", false), // 必填参数被声明为可空（基线集）
        ("022", "Error", true),    // 工具未绑定执行器
        ("023", "Error", true),    // 执行器绑定不成立
        ("024", "Error", true),    // 执行器签名不符
        ("025", "Error", true),    // 执行器构造参数不可解析
        ("026", "Error", true),    // 生成器内部异常兜底（category = Tooling）
        ("027", "Error", true),    // 派生常量名冲突
    ];

    /// <summary>镜像是零容忍的<b>唯一入口</b>：零容忍集必须全部是 Error（否则"构建期阻断"名不副实）。</summary>
    [Fact]
    public void UpstreamSlotMirror_ShouldMarkExactlyTheErrorSlotsAsZeroTolerance()
    {
        var inconsistent = UpstreamSlotMirror
            .Where(static slot => slot.ZeroTolerance != (slot.Severity == "Error"))
            .Select(static slot => $"{slot.Slot}({slot.Severity}, zeroTolerance={slot.ZeroTolerance})")
            .ToArray();

        inconsistent.Should().BeEmpty(
            "零容忍 ⇒ 必须 Error 级，Warning/Info ⇒ 不得零容忍（否则会把截断告警升级为构建阻断，"
            + "或让 Error 悄悄脱离门禁）：{0}", string.Join(", ", inconsistent));

        UpstreamSlotMirror.Where(static s => s.ZeroTolerance).Should().HaveCount(19,
            "零容忍槽位数为 19（引擎侧 ZeroToleranceSlots 的镜像：24 槽 − 5 个 Warning/Info 槽；数量变化即需显式评审）");
    }

    /// <summary>已删除的槽位（007/012/013）不得在本仓镜像中复活——它们是"指向不存在机制的僵尸定义"。</summary>
    [Fact]
    public void UpstreamSlotMirror_ShouldNotResurrectRetiredSlots()
    {
        var retired = new[] { "007", "012", "013" };
        var resurrected = UpstreamSlotMirror
            .Where(slot => retired.Contains(slot.Slot, StringComparer.Ordinal))
            .Select(static slot => slot.Slot)
            .ToArray();

        resurrected.Should().BeEmpty(
            "槽位 {0} 在组件侧已删除（其机制在本仓从来不存在）——复活它们会让覆盖集看起来比实际更广",
            string.Join(", ", resurrected));
    }

    /// <summary>
    /// <b>核心断言</b>：<c>scripts/diagnostics-gate.ps1</c> 的三个 MUDFT 集合
    /// 必须与镜像<b>逐项相等</b>（零容忍 = Error 槽；恒 0 集 ∪ 基线集 = Warning/Info 槽）。
    /// </summary>
    /// <remarks>
    /// 这条替代了原 <c>MudftGateSets_ShouldBeDisjointAndCoverAllDeclaredDiagnostics</c> 的
    /// "覆盖 Diagnostics.cs 全部声明"口径：迁移后本地不再有 <c>Diagnostics.cs</c>，
    /// 真相源改为<b>上游槽位表</b>，故比对对象从"本地定义集"改为"上游镜像"。
    /// </remarks>
    [Fact]
    public void MudftGateSets_ShouldMatchTheUpstreamSlotMirror()
    {
        var gate = File.ReadAllText(GateScriptPath());

        var zero = ReadGateScriptIdList(gate, "$MudftZeroToleranceIds");
        var alwaysZero = ReadGateScriptIdList(gate, "$MudftAlwaysZeroIds");
        var baseline = ReadGateScriptIdList(gate, "$MudftBaselineIds");

        zero.Should().BeEquivalentTo(
            UpstreamSlotMirror.Where(static s => s.ZeroTolerance).Select(static s => s.Slot),
            "门禁的 MUDFT 零容忍集必须等于上游槽位表的 Error 槽集合（多一个=假红风险，少一个=漏守）");

        var nonZeroTolerance = UpstreamSlotMirror
            .Where(static s => !s.ZeroTolerance)
            .Select(static s => s.Slot)
            .OrderBy(static s => s, StringComparer.Ordinal);

        alwaysZero.Concat(baseline).OrderBy(static s => s, StringComparer.Ordinal).Should().BeEquivalentTo(
            nonZeroTolerance,
            "Warning/Info 槽必须全部落在「恒 0 集 ∪ 基线集」内（落到三不管地带 ⇒ 该级别无人守护）");

        alwaysZero.Intersect(baseline).Should().BeEmpty(
            "同一 ID 不得同时属于恒 0 集与基线集（判定口径会互相矛盾）");
    }

    /// <summary>
    /// <b>R-1+2c 新增失败通道</b>：剖面写坏 ⇒ 引擎报 <c>SDKT001/002</c>（不再报 MUDFT）。
    /// 故门禁脚本、本地门禁、CI 三处都必须声明并断言该集合。
    /// </summary>
    [Fact]
    public void SdkToolProfileGate_ShouldBeDeclaredAndAssertedLocallyAndInCi()
    {
        var gate = File.ReadAllText(GateScriptPath());
        var sdkIds = ReadGateScriptIdList(gate, "$SdkToolZeroToleranceIds");
        sdkIds.Should().BeEquivalentTo(
            new[] { "SDKT001", "SDKT002" },
            "剖面契约集必须声明 SDKT001（接口↔特性成对）与 SDKT002（必填槽缺失 ⇒ 工具面静默消失）");

        gate.Should().Contain(
            "Measure-SdkToolDiagnostics",
            "门禁真相源必须提供 SDKT 的测量函数（否则集合声明了也没人用）");

        File.ReadAllText(VerifyBuildScriptPath()).Should().Contain(
            "Measure-SdkToolDiagnostics",
            "本地门禁未断言 SDKT 剖面契约 —— 工具面可能在「构建成功」的表象下静默消失");

        File.ReadAllText(CiWorkflowPath()).Should().Contain(
            "Measure-SdkToolDiagnostics",
            "CI 未断言 SDKT 剖面契约 ⇒ 与本地门禁不同源（R5 根因 R-G 的同型失效）");
    }

    // ────────────────────────────────────────────────────────────────────
    // 守卫 3：迁移台账（R-1+2c 的"产线搬迁"收尾不可回退）
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 本仓<b>不得存在本地工具面生成器实现</b>——生成引擎已上游化到组件侧
    /// <c>Mud.HttpUtils.Generator</c>（R-1+2c），本仓只保留一份剖面声明
    /// （<c>SdkProfile/FeishuToolProfile.cs</c>）作为飞书命名事实的载体。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>为什么按"有无生成器实现"判据，而不是按工程名判据</b>：两套引擎并存会同时向同一编译发射
    /// 同名 hintName 产物 ⇒ 编译期冲突 + 维护双份同构代码，而"两套引擎"这件事的本质是
    /// <b>存在第二个 <c>IIncrementalGenerator</c> 实现</b>，与它挂在哪个工程名下无关。
    /// 早先的判据是"目录 <c>Mud.Feishu.AI.Tools</c> / <c>Tests/Mud.Feishu.AI.Tools.Tests</c> 不存在"，
    /// 它把守卫和一个具体工程名焊死：本仓把工具面工程更名为 <c>Mud.Feishu.AI.Tools</c> 后，
    /// 守卫会因"目录存在"而必红——判据被命名决策而非被不变量触发（假红）。
    /// 下面的判据全部与命名解耦。
    /// </para>
    /// <para>
    /// <b>四条判据</b>：① 无任何 <c>[Generator]</c> 标注（增量生成器的唯一编译期标记）；
    /// ② 无旧引擎实现类型名残留（删了特性但留死代码同样误导维护者）；
    /// ③ <b>ProjectReference 图闭合</b>——任何 <c>ProjectReference</c> 的目标必须是 <c>slnx</c> 登记的
    /// 工程，"新建一个未登记的本地生成器工程并被引用"这条退化路径因此必红；
    /// ④ 本仓无 Roslyn 组件宿主（无工程直接引用 <c>Microsoft.CodeAnalysis.CSharp</c>）——
    /// 这是"本地生成器工程"绕不开的依赖，故与命名无关。
    /// </para>
    /// </remarks>
    [Fact]
    public void RetiredLocalEngine_ShouldNotReappearAsALocalGenerator()
    {
        var root = FindRepositoryRoot();

        // 判据的字面量（[Generator]、旧引擎类型名）就写在**本文件**里，扫描面必须排除自身，
        // 否则守卫恒红（第一次跑就会命中自己）。文件名取自 CallerFilePath 而非硬编码，
        // 本文件改名/移动都不会让排除失效。
        var self = SelfFileName;
        IEnumerable<string> Scan(Func<string, IEnumerable<string>> source)
            => source(root).Where(path => !Path.GetFileName(path).Equals(self, StringComparison.OrdinalIgnoreCase));

        // 判据 ①：全仓（剔 obj/bin）不得出现 [Generator] / [GeneratorWithAttributeSearch]。
        // 这是名称无关的判据：本地引擎工程无论被改成什么名字，只要它带着生成器实现就会被拦。
        var generators = Scan(RepositorySources)
            .Where(static path => GeneratorAttributeRegex().IsMatch(File.ReadAllText(path)))
            .Select(path => RelativeName(root, path))
            .ToArray();

        generators.Should().BeEmpty(
            "工具面生成引擎已上游化到 Mud.HttpUtils.Generator（R-1+2c）——本仓不得再出现任何 "
            + "[Generator] 标注的增量生成器实现（两套引擎并存会产生同名 hintName 产物冲突）：{0}",
            string.Join(" | ", generators));

        // 判据 ②：旧引擎的实现类型名不得残留（特性被摘掉但代码还在，是同一退化的中间态）。
        var legacyTypes = Scan(RepositorySources)
            .Where(static path => LegacyEngineTypeRegex().IsMatch(File.ReadAllText(path)))
            .Select(path => RelativeName(root, path))
            .ToArray();

        legacyTypes.Should().BeEmpty(
            "R-1+2c 已删除的本地引擎实现类型仍有残留（等价能力在组件侧 ToolSurface/）：{0}",
            string.Join(" | ", legacyTypes));

        // 判据 ③：ProjectReference 图必须闭合——目标工程须在 slnx 中登记。
        // 这条同时锁住"新增工程忘了登记"与"引用一个未登记的本地生成器工程"两种退化。
        var registered = RegisteredProjectFileNames(root);

        var unregisteredTargets = Scan(RepositoryProjectFiles)
            .SelectMany(static path => ProjectReferences(path)
                .Select(include => (Owner: Path.GetFileName(path), Target: Path.GetFileName(include))))
            .Where(reference => !registered.Contains(reference.Target))
            .Select(static reference => $"{reference.Owner} → {reference.Target}")
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        unregisteredTargets.Should().BeEmpty(
            "以下 ProjectReference 目标未登记在 Mud.Feishu.slnx 中——未登记的工程既不在方案构建图内，"
            + "也无法被 AOT 严格模式枚举到，是'两套引擎并存'最隐蔽的入口：{0}",
            string.Join(" | ", unregisteredTargets));

        // 判据 ④：本仓不得存在 Roslyn 组件（分析器/生成器）工程——那正是"本地引擎"的宿主形态。
        // 判据取"是否直接引用 Roslyn 编译器 API"而非工程名：生成器工程无论叫什么、放在哪，
        // 都必须引用 Microsoft.CodeAnalysis.CSharp 才能实现 IIncrementalGenerator。
        var roslynHosts = Scan(RepositoryProjectFiles)
            .Where(static path => RoslynApiRegex().IsMatch(StripXmlComments(File.ReadAllText(path))))
            .Select(path => RelativeName(root, path))
            .ToArray();

        roslynHosts.Should().BeEmpty(
            "工具面生成引擎已上游化到 Mud.HttpUtils.Generator（R-1+2c）——本仓不得存在直接引用 Roslyn "
            + "编译器 API（Microsoft.CodeAnalysis.CSharp）的工程，即本地生成器/分析器宿主：{0}",
            string.Join(" | ", roslynHosts));
    }

    /// <summary>
    /// <b>R-1 交付物形态</b>：全部 <c>[FeishuTool(Source = …)]</c> 必须是
    /// <c>nameof(接口) + "." + nameof(接口.方法)</c> 常量拼接，不得残留字符串字面量。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 为什么是拼接而非单段 <c>nameof(接口.方法)</c>：C# 的 <c>nameof</c> 对成员访问只产出
    /// <b>末段标识符</b>（丢掉接口名），而引擎要求 <c>"&lt;接口名&gt;.&lt;方法名&gt;"</c>——
    /// 单段写法会产出无 <c>.</c> 的串，直接被判为"源无法解析"（MUDFT019）。
    /// </para>
    /// <para>
    /// 计数用<b>精确基线</b>（99）：一条"永远为真"的形态守卫与没有守卫等价，
    /// 故同时锁"字面量 = 0"与"nameof 形态 = 99"两侧。
    /// </para>
    /// <para>
    /// ⚠️ 本守卫按<b>文本</b>匹配「等号右侧紧接 <c>nameof(</c> 的赋值」，故注释/文档里写出
    /// 同样的字面文本也会被计数（R6/S2 实测：Curation 头注释里写了一次该形态即让基线 +1）。
    /// 新增域时若基线多出 1，先检查是不是注释被算了进去，再决定是否改基线。
    /// </para>
    /// </remarks>
    [Fact]
    public void ToolSourceDeclarations_ShouldUseNameofForm()
    {
        var directory = Path.Combine(FindRepositoryRoot(), SourceDirectory.Replace('/', Path.DirectorySeparatorChar));
        directory.Should().NotBeNull();

        var literalCount = 0;
        var nameofCount = 0;

        foreach (var file in Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
        {
            var source = File.ReadAllText(file);
            literalCount += Regex.Matches(source, @"Source\s*=\s*""").Count;
            nameofCount += Regex.Matches(source, @"Source\s*=\s*nameof\(").Count;
        }

        literalCount.Should().Be(0,
            "仍有 Source 魔法字符串字面量——SDK 接口改名时它们不会随 IDE 重命名联动（R-1 的收益目标）");

        nameofCount.Should().Be(157,
            "Source 声明数必须恒为 157（新增/删除 Source 时同步本基线——不得改成下限断言，那会让覆盖缩水静默通过）");
    }

    // ────────── 读取与定位 ──────────

    /// <summary>全仓参与构建的 C# 源文件（排除 obj/bin 与版本控制内部目录）。</summary>
    private static IEnumerable<string> RepositorySources(string root)
        => EnumerateBuildableFiles(root, "*.cs");

    /// <summary>全仓参与构建的工程文件（排除 obj/bin 与版本控制内部目录）。</summary>
    private static IEnumerable<string> RepositoryProjectFiles(string root)
        => EnumerateBuildableFiles(root, "*.csproj");

    private static IEnumerable<string> EnumerateBuildableFiles(string root, string pattern)
        => Directory
            .EnumerateFiles(root, pattern, SearchOption.AllDirectories)
            .Where(static path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !path.Contains($"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}", StringComparison.Ordinal));

    /// <summary>把绝对路径压成相对仓库根的、便于写进失败消息的展示形式。</summary>
    private static string RelativeName(string root, string path)
        => Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/');

    /// <summary>剔除 XML 注释后再匹配：多个 csproj 会在注释里解释"已改为组件侧引擎"，那是记录而非声明。</summary>
    private static string StripXmlComments(string content)
        => Regex.Replace(content, "<!--.*?-->", string.Empty, RegexOptions.Singleline);

    /// <summary>读取 <c>slnx</c> 登记的工程文件名集合（判据"图闭合"的权威清单）。</summary>
    private static HashSet<string> RegisteredProjectFileNames(string root)
    {
        var slnx = File.ReadAllText(Path.Combine(root, "Mud.Feishu.slnx"));

        return Regex.Matches(slnx, @"<Project\s+Path=""[^""]*?(?<name>[^/""]+\.csproj)""")
            .Select(static match => match.Groups["name"].Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>读取一个工程文件的全部 <c>ProjectReference</c> 目标（原始 <c>Include</c> 值）。</summary>
    private static IReadOnlyList<string> ProjectReferences(string csprojPath)
        => Regex.Matches(StripXmlComments(File.ReadAllText(csprojPath)), @"<ProjectReference\s+Include=""(?<inc>[^""]+)""")
            .Select(static match => match.Groups["inc"].Value)
            .ToArray();

    // 三条正则用 GeneratedRegex 之外的手写构造：它们只在守卫执行时跑一次，
    // [GeneratedRegex] 会把每次测试运行都变成一次源生成器编译，不划算。
    private static Regex GeneratorAttributeRegex()
        => new(@"\[(?:Generator|GeneratorWithAttributeSearch)\s*[\]\(]", RegexOptions.CultureInvariant);

    private static Regex LegacyEngineTypeRegex()
        => new(@"\b(?:class|record)\s+(?:FeishuToolSchemaGenerator|CuratedToolScanner|ToolHandlerScanner|CapabilityCatalogEmitter|SchemaEmitter|ToolRegistrarEmitter|TypeSchemaResolver)\b",
            RegexOptions.CultureInvariant);

    private static Regex RoslynApiRegex()
        => new(@"PackageReference\s+Include=""Microsoft\.CodeAnalysis\.(?:CSharp|VisualBasic)""", RegexOptions.CultureInvariant);

    private static SdkToolProfileAttribute ReadProfileAttribute()
    {
        var attribute = typeof(FeishuToolProfile).GetCustomAttribute<SdkToolProfileAttribute>();
        attribute.Should().NotBeNull(
            "剖面类必须标注 [SdkToolProfile]（否则引擎发现不到剖面 ⇒ 工具面静默不产出）");
        return attribute!;
    }

    /// <summary>从门禁真相源读取形如 <c>$X = @('001','002')</c> 的数组字面量。</summary>
    private static IReadOnlyList<string> ReadGateScriptIdList(string gateScript, string variableName)
    {
        var block = Regex.Match(
            gateScript,
            $@"{Regex.Escape(variableName)}\s*=\s*@\((?<body>.*?)\)",
            RegexOptions.Singleline);

        return block.Success
            ? Regex.Matches(block.Groups["body"].Value, "'(?<id>[^']+)'")
                .Select(static m => m.Groups["id"].Value)
                .ToArray()
            : [];
    }

    private static string GateScriptPath() => Path.Combine(FindRepositoryRoot(), "scripts", "diagnostics-gate.ps1");

    private static string VerifyBuildScriptPath() => Path.Combine(FindRepositoryRoot(), "scripts", "verify-build.ps1");

    private static string CiWorkflowPath()
        => Path.Combine(FindRepositoryRoot(), ".github", "workflows", "dotnet-publish.yml");

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
