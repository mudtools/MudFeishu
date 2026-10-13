// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Mud.AI.ToolSurface;

/// <summary>
/// 工具面引擎的<b>动态诊断槽位表</b>（设计文档 §6.1：档位 2「引擎通用槽位」）。
/// </summary>
/// <remarks>
/// <para>
/// 与静态 <c>DiagnosticIds.cs</c> 的边界：profile 注入的 ID 是<b>动态组装</b>
/// （<c>{profile.DiagnosticPrefix}{槽位号:D3}</c>），不进静态常量表；
/// 「槽位号 ↔ 语义 ↔ 上报点」的完全相等关系由本表 + <c>ToolSurfaceContractTests</c> 契约守卫锁定
/// （等同上游 <c>Diagnostics_ShouldNotDeclareUnreportedDiagnostics</c> 纪律）。
/// </para>
/// <para>
/// 槽位序号以飞书 <c>MUDFT</c> 既有序号为<b>规范序号</b>（001 缺名、002 命名、003 冲突……），
/// 使上游 <c>MUDFT001..027</c> 与 zero-tolerance 基线可零漂移映射；微信侧得到
/// <c>MUDWX001..</c> 的同构但独立序列。
/// </para>
/// <para>
/// <b>槽位表携带「是否零容忍」列</b>（v2.1）：上游零容忍集不含 009/018/021（Warning/Info），
/// 缺该列会让微信侧误把截断告警升级为构建阻断。
/// </para>
/// <para>
/// <b>类别例外</b>（v2.1 实测）：兜底槽 026 的 category 是 <c>ToolingDiagnosticCategory</c>
/// 而非 <c>DiagnosticCategory</c>（上游 <c>MUDFT026</c> = <c>MudFeishu.Tooling</c>，其余 = <c>MudFeishu.AI</c>）。
/// </para>
/// </remarks>
internal static class ToolSurfaceDiagnostics
{
    // ────────── 槽位语义名（契约守卫按此比对上报点） ──────────

    /// <summary>001：工具特性缺少工具名。</summary>
    public const int SlotMissingToolName = 1;

    /// <summary>002：接口命名不符合 SDK 范式（剖面本地语义，descriptor 消息由 profile 侧提供口径）。</summary>
    public const int SlotInterfaceNamingViolation = 2;

    /// <summary>003：工具名冲突（字面重复）。</summary>
    public const int SlotToolNameConflict = 3;

    /// <summary>004：返回类型不可映射为 OutputSchema。</summary>
    public const int SlotReturnTypeNotMappable = 4;

    /// <summary>005：XML summary 缺失（description 为空）。Warning。</summary>
    public const int SlotMissingXmlSummary = 5;

    /// <summary>006：参数缺 param 说明。Warning。</summary>
    public const int SlotMissingXmlParam = 6;

    /// <summary>008：上传/下载参数类型无法映射 binary。</summary>
    public const int SlotBinaryMappingFailure = 8;

    /// <summary>009：输出 Schema 深度截断 / 循环引用（聚合单条）。Warning，非零容忍。</summary>
    public const int SlotOutputSchemaTruncation = 9;

    /// <summary>010：查询参数对象展开失败。</summary>
    public const int SlotQueryExpansionFailure = 10;

    /// <summary>011：条件必填组（AnyOf）引用不存在的参数名。</summary>
    public const int SlotAnyOfUnknownParameter = 11;

    /// <summary>014：Golden 快照 diff（描述符静默漂移）。</summary>
    public const int SlotGoldenDrift = 14;

    /// <summary>015：Schema 内部不一致（required 不在 properties 键集等）。</summary>
    public const int SlotSchemaInconsistency = 15;

    /// <summary>016：工具身份与承载接口令牌类型不一致。</summary>
    public const int SlotTokenKindMismatch = 16;

    /// <summary>017：读写分类与 SDK 事实脱钩。</summary>
    public const int SlotReadWriteDecoupling = 17;

    /// <summary>018：AI 能力覆盖报告（聚合单条）。Info，非零容忍。</summary>
    public const int SlotCapabilityCoverageReport = 18;

    /// <summary>019：Source 源挂钩无法解析。</summary>
    public const int SlotSourceUnresolvable = 19;

    /// <summary>020：参数 C# 类型不在解包映射表内。</summary>
    public const int SlotUnpackMappingMissing = 20;

    /// <summary>021：必填参数被声明为可空。Warning，非零容忍。</summary>
    public const int SlotRequiredNullableConflict = 21;

    /// <summary>022：工具没有任何执行器绑定。</summary>
    public const int SlotHandlerUnbound = 22;

