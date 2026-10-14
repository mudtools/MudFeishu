// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.AI.Abstractions.Observability;

/// <summary>
/// 中立遥测端口：把「运行管线要埋的事件点」与「具体遥测实现与命名面」解耦。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么是端口而不是直接调 <c>ActivitySource</c></b>：中立包不得认识任何产品的遥测命名面
/// （<c>xxx.agent.*</c> 键、Span 名、Meter 名都是产品资产——改名会让已依赖它的宿主仪表盘失效）。
/// 端口只提供<b>调用入口</b>，不提供命名：所有键名/单位/聚合口径由产品侧实现决定。
/// </para>
/// <para>
/// <b>句柄语义</b>：<see cref="StartTurn"/> 返回的 <see cref="IDisposable"/> 是本轮的遥测作用域
/// （产品侧通常映射为一个 <c>Activity</c>）；返回 <see langword="null"/> 表示不采集。
/// 作用域必须在本轮结束时 <c>Dispose</c>（与 <c>using</c> 语义一致），
/// 且<b>不得</b>承载业务状态——它只是遥测载体，丢失/为 null 时业务行为必须零差异。
/// </para>
/// <para>
/// <b>维度纪律（继承既有原则）</b>：租户/会话/用户等键<b>只进 Span 属性</b>，绝不进 Metrics tag
/// ——高基数维度会让指标后端爆炸。本端口的签名即是该纪律的体现：<c>conversationKey</c>/<c>tenantKey</c>
/// 只出现在 <see cref="StartTurn"/>（作用域）上，而 <see cref="LlmDuration"/> 只有数值与 agent 名。
/// </para>
/// </remarks>
public interface IAgentTelemetry
{
    /// <summary>
    /// 开启一轮会话的遥测作用域。
    /// </summary>
    /// <param name="conversationKey">会话键（只应落 Span 属性，不入 Metrics tag）。</param>
    /// <param name="tenantKey">租户/应用标识（可空；同上）。</param>
    /// <returns>作用域（实现返回 <see langword="null"/> 表示不采集）；释放即结束本轮。</returns>
    IDisposable? StartTurn(string conversationKey, string? tenantKey);

    /// <summary>记录闸门等待耗时（毫秒）——只进当前作用域，不作为指标 tag。</summary>
    /// <param name="turn">当前轮作用域（可空）。</param>
    /// <param name="milliseconds">等待毫秒数。</param>
    void GateWaited(IDisposable? turn, double milliseconds);

    /// <summary>标记本轮命中了「上一轮待补发回复」——即未重跑模型直接补发。</summary>
    /// <param name="turn">当前轮作用域（可空）。</param>
    void Replayed(IDisposable? turn);

    /// <summary>标记本轮回复已经由流式通道送达（不再走整段下发）。</summary>
    /// <param name="turn">当前轮作用域（可空）。</param>
    void Streamed(IDisposable? turn);

    /// <summary>
    /// 记录一次模型调用耗时（毫秒）。
    /// </summary>
    /// <remarks>
    /// <b>失败样本同样要记录</b>（在 <c>finally</c> 中调用）：只记成功路径会让 P95/P99 失真。
    /// </remarks>
    /// <param name="agentName">Agent 名（唯一的指标维度）。</param>
    /// <param name="milliseconds">耗时毫秒数。</param>
    void LlmDuration(string agentName, long milliseconds);

    /// <summary>
    /// 记录一次<b>已发生</b>的降级（通道写入失败、装配器失败等被隔离的异常）。
    /// </summary>
    /// <remarks>降级必须可观测——否则"静默零注入"这类缺陷只能靠读日志发现。</remarks>
    /// <param name="reason">降级原因（产品侧决定具体文案与指标名）。</param>
    void Degraded(string reason);
}

/// <summary>
/// 默认遥测实现：全部为空操作。
/// </summary>
/// <remarks>
/// 中立侧的默认值必须是"零开销且零副作用"——没有遥测宿主的部署不应因为缺少实现而失败，
/// 也不应付出任何分配成本（<see cref="Instance"/> 是无状态单例）。
/// </remarks>
public sealed class NoopAgentTelemetry : IAgentTelemetry
{
    /// <summary>共享单例（无状态）。</summary>
    public static readonly NoopAgentTelemetry Instance = new();

    private NoopAgentTelemetry()
    {
    }

    /// <inheritdoc />
    public IDisposable? StartTurn(string conversationKey, string? tenantKey) => null;

    /// <inheritdoc />
    public void GateWaited(IDisposable? turn, double milliseconds)
    {
    }

    /// <inheritdoc />
    public void Replayed(IDisposable? turn)
    {
    }

    /// <inheritdoc />
    public void Streamed(IDisposable? turn)
    {
    }

    /// <inheritdoc />
    public void LlmDuration(string agentName, long milliseconds)
    {
    }

    /// <inheritdoc />
    public void Degraded(string reason)
    {
    }
}
