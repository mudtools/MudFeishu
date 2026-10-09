// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using Mud.Feishu.AI.Tools;

namespace Mud.Feishu.Agent.Demo;

/// <summary>一条挂起的人工确认请求（<b>续跑必须原样带回</b> <see cref="Framework"/>）。</summary>
/// <param name="CorrelationId">面向用户的短关联号（避免让用户抄 32 位 <c>ficc_*</c>）。</param>
/// <param name="Framework">框架审批请求原件。</param>
/// <param name="CreatedAt">登记时间（UTC）。</param>
/// <param name="ExpiresAt">有效期截止（UTC）。</param>
internal sealed record PendingApproval(
    string CorrelationId,
    FrameworkToolApprovalRequest Framework,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt);

/// <summary>
/// 挂起审批登记表（单例可变状态；<c>/pending</c>、<c>/approve</c>、<c>/deny</c>、<c>/abandon</c> 共用的唯一真相）。
/// </summary>
internal sealed class ConsoleApprovalChannelState
{
    /// <summary>登记项的默认有效期（与 <c>ToolApprovalRequest.ExpiresAt</c> 默认 10 分钟一致）。</summary>
    public static readonly TimeSpan DefaultLifetime = TimeSpan.FromMinutes(10);

    private readonly ConcurrentDictionary<string, PendingApproval> _pending = new(StringComparer.Ordinal);

    /// <summary>是否存在挂起项。</summary>
    public bool HasPending => !_pending.IsEmpty;

    /// <summary>挂起项数量。</summary>
    public int Count => _pending.Count;

    /// <summary>按登记时间升序快照。</summary>
    /// <returns>挂起项数组。</returns>
    public IReadOnlyList<PendingApproval> Snapshot()
        => _pending.Values.OrderBy(static p => p.CreatedAt).ToArray();

    /// <summary>按关联号查找。</summary>
    /// <param name="correlationId">面向用户的短关联号。</param>
    /// <returns>挂起项；不存在时为 <see langword="null"/>。</returns>
    public PendingApproval? FindByCorrelationId(string correlationId)
        => _pending.Values.FirstOrDefault(
            p => string.Equals(p.CorrelationId, correlationId, StringComparison.Ordinal));

    /// <summary>登记一条挂起项（同 <c>RequestId</c> 重复登记时保留首条）。</summary>
    /// <param name="approval">挂起项。</param>
    /// <returns>是否新增成功。</returns>
    public bool TryAdd(PendingApproval approval)
        => approval is not null && _pending.TryAdd(approval.Framework.RequestId, approval);

    /// <summary>摘除一条挂起项（续跑前先摘除，避免续跑期间 <c>/pending</c> 仍显示）。</summary>
    /// <param name="requestId">框架请求标识。</param>
    /// <returns>是否摘除成功。</returns>
    public bool Remove(string requestId) => _pending.TryRemove(requestId, out _);

    /// <summary>清空全部挂起项。</summary>
    /// <returns>被清空的条数。</returns>
    public int Clear()
    {
        var count = _pending.Count;
        _pending.Clear();
        return count;
    }
}

/// <summary>
/// 宿主批准通道：<c>IFeishuToolApprovalChannel</c> 的控制台实现（ADR-04「登记 + 显式续跑」）。
/// </summary>
/// <remarks>
/// <para>
/// <b>为何不在通道内阻塞等待</b>：契约硬性要求（<c>RequestFrameworkApprovalAsync</c> 的注释）——
/// 「实现不应阻塞等待人类点按钮（实现内不得 <c>Task.Delay</c> 轮询等待批准）」，
/// 且该方法在会话语义上是<b>异步</b>的。阻塞式实现虽在控制台场景"体验更顺"，
/// 但会<b>掩盖 HITL 的真实工程形态</b>（飞书卡片 / 工单 / 审批单都是异步回灌）。
/// </para>
/// <para>
/// 因此本实现：<b>打印审批卡片 + 登记 pending + 立即返回关联号</b>，
/// 批准由用户显式输入 <c>/approve &lt;关联号&gt;</c> 触发
/// <c>FeishuAgent.RunApprovalContinuationAsync</c> 续跑。
/// </para>
/// <para>
/// <b>fail-closed</b>：本实现不抛异常；若 Renderer 抛异常，通道异常会让执行链降级为"纯提示"
/// （写工具保持未执行），绝不降级为"自动批准"。
/// 附带收益：等待期可正常使用 <c>/pending</c>、<c>/audit</c>、<c>/tools</c>——恰好演示真实的挂起态。
/// </para>
/// </remarks>
internal sealed class ConsoleApprovalChannel : IFeishuToolApprovalChannel
{
    private readonly ConsoleApprovalChannelState _state;
    private readonly ConsoleRenderer _renderer;
    private int _seq;

