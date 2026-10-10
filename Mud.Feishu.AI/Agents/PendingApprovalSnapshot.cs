// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Agents;

/// <summary>
/// 待人工确认（HITL）项的可持久化快照（R7 / C4a）。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么需要它</b>：<c>IFeishuToolApprovalChannel</c> 是「通知宿主」的同步点，通知完即返回；
/// 进程重启后宿主<b>无从得知</b>还有哪些写操作停在「等待确认」——这正是 R5-12 的孤儿审批形态
/// （会话历史里有未应答的审批请求）。本快照把待确认项落成<b>可查询、可过期、可幂等消费</b>的数据，
/// 宿主据此做「重进进程后仍能列出待办」。
/// </para>
/// <para>
/// <b>不含任何凭据（铁律不变）</b>：快照只带工具名、脱敏入参摘要、租户/会话维度标识与时间——
/// SDK 不签发批准凭据，也不缓存批准状态；批准状态的所有者仍是宿主授权器
/// （<c>IToolExecutionAuthorizer</c>）。
/// </para>
/// <para>
/// <b>入参摘要（<see cref="ArgumentsDigest"/>）</b>来自既有投影，已由执行链脱敏；
/// 它<b>不得</b>回灌给模型（仅供宿主审批界面展示）。
/// </para>
/// </remarks>
/// <param name="RequestId">
/// 框架请求标识（回灌批准响应时必须原样带回，否则框架无法把响应绑定到原始请求）。
/// </param>
/// <param name="ToolName">工具名（注册表契约名）。</param>
/// <param name="AppKey">应用唯一标识（多租户隔离维度：查询与消费都按它分区）。</param>
/// <param name="UserId">触发用户（可空）。</param>
/// <param name="ConversationKey">会话键（可空；续跑必须用同一个键加载会话）。</param>
/// <param name="ChatId">触发会话的 chat_id（可空；续跑重建工具上下文用）。</param>
/// <param name="ToolCallId">模型原始调用 ID（可空；排障用）。</param>
/// <param name="ArgumentsDigest">入参摘要（可空；已脱敏，仅供审批界面展示，不得回灌模型）。</param>
/// <param name="RequiredScopes">工具声明的权限点（查不到目录时为空集合）。</param>
/// <param name="CreatedAt">登记时间（UTC）。</param>
/// <param name="ExpiresAt">
/// 过期时间（UTC）：过期后<b>自动放弃</b>——不得出现「过期的批准仍生效」
/// （见 <see cref="IFeishuPendingApprovalStore"/> 的消费语义）。
/// </param>
public sealed record PendingApprovalSnapshot(
    string RequestId,
    string ToolName,
    string AppKey,
    string? UserId,
    string? ConversationKey,
    string? ChatId,
    string? ToolCallId,
    string? ArgumentsDigest,
    IReadOnlyList<string> RequiredScopes,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt)
{
    /// <summary>
    /// 默认确认有效期（10 分钟）——与 <c>ToolApprovalRequest.ExpiresAt</c> 的既有默认值同口径。
    /// </summary>
    /// <remarks>
    /// 宿主可在宿主侧通道实现里按业务调整（本常量只是 SDK 自动登记的默认值）。
    /// </remarks>
    public static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(10);

    /// <summary>快照是否已过期（<paramref name="now"/> 达到或超过 <see cref="ExpiresAt"/>）。</summary>
    /// <param name="now">当前时间（UTC）。</param>
    /// <returns>是否过期。</returns>
    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;

    /// <summary>
    /// 由框架审批请求投影出快照（执行链自动登记路径的唯一构造入口）。
    /// </summary>
    /// <param name="request">框架审批请求要素。</param>
    /// <param name="now">当前时间（UTC）。</param>
    /// <param name="ttl">有效期（可空 → <see cref="DefaultTtl"/>）。非正值视为无效，回落默认值。</param>
    /// <returns>待确认快照。</returns>
    public static PendingApprovalSnapshot FromRequest(
        FrameworkToolApprovalRequest request,
        DateTimeOffset now,
        TimeSpan? ttl = null)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var effectiveTtl = ttl is { } value && value > TimeSpan.Zero ? value : DefaultTtl;

        return new PendingApprovalSnapshot(
            RequestId: request.RequestId,
            ToolName: request.ToolName,
            AppKey: request.AppKey,
            UserId: request.UserId,
            ConversationKey: request.ConversationKey,
            ChatId: request.ChatId,
            ToolCallId: request.ToolCallId,
            ArgumentsDigest: request.ArgumentsDigest,
            RequiredScopes: request.RequiredScopes ?? [],
            CreatedAt: now,
            ExpiresAt: now + effectiveTtl);
    }
}
