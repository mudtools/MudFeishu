// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tests.Agents;

/// <summary>
/// T4.0 前置探针：验证 MAF <c>ChatClientAgent</c> 是否自带工具调用迭代上限。
/// </summary>
/// <remarks>
/// <para>
/// 替身 <c>IChatClient</c> 永远返回 <c>FunctionCallContent</c>（不返回纯文本），
/// 观察 <c>RunAsync</c> 是否终止、迭代次数、是否抛异常。
/// </para>
/// <para>
/// <b>探针结论（实测）</b>：MAF <c>ChatClientAgent</c> <b>有内建迭代上限</b>——
/// 在约 40~41 次工具调用后自行终止（无异常，返回最后的模型响应）。
/// 因此 T4.1 只需做「上限可配 + 可观测」，不需自建 Decorator 计数器。
/// </para>
/// </remarks>
public class MafIterationLimitProbeTests
{
    /// <summary>
    /// 替身 <see cref="IChatClient"/> 永远返回工具调用——模拟模型无限循环调用工具。
    /// </summary>
    private sealed class InfiniteToolCallChatClient : IChatClient
    {
        private int _callCount;
        public int CallCount => _callCount;

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _callCount);
            var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, [
                new FunctionCallContent("test.echo", Guid.NewGuid().ToString(), new Dictionary<string, object?>
                {
                    ["msg"] = "loop",
                }),
            ]));
            return Task.FromResult(response);
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException("探针不测试流式路径");

        public void Dispose() { }
        public object? GetService(Type serviceType, object? serviceKey = null) => null;
    }

    [Fact]
    public async Task Probe_InfiniteToolCalls_MafHasBuiltInLimit_Around40Calls()
    {
        // 探针结论：MAF ChatClientAgent 有内建迭代上限（约 40 次）
        // 替身永远返回 FunctionCallContent，MAF 在约 40 次后自行终止
        var client = new InfiniteToolCallChatClient();

        var agentOptions = new ChatClientAgentOptions
        {
            ChatOptions = new ChatOptions
            {
                Instructions = "你是测试助手",
            },
        };

        var agent = new ChatClientAgent(client, agentOptions);
        var session = await agent.CreateSessionAsync();

        // 不传 CancellationToken——验证 MAF 自行终止
        _ = await agent.RunAsync("你好", session, cancellationToken: CancellationToken.None);

        // MAF 在约 40 次后终止（不抛异常）
        // 实测值：41 次（MAF 1.20.0 / MEAI 10.9.0）
        client.CallCount.Should().BeLessThan(100,
            "MAF ChatClientAgent 有内建迭代上限，在约 40 次后自行终止");
        client.CallCount.Should().BeGreaterThan(0,
            "替身至少被调用一次");
    }
}
