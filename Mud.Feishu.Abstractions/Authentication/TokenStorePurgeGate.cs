// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Diagnostics;

namespace Mud.Feishu.Abstractions.Authentication;

/// <summary>
/// 令牌存储「待清库」门（按 appKey 引用计数租约，TMR2-P1-5）。
/// </summary>
/// <remarks>
/// <para>
/// <b>背景</b>：凭据变更（AppId / AppSecret）热更新必须保证「清库 → 重建」时序——
/// 新上下文不得在清库完成前从 store 恢复出旧凭据来源的令牌（D10）。
/// 首轮修复（TMR-P1-6 / F6）把清库移出锁并加了 10s 上界，但仍在
/// <c>IOptionsMonitor.OnChange</c> 的<b>回调线程</b>上 <c>Task.Run(...).Wait(10s)</c>：
/// 回调线程最长阻塞 10s、宿主存在 <c>SynchronizationContext</c> 时有死锁面、
/// 线程池饥饿时占用双线程。
/// </para>
/// <para>
/// <b>本设计</b>：把 D10 的实质约束（"清库完成前不得恢复旧令牌"）从「阻塞 <c>OnChange</c>」
/// 下沉到「恢复路径短路」——Phase-P 只<b>打门</b>（同步、微秒级），清库异步执行；
/// <c>FeishuAppTokenManagerBase.TryRestoreFromStoreAsync</c> /
/// <c>UserTokenManager</c> 的恢复路径见到门即跳过 store。
/// </para>
/// <para>
/// <b>计数语义</b>：两段清库（Phase-P 的锁外预清库 + 提交后二次清库）各自
/// <see cref="Mark"/>/<see cref="Release"/> 一次，叠加期间门持续有效，
/// 覆盖"提交窗口内旧上下文回写"的残余窗口。计数归零即撤门。
/// </para>
/// <para>
/// <b>安全性（fail-open）</b>：门带 <see cref="SafetyTimeoutSeconds"/> 上界——
/// 清库异常/挂死时门自动失效并上报一次 <see cref="PurgeGateState.TimedOut"/>，
/// 恢复路径重新启用，绝不因门而永久禁用 store 恢复。
/// </para>
/// <para>
/// <b>多实例语义</b>：门为<b>进程内</b>状态；其他实例在窗口内仍可能恢复旧令牌，
/// 与既有 Redis SCAN 的固有残余窗口同级（既有文档已声明），不构成回归。
/// </para>
/// <para>
/// <b>并发实现</b>：<c>lock</c> + <see cref="Dictionary{TKey,TValue}"/>。
/// 调用点仅有 ①热更新 Phase-P/提交后打门、②清库收尾撤门、③令牌恢复路径查询——
/// 均为低频路径，临界区为字典查找（纳秒级），不构成热路径竞争。
/// 刻意不使用 <c>ConcurrentDictionary</c> + <c>Interlocked</c>：
/// "计数归零即移除"与"并发 Mark 增量"之间存在丢标记窗口，锁版本可证正确。
/// </para>
/// </remarks>
internal static class TokenStorePurgeGate
{
    /// <summary>
    /// 门的最长有效期（秒）；超时后视为失效（fail-open）并上报一次超时。
    /// </summary>
    internal const int SafetyTimeoutSeconds = 30;

    /// <summary>
    /// 安全超时对应的单调时钟刻度数（netstandard2.0 无 <c>Environment.TickCount64</c>，
    /// 统一使用 <see cref="Stopwatch"/> 单调时钟，避免系统时间调整造成的误判/漏判）。
    /// </summary>
    private static readonly long SafetyTimeoutTimestampTicks =
        (long)(Stopwatch.Frequency * (double)SafetyTimeoutSeconds);

    private static readonly object Sync = new();

    /// <summary>
    /// 按 appKey 的租约（计数 + 起始时刻，起始时刻取首个打门者）。
    /// </summary>
    private static readonly Dictionary<string, Lease> Leases = new(StringComparer.Ordinal);

    /// <summary>
    /// 门的查询结果。
    /// </summary>
    internal enum PurgeGateState
    {
        /// <summary>无门（或门已超时失效）：恢复路径可正常读取 store。</summary>
        NotPending = 0,

