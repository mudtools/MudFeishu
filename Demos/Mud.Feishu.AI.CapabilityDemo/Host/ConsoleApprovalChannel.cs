// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
// -----------------------------------------------------------------------

using Mud.Feishu.AI.CapabilityDemo.Common;

namespace Mud.Feishu.AI.CapabilityDemo.Host;

/// <summary>
/// 宿主批准通道：<b>只登记、立即返回</b>——人的决定由后续 <c>/approve</c> 显式触发续跑。
/// </summary>
/// <remarks>
/// 阻塞等待会掩盖 HITL 的真实工程形态（飞书卡片 / 工单都是异步回灌），也与契约
/// "实现内不得轮询等待批准"相悖。附带收益：等待期仍可用 <c>/pending</c>、<c>/audit</c>。
/// </remarks>
internal sealed class ConsoleApprovalChannel(PendingApprovalBoard board) : IFeishuToolApprovalChannel
{
    public Task<string?> RequestFrameworkApprovalAsync(
        FrameworkToolApprovalRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var correlation = board.Register(request);
        var pending = board.Find(correlation)!;

        DemoConsole.ApprovalCard(
            pending.CorrelationId,
            request.ToolName,
            request.ArgumentsDigest,
            request.RequiredScopes,
            pending.ExpiresAt);

        return Task.FromResult<string?>(correlation);
    }

    /// <summary>旧令牌路径：本 Demo 不签发凭据，恒返回 null（正常不可达）。</summary>
    public Task<string?> RequestApprovalAsync(
        ToolApprovalRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult<string?>(null);
}
