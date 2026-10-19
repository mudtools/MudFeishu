// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.AI.Agent.Events;

namespace Mud.Feishu.AI.AgentTools;

/// <summary>
/// 产品作用域端口（<see cref="ITurnScopeFactory"/>）的<b>飞书适配</b>：把中立
/// <see cref="TurnScopeContext"/> 投影为飞书工具执行上下文
/// （<see cref="IFeishuToolContextAccessor.Begin"/> 的 <see cref="FeishuToolContext"/>）。
/// </summary>
/// <remarks>
/// <para>
/// <b>接缝适配器</b>：事件薄壳把「工具执行上下文的建立」交由管线经
/// <see cref="ITurnScopeFactory"/> 端口回调；本适配器是唯一的飞书投影点——
/// 中立管线不命名任何产品类型，产品侧 <c>AsyncLocal</c> 语义原样保留（含所有权校验）。
/// </para>
/// <para>
/// <b>internal</b>：仅被本工程的会话事件薄壳装配（宿主经构造参数 <c>toolContextAccessor</c>
/// 间接获得），不进公开面。
/// </para>
/// </remarks>
/// <param name="accessor">工具执行上下文访问器（产品事实来源）。</param>
internal sealed class FeishuToolContextScopeFactory(IFeishuToolContextAccessor accessor) : ITurnScopeFactory
{
    /// <inheritdoc />
    public IDisposable BeginToolScope(TurnScopeContext context)
        => accessor.Begin(new FeishuToolContext(
            context.TenantKey,
            context.ConversationKey,
            ChatId: context.ChatId,
            UserId: context.UserId,
            ThreadId: context.ThreadId));
}
