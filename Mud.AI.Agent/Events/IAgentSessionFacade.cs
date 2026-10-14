// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.AI.Agent.Events;

/// <summary>
/// <b>会话取/建/存端口</b>（中立管线与审批续跑闭环共用的会话面）。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么落 <c>Mud.AI.Agent</c> 而不是 <c>Mud.AI.Abstractions</c></b>（N2-c 执行时对账 §7.2）：
/// 本端口的方法签名承载 MAF <c>AgentSession</c>，而 <c>Mud.AI.Abstractions</c> 是<b>零 MAF</b>
/// 的编译期契约层（硬纪律 §4.3.1）——§3 目标树把本端口与 <c>ITurnScopeFactory</c> 一起登记在
/// <c>Mud.AI.Agent/Events/</c>，与「§7.2 一行的『迁A』标签」相比，目标树 + 硬纪律是更强的约束，
/// 故按目标树落地（本注记即为两者对账结论）。
/// </para>
/// <para>
/// <b>产品实现</b>：飞书侧 <c>FeishuAgent</c> 直接实现本端口（其 <c>GetOrCreateSessionAsync</c> /
/// <c>SaveSessionAsync</c> 早于本端口存在，签名逐字一致 ⇒ 零改造），故中立管线不引用任何产品类型。
/// </para>
/// <para>
/// <b>失败语义</b>：实现自行决定"坏值 ⇒ 重建"等自愈策略（飞书侧既有语义：载荷损坏按 miss 处理），
/// 端口只定形，不吞异常——异常照常传播给调用方（管线/续跑闭环据此决定补偿）。
/// </para>
/// </remarks>
public interface IAgentSessionFacade
{
    /// <summary>
    /// 会话历史在框架状态袋内的状态键（<b>产品事实</b>：飞书侧是 <c>feishu.agent.history</c>）。
    /// </summary>
    /// <remarks>
    /// <b>为什么它属于本端口</b>：历史存取（<c>TryGetInMemoryChatHistory</c>）是本门面封装的能力，
    /// 历史键是它的实现细节；审批续跑闭环需要该键做「孤儿审批判定」——让封装者自己回答，
    /// 好过把它再散成各处形参（宿主不必知道键名）。
    /// </remarks>
    string HistoryStateKey { get; }

    /// <summary>
    /// 按会话键读取会话；miss（不存在、TTL 过期、载荷损坏）时创建新会话。
    /// </summary>
    /// <param name="conversationKey">会话键（由宿主的会话键治理策略构造）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>可用会话。</returns>
    Task<AgentSession> GetOrCreateSessionAsync(string conversationKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// 序列化并保存会话（刷新存储 TTL）。
    /// </summary>
    /// <param name="conversationKey">会话键。</param>
    /// <param name="session">要持久化的会话（须由同一实例创建）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>任务。</returns>
    Task SaveSessionAsync(string conversationKey, AgentSession session, CancellationToken cancellationToken = default);
}
