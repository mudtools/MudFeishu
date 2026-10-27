// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.CapabilityDemo.Host;

/// <summary>一条挂起的人工确认请求（续跑必须原样带回 <see cref="Framework"/>）。</summary>
internal sealed record PendingApproval(
    string CorrelationId,
    FrameworkToolApprovalRequest Framework,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt);

/// <summary>
/// 挂起审批 + 一次性放行凭据的唯一状态（单例）。
/// </summary>
/// <remarks>
/// <b>为什么需要 grant</b>：框架回执（MAF 的 approved=true）只说明"人点过头"，
/// 而执行链还会再问一次授权器。若批准后不更新授权器状态，续跑轮会被
/// <c>authorization_denied</c> 二次拒绝（见方案 ADR-04 与 R4-1）。
/// grant 的语义是"针对某工具的一次性放行"，消费即失效，避免一次批准放行一串写操作。
/// </remarks>
internal sealed class PendingApprovalBoard
{
    /// <summary>与 SDK <c>PendingApprovalSnapshot.DefaultTtl</c> 对齐（10 分钟）。</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);

    private readonly ConcurrentDictionary<string, PendingApproval> _pending = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, int> _grants = new(StringComparer.Ordinal);
    private int _seq;

    public bool HasPending => !_pending.IsEmpty;

    public int PendingCount => _pending.Count;

    /// <summary>登记并返回面向用户的短关联号（同 RequestId 重复登记保留首条）。</summary>
    public string Register(FrameworkToolApprovalRequest request)
    {
        var now = DateTimeOffset.UtcNow;
        var correlation = $"cap-{Interlocked.Increment(ref _seq):D3}";
        _pending.TryAdd(request.RequestId, new PendingApproval(correlation, request, now, now + Lifetime));
        return _pending.TryGetValue(request.RequestId, out var stored) ? stored.CorrelationId : correlation;
    }

    public IReadOnlyList<PendingApproval> Snapshot()
        => _pending.Values.OrderBy(static p => p.CreatedAt).ToArray();

    public PendingApproval? Find(string correlationId)
        => _pending.Values.FirstOrDefault(
            p => string.Equals(p.CorrelationId, correlationId, StringComparison.OrdinalIgnoreCase));

    /// <summary>摘除一条（过期项同样摘除，返回 null 表示不可批准）。</summary>
    public PendingApproval? Take(string correlationId)
    {
        var pending = Find(correlationId);
        if (pending is null)
        {
            return null;
        }

        _pending.TryRemove(pending.Framework.RequestId, out _);
        return pending.ExpiresAt <= DateTimeOffset.UtcNow ? null : pending;
    }

    public void Remove(string requestId) => _pending.TryRemove(requestId, out _);

    /// <summary>发放一次性 grant（按工具名计数）。</summary>
    public void Grant(string toolName)
        => _grants.AddOrUpdate(toolName, 1, static (_, current) => current + 1);

    /// <summary>消费一张 grant（不存在即 false）。</summary>
    public bool TryConsumeGrant(string toolName)
    {
        while (_grants.TryGetValue(toolName, out var current) && current > 0)
        {
            if (_grants.TryUpdate(toolName, current - 1, current))
            {
                return true;
            }
        }

        return false;
    }

    public int Clear()
    {
        var count = _pending.Count;
        _pending.Clear();
        _grants.Clear();
        return count;
    }
}