        /// <summary>清库进行中：恢复路径必须跳过 store（等价于 D10「清库完成前不得恢复旧令牌」）。</summary>
        Pending = 1,

        /// <summary>门已超时失效（本次查询已将门摘除）：恢复路径重新启用 store，调用方应记一次 Warning。</summary>
        TimedOut = 2,
    }

    /// <summary>
    /// 打门：该 appKey 的租约计数 +1（首次打门记录起始时刻，用于安全超时）。
    /// </summary>
    /// <param name="appKey">应用唯一标识（空值忽略）。</param>
    public static void Mark(string? appKey)
    {
        if (string.IsNullOrEmpty(appKey))
            return;

        lock (Sync)
        {
            if (Leases.TryGetValue(appKey!, out var lease))
            {
                lease.Count++;
                return;
            }

            Leases[appKey!] = new Lease { Count = 1, StartedAtTimestamp = Stopwatch.GetTimestamp() };
        }
    }

    /// <summary>
    /// 撤门：该 appKey 的租约计数 -1，归零即摘除条目。
    /// </summary>
    /// <param name="appKey">应用唯一标识（空值/无租约时忽略）。</param>
    public static void Release(string? appKey)
    {
        if (string.IsNullOrEmpty(appKey))
            return;

        lock (Sync)
        {
            if (!Leases.TryGetValue(appKey!, out var lease))
                return;

            lease.Count--;
            if (lease.Count <= 0)
                Leases.Remove(appKey!);
        }
    }

    /// <summary>
    /// 查询该 appKey 当前的门状态（含安全超时失效判定）。
    /// </summary>
    /// <param name="appKey">应用唯一标识。</param>
    /// <returns>
    /// <see cref="PurgeGateState.Pending"/>：清库进行中，恢复路径必须跳过 store；
    /// <see cref="PurgeGateState.TimedOut"/>：超时失效（条目已被摘除，调用方记一次 Warning 后按无门处理）；
    /// <see cref="PurgeGateState.NotPending"/>：无门。
    /// </returns>
    public static PurgeGateState Query(string? appKey)
        => Query(appKey, Stopwatch.GetTimestamp());

    /// <summary>
    /// 查询门状态（可注入"当前时刻"，供确定性测试超时路径——与
    /// <c>FeishuAppContextRetirement.Sweep(DateTimeOffset now)</c> 同一模式）。
    /// </summary>
    /// <param name="appKey">应用唯一标识。</param>
    /// <param name="nowTimestamp">当前 <see cref="Stopwatch.GetTimestamp"/> 刻度。</param>
    /// <returns>门状态。</returns>
    internal static PurgeGateState Query(string? appKey, long nowTimestamp)
    {
        if (string.IsNullOrEmpty(appKey))
            return PurgeGateState.NotPending;

        lock (Sync)
        {
            if (!Leases.TryGetValue(appKey!, out var lease))
                return PurgeGateState.NotPending;

            if (nowTimestamp - lease.StartedAtTimestamp > SafetyTimeoutTimestampTicks)
            {
                // 超时 fail-open：摘除门使恢复路径重新启用，由调用方记一次 Warning（每租约至多一次）。
                Leases.Remove(appKey!);
                return PurgeGateState.TimedOut;
            }

            return PurgeGateState.Pending;
        }
    }

    /// <summary>
    /// 当前处于门内的 appKey 数量（诊断/测试用）。
    /// </summary>
    internal static int ActiveLeaseCount
    {
        get
        {
            lock (Sync)
            {
                return Leases.Count;
            }
        }
    }

    /// <summary>
    /// 测试用：清空全部门（生产路径不调用）。
    /// </summary>
    internal static void ResetForTest()
    {
        lock (Sync)
        {
            Leases.Clear();
        }
    }

    /// <summary>
    /// 租约状态（仅在 <see cref="Sync"/> 内访问）。
    /// </summary>
    private sealed class Lease
    {
        /// <summary>引用计数（打门 +1，撤门 -1，归零摘除）。</summary>
        public int Count;

        /// <summary>首个打门时刻（<see cref="Stopwatch.GetTimestamp"/>，用于安全超时）。</summary>
        public long StartedAtTimestamp;
    }
}