    /// <summary>023：执行器绑定不成立（名字不在契约表 / 重复绑定）。</summary>
    public const int SlotHandlerBindingInvalid = 23;

    /// <summary>024：执行器方法签名不符。</summary>
    public const int SlotHandlerSignatureMismatch = 24;

    /// <summary>025：执行器构造参数无法作为 DI 服务类型解析。</summary>
    public const int SlotHandlerConstructorUnresolvable = 25;

    /// <summary>026：生成器内部异常兜底（Guard 层）。category 用 ToolingDiagnosticCategory。</summary>
    public const int SlotGeneratorInternalError = 26;

    /// <summary>027：工具名归一后的派生常量名冲突。</summary>
    public const int SlotDerivedConstantNameConflict = 27;

    /// <summary>028：per-tool affordance 资产结构非法（缺节 / 未知节 / 空节 / 同工具名重复占用）。</summary>
    public const int SlotAffordanceStructure = 28;

    /// <summary>029：per-tool affordance 资产文件名不命中任何已策展工具名（资产不会生效）。</summary>
    public const int SlotAffordanceOrphan = 29;

    /// <summary>030：per-tool affordance 资产引用了不存在的工具名（反引号包裹的 <c>域.工具</c>）。</summary>
    public const int SlotAffordanceDanglingReference = 30;

    /// <summary>031：per-tool affordance 资产超字符预算（聚合单条）。Warning，非零容忍。</summary>
    public const int SlotAffordanceBudget = 31;

    /// <summary>槽位定义：序号 + 标题 + 消息模板 + 级别 + 是否零容忍。</summary>
    /// <remarks>不采用 <c>record</c>（netstandard2.0 无 <c>IsExternalInit</c>，仓库纪律）。</remarks>
    internal sealed class SlotDefinition
    {
        public SlotDefinition(int slot, string title, string messageFormat, DiagnosticSeverity severity, bool zeroTolerance)
        {
            Slot = slot;
            Title = title;
            MessageFormat = messageFormat;
            Severity = severity;
            ZeroTolerance = zeroTolerance;
        }

        /// <summary>规范槽位号（拼入 <c>{prefix}{slot:D3}</c>）。</summary>
        public int Slot { get; }

        /// <summary>诊断标题（profile 无关的引擎语义）。</summary>
        public string Title { get; }

        /// <summary>消息模板（不提及任何 SDK 专名；SDK 事实由实参注入）。</summary>
        public string MessageFormat { get; }

        /// <summary>默认级别。</summary>
        public DiagnosticSeverity Severity { get; }

        /// <summary>是否属于零容忍集（构建期阻断口径）。</summary>
        public bool ZeroTolerance { get; }
    }

