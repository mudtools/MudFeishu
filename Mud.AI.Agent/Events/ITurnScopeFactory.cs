// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.AI.Agent.Events;

/// <summary>
/// 回合执行所需的产品侧上下文要素（<see cref="ITurnScopeFactory.BeginToolScope"/> 的入参）。
/// </summary>
/// <param name="TenantKey">租户/应用标识（多租户隔离的唯一维度）。</param>
/// <param name="ConversationKey">会话键（由宿主的会话键治理策略构造）。</param>
/// <param name="ChatId">触发会话的会话目标 ID（可空）。</param>
/// <param name="UserId">触发用户（可空）。</param>
/// <param name="ThreadId">话题（thread）维度（可空；N4 追加为末尾可选参数 ⇒ 既有构造点源码兼容。
/// 回复进话题等平台特性由产品工具执行链按本维度取用）。</param>
/// <remarks>
/// <b>字段全是中立表述</b>：不出现任何生态专有名词（飞书的 <c>AppKey</c> 语义已中性化为
/// <see cref="TenantKey"/>），故本类型可安全落在中立包。
/// </remarks>
public readonly record struct TurnScopeContext(
    string TenantKey,
    string ConversationKey,
    string? ChatId,
    string? UserId,
    string? ThreadId = null);

/// <summary>
/// <b>产品作用域端口</b>：中立管线在「工具执行上下文 / 租户隔离 / 预算」这类<b>产品事实</b>上的
/// 唯一接缝（③ 式委托的接口化形态——产品实现持有 <c>AsyncLocal</c> 等具体手段，中立侧只知<b>进出</b>）。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么必须由产品实现</b>：工具执行上下文承载租户凭证语义（飞书侧是
/// <c>IFeishuToolContextAccessor</c> 的 <c>AsyncLocal</c>，唯一建立点在事件层）。中立管线不能
/// 命名它（撞 `MudAiFamily_ShouldNotDependOnFeishuProjects`），也不该自建第二个上下文槽位
/// （两个槽位 ⇒ 宿主该初始化哪个？与闸门同款推理，见方案 §5.4.1）。
/// </para>
/// <para>
/// <b>作用域语义</b>：<see cref="BeginToolScope"/> 返回的作用域释放时<b>必须</b>恢复进入前的状态
/// （既有实现是 <c>AsyncLocal</c> 栈式恢复 + 所有权校验），否则嵌套回合（续跑轮嵌在事件轮之后）
/// 会读到外层的租户上下文——多租户场景下这是越权读取。
/// </para>
/// <para>
/// <b>续跑轮为什么也用它</b>：续跑不在事件流内，工具上下文不存在 ⇒ 工具会 fail-closed 结构化拒绝。
/// 产线闭环（<c>AgentApprovalContinuation</c>）经本端口重建，正是「批准 ≠ 放行」的落地形态。
/// </para>
/// <para>
/// <b>可空语义</b>：端口实现可空（未注册 ⇒ 中立管线不建立任何作用域，与既有「无工具上下文」
/// 行为逐字节一致）；需要租户上下文的工具链路在缺上下文时自行 fail-closed。
/// </para>
/// </remarks>
public interface ITurnScopeFactory
{
    /// <summary>
    /// 进入工具执行上下文作用域。
    /// </summary>
    /// <param name="context">回合上下文要素。</param>
    /// <returns>作用域（<see cref="IDisposable.Dispose"/> 恢复进入前状态）。</returns>
    IDisposable BeginToolScope(TurnScopeContext context);
}
