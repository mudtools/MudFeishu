// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.HttpUtils 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.AI;
using Mud.AI.Attributes;

namespace ToolSurfaceDemo;

/// <summary>
/// 工具面生成剖面：一份编译期常量声明，驱动 <see cref="ToolSurfaceSourceGenerator"/>（由
/// Mud.HttpUtils.Generator 以分析器形态接入）产出「模型可调用工具」的全部编译期产物。
/// </summary>
/// <remarks>
/// <para>
/// 成对义务：实现 <see cref="ISdkToolProfile"/> 就必须标注 <see cref="SdkToolProfileAttribute"/>
/// （反之亦然），由编译期守卫 SDKT001 强制；必填槽（Name / ToolAttributeName / ToolAttributeNamespace /
/// SdkNamespaceRoot / ProductPrefix / DiagnosticPrefix / DiagnosticCategory）缺失则报 SDKT002 并拒绝产出。
/// </para>
/// <para>
/// 槽位与 demo 程序集/SDK 的对应关系：
/// <list type="bullet">
/// <item><c>OwnerAssembly = "ToolSurfaceDemo"</c>：本工程程序集名——Names/Contracts/Args/Registrars/Guidance 只发射进它；</item>
/// <item><c>SdkNamespaceRoot = "Mud.Demo.Sdk"</c>：SDK 工程程序集名——Tier R 能力目录按此定位 SDK；</item>
/// <item>本工程不含真实 SDK 传输层：SDK 接口只承载 [Get]/[Delete]/[Path] 编译期事实，
/// 运行时由 <c>InMemoryDocSdk</c> 提供内存实现。</item>
/// </list>
/// </para>
/// <para>
/// <b>golden 快照</b>：声明了 <c>GoldenFileName</c>/<c>GoldenUpdatePropertyName</c> 但暂未提交快照文件时，
/// 引擎不报漂移（缺文件 ≠ 不一致）；首次固化流程：把生成的 <c>DemoToolSchemas.g.cs</c> 中
/// 各 <c>SchemaJson</c> 常量按「工具名⇥JSON」逐行落盘为 <c>DemoToolSchemas.golden.txt</c> 并加入
/// AdditionalFiles，之后任何描述符变化都会以槽位 014（Error）显式暴露，并可用
/// <c>-p:DemoToolGoldenUpdate=true</c> 重新固化。
/// </para>
/// </remarks>
[SdkToolProfile("Demo",
    // ── 1. 特性识别（扫描目标）──
    ToolAttributeName = "DemoTool",
    ToolAttributeNamespace = "Mud.Demo.Tools",
    ToolHandlerAttributeName = "DemoToolHandler",
    ToolHandlerAttributeNamespace = "Mud.Demo.Tools",
    ParameterAttributeName = "ToolParameter",
    // ── 2. 源符号解析（Source 挂钩 + Tier R 程序集定位）──
    SdkNamespaceRoot = "Mud.Demo.Sdk",
    // ── 3. 接口命名范式（domain 段解析）──
    InterfaceNameRegex = "^IMudDemo(?:Tenant|User)?V[0-9]+(?<domain>[A-Za-z]+)$",
    // ── 4. 令牌身份推导（承载接口名前缀 → tenant/user）──
    TokenKindStrategy = TokenKindDerivationStrategy.NamePrefix,
    TokenKindMarkers = "IMudDemoTenant=Tenant;IMudDemoUser=User",
    // ── 5-7. 产物前缀 / 诊断 / 危险词 ──
    ProductPrefix = "DemoTool",
    ProductPluralPrefix = "DemoTools",
    DiagnosticPrefix = "MUDDEMO",
    DiagnosticCategory = "MudDemo.AI",
    WriteVerbKeywords = "delete|create",
    // ── 8-11. 描述符与契约产物事实 ──
    SchemaExtensionKey = "x-demo",
    OwnerAssembly = "ToolSurfaceDemo",
    GeneratedNamespace = "Mud.Demo.Tools.Generated",
    ContractNamespace = "Mud.Demo.Tools",
    RegistrationNamespace = "Mud.Demo.Tools.Registration",
    RiskEnumFullName = "Mud.Demo.Tools.DemoToolRisk",
    // ── 12-17. 执行器 / 输出 / 聚合事实 ──
    ResultTypeName = "DemoToolResult",
    BindingTypeName = "DemoToolBinding",
    SdkInterfacePrefix = "IMudDemo",
    // ── 开关与资产 ──
    CapabilityCatalogPropertyName = "DemoToolCatalog",
    GoldenFileName = "DemoToolSchemas.golden.txt",
    GoldenUpdatePropertyName = "DemoToolGoldenUpdate",
    GuidanceDirectory = "/Guidance/",
    // per-tool affordance：文件名 = 契约工具名（doc.list.md / doc.delete.md），
    // 内容进 x-demo.affordance（含 risk/is_write/identity 的同一扩展块）。
    AffordanceDirectory = "/Affordance/")]
sealed class DemoToolProfile : ISdkToolProfile
{
}
