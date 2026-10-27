// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Mcp;

/// <summary>
/// MCP HTTP 传输的请求快照（框架无关）：宿主把自己的 HTTP 对象映射成本类型，
/// 交给 <see cref="FeishuMcpHttpHandler"/> 处理，再把 <see cref="McpHttpResponse"/> 映射回去。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么是快照而不是流</b>：JSON-RPC 消息必须整体解析，本包因此不引入任何 Web 框架依赖
/// （最小 API / MVC / FastEndpoints / 云函数都能适配），也让传输层用例可以零网络断言。
/// 正文由宿主读好后传入，长度上限归宿主（请求体限制是 Web 层职责，不是协议层职责）。
/// </para>
/// <para>
/// <b><see cref="AppKey"/> 与 <see cref="UserId"/> 只准由宿主的认证层填写</b>：本包绝不从 URL、
/// 请求体或任意自定义头自行推导租户身份。协议参数里的租户标识可由模型（乃至提示注入）影响，
/// 一旦采信就等于把「切换租户」交给客户端——与「多租户隔离禁止默认应用兜底」同一条线。
/// 宿主没有认证出身份就留空，传输层按缺凭证拒绝。
/// </para>
/// </remarks>
public sealed class McpHttpRequest
{
    /// <summary>
    /// 构造一条请求快照。
    /// </summary>
    /// <param name="httpMethod">HTTP 方法（<c>POST</c> / <c>GET</c> / <c>DELETE</c>，大小写不敏感）。</param>
    /// <exception cref="ArgumentNullException"><paramref name="httpMethod"/> 为空。</exception>
    public McpHttpRequest(string httpMethod)
    {
        HttpMethod = httpMethod ?? throw new ArgumentNullException(nameof(httpMethod));
    }

    /// <summary>HTTP 方法。</summary>
    public string HttpMethod { get; }

    /// <summary>JSON-RPC 正文（UTF-8 文本；仅 <c>POST</c> 需要）。</summary>
    public string? Body { get; set; }

    /// <summary><c>Mcp-Session-Id</c> 头（<c>initialize</c> 之前为空）。</summary>
    public string? SessionId { get; set; }

    /// <summary><c>MCP-Protocol-Version</c> 头（2025-06-18 修订起客户端应随每条请求带上）。</summary>
    public string? ProtocolVersion { get; set; }

    /// <summary><c>Origin</c> 头（浏览器客户端才有；用于跨源/重绑定防护）。</summary>
    public string? Origin { get; set; }

    /// <summary><c>Content-Type</c> 头（必须是 <c>application/json</c>）。</summary>
    public string? ContentType { get; set; }

    /// <summary><c>Accept</c> 头（须能接受 <c>application/json</c>）。</summary>
    public string? Accept { get; set; }

    /// <summary>
    /// 宿主认证层解析出的应用标识（<b>必填</b>；空 = 未认证 ⇒ 拒绝）。
    /// </summary>
    public string? AppKey { get; set; }

    /// <summary>宿主认证层解析出的用户标识（可空；user 身份工具必需，同由认证层提供）。</summary>
    public string? UserId { get; set; }
}
