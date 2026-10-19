// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Reflection;
using System.Text.RegularExpressions;
using Mud.Feishu.AI.Tools.SdkProfile;
// v2.3 P1-E：引导契约（ISdkToolProfile / [SdkToolProfile]）随声明契约上移 Mud.AI.Abstractions
// ——命名空间是 Mud.AI / Mud.AI.Attributes（不是程序集名后缀）。
using Mud.AI;
using Mud.AI.Attributes;

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
/// 故此处把 32 个槽逐一冻结（见 <see cref="ExpectedSlots"/>），任一槽被改即红，必须显式评审。</item>
/// <item><b>上游槽位镜像</b>：零容忍诊断集、严重级别、门禁三集合的真相源已上移到组件侧
/// <c>ToolSurfaceDiagnostics.Slots</c>。本仓无法符号引用该 internal 类型，故镜像为
/// <see cref="UpstreamSlotMirror"/> 并与 <c>scripts/diagnostics-gate.ps1</c> 逐集合比对；
/// <b>镜像与门禁脚本任一侧漂移即红</b>——这替代了原 <c>GeneratorDiagnosticsContractGuards</c>
/// 中依赖已删除的 <c>Diagnostics.cs</c> 的"定义集 == 上报点集"守卫。</item>
/// </list>
/// <para>
/// 另附一条迁移台账守卫（<see cref="ToolSourceDeclarations_ShouldUseNameofForm"/>）：
/// 锁定 R-1 的交付物形态（<c>Source</c> 声明全部 nameof 化）。
/// </para>
/// <para>
/// <b>v2.3 变更 / 独立化后更新</b>：原 <c>RetiredLocalEngine_ShouldNotReappearAsALocalGenerator</c>（四判据）
/// 已<b>退役删除</b>——工具面引擎自组件仓迁入 <c>Mud.AI.Generator</c>，其后随 <c>Mud.AI</c> 家族独立化
/// 迁至独立仓（本仓改以 NuGet 分析器包消费）。「仓内零生成器」的口径为「仓内无源级生成器宿主」，
/// 由 <see cref="SingleToolSurfaceEngineContractGuards"/> 接棒（收窄而非放开）。
/// </para>
/// </remarks>
public class FeishuToolProfileContractGuards
{
    private const string ProfileNamespacePrefix = "Mud.Feishu.AI.Tools.SdkProfile.";
    private const string SourceDirectory = "Mud.Feishu.AI.Tools/Curation";



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
        // v2.3 P1-E：声明契约中立化上移 Mud.AI.Abstractions —— [FeishuTool] → [AgentTool]、
        // [FeishuToolHandler] → [ToolHandler]，命名空间随之改投 Mud.AI.Abstractions.Tools(.Handlers)。
        ["ToolAttributeName"] = "AgentTool",
        ["ToolAttributeNamespace"] = "Mud.AI.Abstractions.Tools",
        ["ToolHandlerAttributeName"] = "ToolHandler",
        // 两槽塌陷为同值会让引擎失去「声明面 vs 执行器绑定面」的可寻址性，故 handler 特性
        // 独占 .Tools.Handlers 子命名空间。声明契约（[AgentTool]）按 BUG-1 位于依赖图最底层
        // 的 Mud.AI.Abstractions —— 非 owner 声明程序集（AI.Tests 样例）不能引用 owner 程序集
        // （CS0433），必须从依赖方向下游拿到该特性（Abstractions 比原 Mud.Feishu.AI 更靠下）。
        ["ToolHandlerAttributeNamespace"] = "Mud.AI.Abstractions.Tools.Handlers",
        ["ParameterAttributeName"] = "ToolParameter",
        // 2. 源解析（旧 Extractors.ResolveSourceMember 的 "Mud.Feishu." 前缀）
        ["SdkNamespaceRoot"] = "Mud.Feishu",
        // N1/N2 中立化：能力域接口不再只住在 Mud.Feishu*（知识检索器等中立契约
        // Mud.AI.Abstractions.Knowledge.IRetriever 同样表达"该域可软缺席"）——
        // 该事实经**独立槽**表达，而不是放宽 SdkNamespaceRoot
        //（后者还承担 SDK 程序集定位，放宽会让能力目录静默零产出）。
        ["SoftAbsentNamespaceRoots"] = "Mud.AI",
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
        ["RiskEnumFullName"] = "Mud.Feishu.AI.AgentTools.FeishuToolRisk",
        // 12-17. 执行器 / 输出 / 聚合事实（旧 ToolHandlerScanner 返回类型名、ToolRegistrarEmitter
        //        绑定类型名、TypeSchemaResolver 解包表、CapabilityCatalogEmitter 接口前缀）
        // v2.3 P1-D：骨架中立化上移 Mud.AI.Agent.Tools，FeishuToolResult → ToolResult 硬改名（槽同批改）。
        ["ResultTypeName"] = "ToolResult",
        ["BindingTypeName"] = "FeishuToolBinding",
        ["OutputWrapperNamespace"] = "Mud.Feishu.DataModels",
        ["OutputWrapperTypeNames"] = "FeishuApiResult|FeishuApiPageListResult|FeishuApiListResult|FeishuApiPageListTotalResult|FeishuNullDataApiResult",
        ["SdkInterfacePrefix"] = "IFeishu",
        // 开关与资产
        ["CapabilityCatalogPropertyName"] = "FeishuToolCatalog",
        ["GoldenFileName"] = "FeishuToolSchemas.golden.txt",
        ["GoldenUpdatePropertyName"] = "FeishuToolGoldenUpdate",
        ["GuidanceDirectory"] = "/Guidance/",

