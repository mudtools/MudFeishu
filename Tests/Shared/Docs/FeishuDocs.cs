// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Tests.Docs;

/// <summary>
/// 受治理文档的<b>位置注册表</b>（单一真相源）——<b>测试库资产，与生产运行期无任何关系</b>。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么需要它</b>：契约守卫曾各自硬编码文档路径——搬一次文档要改十几处、漏一处即测试红，
/// 且没有任何机制提示"还有谁引用这个文档"（2026-10-13 迁移《工具错误契约》的实证：
/// 12 处引用全靠 grep 才发现）。与代码闭集同理：文档位置也只许有一个真相源。
/// </para>
/// <para>
/// <b>为什么在测试库而不是生产库</b>：它的唯一消费者是契约守卫（<c>File.Exists/ReadAllText</c>
/// 型测试），生产代码零引用——放在生产程序集里只会让"搬文档"被迫走一次生产库发布。
/// </para>
/// <para>
/// <b>为什么用 <c>Compile Include</c> 链接而不是建共享工程</b>：消费方横跨两个测试工程
/// （AI.Tools.Tests / AI.Tests），而 AI.Tests 按架构**不得**引用 AI.Tools（否则其自生成的
/// <c>FeishuToolSchemas</c> 会因双份声明产生 CS0433）。新建共享工程则会落入
/// <c>verify-build.ps1</c> 步骤 4 的"每个 Tests/** 工程都必须产出 TRX"断言（非测试工程必红）。
/// 链接编译是仓库既有先例（<c>FeishuToolProfile.cs</c> 同样以 Compile Include 进入 AI.Tests），
/// 且同一份物理文件编译进两个程序集不产生任何标识问题（各测试工程独立运行）。
/// </para>
/// <para>
/// <b>使用规则</b>：
/// <list type="bullet">
/// <item>新增受守卫守护的文档 ⇒ 在此登记一条（路径相对仓库根、用 <c>/</c> 分隔）；
/// <c>FeishuDocsRegistryContractGuards</c> 会反射本表逐条校验路径存在；</item>
/// <item>移动/改名文档 ⇒ <b>只改这里</b>，守卫失败消息会直接指向本文件；</item>
/// <item>消费侧（守卫）只经本表取路径，不得再出现字面量；</item>
/// <item>运行期/公开常量（如 MCP <c>ErrorContractSummary</c>、scopes 资源文本）只引<b>文档标题</b>、
///       不引路径——客户端的工作目录由客户端决定，仓库路径对运行期消费者无意义，
///       嵌进去只会让"搬文档"变成"公开 API 变更"。</item>
/// </list>
/// </para>
/// </summary>
internal static class FeishuDocs
{
    /// <summary>
    /// 《工具错误契约》——错误 category/subtype 闭集的对外承诺，
    /// 由 <c>ToolErrorDocContractGuards</c> 与代码闭集机械比对（防文档漂移）。
    /// </summary>
    public const string ToolErrorContract = "documents/AIAgent/工具错误契约.md";

    /// <summary>
    /// 《单元测试体系过度测试分析与解决方案》——测试体系治理（TFM 收敛、门禁分档、
    /// 守卫瘦身判据）的判据来源。
    /// </summary>
    public const string TestGovernanceReport = "documents/AIAgent/单元测试体系过度测试分析与解决方案.md";

    /// <summary>
    /// 《工具权限对照表》——工具名 ↔ scope 的对外承诺，
    /// 由 <c>PermissionMappingDocContractGuards</c> 与契约表机械比对。
    /// </summary>
    public const string ToolPermissionTable = "documents/AIAgent/工具权限对照表.md";

    /// <summary>
    /// <c>scope-authority.json</c>——scope 与 PII 工具清单的权威文件（人工从开放平台抄录），
    /// 由 <c>ScopeAuthorityContractGuards</c> / <c>PiiToolDisciplineContractGuards</c> 消费。
    /// </summary>
    public const string ScopeAuthority = "documents/AIAgent/scope-authority.json";

    /// <summary>
    /// <c>public-surface-rationale.json</c>——公开面冻结基线 + 消费方理由登记，
    /// 由 <c>PublicSurfaceRationaleContractGuards</c> 消费（公开面即产品面）。
    /// </summary>
    public const string PublicSurfaceRationale = "documents/AIAgent/public-surface-rationale.json";

    /// <summary>
    /// 《MCP多租户部署Runbook》——MCP 多租户部署手册，
    /// 由 <c>McpRunbookContractGuards</c> 断言存在与结构。
    /// </summary>
    public const string McpMultiTenantRunbook = "documents/AIAgent/MCP多租户部署Runbook.md";

    /// <summary>
    /// <c>decisions/ADR-0001</c>——会话摘要器与 MAF 压缩管线的取舍（决策记录的唯一家；
    /// 代码侧只留一行路径引用）。
    /// </summary>
    public const string AdrSummarizerVsMafCompaction =
        "documents/AIAgent/decisions/ADR-0001-summarizer-vs-maf-compaction.md";

    /// <summary>
    /// <c>comment-budget-baseline.json</c>——注释预算冻结基线（手写运行时层的注释行数、最长注释块、
    /// 未解析评审编号与过程叙述清单），由 <c>CommentConventionContractGuards</c> 消费并重新固化。
    /// </summary>
    public const string CommentBudgetBaseline = "documents/AIAgent/comment-budget-baseline.json";
}