    /// <summary>
    /// 全部引擎通用槽位（与上游 <c>MUDFT</c> 描述符一一对应；序号 007/011 旧僵尸位中
    /// 007/012/013 已在上游 AT-B14 清理删除，本表不复活）。
    /// </summary>
    public static readonly ImmutableArray<SlotDefinition> Slots =
    [
        new(SlotMissingToolName, "工具缺少名称", "[{0}] 标注的接口 {1} 未提供工具名", DiagnosticSeverity.Error, true),
        new(SlotInterfaceNamingViolation, "接口命名不符合 SDK 范式", "接口 {0} 的命名不符合 {1} SDK 命名范式，无法推导令牌身份与能力归属", DiagnosticSeverity.Error, true),
        new(SlotToolNameConflict, "工具名冲突", "工具名 '{0}' 冲突：已被接口 {1} 占用", DiagnosticSeverity.Error, true),
        new(SlotReturnTypeNotMappable, "返回类型不可映射", "方法 {0}.{1} 的返回类型 {2} 无法映射为 OutputSchema——需标注返回 override", DiagnosticSeverity.Error, true),
        new(SlotMissingXmlSummary, "XML summary 缺失", "方法 {0}.{1} 缺少 XML <summary> 文档注释——工具 description 将为空", DiagnosticSeverity.Warning, false),
        new(SlotMissingXmlParam, "参数缺 param 说明", "方法 {0}.{1} 的参数 {2} 缺少 XML <param> 文档注释", DiagnosticSeverity.Warning, false),
        new(SlotBinaryMappingFailure, "上传/下载参数类型无法映射 binary", "方法 {0}.{1} 的参数 {2} 标注了表单文件但类型 {3} 无法映射为 format:binary", DiagnosticSeverity.Error, true),
        new(SlotOutputSchemaTruncation, "输出 Schema 深度截断或循环引用", "OutputSchema 截断（深度超限或循环引用）：{0}。样本：{1}", DiagnosticSeverity.Warning, false),
        new(SlotQueryExpansionFailure, "查询参数对象展开失败", "方法 {0}.{1} 的参数 {2}（类型 {3}）为复合查询参数但展开后无任何可序列化属性——模型无法得知可传字段", DiagnosticSeverity.Error, true),
        new(SlotAnyOfUnknownParameter, "条件必填组引用了不存在的参数", "工具 {0} 的 AnyOf 组 \"{1}\" 引用了签名中不存在的参数 \"{2}\"（可用参数：{3}）——该约束不会生效，请修正参数名", DiagnosticSeverity.Error, true),
        new(SlotGoldenDrift, "Golden 快照 diff", "工具描述符与 golden 快照不一致：{0}（重新固化：{1}）", DiagnosticSeverity.Error, true),
        new(SlotSchemaInconsistency, "Schema 内部不一致", "工具 '{0}' 的 Schema 内部不一致：{1}", DiagnosticSeverity.Error, true),
        new(SlotTokenKindMismatch, "工具身份与接口令牌类型不一致", "工具 '{0}' 声明身份 {1}，但承载接口 {2} 的令牌类型不符", DiagnosticSeverity.Error, true),
        new(SlotReadWriteDecoupling, "读写分类与 SDK 事实脱钩", "工具 '{0}' 未标记为写工具，但其 SDK 源 {1} 为 {2}（风险 {3}）——写面必须经授权门禁，不得归类为只读", DiagnosticSeverity.Error, true),
        new(SlotCapabilityCoverageReport, "AI 能力覆盖报告", "SDK 能力 {0} 项；已策展工具 {1} 项（覆盖率 {2}）；覆盖能力分组 {3} 个。开启 {4} 时产出此报告（Info 级，不进构建输出）", DiagnosticSeverity.Info, false),
        new(SlotSourceUnresolvable, "SDK 源无法解析", "[{0}] \"{1}\" 声明的 Source \"{2}\" 无法解析（{3}）——工具面与 SDK 不得脱钩", DiagnosticSeverity.Error, true),
        new(SlotUnpackMappingMissing, "参数类型无解包映射", "工具 '{0}' 的参数 {1}（C# 类型 {2}）不在解包映射表内——须先在引擎 ToolArgs 补 helper 并登记映射", DiagnosticSeverity.Error, true),
        new(SlotRequiredNullableConflict, "必填参数被声明为可空", "工具 '{0}' 的参数 {1} 同时为 Required=true 与可空——Schema 的 required 与解包语义不一致（二选一：去掉 Required 或改为非空类型）", DiagnosticSeverity.Warning, false),
        new(SlotHandlerUnbound, "工具未绑定执行器方法", "[{0}] 工具 '{1}' 没有执行器绑定——每个工具必须恰好绑定一个执行器方法", DiagnosticSeverity.Error, true),
        new(SlotHandlerBindingInvalid, "执行器绑定不成立", "执行器绑定不成立：{0}——{1}", DiagnosticSeverity.Error, true),
        new(SlotHandlerSignatureMismatch, "执行器方法签名不符", "工具 '{0}' 的执行器方法 {1} 签名不符——应为 public Task<{2}> 方法(IReadOnlyDictionary<string, object?> args, CancellationToken ct)，实际 {3}", DiagnosticSeverity.Error, true),
        new(SlotHandlerConstructorUnresolvable, "执行器构造参数无法解析", "工具 '{0}' 的执行器 {1} 的构造参数 {2}（类型 {3}）无法作为 DI 服务类型解析——仅支持类/接口（非开放泛型、非元组、非值类型）", DiagnosticSeverity.Error, true),
        new(SlotGeneratorInternalError, "工具面生成器内部异常", "生成器内部异常（程序集 {0}）：{1}: {2}——已兜底，工具面本次不完整，请按堆栈修复生成器", DiagnosticSeverity.Error, true),
        new(SlotDerivedConstantNameConflict, "工具名派生常量名冲突", "工具名 '{0}' 与 '{1}' 归一为同一编译期常量名 '{2}'——请改名使派生常量唯一", DiagnosticSeverity.Error, true),
        new(SlotAffordanceStructure, "per-tool affordance 资产结构非法", "工具 '{0}' 的 affordance 资产 {1} 结构非法：{2}（固定四节且各节非空：WHEN / Avoid when / Prerequisites / Tips）", DiagnosticSeverity.Error, true),
        new(SlotAffordanceOrphan, "per-tool affordance 资产不属于任何工具", "affordance 资产 {0} 的文件名 '{1}' 不命中任何已策展工具名——该资产不会被发射进描述符（请改名或删除）", DiagnosticSeverity.Error, true),
        new(SlotAffordanceDanglingReference, "per-tool affordance 资产引用了不存在的工具", "工具 '{0}' 的 affordance 资产 {1} 引用了工具名契约表中不存在的 {2}——反引号包裹的 '域.工具' 会被机械校验，请核对工具名", DiagnosticSeverity.Error, true),
        new(SlotAffordanceBudget, "per-tool affordance 资产超预算", "per-tool affordance 资产超预算：{0} 份（单资产上界 {1} 字符 / 单小节上界 {2} 字符）。样本：{3}", DiagnosticSeverity.Warning, false),
    ];