        // R-3 / F-7（上游 3.0.6 新增的**可空**槽）：per-tool affordance 资产目录片段。
        // 空串 = 不启用（既有消费方零影响）；本仓显式给出后，`Affordance/{工具名}.md` 会被
        // 同 pass 发射进 `x-feishu.affordance`。改这个值必须与 csproj 的 AdditionalFiles 目录、
        // 资产目录名三者同批——三者不一致的症状是"资产写了但整键消失且构建全绿"。
        ["AffordanceDirectory"] = "/Affordance/",
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
    /// 来源：独立仓 <c>Mud.AI</c> 的 <c>Mud.AI.Generator/ToolSurface/ToolSurfaceDiagnostics.cs</c>
    /// （v2.3 P1-E 起为 <c>Mud.AI.Generator</c> 引擎，独立化后随包跨仓；
    /// 更早镜像自组件侧 <c>Mud.HttpUtils.Generator</c> 3.0.6 的同名文件）。
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

        // ── R-3 / F-7（上游 3.0.6）：per-tool affordance 资产诊断 ──
        // 前三条是**策展错误**（人把资产写错了），必须零容忍：它们不会让构建失败以外的方式
        // 暴露，症状是"资产写了但模型看不到"，从日志上完全看不出来。
        ("028", "Error", true),    // affordance 资产结构非法（缺节/未知节/空节/同工具名重复占用）
        ("029", "Error", true),    // affordance 资产文件名不命中任何已策展工具名（资产不会生效）
        ("030", "Error", true),    // affordance 资产引用了不存在的工具名（反引号 `域.工具`）
        ("031", "Warning", false), // affordance 资产超字符预算（聚合单条，基线集）
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

        UpstreamSlotMirror.Where(static s => s.ZeroTolerance).Should().HaveCount(22,
            "零容忍槽位数为 22（引擎侧 ZeroToleranceSlots 的镜像：28 槽 − 6 个 Warning/Info 槽；数量变化即需显式评审）");
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
    // 守卫 3：R-1 交付物形态（Source 全部 nameof 化）
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>R-1 交付物形态</b>：全部 <c>[AgentTool(Source = …)]</c> 必须是
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

        nameofCount.Should().Be(339,
            "Source 声明数必须恒为 339（新增/删除 Source 时同步本基线——不得改成下限断言，那会让覆盖缩水静默通过）");
    }

    // ────────── 读取与定位 ──────────

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