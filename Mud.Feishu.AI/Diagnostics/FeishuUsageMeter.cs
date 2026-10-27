// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任。
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Diagnostics;

/// <summary>
/// 成本核算出口（E-2）：按 <c>(租户, 会话)</c> 与「工具」两个视角聚合 token 用量与工具调用。
/// </summary>
/// <remarks>
/// <para>
/// <b>与 Metrics 的分工</b>：Metrics 的 tag 纪律（原则 8）禁止会话/租户等高基数维度进标签，
/// 但"这个会话花了多少 token、哪个工具最费时"恰恰是宿主账务与运维要看的——故这部分口径走
/// <b>进程内聚合快照</b>（宿主拉到自己的存储/看板），不进 OTel 标签。
/// </para>
/// <para>
/// <b>默认实现有界</b>：<see cref="InMemoryFeishuUsageMeter"/> 按会话键分桶并在超过上限时淘汰最旧桶，
/// 防止长跑进程的字典无界增长；需要持久化的宿主替换本接口即可（记账与落库是宿主职责）。
/// </para>
/// </remarks>
public interface IFeishuUsageMeter
{
    /// <summary>记录一次模型调用的 token 用量（未上报的项传 <see langword="null"/>，不得填 0）。</summary>
    /// <param name="appKey">应用键（租户）。</param>
    /// <param name="conversationKey">会话键（可空）。</param>
    /// <param name="inputTokens">输入 token 数（可空）。</param>
    /// <param name="outputTokens">输出 token 数（可空）。</param>
    void RecordModelUsage(string appKey, string? conversationKey, long? inputTokens, long? outputTokens);

    /// <summary>记录一次工具调用（拒绝/失败分别计数，便于算成功率与成本归因）。</summary>
    /// <param name="appKey">应用键（租户）。</param>
    /// <param name="conversationKey">会话键（可空）。</param>
    /// <param name="toolName">工具名。</param>
    /// <param name="isWrite">是否写工具。</param>
    /// <param name="denied">是否被授权门禁拒绝（零下游调用）。</param>
    /// <param name="failed">是否以结构化错误/异常结束。</param>
    /// <param name="durationMs">耗时毫秒。</param>
    void RecordToolCall(
        string appKey, string? conversationKey, string toolName, bool isWrite, bool denied, bool failed, long durationMs);

    /// <summary>取当前累计快照（默认实现为「累计值」，不清零——清零语义由宿主决定）。</summary>
    /// <returns>结构化用量快照。</returns>
    FeishuUsageSnapshot TakeSnapshot();
}

/// <summary>按 <c>(租户, 会话)</c> 聚合的一行用量。</summary>
/// <param name="AppKey">应用键（租户）。</param>
/// <param name="ConversationKey">会话键（可空）。</param>
/// <param name="ToolCalls">工具调用次数。</param>
/// <param name="WriteCalls">其中写工具次数。</param>
/// <param name="DeniedCalls">授权拒绝次数。</param>
/// <param name="FailedCalls">失败次数（结构化错误或异常）。</param>
/// <param name="InputTokens">输入 token 累计。</param>
/// <param name="OutputTokens">输出 token 累计。</param>
public sealed record FeishuUsageSlice(
    string AppKey,
    string? ConversationKey,
    long ToolCalls,
    long WriteCalls,
    long DeniedCalls,
    long FailedCalls,
    long InputTokens,
    long OutputTokens);

/// <summary>按工具聚合的一行用量。</summary>
/// <param name="ToolName">工具名。</param>
/// <param name="Calls">调用次数。</param>
/// <param name="DeniedCalls">拒绝次数。</param>
/// <param name="FailedCalls">失败次数。</param>
/// <param name="DurationMs">累计耗时毫秒。</param>
public sealed record FeishuToolUsageSlice(
    string ToolName,
    long Calls,
    long DeniedCalls,
    long FailedCalls,
    long DurationMs);

/// <summary>用量快照（两个视角 + 采样时刻）。</summary>
/// <param name="CapturedAt">采样时刻（UTC）。</param>
/// <param name="Sessions">按 <c>(租户, 会话)</c> 的聚合。</param>
/// <param name="Tools">按工具的聚合。</param>
public sealed record FeishuUsageSnapshot(
    DateTimeOffset CapturedAt,
    IReadOnlyList<FeishuUsageSlice> Sessions,
    IReadOnlyList<FeishuToolUsageSlice> Tools);

/// <summary>
/// <see cref="IFeishuUsageMeter"/> 的进程内实现（线程安全、有界、无 IO）。
/// </summary>
/// <remarks>
/// <b>有界策略</b>：会话桶超过 <see cref="MaxSessions"/> 时按<b>最近一次写入时间的插入顺序</b>
/// 淘汰最旧桶（不排序、不算热度），代价是 O(1) 且淘汰行为可预测；被淘汰桶的用量不再出现在快照里。
/// </remarks>
public sealed class InMemoryFeishuUsageMeter : IFeishuUsageMeter
{
    private const int MaxSessions = 4096;
    private const int MaxTools = 4096;

