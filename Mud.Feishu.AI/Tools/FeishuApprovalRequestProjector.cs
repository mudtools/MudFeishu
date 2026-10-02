// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Mud.Feishu.AI.Tools;

/// <summary>
/// 框架审批请求（<see cref="ToolApprovalRequestContent"/>）→ 宿主可见投影
/// （<see cref="FrameworkToolApprovalRequest"/>）的<b>唯一</b>实现（R5-11 抽取）。
/// </summary>
/// <remarks>
/// <para>
/// 抽取动机与 R2-01b 同款：投影 + 通知此前只存在于 <c>ConversationalFeishuEventHandler</c> 内，
/// R5-11 的续跑闭环需要同一份逻辑——「每个新入口各写一遍」正是机制缺失，缺陷表现为
/// 「续跑轮的审批请求静默丢失」。抽取后：事件层（非流式/流式两处收集点）与续跑扩展共用同一实现。
/// </para>
/// <para>
/// <b>fail-closed 不变</b>：宿主批准通道未注册或抛异常时，写工具保持未执行，绝不降级为「自动批准」。
/// </para>
/// </remarks>
internal static class FeishuApprovalRequestProjector
{
    /// <summary>
    /// 从一组消息中提取待审批请求（无则空集合）。
    /// </summary>
    /// <param name="messages">模型回应消息集合（可空）。</param>
    /// <param name="appKey">应用唯一标识。</param>
    /// <param name="conversationKey">会话键（宿主关联审批界面用）。</param>
    /// <param name="chatId">触发会话的 chat_id（可空）。</param>
    /// <param name="userId">触发用户（可空）。</param>
    /// <returns>待提交宿主的审批请求。</returns>
    internal static List<FrameworkToolApprovalRequest> FromMessages(
        IEnumerable<ChatMessage>? messages,
        string? appKey,
        string? conversationKey,
        string? chatId,
        string? userId)
    {
        var collected = new List<FrameworkToolApprovalRequest>();
        if (messages is null)
        {
            return collected;
        }

        foreach (var message in messages)
        {
            Collect(message.Contents, appKey, conversationKey, chatId, userId, collected);
        }

        return collected;
    }

    /// <summary>
    /// 从一组内容中收集待审批请求（流式路径按更新逐个收集）。
    /// </summary>
    /// <param name="contents">内容集合（可空）。</param>
    /// <param name="appKey">应用唯一标识。</param>
    /// <param name="conversationKey">会话键。</param>
    /// <param name="chatId">触发会话的 chat_id（可空）。</param>
    /// <param name="userId">触发用户（可空）。</param>
    /// <param name="sink">收集目标。</param>
    internal static void Collect(
        IList<AIContent>? contents,
        string? appKey,
        string? conversationKey,
        string? chatId,
        string? userId,
        List<FrameworkToolApprovalRequest> sink)
    {
        if (contents is null)
        {
            return;
        }

        foreach (var content in contents)
        {
            if (content is not ToolApprovalRequestContent approval)
            {
                continue;
            }

            // ToolCallContent 是基类；具体形态是 FunctionCallContent（含 Name/Arguments）。
            var toolName = approval.ToolCall is FunctionCallContent call ? call.Name : null;

            sink.Add(new FrameworkToolApprovalRequest(
                RequestId: approval.RequestId,
                ToolName: toolName ?? "(unknown)",
                ToolCallId: approval.ToolCall?.CallId,
                AppKey: appKey ?? string.Empty,
                UserId: userId,
                ConversationKey: conversationKey,
                ArgumentsDigest: null,
                RequiredScopes: [],
                ChatId: chatId));
        }
    }

    /// <summary>
    /// 把待确认项交给宿主批准通道（未注册即静默跳过 = 降级为「纯提示」），并逐项记 Warning。
    /// </summary>
    /// <param name="channel">宿主批准通道（可空）。</param>
    /// <param name="logger">日志（可空）。</param>
    /// <param name="pending">待确认项。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    internal static async Task NotifyAsync(
        IFeishuToolApprovalChannel? channel,
        ILogger? logger,
        IReadOnlyList<FrameworkToolApprovalRequest> pending,
        CancellationToken cancellationToken)
    {
        foreach (var item in pending)
        {
            logger?.LogWarning(
                "写工具待人工确认（工具: {ToolName}, requestId: {RequestId}, appKey: {AppKey}）——工具尚未执行，宿主批准后回灌批准响应方可继续",
                item.ToolName, item.RequestId, item.AppKey);
        }

        if (channel is null)
        {
            return;
        }

        foreach (var item in pending)
        {
            try
            {
                await channel
                    .RequestFrameworkApprovalAsync(item, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // 与 FeishuToolBinding 同侧哲学：通道故障只降级为「无法发起确认」，不自动放行。
                logger?.LogWarning(ex,
                    "宿主批准通道提交失败（工具: {ToolName}, requestId: {RequestId}）——写工具保持未执行",
                    item.ToolName, item.RequestId);
            }
        }
    }
}
