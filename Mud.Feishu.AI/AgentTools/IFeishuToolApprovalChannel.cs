// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.AgentTools;

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
    /// 本方法是会话语义上的<b>异步</b>：宿主在此登记待确认项，随后在自有界面完成批准，再<b>回灌续跑</b>
    /// （见下方宿主续跑义务）。故实现不应阻塞等待人类点按钮（实现内不得 <c>Task.Delay</c> 轮询等待批准）。
    /// </para>
    /// <para>
    /// <b>宿主续跑义务（R5-2：批准之后的唯一正确路径）</b>——推荐直接调用 SDK 闭环
    /// <see cref="Mud.Feishu.AI.Agents.FeishuAgentApprovalExtensions.RunApprovalContinuationAsync"/>，
    /// 它一次完成下列全部动作。手工实现时四步缺一不可：
    /// <list type="number">
    /// <item>
    /// 用<b>同一个</b>会话键（<see cref="FrameworkToolApprovalRequest.ConversationKey"/>）在<b>同一</b>
    /// <c>FeishuAgent</c> 上加载会话（框架的待审批记录驻留会话状态袋，框架记录即批准权威；换键/换实例即失配）；
    /// </item>
    /// <item>
    /// 构造 <c>ToolApprovalResponseContent</c> 装入 <c>ChatMessage(ChatRole.User, …)</c>：
    /// 最稳妥的构造方式是取框架记录的原始请求
    /// （<c>ToolApprovalRequestContent.CreateResponse(approved, reason)</c>，SDK 闭环内部即如此），
    /// 使 CallId 与入参同源；即使宿主自行构造，绑定层也会按 <see cref="FrameworkToolApprovalRequest.RequestId"/>
    /// 把工具调用重绑定到框架记录的原始请求上，故 <c>RequestId</c> 必须原样带回；
    /// </item>
    /// <item>
    /// <b>RunAsync 之前</b>重建工具执行上下文：
    /// <c>toolContextAccessor.Begin(new FeishuToolContext(AppKey, ConversationKey, ChatId, UserId))</c>
    /// ——续跑轮不在事件流内，缺此步工具将 fail-closed 结构化拒绝（多租户隔离禁止默认 appKey 兜底，
    /// TMA2-20）。<b>批准 ≠ 放行</b>：租户上下文必须由宿主显式重建；
    /// </item>
    /// <item>
    /// 跑完 <c>SaveSessionAsync</c> 落库（含框架绑定层消费掉的审批记录），并把本轮回复投递给用户
    /// （回复下发不在 SDK 闭环内——<c>ReplyAsync</c> 属派生事件处理器）。
    /// </item>
    /// </list>
    /// 未按上述执行的续跑会得到工具拒绝或审批绑定失败——均为 fail-closed，不产生越权执行。
    /// </para>
    /// <para>
    /// <b>未续跑 ≠ 无害（R5-12）</b>：发起确认的那一轮会把「待应答审批请求」写进会话历史；MEAI
    /// <c>FunctionInvokingChatClient</c> 对<b>整段入站历史</b>做审批配对校验，残留未应答的审批请求会让
    /// <b>该会话的后续每一轮</b>直接抛 <c>InvalidOperationException</c>（工具调用未被放行，但会话不可用）。
    /// SDK 已在事件层对该形态做自愈（新的用户轮次会放弃该待确认项并摘除孤儿审批、清空框架记录），
    /// 因此「宿主从不续跑」只会退化为「该次写操作被放弃」，不会毒化会话。
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
/// <param name="ChatId">
/// 触发会话的 chat_id（可空；R5-11 追加）。续跑轮重建工具执行上下文
/// （<see cref="FeishuToolContext.ChatId"/>）时使用——续跑不在事件流内，宿主侧只有本投影可依。
/// </param>
public sealed record FrameworkToolApprovalRequest(
    string RequestId,
    string ToolName,
    string? ToolCallId,
    string AppKey,
    string? UserId,
    string? ConversationKey,
    string? ArgumentsDigest,
    IReadOnlyList<string> RequiredScopes,
    string? ChatId = null);

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
