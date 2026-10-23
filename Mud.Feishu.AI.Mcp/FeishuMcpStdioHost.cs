// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text;

namespace Mud.Feishu.AI.Mcp;

/// <summary>
/// stdio 传输的 MCP 宿主循环：<b>一行一条 JSON-RPC 消息</b>，读 → 处理 → 写响应 → flush。
/// </summary>
/// <remarks>
/// <para><b>逐行顺序处理（不做并发）</b>：工具执行会切换租户上下文（<c>AsyncLocal</c>），
/// 并发处理会让不同调用共享租户状态——顺序处理把跨租户串号风险从"依赖实现正确"降为"结构上不存在"。</para>
/// <para><b>有界读</b>：超过 <see cref="FeishuMcpServerOptions.MaxRequestBytes"/> 的行不累积
/// （继续读到行尾保持边界同步），回 <c>-32600</c> 并继续。取消经断开 stdin 表达（stdio 下 <c>notifications/cancelled</c> 不适用）。</para>
/// </remarks>
public sealed class FeishuMcpStdioHost
{
    /// <summary>单条请求的默认字符上限（与配置默认值一致；未注入 options 时使用）。</summary>
    private const int DefaultMaxRequestChars = 1_048_576;

    private readonly FeishuMcpToolServer _server;
    private readonly TextReader _input;
    private readonly TextWriter _output;
    private readonly ILogger? _logger;
    private readonly int _maxRequestChars;

    /// <summary>流读取缓冲（跨行复用：一次 ReadAsync 可能带回多行）。</summary>
    private readonly char[] _readBuffer = new char[512];
    private int _readBufferCount;
    private int _readBufferOffset;

    /// <summary>
    /// 初始化 <see cref="FeishuMcpStdioHost"/>。
    /// </summary>
    /// <param name="server">协议层服务端。</param>
    /// <param name="input">输入（通常 <c>Console.In</c>）。</param>
    /// <param name="output">输出（通常 <c>Console.Out</c>）。</param>
    /// <param name="logger">日志（可空）。</param>
    /// <param name="options">MCP 配置（可空；仅用于读取单条请求的字符上限，缺省时用内置默认值）。</param>
    public FeishuMcpStdioHost(
        FeishuMcpToolServer server,
        TextReader input,
        TextWriter output,
        ILogger<FeishuMcpStdioHost>? logger = null,
        IOptions<FeishuMcpServerOptions>? options = null)
    {
        _server = server ?? throw new ArgumentNullException(nameof(server));
        _input = input ?? throw new ArgumentNullException(nameof(input));
        _output = output ?? throw new ArgumentNullException(nameof(output));
        _logger = logger;

        // 上限语义 = 字符数（每个 char 2 字节，内存上界约 2×配置值）；
        // 纯 ASCII 请求下字符数 == 字节数，含中文时按字符计更保守（字符数 ≤ 字节数）。
        var configured = options?.Value?.MaxRequestBytes ?? 0;
        _maxRequestChars = configured > 0 ? configured : DefaultMaxRequestChars;
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
            var (line, overflow) = await ReadBoundedLineAsync().ConfigureAwait(false);

            if (line is null)
            {
                // EOF：客户端关闭了 stdin（MCP stdio 的标准终止方式）。
                break;
            }

            if (overflow)
            {
                // 超限：回 -32600 并继续；不记完整内容（超长行可能是攻击载荷），只记上限。
                _logger?.LogWarning("MCP stdio 请求超过 {Max} 字符上限，已拒绝该行", _maxRequestChars);
                await _output.WriteLineAsync(BuildOversizedEnvelope()).ConfigureAwait(false);
                await _output.FlushAsync().ConfigureAwait(false);
                handled++;
                continue;
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

    /// <summary>有界读一行：最多累积 <see cref="_maxRequestChars"/> 个字符；超限仍读到行尾以保持边界同步。</summary>
    /// <returns><c>Line</c> 为行文本（EOF 且无内容时为 <see langword="null"/>）；<c>Overflow</c> 为是否超限。</returns>
    private async Task<(string? Line, bool Overflow)> ReadBoundedLineAsync()
    {
        var builder = new StringBuilder();
        var overflow = false;
        var sawAny = false;

        while (true)
        {
            if (_readBufferOffset >= _readBufferCount)
            {
                // 缓冲耗尽：从流再读一块（可能带回多行，未消费部分留在缓冲里供下次使用）。
                _readBufferCount = await _input.ReadAsync(_readBuffer, 0, _readBuffer.Length).ConfigureAwait(false);
                _readBufferOffset = 0;

                if (_readBufferCount == 0)
                {
                    // EOF：有内容则是"无换行结尾的最后一行"，否则返回 null 表示输入结束。
                    return sawAny ? (builder.ToString(), overflow) : (null, false);
                }
            }

            var ch = _readBuffer[_readBufferOffset++];
            sawAny = true;

            if (ch == '\n')
            {
                return (builder.ToString(), overflow);
            }

            if (ch == '\r')
            {
                // 兼容 CRLF：\r 不入行内容（与 ReadLine 一致）。
                continue;
            }

            if (builder.Length >= _maxRequestChars)
            {
                overflow = true;
                continue; // 丢弃超限后的字符，但继续读到行尾（保持行边界同步）。
            }

            builder.Append(ch);
        }
    }

    /// <summary>构造"请求过大"的 JSON-RPC 错误响应（<c>-32600</c>；无 <c>id</c>，因行内容已被丢弃）。</summary>
    private string BuildOversizedEnvelope()
    {
        // AOT 安全：JsonNode 有自定义 converter，不触发反射序列化；
        // 序列化选项取 McpWireJson 单一实例（stdio 与 HTTP 共用同一份编码器）。
        var message = "请求超过 " + _maxRequestChars.ToString(CultureInfo.InvariantCulture) + " 字符上限，已拒绝";
        return new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = null,
            ["error"] = new JsonObject
            {
                ["code"] = -32600,
                ["message"] = message,
            },
        }.ToWire();
    }
}
