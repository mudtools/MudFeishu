// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.IO;
using System.Net;
using System.Text;

namespace Mud.Feishu.AI.Mcp;

/// <summary>
/// HTTP 传输的 MCP 宿主（R-7/A）：把 <c>POST {path}</c> 的 JSON-RPC 请求转给
/// <see cref="FeishuMcpToolServer.HandleMessageAsync"/>，回 JSON 响应。
/// </summary>
/// <remarks>
/// <para><b>HttpListener 而非 ASP.NET Core</b>：本包是可选包，HttpListener 是 BCL 一部分（四 TFM 统一、
/// 零新依赖）。需认证/TLS/中间件时宿主应把 <see cref="FeishuMcpToolServer.HandleMessageAsync"/>
/// 接入自己的端点（协议接缝是纯函数）。</para>
/// <para><b>无状态 + 并发</b>：每请求独立执行上下文（<c>AsyncLocal</c> 租户上下文按请求隔离），
/// 转发时传 <c>stateless: true</c>。不内置认证——对外暴露须由反向代理承担 TLS 与认证。</para>
/// </remarks>
public sealed class FeishuMcpHttpHost : IDisposable
{
    private readonly FeishuMcpToolServer _server;
    private readonly FeishuMcpServerOptions _options;
    private readonly ILogger? _logger;
    private readonly HttpListener _listener;
    private bool _disposed;

    /// <summary>
    /// 初始化 <see cref="FeishuMcpHttpHost"/>。
    /// </summary>
    /// <param name="server">协议层服务端。</param>
    /// <param name="options">MCP 配置（<c>AppKey</c> 必填；<c>HttpPrefix</c> / <c>HttpPath</c> 决定监听面）。</param>
    /// <param name="logger">日志（可空）。</param>
    /// <exception cref="InvalidOperationException">配置非法（fail-fast）。</exception>
    public FeishuMcpHttpHost(
        FeishuMcpToolServer server,
        IOptions<FeishuMcpServerOptions> options,
        ILogger<FeishuMcpHttpHost>? logger = null)
    {
        _server = server ?? throw new ArgumentNullException(nameof(server));
        _options = (options ?? throw new ArgumentNullException(nameof(options))).Value;

        // 解析即校验（缺 appKey / 非法前缀在构造期响亮失败，而不是等第一个请求）。
        _options.Validate();

        _logger = logger;
        _listener = new HttpListener();
        _listener.Prefixes.Add(_options.HttpPrefix);
    }

    /// <summary>监听前缀（可能含端口 0 = 由系统分配；仅供日志/自省）。</summary>
    public string Prefix => _options.HttpPrefix;

    /// <summary>HTTP 路径（如 <c>/mcp</c>）。</summary>
    public string Path => _options.HttpPath;

    /// <summary>
    /// 运行 HTTP 宿主循环直到取消。请求<b>并发</b>处理（HTTP 天生并发；租户上下文按请求隔离）。
    /// </summary>
    /// <param name="cancellationToken">取消令牌（取消后停止监听并退出循环）。</param>
    /// <remarks>
    /// <c>HttpListener</c> 需要进程具备监听该前缀的权限：回环地址（默认）通常无需额外配置；
    /// 绑定非回环地址在 Windows 上可能需要 URL ACL（<c>netsh http add urlacl</c>）或管理员权限。
    /// </remarks>
    /// <exception cref="HttpListenerException">前缀无法绑定（权限或端口占用）。</exception>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _listener.Start();
        _logger?.LogInformation("MCP HTTP 宿主已启动：{Prefix}（路径 {Path}）", _options.HttpPrefix, _options.HttpPath);

        using var registration = cancellationToken.Register(static state => StopListener((HttpListener)state!), _listener);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                HttpListenerContext context;
                try
                {
                    context = await _listener.GetContextAsync().ConfigureAwait(false);
                }
                catch (Exception ex) when (
                    ex is HttpListenerException or ObjectDisposedException or InvalidOperationException
                    && cancellationToken.IsCancellationRequested)
                {
                    // 守卫白名单：取消触发的 Stop() 会打断 GetContextAsync——这是正常的退出路径。
                    break;
                }
                catch (HttpListenerException)
                {
                    // 守卫白名单：listener 被外部停止（Dispose）——同样退出。
                    break;
                }

