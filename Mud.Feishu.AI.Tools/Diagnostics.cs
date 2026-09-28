// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;

namespace Mud.Feishu.AI.Tools;

/// <summary>
/// 诊断描述符常量表（MUDFT001–MUDFT014）。
/// </summary>
/// <remarks>
/// 零容忍集：<see cref="MUDFT001"/>/<see cref="MUDFT002"/>/<see cref="MUDFT003"/>/
/// <see cref="MUDFT004"/>/<see cref="MUDFT008"/>/<see cref="MUDFT010"/>/<see cref="MUDFT014"/>。
/// 其余计入覆盖率报告。
/// </remarks>
internal static class Diagnostics
{
    // ────────── 零容忍（Error） ──────────

    /// <summary>[FeishuTool] 缺少工具名（既有，语义不变）。</summary>
    public static readonly DiagnosticDescriptor MUDFT001 = new(
        id: "MUDFT001",
        title: "FeishuTool 缺少工具名",
        messageFormat: "[FeishuTool] 标注的接口 {0} 未提供工具名",
        category: "MudFeishu.AI",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>接口命名不符合 SDK 范式（无法推导工具名 / 无法校验源挂钩）。</summary>
    public static readonly DiagnosticDescriptor MUDFT002 = new(
        id: "MUDFT002",
        title: "接口命名不符合 SDK 范式",
        messageFormat: "接口 {0} 的命名不符合 Feishu SDK 命名范式（IFeishu[Tenant|User]V{n}{Domain}{Resource}），无法推导令牌身份与能力归属",
        category: "MudFeishu.AI",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>工具名冲突（手写 × 派生、或同模块内重名）。</summary>
    public static readonly DiagnosticDescriptor MUDFT003 = new(
        id: "MUDFT003",
        title: "工具名冲突",
        messageFormat: "工具名 '{0}' 冲突：已被接口 {1} 占用",
        category: "MudFeishu.AI",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>返回类型不可映射（含 HttpResponseMessage）。</summary>
    public static readonly DiagnosticDescriptor MUDFT004 = new(
        id: "MUDFT004",
        title: "返回类型不可映射",
        messageFormat: "方法 {0}.{1} 的返回类型 {2} 无法映射为 OutputSchema——需标注 [FeishuToolReturn] override",
        category: "MudFeishu.AI",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>上传/下载参数类型无法映射 binary。</summary>
    public static readonly DiagnosticDescriptor MUDFT008 = new(
        id: "MUDFT008",
        title: "上传/下载参数类型无法映射 binary",
        messageFormat: "方法 {0}.{1} 的参数 {2} 标注了 [FormContent] 但类型 {3} 无法映射为 format:binary",
        category: "MudFeishu.AI",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>查询参数对象展开失败（DTO 无可序列化属性，模型无法得知可传字段）。</summary>
    public static readonly DiagnosticDescriptor MUDFT010 = new(
        id: "MUDFT010",
        title: "查询参数对象展开失败",
        messageFormat: "方法 {0}.{1} 的参数 {2}（类型 {3}）为复合查询参数但展开后无任何 [JsonPropertyName] 属性——模型无法得知可传字段",
        category: "MudFeishu.AI",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>Golden 快照 diff（描述符静默漂移）。</summary>
    public static readonly DiagnosticDescriptor MUDFT014 = new(
        id: "MUDFT014",
        title: "Golden 快照 diff",
        messageFormat: "工具描述符与 golden 快照不一致：{0}（使用 -p:FeishuToolGoldenUpdate=true 重新固化）",
        category: "MudFeishu.AI",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // ────────── 覆盖率计数的 Warning ──────────

    /// <summary>XML summary 缺失（description 为空）。</summary>
    public static readonly DiagnosticDescriptor MUDFT005 = new(
        id: "MUDFT005",
        title: "XML summary 缺失",
        messageFormat: "方法 {0}.{1} 缺少 XML <summary> 文档注释——工具 description 将为空",
        category: "MudFeishu.AI",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>参数缺 param 说明。</summary>
    public static readonly DiagnosticDescriptor MUDFT006 = new(
        id: "MUDFT006",
        title: "参数缺 param 说明",
        messageFormat: "方法 {0}.{1} 的参数 {2} 缺少 XML <param> 文档注释",
        category: "MudFeishu.AI",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>缺 [FeishuScopes]。</summary>
    public static readonly DiagnosticDescriptor MUDFT007 = new(
        id: "MUDFT007",
        title: "缺 FeishuScopes 注解",
        messageFormat: "方法 {0}.{1} 缺少 [FeishuScopes] 注解——_meta.scopes 将为空",
        category: "MudFeishu.AI",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>输出 Schema 深度截断 / 循环引用。</summary>
    public static readonly DiagnosticDescriptor MUDFT009 = new(
        id: "MUDFT009",
        title: "输出 Schema 深度截断或循环引用",
        messageFormat: "方法 {0}.{1} 的 OutputSchema 在路径 {2} 处被截断（深度超限或循环引用）",
        category: "MudFeishu.AI",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>AOT TypeInfoPropertyName 重复风险（SYSLIB1031）。</summary>
    public static readonly DiagnosticDescriptor MUDFT011 = new(
        id: "MUDFT011",
        title: "AOT TypeInfoPropertyName 重复风险",
        messageFormat: "类型 {0} 的 JSON TypeInfo 名称与 {1} 重复——可能触发 SYSLIB1031",
        category: "MudFeishu.AI",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>危险词命中却被 override 为非高风险。</summary>
    public static readonly DiagnosticDescriptor MUDFT012 = new(
        id: "MUDFT012",
        title: "危险词命中但 Risk 被 override 为非高风险",
        messageFormat: "方法 {0}.{1} 命中危险词 '{2}' 但 [FeishuToolRisk] override 为 {3}——须人工确认",
        category: "MudFeishu.AI",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>声明 [JsonPropertyName] 属性被深度/循环裁剪。</summary>
    public static readonly DiagnosticDescriptor MUDFT013 = new(
        id: "MUDFT013",
        title: "JsonPropertyName 属性被裁剪",
        messageFormat: "类型 {0} 的属性 {1} 标注了 [JsonPropertyName] 但因深度/循环被裁剪出 OutputSchema",
        category: "MudFeishu.AI",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    // ────────── Schema 内部一致性（L4 校验器产物；定义与上报点同源） ──────────

    /// <summary>Schema 内部不一致（<c>required</c> 不在 <c>properties</c> 键集内等）。</summary>
    public static readonly DiagnosticDescriptor MUDFT015 = new(
        id: "MUDFT015",
        title: "Schema 内部不一致",
        messageFormat: "工具 '{0}' 的 Schema 内部不一致：{1}",
        category: "MudFeishu.AI",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>工具身份与承载接口令牌类型不一致。</summary>
    public static readonly DiagnosticDescriptor MUDFT016 = new(
        id: "MUDFT016",
        title: "工具身份与接口令牌类型不一致",
        messageFormat: "工具 '{0}' 声明身份 {1}，但承载接口 {2} 的令牌类型不符",
        category: "MudFeishu.AI",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>读写分类与 SDK 事实脱钩（SDK 源为写/DELETE 但未标记为写工具）。</summary>
    public static readonly DiagnosticDescriptor MUDFT017 = new(
        id: "MUDFT017",
        title: "读写分类与 SDK 事实脱钩",
        messageFormat: "工具 '{0}' 未标记为写工具，但其 SDK 源 {1} 为 {2}（风险 {3}）——写面必须经授权门禁，不得归类为只读",
        category: "MudFeishu.AI",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // ────────── 源挂钩与覆盖报告 ──────────

    /// <summary>[FeishuTool] 声明的 <c>Source</c>（SDK 能力来源）无法解析。</summary>
    public static readonly DiagnosticDescriptor MUDFT019 = new(
        id: "MUDFT019",
        title: "SDK 源无法解析",
        messageFormat: "[FeishuTool] \"{0}\" 声明的 Source \"{1}\" 无法解析（{2}）——工具面与 SDK 不得脱钩",
        category: "MudFeishu.AI",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>AI 能力覆盖报告（聚合单条，避免逐方法刷屏）。</summary>
    public static readonly DiagnosticDescriptor MUDFT018 = new(
        id: "MUDFT018",
        title: "AI 能力覆盖报告",
        messageFormat: "SDK 能力 {0} 项；已策展工具 {1} 项（覆盖率 {2}）；覆盖能力分组 {3} 个。开启 build_property.FeishuToolCatalog 时产出此报告（Info 级，不进构建输出）",
        category: "MudFeishu.AI",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    // ────────── 零容忍集合 ──────────

    /// <summary>
    /// 零容忍诊断 ID 集合（构建期阻断）。
    /// </summary>
    /// <remarks>
    /// <b>纪律</b>：本集合中每个 ID <b>必须</b>在生产生成器中有真实上报点——由契约守卫
    /// <c>ZeroToleranceDiagnostics_ShouldHaveReportSites</c> 机械断言（防「定义即死代码」复发）。
    /// 新增零容忍项须同批补上报点与反例用例。
    /// </remarks>
    public static readonly string[] ZeroToleranceIds =
    [
        "MUDFT001", "MUDFT002", "MUDFT003", "MUDFT004",
        "MUDFT008", "MUDFT010", "MUDFT014", "MUDFT015",
        "MUDFT016", "MUDFT017", "MUDFT019"
    ];

    // ────────── 集中上报 ──────────

    /// <summary>上报单条诊断。</summary>
    public static void Report(SourceProductionContext context, DiagnosticDescriptor descriptor, Location? location, params object[] args)
    {
        context.ReportDiagnostic(Diagnostic.Create(descriptor, location ?? Location.None, args));
    }

    /// <summary>上报多条诊断。</summary>
    public static void ReportAll(SourceProductionContext context, IEnumerable<Diagnostic> diagnostics)
    {
        foreach (var diagnostic in diagnostics)
        {
            context.ReportDiagnostic(diagnostic);
        }
    }
}
