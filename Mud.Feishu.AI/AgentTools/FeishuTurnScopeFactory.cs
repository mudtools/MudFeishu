// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.AI.Agent.Events;

namespace Mud.Feishu.AI.AgentTools;

/// <summary>
/// 产品作用域端口的中立化实现：把中立 <see cref="TurnScopeContext"/> 映射为飞书工具执行的
/// 租户上下文作用域（<see cref="IFeishuToolContextAccessor"/>）。
/// </summary>
/// <remarks>
/// <para>
/// <b>唯一适配点</b>：中立管线/续跑闭环只能经端口进出产品上下文，
/// 而飞书侧的唯一建立点就是 <see cref="IFeishuToolContextAccessor.Begin"/>——
/// 本类是该事实的<b>唯一</b>适配点，任何新增入口都不该再写一遍映射。
/// </para>
/// <para>
/// <b>作用域语义</b>：<see cref="IFeishuToolContextAccessor.Begin"/> 返回的作用域释放时恢复
/// 进入前的上下文（既有实现已保证幂等 + 所有权校验），本类<b>不包装</b>该语义（避免双重恢复逻辑）。
/// </para>
/// </remarks>
/// <param name="accessor">飞书工具执行上下文访问器。</param>
internal sealed class FeishuTurnScopeFactory(IFeishuToolContextAccessor accessor) : ITurnScopeFactory
{
    private readonly IFeishuToolContextAccessor _accessor =
        accessor ?? throw new ArgumentNullException(nameof(accessor));

    /// <inheritdoc />
    public IDisposable BeginToolScope(TurnScopeContext context)
        => _accessor.Begin(new FeishuToolContext(
            context.TenantKey,
            context.ConversationKey,
            ChatId: context.ChatId,
            UserId: context.UserId,
            ThreadId: context.ThreadId));
}