                // 并发处理：每个请求独立 Task（不 await）。HTTP 的隔离来自"每请求独立执行上下文"，
                // 而非 stdio 的顺序化——这里刻意与 FeishuMcpStdioHost 相反。
                _ = HandleContextAsync(context, cancellationToken);
            }
        }
        finally
        {
            StopListener(_listener);
            _logger?.LogInformation("MCP HTTP 宿主已停止");
        }
    }

    /// <summary>停止监听（幂等；供取消回调与 Dispose 共用）。</summary>
    private static void StopListener(HttpListener listener)
    {
        try
        {
            if (listener.IsListening)
            {
                listener.Stop();
            }
        }
        catch (ObjectDisposedException)
        {
            // 守卫白名单：已关闭：无需处理（幂等）。
        }
    }

    private async Task HandleContextAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        try
        {
            var request = context.Request;
            var response = context.Response;

            var path = request.Url?.AbsolutePath ?? string.Empty;
            if (!string.Equals(path, _options.HttpPath, StringComparison.Ordinal))
            {
                response.StatusCode = 404;
                response.Close();
                return;
            }

            if (!string.Equals(request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase))
            {
                // 本服务不提供 GET 的 SSE 推送通道（无服务端主动推送需求）。
                response.StatusCode = 405;
                response.Headers["Allow"] = "POST";
                response.Close();
                return;
            }

            var (body, tooLarge) = await ReadBodyAsync(request.InputStream, _options.MaxRequestBytes, cancellationToken)
                .ConfigureAwait(false);

            if (tooLarge)
            {
                _logger?.LogWarning("MCP HTTP 请求超过 {Max} 字节上限，已拒绝", _options.MaxRequestBytes);
                response.StatusCode = 413;
                response.Close();
                return;
            }

            // stateless: true —— HTTP 形态无服务端会话状态（每请求自包含）。
            var result = await _server.HandleMessageAsync(body, cancellationToken, stateless: true).ConfigureAwait(false);

            if (result is null)
            {
                // 通知：协议不产生响应体。
                response.StatusCode = 202;
                response.Close();
                return;
            }

            var bytes = Encoding.UTF8.GetBytes(result);
            response.StatusCode = 200;
            response.ContentType = "application/json";
            response.ContentLength64 = bytes.Length;
            await response.OutputStream.WriteAsync(bytes, 0, bytes.Length, cancellationToken).ConfigureAwait(false);
            response.Close();
        }
        catch (OperationCanceledException)
        {
            // 守卫白名单：宿主取消：静默结束本请求（不断连的语义由 Stop() 承担）。
            TryAbort(context.Response);
        }
        catch (Exception ex)
        {
            // 协议层不抛异常，走到这里的是传输/编码层缺陷：回 500 而不是让连接半开。
            _logger?.LogWarning(ex, "MCP HTTP 请求处理失败");
            try
            {
                context.Response.StatusCode = 500;
                context.Response.Close();
            }
            catch (Exception)
            {
                // 守卫白名单：设置 500 已失败，尽力 abort 连接（幂等）。
                TryAbort(context.Response);
            }
        }
    }

    /// <summary>有界读请求体（超过 <paramref name="maxBytes"/> 立即判定，不把超大载荷读进内存）。</summary>
    private static async Task<(string Body, bool TooLarge)> ReadBodyAsync(
        Stream stream,
        int maxBytes,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];
        using var memory = new MemoryStream();

        while (true)
        {
            var read = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                break;
            }

            if (memory.Length + read > maxBytes)
            {
                return (string.Empty, true);
            }

            memory.Write(buffer, 0, read);
        }

        return (Encoding.UTF8.GetString(memory.ToArray()), false);
    }

    private static void TryAbort(HttpListenerResponse response)
    {
        try
        {
            response.Abort();
        }
        catch (Exception)
        {
            // 守卫白名单：响应已关闭/已断开：幂等无操作。
        }
    }

    /// <summary>停止监听并释放底层 <see cref="HttpListener"/>。</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        StopListener(_listener);
        _listener.Close();
    }
}