// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

// 契约命名空间实测（R-1+2c）：标记接口 ISdkToolProfile / SdkTokenKind / InterfaceIdentity 位于
// 包 Mud.HttpUtils.Abstractions 内，但**命名空间是 Mud.HttpUtils**（不是 Mud.HttpUtils.Abstractions）；
// 数据特性 [SdkToolProfile] 位于包 Mud.HttpUtils.Attributes，命名空间 Mud.HttpUtils.Attributes。
using Mud.HttpUtils;
using Mud.HttpUtils.Attributes;

namespace Mud.Feishu.AI.FeishuTools.SdkProfile;

/// <summary>
/// 飞书工具生成剖面：把「<c>Mud.Feishu</c> 命名事实」收拢成上游通用工具 Schema 生成引擎
/// （<c>Mud.HttpUtils.Generator</c> 的 <c>ToolSurface</c> 家族）可消费的编译期剖面数据。
/// </summary>
/// <remarks>
/// <para>
/// <b>这一份类 = 旧本地引擎 <c>Mud.Feishu.AI.Tools</c> 全部飞书硬编码的收拢</b>（R-1+2c 迁移）。
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
/// 凡声明了 <c>[FeishuTool]</c> 接口、且需要生成产物落在自己程序集内的工程，都必须携带本类型
/// （当前：<c>Mud.Feishu.AI.FeishuTools</c> 本体 + <c>Tests/Mud.Feishu.AI.Tests</c> 样例工程，
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
    ToolAttributeName = "FeishuTool",
    ToolAttributeNamespace = "Mud.Feishu.AI.Tools",
    ToolHandlerAttributeName = "FeishuToolHandler",
    // 注意：handler 特性与工具特性**不同命名空间**（旧 ToolHandlerScanner.AttributeNamespace）。
    ToolHandlerAttributeNamespace = "Mud.Feishu.AI.FeishuTools",
    ParameterAttributeName = "ToolParameter",

    // ────────── 2. 源符号解析 ──────────
    SdkNamespaceRoot = "Mud.Feishu",

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
    WriteVerbKeywords = "delete|remove|transfer|cancel|revoke|resign|permission|reset_secret|password|dismiss|purge|wipe",

    // ────────── 8-11. 描述符与契约产物事实 ──────────
    SchemaExtensionKey = "x-feishu",
    OwnerAssembly = "Mud.Feishu.AI.FeishuTools",
    GeneratedNamespace = "Mud.Feishu.AI.Tools.Generated",
    ContractNamespace = "Mud.Feishu.AI.FeishuTools",
    RegistrationNamespace = "Mud.Feishu.AI.FeishuTools.Registration",
    RiskEnumFullName = "Mud.Feishu.AI.Tools.FeishuToolRisk",

    // ────────── 12-17. 执行器 / 输出 / 聚合事实 ──────────
    ResultTypeName = "FeishuToolResult",
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
    GuidanceDirectory = "/Guidance/")]
internal sealed class FeishuToolProfile : ISdkToolProfile
{
    // 空标记实现：数据全部由 [SdkToolProfile] 承载（引擎只能读编译期常量，不能执行 getter）。
}
