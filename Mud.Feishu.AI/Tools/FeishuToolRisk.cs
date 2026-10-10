// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.AgentTools;

/// <summary>
/// 工具风险分级：<b>唯一真相源是编译期 Schema 的 <c>x-feishu.risk</c></b>
/// （由源生成器从 SDK 事实派生：危险词 → <c>high-risk-write</c>；<c>PUT/PATCH/DELETE</c> → <c>write</c>；
/// 其余 → <c>read</c>）。运行时<b>不得手写</b>本值，否则产生第二真相源并与 golden 脱钩。
/// </summary>
/// <remarks>
/// <para>
/// 取值与生成器内部 <c>ToolRisk</c> 逐一对齐（<c>read</c>=0 / <c>write</c>=1 / <c>high-risk-write</c>=2），
/// 使"风险单调可比较"——配置键 <c>FeishuAgent:MaxToolRisk</c> 的判定依赖该序关系。
/// </para>
/// <para>
/// <b>归属（BUG-1 / 方案 C）</b>：本枚举与 <see cref="FeishuToolRiskNames"/> /
/// <see cref="FeishuToolIdentityNames"/> / <see cref="ContentSafetyModes"/> 同属"策略词汇"，
/// 由 AI 侧（<c>FeishuAgentOptions.Validate</c>、<c>FeishuToolDiagnostics</c>、执行链策略轴）消费，
/// 故落在 <c>Mud.Feishu.AI.AgentTools</c>；工具面（<c>FeishuToolDefinition.Risk</c>）与 MCP
/// （<c>McpToolInfo.Risk</c>）经单向引用消费之。
/// </para>
/// </remarks>
public enum FeishuToolRisk
{
    /// <summary>只读（GET）。</summary>
    Read = 0,

    /// <summary>写操作（POST/PUT/PATCH/DELETE，未命中危险词）。</summary>
    Write = 1,

    /// <summary>高风险写操作（命中危险词表：删除/清空/关闭/解散/撤回等）。</summary>
    HighRiskWrite = 2,
}
