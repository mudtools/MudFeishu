// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.AI.Attributes;

/// <summary>
/// SDK 工具生成剖面数据载体：承载工具 Schema 生成引擎所需的<b>全部</b>编译期命名事实常量。
/// </summary>
/// <remarks>
/// <para>
/// v2.3 随引擎进仓（原 <c>Mud.HttpUtils.Attributes.SdkToolProfileAttribute</c>）：
/// 引擎（<c>ProfileContractGuard</c> / <c>SdkToolProfileModel</c>）以<b>硬编码命名空间
/// <c>Mud.AI.Attributes</c></b> 识别本特性——命名空间即契约，改名须同批改引擎常量。
/// </para>
/// <para>
/// 与 <see cref="ISdkToolProfile"/>（标记接口，命名空间 <c>Mud.AI</c>）成对使用：标了本特性就必须实现该接口，反之亦然，
/// 由编译期守卫诊断 <c>SDKT001</c> 强制。引擎经 <c>AttributeData</c> 读取本特性的构造参数与命名参数，
/// 产出编译期值对象 <c>SdkToolProfileModel</c>。
/// </para>
/// <para>
/// <b>为什么全部是字符串常量</b>：源生成器运行在编译器进程内，无法执行任何用户代码，
/// 只能读编译期常量。因此「策略」也只能表达为枚举值 + 分隔串标记表，而非委托。
/// </para>
/// <para>
/// <b>有意<b>不</b>入槽的派生名</b>：产物名中凡是能由
/// <see cref="ProductPrefix"/> / <see cref="ProductPluralPrefix"/> 完全推导的，都不设槽：
/// <c>{P}Schemas</c>、<c>{P}Names</c>、<c>{P}Contracts</c>、<c>{P}Args/*</c>、
/// <c>{P}DomainRegistrars/*</c>、<c>{P}Guidance</c>、<c>{Plural}ServiceCollectionCoreExtensions</c>、
/// <c>{P}CapabilityCatalog</c>。消费方若在源码中<b>硬引用</b>这些类型，必须按派生名书写，否则 CS0103。
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [SdkToolProfile("Feishu",
///     SdkNamespaceRoot = "Mud.Feishu",
///     ProductPrefix = "FeishuTool",
///     DiagnosticPrefix = "MUDFT",
///     DiagnosticCategory = "MudFeishu.AI",
///     ToolAttributeName = "AgentTool",
///     ToolAttributeNamespace = "Mud.AI.Abstractions.Tools",
///     TokenKindStrategy = TokenKindDerivationStrategy.NamePrefix,
///     TokenKindMarkers = "IFeishuTenant=Tenant;IFeishuUser=User",
///     SchemaExtensionKey = "x-feishu",
///     OwnerAssembly = "Mud.Feishu.AI.Tools",
///     /* …其余槽见设计文档清单… */)]
/// sealed class FeishuToolProfile : ISdkToolProfile { }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class SdkToolProfileAttribute : Attribute
{
    /// <summary>
    /// 构造剖面声明。
    /// </summary>
    /// <param name="name">SDK 名（<c>Feishu</c> / <c>Wechat</c>）；同一编译内唯一，用于多剖面排序与诊断定位。</param>
    /// <exception cref="ArgumentException"><paramref name="name"/> 为 null 或空白。</exception>
    public SdkToolProfileAttribute(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("SDK 名不可为空。", nameof(name));
        }

        Name = name;
    }

    /// <summary>SDK 名（<c>Feishu</c> / <c>Wechat</c>），同一编译内唯一。</summary>
    public string Name { get; }

    // ────────── 1. 特性识别（扫描目标） ──────────

    /// <summary>工具特性简单名（如 <c>AgentTool</c>，引擎按 <c>{Name}Attribute</c> 匹配）。</summary>
    public string ToolAttributeName { get; set; } = string.Empty;

    /// <summary>工具特性所在命名空间（如 <c>Mud.AI.Abstractions.Tools</c>，防同名特性误命中）。</summary>
    public string ToolAttributeNamespace { get; set; } = string.Empty;

    /// <summary>执行器特性简单名（如 <c>ToolHandler</c>）。</summary>
    public string ToolHandlerAttributeName { get; set; } = string.Empty;

    /// <summary>
    /// 执行器特性所在命名空间。
    /// </summary>
    /// <remarks>
    /// <b>不得复用 <see cref="ToolAttributeNamespace"/></b>：引擎靠 (命名空间, 特性名) 二元组
    /// 区分「声明面」与「执行器绑定面」，两者塌陷为同值即失去可寻址性。
    /// </remarks>
    public string ToolHandlerAttributeNamespace { get; set; } = string.Empty;

    /// <summary>参数特性简单名（如 <c>ToolParameter</c>）。</summary>
    public string ParameterAttributeName { get; set; } = string.Empty;

    // ────────── 2. 源符号解析 ──────────

    /// <summary>SDK 根命名空间（<c>Mud.Feishu</c> / <c>Mud.Wechat</c>），用于 <c>Source</c> 挂钩的类型名解析。</summary>
    public string SdkNamespaceRoot { get; set; } = string.Empty;

    // ────────── 3. 接口命名范式 ──────────

    /// <summary>接口名解析正则（含 <c>domain</c>/<c>resource</c> 命名组；飞书 <c>^IFeishu(?:Tenant|User)?V\d+…$</c>）。</summary>
    public string InterfaceNameRegex { get; set; } = string.Empty;

    // ────────── 4. 令牌身份推导 ──────────

    /// <summary>令牌身份推导策略（前缀 / 后缀 / 中缀）。</summary>
    public TokenKindDerivationStrategy TokenKindStrategy { get; set; } = TokenKindDerivationStrategy.Unspecified;

    /// <summary>
    /// 「标记串=<see cref="SdkTokenKind"/> 成员名」表，以 <c>;</c> 分隔。
    /// 例：飞书 <c>"IFeishuTenant=Tenant;IFeishuUser=User"</c>；
    /// 微信 <c>"_Provider=ThirdParty;_ThirdParty=ThirdParty;_Internal=Internal"</c>。
    /// </summary>
    /// <remarks>
    /// <b>适用面（引擎对两侧都查）</b>：标记表既用于 ①<b>SDK 接口</b>（<c>Source</c> 挂钩的目标），
    /// 也用于 ②<b>工具的承载接口</b>（声明 <c>[AgentTool]</c> 方法的那个接口）：
    /// 把「工具声明的 identity」与「承载接口名按本表推导出的令牌类型」比对，不一致即 Error。
    /// ⇒ <b>承载接口名自身必须携带标记串</b>（如 <c>IFeishuTenant*Tool</c> / <c>IFeishuUser*Tool</c>）。
    /// </remarks>
    public string TokenKindMarkers { get; set; } = string.Empty;

    // ────────── 5-7. 产物前缀 / 诊断 / 危险词 ──────────

    /// <summary>产物前缀（<c>FeishuTool</c>），驱动 <c>{P}Schemas</c>/<c>{P}Args</c> 等产物名。</summary>
    public string ProductPrefix { get; set; } = string.Empty;

    /// <summary>复数产物前缀（<c>FeishuTools</c>，驱动 DI 装配产物名）。<b>不等于</b> <see cref="ProductPrefix"/> + "s" 的推导假设，独立成槽。</summary>
    public string ProductPluralPrefix { get; set; } = string.Empty;

    /// <summary>诊断 ID 前缀（<c>MUDFT</c> / <c>MUDWX</c>），与固定槽位序号拼成最终 ID。</summary>
    public string DiagnosticPrefix { get; set; } = string.Empty;

    /// <summary>诊断类别（<c>MudFeishu.AI</c>）。注意：兜底槽（026）用 <see cref="ToolingDiagnosticCategory"/>。</summary>
    public string DiagnosticCategory { get; set; } = string.Empty;

    /// <summary>生成器内部异常兜底诊断的类别（飞书为 <c>MudFeishu.Tooling</c>，与其余诊断不同类别）。</summary>
    public string ToolingDiagnosticCategory { get; set; } = string.Empty;

    /// <summary>危险词表（<c>|</c> 分隔，snake_case 命中即 high-risk-write）。只匹配方法名，不匹配路由。</summary>
    public string WriteVerbKeywords { get; set; } = string.Empty;

    // ────────── 8-11. 描述符与契约产物事实 ──────────

    /// <summary>描述符信封的厂商扩展键（<c>x-feishu</c> / <c>x-wechat</c>）。<see cref="SchemaExtensionKey"/> 进逐字节 golden。</summary>
    public string SchemaExtensionKey { get; set; } = string.Empty;

    /// <summary>
    /// 名字契约 / Contracts / Args / DomainRegistrars 四族产物的 owner 程序集名
    /// （如 <c>Mud.Feishu.AI.Tools</c>）。<b>不可由 <see cref="ProductPrefix"/> 推导</b>。
    /// </summary>
    public string OwnerAssembly { get; set; } = string.Empty;

    /// <summary>Schemas / Contracts 产物命名空间（如 <c>Mud.Feishu.AI.Tools.Generated</c>）。</summary>
    public string GeneratedNamespace { get; set; } = string.Empty;

    /// <summary>Names / Args 产物命名空间（如 <c>Mud.Feishu.AI.Tools</c>）。</summary>
    public string ContractNamespace { get; set; } = string.Empty;

    /// <summary>域注册器命名空间（如 <c>Mud.Feishu.AI.Tools.Registration</c>）。</summary>
    public string RegistrationNamespace { get; set; } = string.Empty;

    /// <summary>风险枚举的消费方全名（如 <c>Mud.Feishu.AI.AgentTools.FeishuToolRisk</c>），契约表字面量引用。</summary>
    public string RiskEnumFullName { get; set; } = string.Empty;

    // ────────── 12-17. 执行器 / 输出 / 聚合事实 ──────────

    /// <summary>执行器返回类型简单名（签名契约 <c>Task&lt;{ResultTypeName}&gt;</c>，如 <c>ToolResult</c>）。</summary>
    public string ResultTypeName { get; set; } = string.Empty;

    /// <summary>绑定类型名（域注册器构造实参，如 <c>FeishuToolBinding</c>）。</summary>
    public string BindingTypeName { get; set; } = string.Empty;

    /// <summary>返回包装类型所在命名空间（output schema 解包表，如 <c>Mud.Feishu.DataModels</c>）。</summary>
    public string OutputWrapperNamespace { get; set; } = string.Empty;

    /// <summary>返回包装泛型类型简单名（<c>|</c> 分隔，如 <c>FeishuApiResult|FeishuApiPageListResult</c>）。</summary>
    public string OutputWrapperTypeNames { get; set; } = string.Empty;

    /// <summary>SDK 接口扫描前缀（能力目录聚合 <c>StartsWith</c> 判定，如 <c>IFeishu</c>）。</summary>
    public string SdkInterfacePrefix { get; set; } = string.Empty;

    // ────────── 开关与资产 ──────────

    /// <summary>能力目录开关的 MSBuild 属性名（如 <c>FeishuToolCatalog</c>，消费方须自行 <c>CompilerVisibleProperty</c> 注册）。</summary>
    public string CapabilityCatalogPropertyName { get; set; } = string.Empty;

    /// <summary>golden 快照文件名（<b>全名</b>匹配，如 <c>FeishuToolSchemas.golden.txt</c>；后缀匹配会误吞同工程其他 golden）。</summary>
    public string GoldenFileName { get; set; } = string.Empty;

    /// <summary>golden 更新属性名（如 <c>FeishuToolGoldenUpdate</c>，供 <c>-p:…=true</c> 重新固化）。</summary>
    public string GoldenUpdatePropertyName { get; set; } = string.Empty;

    /// <summary>guidance 资产目录片段（如 <c>/Guidance/</c>，AdditionalFiles 路径匹配）。</summary>
    public string GuidanceDirectory { get; set; } = string.Empty;

    /// <summary>
    /// per-tool affordance 资产目录片段（如 <c>/Affordance/</c>，AdditionalFiles 路径匹配）。
    /// 空串（默认）= 该剖面<b>不启用</b>本特性 ⇒ 描述符不发射 affordance 子块，既有 golden 逐字节不变。
    /// </summary>
    /// <remarks>
    /// <b>资产形态</b>：<c>{AffordanceDirectory}{契约工具名}.md</c>（如 <c>/Affordance/bitable.list_tables.md</c>），
    /// 文件名去掉 <c>.md</c> 后必须<b>逐字符等于</b>契约工具名，否则资产不会生效——由引擎 Error 诊断机械拦下。
    /// <b>与 <see cref="GuidanceDirectory"/> 的分工</b>：guidance 是<b>域级/跨工具</b>的工作流与路由；
    /// affordance 是<b>单工具</b>的选型与前置条件。同一结论不得在两处给出互斥表述。
    /// </remarks>
    public string AffordanceDirectory { get; set; } = string.Empty;
}
