// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.WebSocket;

/// <summary>
/// 连接存活探针快照（F1/P0-1 的结构性兜底）。
/// </summary>
/// <remarks>
/// <b>为什么需要</b>：模块的连接存活检测此前只有一条通道——"接收循环抛异常"
/// （见 <see cref="HeartbeatManager"/> 的类级注释）。D1/I13 补上了"接收循环因取消退出"这条路径，
/// 但两者都只能覆盖 **SDK 自身观察到** 的终止。对下列"外部静默僵死"无解：
/// <list type="bullet">
/// <item>内核 socket 进入假死（对端进程挂起、中间设备静默丢包、TCP 零窗口持续）；</item>
/// <item>对端半开连接（half-open）——本端 <see cref="System.Net.WebSockets.WebSocketState.Open"/>，对端已不认这条连接。</item>
/// </list>
/// 这类形态下"健康检查仍报 Healthy、但永远收不到事件"，故必须以**可观测事实**（接收循环是否存活 +
/// 距上次收帧的静默时长）兜底，而不是继续从 <c>WebSocketState</c> 推断。
/// <para>
/// <see cref="IsZombie"/> 的判定刻意保守（只覆盖"连接被判定为已连接，但接收管道已死"这一**确定性矛盾**），
/// 不把"长时间无帧"单列为僵尸——飞书长连接在空闲时段本就没有事件帧（心跳只在客户端 → 服务端单向发送），
/// 把"静默"直接判死会造成周期性误报。
/// </para>
/// </remarks>
internal readonly struct ConnectionLiveness
{
    /// <summary>
    /// 初始化存活探针快照。
    /// </summary>
    /// <param name="receiveLoopAlive">接收循环任务是否仍在运行</param>
    /// <param name="lastReceiveUtc">最近一次收到帧的 UTC 时间；从未收到时为 <c>null</c></param>
    /// <param name="isConnected">连接是否被判定为已连接（<see cref="System.Net.WebSockets.WebSocketState.Open"/>）</param>
    public ConnectionLiveness(bool receiveLoopAlive, DateTime? lastReceiveUtc, bool isConnected)
    {
        ReceiveLoopAlive = receiveLoopAlive;
        LastReceiveUtc = lastReceiveUtc;
        IsConnected = isConnected;

        IdleMs = lastReceiveUtc.HasValue
            ? (long)Math.Max(0d, (DateTime.UtcNow - lastReceiveUtc.Value).TotalMilliseconds)
            : -1;   // -1 = 尚无收帧样本（不代表"静默"）
    }

    /// <summary>接收循环任务是否仍在运行（<c>_receiveTask is { IsCompleted: false }</c>）。</summary>
    public bool ReceiveLoopAlive { get; }

    /// <summary>最近一次收到帧的 UTC 时间；从未收到时为 <c>null</c>。</summary>
    public DateTime? LastReceiveUtc { get; }

    /// <summary>距最近一次收帧的毫秒数；无样本时为 <c>-1</c>。</summary>
    public long IdleMs { get; }

    /// <summary>连接是否被判定为已连接。</summary>
    public bool IsConnected { get; }

    /// <summary>
    /// 是否处于"僵尸态"：被告知已连接，但接收管道已停止。
    /// </summary>
    /// <remarks>
    /// 这是唯一可以**确定性**判死的组合：连接仍为 <c>Open</c> 却没有任何循环在读帧，
    /// 意味着后续事件永远不会被消费，而 <c>IsConnected</c> 类判定（健康检查 / 重连触发条件）
    /// 都会因 <c>State == Open</c> 而拒绝恢复。必须判为 Unhealthy 以触发人工或自动恢复。
    /// </remarks>
    public bool IsZombie => IsConnected && !ReceiveLoopAlive;
}
