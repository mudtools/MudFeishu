// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
// -----------------------------------------------------------------------

using Mud.Feishu.AI.CapabilityDemo.Host;

namespace Mud.Feishu.AI.CapabilityDemo.SelfCheck;

/// <summary>组 5：审批板（登记即返回、grant 一次性、重复批准无效、清场）。</summary>
internal static class ApprovalBoardChecks
{
    private const string Tool = "bitable.update_record";

    public static IEnumerable<CheckResult> Run()
    {
        const string group = "approval-board";
        var board = new PendingApprovalBoard();
        var request = Request("req-1");

        var correlation = board.Register(request);
        yield return new CheckResult(group, "登记返回关联号", correlation.StartsWith("cap-", StringComparison.Ordinal),
            "cap-xxx", correlation);

        yield return new CheckResult(group, "登记后 HasPending", board.HasPending);

        var duplicate = board.Register(request);
        yield return new CheckResult(group, "同 RequestId 重复登记保留首条",
            string.Equals(duplicate, correlation, StringComparison.Ordinal) && board.PendingCount == 1,
            correlation, $"{duplicate} / count={board.PendingCount}");

        board.Grant(Tool);
        yield return new CheckResult(group, "grant 一次性：第一次放行", board.TryConsumeGrant(Tool));
        yield return new CheckResult(group, "grant 一次性：第二次拒绝", !board.TryConsumeGrant(Tool));

        var taken = board.Take(correlation);
        yield return new CheckResult(group, "Take 返回原请求", taken?.Framework.RequestId == request.RequestId);
        yield return new CheckResult(group, "Take 后不可重复批准", board.Take(correlation) is null);
        yield return new CheckResult(group, "Take 后挂起清零", !board.HasPending);

        board.Grant(Tool);
        var cleared = board.Clear();
        yield return new CheckResult(group, "Clear 清空挂起与 grant",
            cleared == 0 && !board.HasPending && !board.TryConsumeGrant(Tool),
            "0 / 无挂起 / 无 grant",
            $"{cleared} / pending={board.PendingCount}");

        yield return new CheckResult(group, "未知关联号返回 null", board.Take("cap-999") is null);
    }

    private static FrameworkToolApprovalRequest Request(string requestId)
        => new(
            RequestId: requestId,
            ToolName: Tool,
            ToolCallId: "call-1",
            TenantKey: "app-a",
            UserId: "ou_cap_demo_user",
            ConversationKey: "cap:app-a:conversation:user:a5",
            ArgumentsDigest: "table_id:string(7)",
            RequiredScopes: ["bitable:app"]);
}
