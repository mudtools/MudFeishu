// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

// 契约命名空间实测：标记接口 ISdkToolProfile / SdkTokenKind / InterfaceIdentity 的**命名空间是
// Mud.AI**（不是包/程序集名后缀）；数据特性 [SdkToolProfile] 在命名空间 Mud.AI.Attributes。
// v2.3 P1-E：二者随「引导/声明契约」由组件包 Mud.HttpUtils.* 迁入本仓 Mud.AI.Abstractions，
// 剖面改引仓内契约（引擎 ProfileDiscovery 按 `Mud.AI.ISdkToolProfile` +
// `Mud.AI.Attributes.SdkToolProfileAttribute` 发现剖面，指向组件包会零产出）。
using Mud.AI;
using Mud.AI.Attributes;

namespace Mud.Feishu.AI.Tools.SdkProfile;

/// <summary>
/// 飞书工具生成剖面：把「<c>Mud.Feishu</c> 命名事实」收拢成上游通用工具 Schema 生成引擎
/// （<c>Mud.HttpUtils.Generator</c> 的 <c>ToolSurface</c> 家族）可消费的编译期剖面数据。
/// </summary>
/// <remarks>
/// <para>
/// <b>这一份类 = 已删除的本地生成器引擎全部飞书硬编码的收拢</b>（R-1+2c 迁移）。
/// 迁移前这些事实散落在 11 个文件里：<c>Extractors</c>（特性名/命名空间/源解析/接口正则/危险词）、
/// <c>CuratedToolScanner</c>（令牌身份前缀）、<c>SchemaEmitter</c>（owner 程序集/产物命名空间/风险枚举全名）、
/// <c>ToolHandlerScanner</c>（handler 特性命名空间/返回类型名）、<c>ToolArgsEmitter</c>/<c>ToolRegistrarEmitter</c>
/// （Args 目录/注册器命名空间/绑定类型名）、<c>CapabilityCatalogEmitter</c>（SDK 程序集名/接口前缀）、
/// <c>SchemaWriter</c>（<c>x-feishu</c> 扩展键）、<c>TypeSchemaResolver</c>（返回包装解包表）、
/// <c>Diagnostics</c>（<c>MUDFT</c>/<c>MudFeishu.AI</c>）。
/// </para>
/// <para>
/// <b>为什么每个槽的值都必须逐字节忠实</b>：这些值直接进 golden 快照、生成代码（命名空间/类型名）
/// 与诊断 ID。任一槽写错都不会编译失败，而是表现为 golden 漂移、<c>CS0246/CS0103</c>
/// 或诊断口径分叉——故 <c>FeishuToolProfileContractGuards</c> 以反射逐槽锁定这些值。
/// </para>
/// <para>
/// <b>发现粒度</b>：剖面经 <c>CompilationProvider</c> 按<b>编译单元</b>发现 ⇒
/// 凡声明了 <c>[AgentTool]</c> 接口、且需要生成产物落在自己程序集内的工程，都必须携带本类型
/// （当前：<c>Mud.Feishu.AI.Tools</c> 本体 + <c>Tests/Mud.Feishu.AI.Tests</c> 样例工程，
/// 后者以 <c>Compile Include</c> 链接本文件共享同一份真相源）。
/// </para>
/// <para>
/// <b>owner 门槛</b>：<c>OwnerAssembly</c> 槽才是「名字契约/契约表/Args/域注册器/Guidance」五族产物的
/// 发射门槛（<c>{P}Schemas</c> 不受限）。非 owner 程序集（如 AI.Tests）只得到 <c>FeishuToolSchemas</c>。
/// </para>
/// </remarks>
[SdkToolProfile(
    "Feishu",

    // ────────── 1. 特性识别（扫描目标）──────────
    // v2.3 P1-E：声明契约中立化（决策：工具声明面 = 产品族中立）——
    // [FeishuTool] → [AgentTool]、[FeishuToolHandler] → [ToolHandler]，两者随声明契约
    // 上移 `Mud.AI.Abstractions`（`Mud.AI.Abstractions.Tools(.Handlers)`）。
    // 门槛理由不变（BUG-1 / 方案 C）：声明契约必须能被**任何声明工具的程序集**看到，
    // 包括非 owner 声明程序集（如 Tests/Mud.Feishu.AI.Tests 自带同一份剖面、在同名命名空间
    // 生成自己的 Schemas，不能引用 owner 程序集否则 CS0433）——`Mud.AI.Abstractions`
    // 位于依赖图最底层，天然满足该门槛（比原 Mud.Feishu.AI 更靠下，覆盖面只增不减）。
    ToolAttributeName = "AgentTool",
    ToolAttributeNamespace = "Mud.AI.Abstractions.Tools",
    ToolHandlerAttributeName = "ToolHandler",
    // handler 特性与工具特性**必须处于不同命名空间**，否则引擎无法靠 (命名空间, 特性名)
    // 二元组把 [AgentTool]（声明面）与 [ToolHandler]（执行器绑定面）区分开
    // ——故 handler 特性独占 `.Tools.Handlers` 子命名空间以保住该可寻址性。
    ToolHandlerAttributeNamespace = "Mud.AI.Abstractions.Tools.Handlers",
    ParameterAttributeName = "ToolParameter",

    // ────────── 2. 源符号解析 ──────────
    SdkNamespaceRoot = "Mud.Feishu",
    // 中立化（N1/N2）：执行器的**软缺席**依赖不再只住在 `Mud.Feishu*` 里——
    // 知识检索器（`Mud.AI.Abstractions.Knowledge.IRetriever`）等中立契约同样表达
    // 「该能力域缺席 ⇒ 该域工具不进注册表」。不放宽 SdkNamespaceRoot 而新增本槽，
    // 是因为前者还承担「SDK 程序集定位」（按程序集名 == 根名）与 `Source` 前缀剥离，
    // 放宽到 "Mud" 会让程序集定位去找名为 Mud 的程序集而静默零产出。
    SoftAbsentNamespaceRoots = "Mud.AI",

    // ────────── 3. 接口命名范式（与旧 Extractors.SdkInterfaceNameRegex 逐字符一致）──────────
    InterfaceNameRegex = @"^IFeishu(?:Tenant|User)?V\d+(?<domain>[A-Z][a-zA-Z0-9]*)(?<resource>[A-Z][a-zA-Z0-9]*)?$",

    // ────────── 4. 令牌身份推导（旧 DeriveIdentityFromSource 的 StartsWith 前缀语义）──────────
    TokenKindStrategy = TokenKindDerivationStrategy.NamePrefix,
    TokenKindMarkers = "IFeishuTenant=Tenant;IFeishuUser=User",

    // ────────── 5-7. 产物前缀 / 诊断 / 危险词 ──────────
    ProductPrefix = "FeishuTool",
    ProductPluralPrefix = "FeishuTools",
    DiagnosticPrefix = "MUDFT",
    DiagnosticCategory = "MudFeishu.AI",
    // MUDFT026（生成器内部异常兜底）在上游与其余诊断**不同类别**。
    ToolingDiagnosticCategory = "MudFeishu.Tooling",
    // 旧 Extractors.DangerWords（方法名 snake_case 子串命中即 high-risk-write）。
    // urgent（2026-10-28 T-6）：加急是强打扰动作（客户端弹窗/短信双通道），
    // 与删除同级打扰面，按行为强度而非仅名字词表收录。
    WriteVerbKeywords = "delete|remove|transfer|cancel|revoke|resign|permission|reset_secret|password|dismiss|purge|wipe|urgent",

    // ────────── 8-11. 描述符与契约产物事实 ──────────
    SchemaExtensionKey = "x-feishu",
    OwnerAssembly = "Mud.Feishu.AI.Tools",
    GeneratedNamespace = "Mud.Feishu.AI.Tools.Generated",
    ContractNamespace = "Mud.Feishu.AI.Tools",
    RegistrationNamespace = "Mud.Feishu.AI.Tools.Registration",
    // BUG-1 / 方案 C：风险枚举由 AI 侧（FeishuAgentOptions 策略轴、FeishuToolDiagnostics、
    // FeishuToolRiskNames）与工具面（契约 Risk 属性）**共同消费**，按依赖方向必须落在
    // Mud.Feishu.AI 程序集的 Mud.Feishu.AI.AgentTools 命名空间（工具包单向引用 AI）。
    RiskEnumFullName = "Mud.Feishu.AI.AgentTools.FeishuToolRisk",

    // ────────── 12-17. 执行器 / 输出 / 聚合事实 ──────────
    // v2.3 P1-D：执行器骨架中立化上移 Mud.AI.Agent.Tools（决策⑮）——FeishuToolResult → ToolResult
    // 硬改名（AI 族未发布，无 Obsolete 转发）；生成产物（域注册器/Args 的 handler 签名）随槽同批改。
    ResultTypeName = "ToolResult",
    BindingTypeName = "FeishuToolBinding",
    OutputWrapperNamespace = "Mud.Feishu.DataModels",
    // 旧 TypeSchemaResolver 的 4 个泛型信封 + FeishuNullDataApiResult（非泛型 → 无载荷）。
    // 引擎把「4 个泛型」的三种分支收敛为一条结构规则（上溯 Data 属性 → 回退单泛型实参）：
    // 对 FeishuApiListResult<T> 属**有意收敛**（旧实现返回信封自身），登记见迁移方案 §七。
    OutputWrapperTypeNames = "FeishuApiResult|FeishuApiPageListResult|FeishuApiListResult|FeishuApiPageListTotalResult|FeishuNullDataApiResult",
    SdkInterfacePrefix = "IFeishu",

    // ────────── 开关与资产 ──────────
    CapabilityCatalogPropertyName = "FeishuToolCatalog",
    GoldenFileName = "FeishuToolSchemas.golden.txt",
    GoldenUpdatePropertyName = "FeishuToolGoldenUpdate",
    GuidanceDirectory = "/Guidance/",

    // R-3 / F-7（上游 3.0.6 新增槽）：per-tool affordance 资产目录片段。
    // 资产 = `Affordance/{契约工具名}.md`（四节 WHEN / Avoid when / Prerequisites / Tips，
    // 各节非空）⇒ 由引擎同 pass 发射进 `x-feishu.affordance`（四键恒出现，位置在 source 之后、
    // output_schema 之前），并对以下三类失败报 Error 级诊断：028 结构非法 / 029 文件名不命中工具名 /
    // 030 反引号包裹的 `域.工具` 引用不存在（预算超界 = 031 Warning，走基线拦增量）。
    // 与 GuidanceDirectory 的分工：guidance 是**域级**工作流（键 = 域/主题，按域读取、按需拉取）；
    // affordance 是**单工具**的选型与前置条件（随描述符推给模型可见面）。同一结论不得两处互斥表述。
    AffordanceDirectory = "/Affordance/")]
internal sealed class FeishuToolProfile : ISdkToolProfile
{
    // 空标记实现：数据全部由 [SdkToolProfile] 承载（引擎只能读编译期常量，不能执行 getter）。
}
