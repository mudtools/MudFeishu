// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.AI.Agent.Tools;

/// <summary>
/// 单轮工具调用预算：经 <see cref="System.Threading.AsyncLocal{T}"/> 在事件入口开启、随
/// <see langword="using"/> 释放，工具执行链通过 <see cref="TryConsume"/> 原子递增并判定是否越限。
/// </summary>
/// <remarks>
/// <b>归属</b>：本类型同时被 <c>Mud.Feishu.AI</c> 事件入口（<c>ConversationalFeishuEventHandler</c> Begin）
/// 与 <c>Mud.Feishu.AI.Tools</c> 工具执行链（<c>ToolBudgetAIFunction</c>）引用，属"AI 运行时 ↔ 工具面共享接缝"，
/// 故落在 <c>Mud.Feishu.AI.AgentTools</c>（与 <see cref="FeishuToolContext"/> 同位置）。
/// <para>
/// <b>为什么不用 MAF 内建上限</b>：MAF <c>ChatClientAgent</c> 的内建上限约 40 次且<b>不可配</b>，成本过高；
/// 本类型给<b>可配的、更收紧的</b>每轮预算（默认 10），耗尽时短路返回结构化错误（不抛异常，防重复计费）。
/// </para>
/// <para>
/// <b>per-run</b>：预算是「一轮 <c>RunAsync</c> 的总工具数」，由事件层开启、随 <c>finally</c> 释放
/// （与 <c>FeishuToolContext</c> 同一生命周期与 AsyncLocal 纪律）。
/// </para>
/// <para>
/// <b>并发与边界</b>：计数经 <see cref="Interlocked"/> 保证原子性——MAF 的工具调用同轮内可能并行
/// （当前实现是串行的，但不可依赖该实现细节）；<c>maxToolCalls = 0</c> 视为「不限制」，
/// 与 <c>FeishuAgentOptions.MaxToolCallsPerTurn = 0</c> 一致。
/// </para>
/// </remarks>
public sealed class ToolCallBudget
{
    private readonly int _max;
    private int _used;

    private static readonly AsyncLocal<ToolCallBudget?> _ambient = new();

    /// <summary>
    /// 当前异步流的工具调用预算（未设置时为 <see langword="null"/>——不限制）。
    /// </summary>
    public static ToolCallBudget? Current => _ambient.Value;

    /// <summary>
    /// 初始化 <see cref="ToolCallBudget"/>。
    /// </summary>
    /// <param name="maxToolCalls">最大工具调用次数（0 = 不限制）。</param>
    public ToolCallBudget(int maxToolCalls)
    {
        _max = maxToolCalls;
        _used = 0;
    }

    /// <summary>
    /// 开始一轮预算：把自身设为当前异步流值，返回的作用域释放时恢复。
    /// </summary>
    /// <returns>作用域（<see cref="IDisposable.Dispose"/> 恢复原值）。</returns>
    public IDisposable Begin()
    {
        var previous = _ambient.Value;
        _ambient.Value = this;
        return new BudgetScope(previous, this);
    }

    /// <summary>
    /// 原子递增并判定是否越限。
    /// </summary>
    /// <returns>
    /// <see langword="true"/> = 预算未耗尽，可执行；<see langword="false"/> = 越限，应短路返回结构化错误。
    /// </returns>
    public bool TryConsume()
    {
        // 0 = 不限制
        if (_max <= 0)
            return true;

        var incremented = Interlocked.Increment(ref _used);
        return incremented <= _max;
    }

    /// <summary>已使用次数（可观测用）。</summary>
    public int Used => Volatile.Read(ref _used);

    /// <summary>最大次数（0 = 不限制）。</summary>
    public int Max => _max;

    private sealed class BudgetScope : IDisposable
    {
        private readonly ToolCallBudget? _previous;
        private readonly ToolCallBudget _owner;
        private bool _disposed;

        public BudgetScope(ToolCallBudget? previous, ToolCallBudget owner)
        {
            _previous = previous;
            _owner = owner;
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;

            // 所有权校验（与工具执行上下文的恢复同款纪律）：
            // 只有「当前值仍是我写入的值」才回滚，避免乱序释放覆盖内层。
            if (ReferenceEquals(_ambient.Value, _owner))
            {
                _ambient.Value = _previous;
            }
        }
    }
}
