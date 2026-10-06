// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;

namespace Mud.Feishu.AI.Tools;

/// <summary>
/// 诊断描述符常量表。
/// </summary>
/// <remarks>
/// <para>
/// 零容忍集：<see cref="MUDFT001"/>/<see cref="MUDFT002"/>/<see cref="MUDFT003"/>/
/// <see cref="MUDFT004"/>/<see cref="MUDFT008"/>/<see cref="MUDFT010"/>/<see cref="MUDFT011"/>/<see cref="MUDFT014"/>/
/// <see cref="MUDFT015"/>/<see cref="MUDFT016"/>/<see cref="MUDFT017"/>/<see cref="MUDFT019"/>/
/// <see cref="MUDFT020"/>/<see cref="MUDFT022"/>/<see cref="MUDFT023"/>/<see cref="MUDFT024"/>/
/// <see cref="MUDFT025"/>/<see cref="MUDFT026"/>/<see cref="MUDFT027"/>（见 <see cref="ZeroToleranceIds"/>）。
/// </para>
/// <para>
/// <b>AT-B14 清理记录（R3 评审 C-2）</b>：本表原先还声明了
/// <c>MUDFT005/006/007/009/011/012/013</c>——其中 <b>007/011/012/013 引用的机制在仓库中根本不存在</b>
/// （<c>[FeishuScopes]</c> / <c>[FeishuToolRisk]</c> / AOT TypeInfoPropertyName 检测 / JsonPropertyName 裁剪追踪
/// 在全仓 <c>.cs</c> 中<b>仅命中本文件自身的描述文本</b>），属"指向不存在机制的僵尸定义"，
/// 已<b>整体删除</b>（死定义会让覆盖集看起来比实际更广）。
/// <c>005/006/009</c> 有真实消费点，已<b>接线</b>（005/006 见 <c>CuratedToolScanner</c>，
/// 009 见 <c>FeishuToolSchemaGenerator.ReportOutputSchemaTruncations</c>，聚合为单条）。
/// </para>
/// <para>
/// <b>纪律</b>：本表的定义集必须与生产源码的上报点集<b>完全相等</b>——由契约守卫
/// <c>Diagnostics_ShouldNotDeclareUnreportedDiagnostics</c> 机械断言（无单侧多余）。
/// </para>
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

    /// <summary>
    /// 工具名归一后的<b>派生常量名</b>冲突（<c>foo.bar_baz</c> 与 <c>foo.bar.baz</c> 都派生出 <c>FooBarBaz</c>）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="MUDFT003"/> 只保证工具名<b>字面</b>唯一；而 <c>FeishuToolNames</c> 的常量、<c>{Tool}Args</c>
    /// 类型名与其中间 hintName 都由 <c>SchemaEmitter.BuildNameConstant</c>（丢弃 <c>.</c>/<c>_</c>/<c>-</c>
    /// 后 PascalCase）派生——字面不同的两个名字可以归一到同一个派生名，产物随即撞成
    /// <c>CS0101</c>（重复常量/类型）或 <c>AddSource</c> 重复 hintName 异常（被 <c>MUDFT026</c> 兜底，报的是
    /// "生成器内部异常"这一表面症状）。
    /// </para>
    /// <para>
    /// <b>为什么不消歧而是报错</b>：派生常量是编译期契约标识符（守卫用它把生成类型映射回工具名），
    /// 静默加后缀会让两个"看起来一样"的工具各自持有一个常量，掩盖建模错误。改名是唯一正解。
    /// </para>
    /// </remarks>
    public static readonly DiagnosticDescriptor MUDFT027 = new(
        id: "MUDFT027",
        title: "工具名派生常量名冲突",
        messageFormat: "工具名 '{0}' 与 '{1}' 归一为同一编译期常量名 '{2}'——请改名使派生常量唯一",
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

    /// <summary>
    /// 条件必填组（<c>AnyOf</c>）引用了工具签名中<b>不存在</b>的参数名。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>为什么零容忍（R5 / B-6）</b>：<c>AnyOf = ["user_id|room_id"]</c> 这类声明会渲染成
    /// <c>"anyOf":[{"required":["user_id"]},{"required":["room_id"]}]</c>。若组内参数名拼错，
    /// 渲染出的约束<b>指向一个不存在的字段</b> ⇒ 该约束对模型<b>永久失效</b>且无任何症状
    /// （构建通过、Schema 合法、只是约束了空气）。这正是"静默失效"的标准形态，故设为 Error。
    /// </para>
    /// <para>
    /// <b>反向也校验</b>：<see cref="CuratedToolScanner"/> 同时要求组内参数<b>不能</b>是
    /// <c>Required = true</c> 的参数——那会让"至少一个"退化为"全部必填"，语义相反。
    /// </para>
    /// </remarks>
    public static readonly DiagnosticDescriptor MUDFT011 = new(
        id: "MUDFT011",
        title: "条件必填组引用了不存在的参数",
        messageFormat: "工具 {0} 的 AnyOf 组 \"{1}\" 引用了签名中不存在的参数 \"{2}\"（可用参数：{3}）——该约束不会生效，请修正参数名",
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

    /// <summary>
    /// 输出 Schema 深度截断 / 循环引用（<b>聚合上报</b>：一次编译最多一条）。
    /// </summary>
    /// <remarks>
    /// <b>保持 Warning 且不纳入 <see cref="ZeroToleranceIds"/></b>：截断是防 Schema 爆炸的有意设计，
    /// 且当前 SDK 必然存在深层 DTO——纳入零容忍会立即阻断构建（AT-B14 / R3 评审 C-2 的边界约定）。
    /// </remarks>
    public static readonly DiagnosticDescriptor MUDFT009 = new(
        id: "MUDFT009",
        title: "输出 Schema 深度截断或循环引用",
        messageFormat: "OutputSchema 截断（深度超限或循环引用）：{0}。样本：{1}",
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

    // ────────── 参数解包器映射（ToolArgsEmitter 产物） ──────────

    /// <summary>参数 C# 类型不在 <c>ToolArgs</c> 解包映射表内。</summary>
    /// <remarks>
    /// <b>扩展触发点</b>：新增参数类型时，先在 <c>ToolArgs</c> 补 helper、再在
    /// <c>ToolArgsEmitter.TryResolveReader</c> 登记映射，否则构建失败——比运行期
    /// <c>null</c> 静默穿透更早（生成产物会因缺少读取表达式而根本无法编译）。
    /// </remarks>
    public static readonly DiagnosticDescriptor MUDFT020 = new(
        id: "MUDFT020",
        title: "参数类型无解包映射",
        messageFormat: "工具 '{0}' 的参数 {1}（C# 类型 {2}）不在 ToolArgs 解包映射表内——须先在 ToolArgs 补 helper 并在 ToolArgsEmitter 登记映射",
        category: "MudFeishu.AI",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>必填参数被声明为可空（<c>required</c> 与解包语义不一致）。</summary>
    /// <remarks>
    /// Schema 会把它放进 <c>required</c>（模型必须给值），而 <c>Unpack</c> 读的是必填读取器——
    /// 该组合只在「<c>[ToolParameter(Required = true)]</c> 标在可空参数上」时出现，属声明自相矛盾：
    /// 要么去掉 <c>Required</c>（真可选），要么把参数类型改为非空（真必填）。
    /// </remarks>
    public static readonly DiagnosticDescriptor MUDFT021 = new(
        id: "MUDFT021",
        title: "必填参数被声明为可空",
        messageFormat: "工具 '{0}' 的参数 {1} 同时为 Required=true 与可空——Schema 的 required 与解包语义不一致（二选一：去掉 Required 或改为非空类型）",
        category: "MudFeishu.AI",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    // ────────── 注册器 / DI 装配产物（ToolRegistrarEmitter） ──────────

    /// <summary><c>[FeishuTool]</c> 工具没有任何 <c>[FeishuToolHandler]</c> 绑定。</summary>
    /// <remarks>
    /// <b>漂移守卫</b>：新增工具时若忘记在某个执行器方法上标注 handler，构建即失败——
    /// 取代此前「工具不进注册表 → 白名单期 fail-fast」的运行期定位（错误面再提前一层，
    /// 且不再依赖"注册表完整性"这条测试断言）。
    /// </remarks>
    public static readonly DiagnosticDescriptor MUDFT022 = new(
        id: "MUDFT022",
        title: "工具未绑定执行器方法",
        messageFormat: "[FeishuTool] 工具 '{0}' 没有 [FeishuToolHandler] 绑定——每个工具必须恰好绑定一个执行器方法",
        category: "MudFeishu.AI",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary><c>[FeishuToolHandler]</c> 绑定不成立（工具名不在契约表 / 同一工具被多个方法绑定）。</summary>
    /// <remarks>
    /// 两种形态都可运行期表现为「注册器把工具注册了两次（工具已注册）」或「绑定了不存在的契约名」，
    /// 故同属"绑定关系不成立"这一类，用同一 ID + 不同说明区分（避免零容忍集无谓膨胀）。
    /// </remarks>
    public static readonly DiagnosticDescriptor MUDFT023 = new(
        id: "MUDFT023",
        title: "执行器绑定不成立",
        messageFormat: "[FeishuToolHandler] 绑定不成立：{0}——{1}",
        category: "MudFeishu.AI",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>执行器方法签名不符合执行器契约。</summary>
    public static readonly DiagnosticDescriptor MUDFT024 = new(
        id: "MUDFT024",
        title: "执行器方法签名不符",
        messageFormat: "工具 '{0}' 的执行器方法 {1} 签名不符——应为 public Task<FeishuToolResult> 方法(IReadOnlyDictionary<string, object?> args, CancellationToken ct)，实际 {2}",
        category: "MudFeishu.AI",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>执行器构造参数无法作为 DI 服务类型解析。</summary>
    public static readonly DiagnosticDescriptor MUDFT025 = new(
        id: "MUDFT025",
        title: "执行器构造参数无法解析",
        messageFormat: "工具 '{0}' 的执行器 {1} 的构造参数 {2}（类型 {3}）无法作为 DI 服务类型解析——仅支持类/接口（非开放泛型、非元组、非值类型）",
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

    /// <summary>
    /// 生成器内部异常兜底（W2：故障隔离）。
    /// </summary>
    /// <remarks>
    /// 生成器抛出未捕获异常时 Roslyn 会产出 <c>CS8785</c>（"Generator failed"）——
    /// 该诊断不在零容忍集内，不会阻断构建，但工具面会不完整且<b>无任何可定位提示</b>。
    /// 本诊断在生成器 catch 块中上报，使故障可定位（含程序集名、异常类型与消息）。
    /// </remarks>
    public static readonly DiagnosticDescriptor MUDFT026 = new(
        id: "MUDFT026",
        title: "工具面生成器内部异常",
        messageFormat: "生成器内部异常（程序集 {0}）：{1}: {2}——已兜底，工具面本次不完整，请按堆栈修复生成器",
        category: "MudFeishu.Tooling",
        defaultSeverity: DiagnosticSeverity.Error,
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
        "MUDFT027",
        "MUDFT008", "MUDFT010", "MUDFT011", "MUDFT014", "MUDFT015",
        "MUDFT016", "MUDFT017", "MUDFT019", "MUDFT020",
        "MUDFT022", "MUDFT023", "MUDFT024", "MUDFT025",
        "MUDFT026"
    ];

    // ────────── 集中上报 ──────────

    /// <summary>上报单条诊断。</summary>
    public static void Report(SourceProductionContext context, DiagnosticDescriptor descriptor, Location? location, params object[] args)
    {
        context.ReportDiagnostic(Diagnostic.Create(descriptor, location ?? Location.None, args));
    }

}
