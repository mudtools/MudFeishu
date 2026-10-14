// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.AI.Abstractions.Conversations;

namespace Mud.AI.Agent.Events;

/// <summary>
/// 回复下发端口（② 式）：中立回合管线把「整段回复文本送达用户」委托给宿主实现。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么是端口</b>：回复通道是<b>产品事实</b>（飞书侧经 <c>IFeishuTenantV1Message.ReplyMessageAsync</c>
/// 以触发消息为父消息回复；其他生态各有通道）。中立管线不命名任何产品类型（方向性守卫），
/// 故以端口承载——产品事件处理器薄壳用自身的 <c>ReplyAsync</c> 钩子实现本端口。
/// </para>
/// <para>
/// <b>失败语义</b>：实现内异常照常传播给管线（管线不捕获）——幂等回滚 + 重投递由事件层
/// at-least-once 契约承接（与既有行为逐字节一致）。租户作用域（回复前切租户）也由实现自理：
/// 管线对租户上下文零感知。
/// </para>
/// <para>
/// <b>补发（outbox replay）也走本端口</b>：管线在「上一轮已生成未确认送达」的补发路径上
/// 调用同一端口，宿主无需区分首次回复与补发。
/// </para>
/// </remarks>
public interface IConversationReplySink
{
    /// <summary>
    /// 把模型最终回答（或补发文本）送达用户。
    /// </summary>
    /// <param name="request">规范化会话请求（实现据此定位回复目标）。</param>
    /// <param name="responseText">回复文本（非空；空回复已被管线守卫拦截）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>任务。实现内异常向上传播（不捕获）。</returns>
    Task DeliverAsync(ConversationRequest request, string responseText, CancellationToken cancellationToken = default);
}
