// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.Conversations;

namespace Mud.Feishu.AI.Events;

/// <summary>
/// 会话上下文装配器：把结构化事件事实拼进 Prompt 的插件（可多注册）。
/// </summary>
/// <remarks>
/// <para>
/// 只放已脱敏的非敏感内容（Phase 1 §3.1）；实现方按注册顺序依次产出片段，
/// 由 <see cref="ConversationalFeishuEventHandler{T}"/> 统一拼接为用户消息。
/// </para>
/// </remarks>
public interface IContextAssembler
{
    /// <summary>装配顺序（小者在前）。</summary>
    int Order { get; }

    /// <summary>
    /// 装配一条 Prompt 片段。
    /// </summary>
    /// <param name="request">规范化会话请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>片段文本；返回空字符串/null 表示本装配器对本次事件无贡献。</returns>
    Task<string?> AssembleAsync(ConversationRequest request, CancellationToken cancellationToken = default);
}