    /// <summary>
    /// 初始化批准通道。
    /// </summary>
    /// <param name="state">挂起登记表。</param>
    /// <param name="renderer">终端渲染器。</param>
    public ConsoleApprovalChannel(ConsoleApprovalChannelState state, ConsoleRenderer renderer)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
    }

    /// <inheritdoc />
    /// <remarks>
    /// P4-1 后的正常路径：写工具经 <c>ApprovalRequiredAIFunction</c> 包装后，由 MAF 在调用<b>之前</b>
    /// 产出框架审批请求（<c>RequestId = ficc_{callId}</c>），工具尚未执行。
    /// </remarks>
    public Task<string?> RequestFrameworkApprovalAsync(
        FrameworkToolApprovalRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var now = DateTimeOffset.UtcNow;
        var pending = new PendingApproval(
            CorrelationId: NextCorrelationId(),
            Framework: request,
            CreatedAt: now,
            ExpiresAt: now + ConsoleApprovalChannelState.DefaultLifetime);

        _state.TryAdd(pending);

        _renderer.ApprovalCard(new PendingApprovalViewModel(
            CorrelationId: pending.CorrelationId,
            RequestId: request.RequestId,
            ToolName: request.ToolName,
            ArgumentsDigest: request.ArgumentsDigest,
            RequiredScopes: request.RequiredScopes,
            ExpiresAt: pending.ExpiresAt));

        // ★ 不阻塞、不轮询：立即返回（契约要求，ADR-04）。
        return Task.FromResult<string?>(pending.CorrelationId);
    }

    /// <inheritdoc />
    /// <remarks>
    /// <b>非框架路径</b>（P4-1 之前的自研令牌路径）。正常不可达；若被调用说明出现了预期外路径，
    /// 故以 <see cref="NoticeLevel.Warn"/> 呈现，但仍按同一语义登记 + 打印，保证行为一致。
    /// </remarks>
    public Task<string?> RequestApprovalAsync(
        ToolApprovalRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var correlationId = NextCorrelationId();
        _renderer.Notice(
            NoticeLevel.Warn,
            $"收到非框架审批请求（{request.ToolName}）——P4-1 之后该路径预期不可达，请检查调用链。");

        // 合成框架要素以复用同一挂起登记（RequestId 由关联号派生，仅本 Demo 内部闭环使用）。
        var synthesized = new FrameworkToolApprovalRequest(
            RequestId: $"legacy-{correlationId}",
            ToolName: request.ToolName,
            ToolCallId: null,
            AppKey: request.AppKey,
            UserId: request.UserId,
            ConversationKey: request.ConversationKey,
            ArgumentsDigest: request.ArgumentsDigest,
            RequiredScopes: request.RequiredScopes);

        var now = DateTimeOffset.UtcNow;
        var pending = new PendingApproval(
            CorrelationId: correlationId,
            Framework: synthesized,
            CreatedAt: now,
            ExpiresAt: request.ExpiresAt == default ? now + ConsoleApprovalChannelState.DefaultLifetime : request.ExpiresAt);

        _state.TryAdd(pending);

        _renderer.ApprovalCard(new PendingApprovalViewModel(
            CorrelationId: pending.CorrelationId,
            RequestId: synthesized.RequestId,
            ToolName: synthesized.ToolName,
            ArgumentsDigest: synthesized.ArgumentsDigest,
            RequiredScopes: synthesized.RequiredScopes,
            ExpiresAt: pending.ExpiresAt));

        return Task.FromResult<string?>(correlationId);
    }

    private string NextCorrelationId()
        => $"demo-approval-{Interlocked.Increment(ref _seq):D3}";
}