    /// <summary>
    /// 按剖面实例化描述符工厂：同一槽位号在不同 profile 下得到 <c>{prefix}{slot:D3}</c> 的独立 ID。
    /// </summary>
    /// <remarks>
    /// 描述符按 (profile, slot) 缓存于实例内——<see cref="DiagnosticDescriptor"/> 进入
    /// <c>Diagnostic.Create</c> 后其 ID/category/级别即固化，缓存避免每条上报路径重复构造。
    /// </remarks>
    internal sealed class Factory
    {
        private readonly SdkToolProfileModel _profile;
        private readonly DiagnosticDescriptor?[] _cache = new DiagnosticDescriptor?[MaxSlot + 1];

        public Factory(SdkToolProfileModel profile)
        {
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
        }

        private const int MaxSlot = 31;

        /// <summary>取槽位描述符（槽位号必须存在于 <see cref="Slots"/>，否则 <c>ArgumentOutOfRangeException</c>）。</summary>
        public DiagnosticDescriptor this[int slot]
        {
            get
            {
                if (slot < 0 || slot > MaxSlot)
                {
                    throw new ArgumentOutOfRangeException(nameof(slot), slot, "未知的工具面诊断槽位号");
                }

                var cached = _cache[slot];
                if (cached is not null)
                {
                    return cached;
                }

                var definition = Slots.FirstOrDefault(d => d.Slot == slot);
                if (definition is null)
                {
                    throw new ArgumentOutOfRangeException(nameof(slot), slot, "未知的工具面诊断槽位号");
                }

                // 兜底槽（026）用独立 Tooling 类别（v2.1 实测例外）。
                var category = slot == SlotGeneratorInternalError
                    ? _profile.ToolingDiagnosticCategory
                    : _profile.DiagnosticCategory;

                // [P3-3] 兜底槽 026 与 PAYLOAD001 同为「引擎内部异常」语义，但刻意**不加**
                // WellKnownDiagnosticTags.NotConfigurable（与 PAYLOAD001 不同待遇的显式取舍）：
                // 该标签会封死 #pragma/NoWarn/.editorconfig 全部抑制通道，而 026 报告的是剖面引擎
                // 内部异常，消费方可能需先经 .editorconfig 降级恢复构建再排查根因；且无标签不会触发
                // csc 对「不可抑制 Error」的提前 return，避免同编译中其他分析器诊断被连坐隐藏
                // （见 Diagnostics.cs 诊断治理说明 / DiagnosticTagPolicyTests）。
                var descriptor = new DiagnosticDescriptor(
                    id: _profile.DiagnosticId(slot),
                    title: definition.Title,
                    messageFormat: definition.MessageFormat,
                    category: category,
                    defaultSeverity: definition.Severity,
                    isEnabledByDefault: true);

                _cache[slot] = descriptor;
                return descriptor;
            }
        }

        /// <summary>上报单条槽位诊断。</summary>
        public void Report(SourceProductionContext context, int slot, Location? location, params object[] args)
            => context.ReportDiagnostic(Diagnostic.Create(this[slot], location ?? Location.None, args));
    }

    /// <summary>零容忍槽位号集合（消费方 <c>.editorconfig</c> 升级 <c>{prefix}*:error</c> 时的机械口径来源）。</summary>
    public static ImmutableArray<int> ZeroToleranceSlots
        => Slots.Where(static s => s.ZeroTolerance).Select(static s => s.Slot).ToImmutableArray();

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<SdkToolProfileModel, Factory> ProfileFactories = new();

    /// <summary>
    /// 按剖面取共享工厂（描述符缓存跨扫描复用）：<c>SdkToolProfileModel</c> 是全字段值相等的不可变载体，
    /// 可安全作字典键；剖面数量级为个位数，缓存无界风险不存在。
    /// </summary>
    public static Factory For(SdkToolProfileModel profile)
        => ProfileFactories.GetOrAdd(profile, static p => new Factory(p));
}