// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任。
// -----------------------------------------------------------------------

using Mud.Feishu.AI.Diagnostics;

namespace Mud.Feishu.AI.Tools.Hosting;

/// <summary>
/// 审计出口 → 成本核算出口的桥（E-2）：把每次工具调用的审计记录折进
/// <see cref="IFeishuUsageMeter"/>，同时<b>原样转发</b>给内层 sink（宿主自己的落库链零变化）。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么用装饰器而不是改审计记录形状</b>：<see cref="ToolExecutionAuditRecord"/> 是宿主落库契约，
/// 加字段会让既有宿主的 JSONL/表结构被迫迁移，而"用量聚合"只有部分宿主需要——故按能力面叠加，
/// 不按数据面扩张（审计记录已有的 <c>AppKey</c> / <c>Decision</c> / <c>ConversationKey</c> 足够归因）。
/// </para>
/// <para>
/// <b>失败判据</b>：<c>Decision</c> 只有 <c>allowed</c> / <c>denied</c> 两态；"放行但业务失败"的样本
/// 以 <c>Reason</c> 非空表达（拒绝路径同样带 Reason，故先判 denied 再判 failed）。
/// </para>
/// </remarks>
/// <param name="meter">用量聚合（必填）。</param>
/// <param name="inner">内层审计出口（可空 = 仅聚合不落库）。</param>
public sealed class FeishuUsageAuditSink(IFeishuUsageMeter meter, IToolExecutionAuditSink? inner = null)
    : IToolExecutionAuditSink
{
    private readonly IFeishuUsageMeter _meter = meter ?? throw new ArgumentNullException(nameof(meter));

    /// <inheritdoc />
    public async Task WriteAsync(ToolExecutionAuditRecord record, CancellationToken cancellationToken = default)
    {
        if (record is null)
            throw new ArgumentNullException(nameof(record));

        var denied = string.Equals(record.Decision, FeishuToolDiagnostics.DecisionDenied, StringComparison.Ordinal);
        var failed = !denied && record.Reason is not null;

        _meter.RecordToolCall(
            record.AppKey,
            record.ConversationKey,
            record.ToolName,
            record.IsWrite,
            denied,
            failed,
            record.DurationMs);

        if (inner is not null)
        {
            await inner.WriteAsync(record, cancellationToken).ConfigureAwait(false);
        }
    }
}