    private readonly object _gate = new();
    private readonly Dictionary<(string AppKey, string? ConversationKey), SessionCounters> _sessions = [];
    private readonly Queue<(string AppKey, string? ConversationKey)> _sessionOrder = new();
    private readonly Dictionary<string, ToolCounters> _tools = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public void RecordModelUsage(string appKey, string? conversationKey, long? inputTokens, long? outputTokens)
    {
        if (string.IsNullOrEmpty(appKey))
            throw new ArgumentException("appKey 不能为空", nameof(appKey));

        var counters = RentSession(appKey, conversationKey);
        counters.AddTokens(inputTokens ?? 0, outputTokens ?? 0);
    }

    /// <inheritdoc />
    public void RecordToolCall(
        string appKey, string? conversationKey, string toolName, bool isWrite, bool denied, bool failed, long durationMs)
    {
        if (string.IsNullOrEmpty(appKey))
            throw new ArgumentException("appKey 不能为空", nameof(appKey));
        if (string.IsNullOrEmpty(toolName))
            throw new ArgumentException("toolName 不能为空", nameof(toolName));

        RentSession(appKey, conversationKey).AddToolCall(isWrite, denied, failed);

        lock (_gate)
        {
            if (!_tools.TryGetValue(toolName, out var counters))
            {
                if (_tools.Count >= MaxTools)
                {
                    // 工具名空间有界（注册表规模），触顶说明装配异常；直接不再新增而不是静默改口径。
                    return;
                }

                counters = new ToolCounters();
                _tools[toolName] = counters;
            }

            counters.Add(denied, failed, durationMs);
        }
    }

    /// <inheritdoc />
    public FeishuUsageSnapshot TakeSnapshot()
    {
        lock (_gate)
        {
            var sessions = new List<FeishuUsageSlice>(_sessions.Count);
            foreach (var pair in _sessions)
            {
                var counters = pair.Value;
                sessions.Add(new FeishuUsageSlice(
                    pair.Key.AppKey,
                    pair.Key.ConversationKey,
                    counters.Calls,
                    counters.WriteCalls,
                    counters.DeniedCalls,
                    counters.FailedCalls,
                    counters.InputTokens,
                    counters.OutputTokens));
            }

            var tools = new List<FeishuToolUsageSlice>(_tools.Count);
            foreach (var pair in _tools)
            {
                tools.Add(new FeishuToolUsageSlice(
                    pair.Key,
                    pair.Value.Calls,
                    pair.Value.DeniedCalls,
                    pair.Value.FailedCalls,
                    pair.Value.DurationMs));
            }

            return new FeishuUsageSnapshot(DateTimeOffset.UtcNow, sessions, tools);
        }
    }

    private SessionCounters RentSession(string appKey, string? conversationKey)
    {
        var key = (appKey, conversationKey);
        lock (_gate)
        {
            if (_sessions.TryGetValue(key, out var existing))
            {
                return existing;
            }

            if (_sessions.Count >= MaxSessions)
            {
                while (_sessionOrder.Count > 0 && _sessions.Count >= MaxSessions)
                {
                    _sessions.Remove(_sessionOrder.Dequeue());
                }
            }

            var created = new SessionCounters();
            _sessions[key] = created;
            _sessionOrder.Enqueue(key);
            return created;
        }
    }

    /// <summary>会话桶计数器（写入侧用 <see cref="Interlocked"/>，读取侧容忍弱一致）。</summary>
    private sealed class SessionCounters
    {
        private long _calls;
        private long _writeCalls;
        private long _deniedCalls;
        private long _failedCalls;
        private long _inputTokens;
        private long _outputTokens;

        public long Calls => Interlocked.Read(ref _calls);

        public long WriteCalls => Interlocked.Read(ref _writeCalls);

        public long DeniedCalls => Interlocked.Read(ref _deniedCalls);

        public long FailedCalls => Interlocked.Read(ref _failedCalls);

        public long InputTokens => Interlocked.Read(ref _inputTokens);

        public long OutputTokens => Interlocked.Read(ref _outputTokens);

        public void AddTokens(long input, long output)
        {
            Interlocked.Add(ref _inputTokens, input);
            Interlocked.Add(ref _outputTokens, output);
        }

        public void AddToolCall(bool isWrite, bool denied, bool failed)
        {
            Interlocked.Increment(ref _calls);
            if (isWrite)
            {
                Interlocked.Increment(ref _writeCalls);
            }

            if (denied)
            {
                Interlocked.Increment(ref _deniedCalls);
            }

            if (failed)
            {
                Interlocked.Increment(ref _failedCalls);
            }
        }
    }

    /// <summary>工具维计数器（无并发拆分需求：仅统计，读侧容忍弱一致）。</summary>
    private sealed class ToolCounters
    {
        public long Calls;
        public long DeniedCalls;
        public long FailedCalls;
        public long DurationMs;

        public void Add(bool denied, bool failed, long durationMs)
        {
            Calls++;
            DurationMs += durationMs;
            if (denied)
            {
                DeniedCalls++;
            }

            if (failed)
            {
                FailedCalls++;
            }
        }
    }
}
