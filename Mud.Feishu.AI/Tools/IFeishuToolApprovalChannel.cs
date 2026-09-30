// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools;

/// <summary>
/// 工具人工确认（HITL）的<b>宿主批准通道</b>：SDK 把「待确认」事件的全部要素交给宿主，
/// 由宿主在自有界面（飞书卡片/工单/审批单）完成批准——<b>批准状态所有权归宿主授权器，SDK 不签发任何凭据</b>。
/// </summary>
/// <remarks>
/// <para>
/// <b>三层分工</b>（WP3 后的 HITL 契约）：
/// </para>
/// <para>
/// 1. <b>MAF 审批管线</b>（<c>ApprovalRequiredAIFunction</c>）：写类工具的调用前拦截；
/// 批准只能来自宿主，模型侧不可自批复。
/// </para>
/// <para>
/// 2. <b>宿主授权器</b>（<see cref="IToolExecutionAuthorizer"/>）：批准状态的唯一所有者——
/// 首次收到 <c>NeedsUserConfirmation</c> → 记为挂起；经批准通道拿到摘要后建立「已批准」上下文；
/// 下次同 <c>(tool, argsDigest, appKey, userId)</c> 调用返回 <c>Allowed</c>。
/// </para>
/// <para>
/// 3. <b>SDK 执行链</b>：咨询授权器、通知宿主、<b>中性拒绝</b>（不含任何凭据）。
/// 不签发/不校验/不缓存批准状态。
/// </para>
/// <para>
/// <b>未注册 = 降级为「纯提示」</b>：模型只会收到「需要人工确认」，拿不到任何凭据，无法自批复（fail-closed）。
/// </para>
/// <para>
/// SDK 只定契约、不内实现（与 <see cref="IToolExecutionAuthorizer"/> 同款定位）。
/// </para>
/// </remarks>
public interface IFeishuToolApprovalChannel
{
    /// <summary>
    /// 提交一次待人工确认的工具调用，返回宿主侧关联 ID（批准界面据此回填）。
    /// </summary>
    /// <param name="request">待确认要素（含已脱敏的参数摘要）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>
    /// 宿主侧关联 ID；返回 <see langword="null"/> 表示宿主未生成关联号（仍按「已发起确认」处理）。
    /// </returns>
    /// <remarks>
    /// 通道抛异常时执行链<b>降级为纯提示</b>并记日志，绝不把敏感信息写进任何回填模型的文本。
    /// </remarks>
    Task<string?> RequestApprovalAsync(ToolApprovalRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// P4-1：提交一次<b>框架原生</b>待确认的工具调用（写工具经 <c>ApprovalRequiredAIFunction</c> 包装后，
    /// MAF <c>FunctionInvokingChatClient</c> 在调用<b>之前</b>产出的审批请求）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 与 <see cref="RequestApprovalAsync"/> 的区别：本方法<b>不涉及确认令牌</b>——
    /// 批准资格由 MAF 的 <c>ApprovalResponseBindingChatClient</c> 保证（只接受与框架请求绑定的响应），
    /// 因此不再需要「令牌往返模型上下文」这条危险路径。
    /// </para>
    /// <para>
    /// 本方法是会话语义上的<b>异步</b>：宿主在此登记待确认项，随后在自有界面完成批准，
    /// 再经 <c>ConversationalFeishuEventHandler.ResumeWithApprovalsAsync</c> 回灌结果继续本轮。
    /// 故实现不应阻塞等待人类点按钮。
    /// </para>
    /// <para>
    /// 通道抛异常时同样 <b>fail-closed</b>：写工具保持未执行，绝不降级为「自动批准」。
    /// </para>
    /// </remarks>
    /// <param name="request">框架审批请求要素。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>宿主侧关联号（可空）。</returns>
    Task<string?> RequestFrameworkApprovalAsync(
        FrameworkToolApprovalRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// P4-1：框架原生审批请求要素（由 MAF <see cref="Microsoft.Extensions.AI.ToolApprovalRequestContent"/>
/// 投影而来；<b>不含确认令牌</b>——批准资格由框架绑定保证）。
/// </summary>
/// <param name="RequestId">
/// 框架请求标识（实测形如 <c>ficc_{callId}</c>）。回灌响应时必须带回该值，
/// 否则 <c>ApprovalResponseBindingChatClient</c> 无法把响应绑定到原始请求 ⇒ 批准不生效。
/// </param>
/// <param name="ToolName">工具名（注册表契约名）。</param>
/// <param name="ToolCallId">模型原始调用 ID（可空；排障用）。</param>
/// <param name="AppKey">应用唯一标识。</param>
/// <param name="UserId">触发用户（可空）。</param>
/// <param name="ConversationKey">会话键（可空）。</param>
/// <param name="ArgumentsDigest">
/// 入参摘要（可空；供审批界面展示——<b>不得</b>回灌给模型）。
/// </param>
/// <param name="RequiredScopes">工具声明的权限点（查不到目录时为空）。</param>
public sealed record FrameworkToolApprovalRequest(
    string RequestId,
    string ToolName,
    string? ToolCallId,
    string AppKey,
    string? UserId,
    string? ConversationKey,
    string? ArgumentsDigest,
    IReadOnlyList<string> RequiredScopes);

/// <summary>
/// 待人工确认的工具调用要素（入参以摘要形式提供；不含模型原文）。
/// </summary>
/// <param name="ToolName">工具名（注册表契约名）。</param>
/// <param name="AppKey">应用唯一标识。</param>
/// <param name="UserId">触发用户（可空）。</param>
/// <param name="ConversationKey">会话键（可空）。</param>
/// <param name="ArgumentsDigest">参数摘要（已脱敏；供宿主建立"已批准"上下文，宿主不得修改）。</param>
/// <param name="RequiredScopes">工具声明的权限点。</param>
    /// <param name="Reason">授权器给出的待确认原因。</param>
    /// <param name="ExpiresAt">确认有效期（宿主批准界面据此显示倒计时；默认 10 分钟）。</param>
public sealed record ToolApprovalRequest(
    string ToolName,
    string AppKey,
    string? UserId,
    string? ConversationKey,
    string ArgumentsDigest,
    IReadOnlyList<string> RequiredScopes,
    string? Reason,
    DateTimeOffset ExpiresAt);
