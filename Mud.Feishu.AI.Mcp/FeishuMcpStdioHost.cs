// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Mcp;

/// <summary>
/// stdio 传输的 MCP 宿主循环：<b>一行一条 JSON-RPC 消息</b>（MCP stdio 的线格式），
/// 读 → 处理 → 写响应 → flush。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么逐行顺序处理（不做并发）</b>：MCP stdio 的响应靠 <c>id</c> 关联，理论上可乱序；
/// 但工具执行会切换租户上下文（<c>AsyncLocal</c> + <c>BeginScope</c>），并发处理会让
/// "同一进程内的不同调用"共享同一份租户状态，一旦有实现缺陷就变成跨租户串号。
/// 顺序处理把该风险从"依赖实现正确"降为"结构上不存在"；代价是吞吐，而 MCP 的调用形态
/// （Agent 逐个工具调用）对吞吐不敏感。
/// </para>
/// <para>
/// <b>EOF 即正常退出</b>：客户端关闭 stdin（<c>ReadLineAsync</c> 返回 <see langword="null"/>）时，
/// 宿主循环以返回已处理条数的方式结束——这是 MCP stdio 的标准终止方式，不是错误。
/// </para>
/// <para>
/// <b>不释放宿主 writer</b>：<c>Console.Out</c> 由进程持有，包装它的 <see cref="TextWriter"/>
/// 若被本类释放会连带影响调用方后续输出（既有 <c>FeishuEventNdjsonBridge</c> 同款纪律）。
/// </para>
/// </remarks>
public sealed class FeishuMcpStdioHost
{
    private readonly FeishuMcpToolServer _server;
    private readonly TextReader _input;
    private readonly TextWriter _output;
    private readonly ILogger? _logger;

    /// <summary>
    /// 初始化 <see cref="FeishuMcpStdioHost"/>。
    /// </summary>
    /// <param name="server">协议层服务端。</param>
    /// <param name="input">输入（通常 <c>Console.In</c>）。</param>
    /// <param name="output">输出（通常 <c>Console.Out</c>）。</param>
    /// <param name="logger">日志（可空）。</param>
    public FeishuMcpStdioHost(
        FeishuMcpToolServer server,
        TextReader input,
        TextWriter output,
        ILogger<FeishuMcpStdioHost>? logger = null)
    {
        _server = server ?? throw new ArgumentNullException(nameof(server));
        _input = input ?? throw new ArgumentNullException(nameof(input));
        _output = output ?? throw new ArgumentNullException(nameof(output));
        _logger = logger;
    }

    /// <summary>
    /// 运行宿主循环直到输入结束（或取消）。
    /// </summary>
    /// <param name="cancellationToken">取消令牌（取消后循环在下一次读之前退出）。</param>
    /// <returns>已处理的请求数（通知不计入）。</returns>
    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        var handled = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await _input.ReadLineAsync().ConfigureAwait(false);
            if (line is null)
            {
                // EOF：客户端关闭了 stdin（MCP stdio 的标准终止方式）。
                break;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var response = await _server.HandleMessageAsync(line, cancellationToken).ConfigureAwait(false);
            if (response is null)
            {
                continue; // 通知：按协议不产生响应
            }

            await _output.WriteLineAsync(response).ConfigureAwait(false);

            // 每条响应必须立刻 flush：stdio 是管道而非终端，缓冲会让客户端"看起来没有响应"。
            await _output.FlushAsync().ConfigureAwait(false);
            handled++;
        }

        _logger?.LogInformation("MCP stdio 会话结束：已处理 {Count} 条请求", handled);
        return handled;
    }
}
