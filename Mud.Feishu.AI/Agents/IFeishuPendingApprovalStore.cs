// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Agents;

/// <summary>
/// 待人工确认项（HITL）的<b>宿主契约</b>：持久化 / 查询 / 取消（R7 / C4a）。
/// </summary>
/// <remarks>
/// <para>
/// <b>与批准状态的所有权边界（不变）</b>：本契约只管「有哪些写操作停在等待确认」，
/// <b>不</b>持有、<b>不</b>校验、<b>不</b>签发批准状态——那是宿主授权器
/// （<c>IToolExecutionAuthorizer</c>）的职责。消费快照<b>不等于</b>放行：执行链仍会二次咨询授权器。
/// </para>
/// <para>
/// <b>软缺席语义</b>：未注册实现 ⇒ 执行链退化为「只通知不落库」（单进程内仍可用，
/// 但重进进程后列不出待办）。SDK 提供内存实现（<see cref="InMemoryPendingApprovalStore"/>）作为默认；
/// 多实例部署需宿主或后续包提供 Redis 实现（键含 <see cref="PendingApprovalSnapshot.AppKey"/>
/// 以保证租户隔离）。
/// </para>
/// <para>
/// <b>时间与过期</b>：所有查询/消费都以「当前时间」判定过期；过期项<b>不得</b>被列出、
/// 也<b>不得</b>被消费成功（fail-closed：过期的批准绝不生效）。
/// </para>
/// </remarks>
public interface IFeishuPendingApprovalStore
{
    /// <summary>登记（或覆盖）一条待确认快照。</summary>
    /// <remarks>
    /// 同 <c>(AppKey, RequestId)</c> 重复登记按<b>覆盖</b>语义处理（框架重投递同一请求时幂等）。
    /// </remarks>
    /// <param name="snapshot">待确认快照。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task SaveAsync(PendingApprovalSnapshot snapshot, CancellationToken cancellationToken = default);

    /// <summary>列出某应用下<b>未过期</b>的待确认项（宿主审批界面用）。</summary>
    /// <param name="appKey">应用唯一标识（租户隔离维度）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>未过期的快照（顺序不保证；空集合表示无待办）。</returns>
    Task<IReadOnlyList<PendingApprovalSnapshot>> ListPendingAsync(string appKey, CancellationToken cancellationToken = default);

    /// <summary>按请求标识查询待确认项；<b>已过期项返回 <see langword="null"/></b>。</summary>
    /// <param name="appKey">应用唯一标识。</param>
    /// <param name="requestId">框架请求标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>未过期的快照；不存在或已过期时为 <see langword="null"/>。</returns>
    Task<PendingApprovalSnapshot?> FindAsync(string appKey, string requestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// <b>幂等消费</b>：把待确认项标记为已处理并摘除；同一请求标识<b>只有第一次</b>返回
    /// <see langword="true"/>。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 宿主在批准回调中调用：返回 <see langword="true"/> 表示「本次批准有效，可继续」
    /// （仍须按既有铁律同步更新授权器状态——框架批准 ≠ 执行链放行）；
    /// 返回 <see langword="false"/> 表示「不存在 / 已过期 / 已被消费」⇒ <b>丢弃迟到批准</b>
    /// （fail-closed，绝不重放）。
    /// </para>
    /// <para>
    /// 过期项被消费时返回 <see langword="false"/>，并同时<b>摘除</b>该快照（不再出现在待办列表中）。
    /// </para>
    /// </remarks>
    /// <param name="appKey">应用唯一标识。</param>
    /// <param name="requestId">框架请求标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>是否消费成功（首次且未过期）。</returns>
    Task<bool> TryConsumeAsync(string appKey, string requestId, CancellationToken cancellationToken = default);

    /// <summary>取消（摘除）一条待确认项——宿主侧「放弃该次写操作」。</summary>
    /// <param name="appKey">应用唯一标识。</param>
    /// <param name="requestId">框架请求标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>是否确实摘除了（不存在时为 <see langword="false"/>）。</returns>
    Task<bool> RemoveAsync(string appKey, string requestId, CancellationToken cancellationToken = default);

    /// <summary>清理全部已过期项（宿主定时任务可周期性调用；也可由批准路径按需触发）。</summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>清理条数。</returns>
    Task<int> RemoveExpiredAsync(CancellationToken cancellationToken = default);
}
