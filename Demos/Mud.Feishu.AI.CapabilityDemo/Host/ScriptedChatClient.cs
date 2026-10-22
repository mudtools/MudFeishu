// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.CapabilityDemo.Host;

/// <summary>
/// 脚本化模型：首轮返回固定 <c>FunctionCallContent</c>，其后返回终态文本。
/// </summary>
/// <remarks>
/// A4 / <c>--verify</c> 用它把"执行链的拒绝"与"模型是否听话"解耦：
/// 无 API Key、无 token 成本、结果可复现。
/// </remarks>
internal sealed class ScriptedChatClient(Func<ChatMessage> firstResponse, string finalText) : IChatClient
{
    private int _calls;

    public int Calls => _calls;

    /// <summary>
    /// 终态轮从 <c>tool</c> 消息里取到的<b>模型可见回填文本</b>（<c>ToolResult.ToString()</c> 口径）。
    /// </summary>
    /// <remarks>
    /// 自检用它读取工具的真实回填：审计 sink 只覆盖<b>绑定内</b>的决策，而「上下文缺失 / 空 appKey」
    /// 在执行入口就失败、不进审计——那两条只能用模型看到的东西来断言。
    /// </remarks>
    public string? LastToolResultText { get; private set; }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var index = Interlocked.Increment(ref _calls) - 1;

        if (index > 0)
        {
            LastToolResultText = messages
                .SelectMany(static m => m.Contents)
                .OfType<FunctionResultContent>()
                .Select(static c => c.Result?.ToString())
                .LastOrDefault(static text => !string.IsNullOrEmpty(text));
        }

        var message = index == 0
            ? firstResponse()
            : new ChatMessage(ChatRole.Assistant, finalText);

        return Task.FromResult(new ChatResponse(message));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("本 Demo 的脚本化路径只覆盖非流式。");

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }

    /// <summary>构造"调用某工具"的首轮响应。</summary>
    public static Func<ChatMessage> ToolCall(string toolName, IDictionary<string, object?> arguments)
        => () => new ChatMessage(
            ChatRole.Assistant,
            [new FunctionCallContent($"call-{Guid.NewGuid():N}", toolName, arguments)]);
}

/// <summary>不需要模型的幕（A6 / MCP）：被误用时立刻响亮失败，而不是静默返回空。</summary>
internal sealed class UnusedChatClient : IChatClient
{
    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("本幕不装配模型客户端；若被调用说明装配错位。");

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("本幕不装配模型客户端；若被调用说明装配错位。");

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}
