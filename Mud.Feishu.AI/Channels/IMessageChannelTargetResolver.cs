// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.Conversations;

namespace Mud.Feishu.AI.Channels;

/// <summary>
/// 流式目标解析能力接口（可选能力，AI-FD-D12 P2D-2a/2b）：<see cref="IMessageChannel"/>
/// 实现的目标参数语义由各通道自解释（编辑通道 = chat_id；应用消息卡片通道 = 接收用户 open_id）。
/// 实现本接口的通道可按会话请求解析出自己的目标；事件处理器优先取其结果，
/// 缺省回退 <c>ResolveStreamTargetChatId</c> 既有逻辑（<c>request.ChatId</c> → 群聊 SubjectId）。
/// </summary>
public interface IMessageChannelTargetResolver
{
    /// <summary>
    /// 按会话请求解析本通道的流式目标。
    /// </summary>
    /// <param name="request">规范化会话请求。</param>
    /// <returns>目标标识（通道自解释）；返回 <see langword="null"/> 表示本通道对本次事件不适用（如卡片流不投群聊），调用方回退默认解析。</returns>
    string? ResolveStreamTarget(ConversationRequest request);
}

/// <summary>
/// 流式会话请求环境量（AI-FD-D12 P2D-2a）：把 <see cref="ConversationRequest"/>
/// 经 <see cref="AsyncLocal{T}"/> 传递给通道降级链——链内各子通道目标语义不同
/// （卡片流要 open_id、编辑通道要 chat_id），降级时需按子通道各自重解析，事件处理器零感知。
/// </summary>
/// <remarks>
/// 仅在事件处理器与 SDK 自带通道之间传递，作用域释放时恢复进入前的值（嵌套 Begin 语义正确）；
/// 外部直调通道时环境量为空，通道按入参原样使用（契约文档见各通道实现）。
/// </remarks>
public static class StreamingRequestContext
{
    private static readonly AsyncLocal<ConversationRequest?> CurrentRequest = new();

    /// <summary>当前会话请求（无环境时为 <see langword="null"/>）。</summary>
    public static ConversationRequest? Current => CurrentRequest.Value;

    /// <summary>建立环境（事件处理器在流式管线进入前调用；返回值 Dispose 时恢复进入前的值）。</summary>
    /// <param name="request">规范化会话请求。</param>
    /// <returns>作用域释放器。</returns>
    public static IDisposable Begin(ConversationRequest request)
    {
        var previous = CurrentRequest.Value;
        CurrentRequest.Value = request ?? throw new ArgumentNullException(nameof(request));
        return new Scope(previous);
    }

    private sealed class Scope(ConversationRequest? previous) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            // 嵌套 Begin 时恢复外层值（对齐 FeishuToolContextAccessor.RestoreScope）；
            // 直接置 null 会让嵌套场景少读一层的环境（P2-3）。
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                CurrentRequest.Value = previous;
            }
        }
    }
}
