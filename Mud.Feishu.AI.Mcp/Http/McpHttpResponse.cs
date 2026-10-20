// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Mcp;

/// <summary>
/// MCP HTTP 传输的响应（框架无关）：状态码 + 头部 + 可选正文，由宿主写回自己的 HTTP 上下文。
/// </summary>
/// <remarks>
/// <b>两类错误形态刻意分开</b>：JSON-RPC 层错误（方法不存在、参数非法、工具执行失败）带
/// <c>200</c> 与 <c>{ "error": { "code", "message" } }</c> 正文——那是协议规定给客户端程序读的；
/// 传输层拒绝（缺会话、越租户、Origin 不合法）带 4xx 与 <c>{ "error": "&lt;原因&gt;" }</c> 正文——
/// 那是 HTTP 语义，不该伪装成一次合法的 JSON-RPC 往返。
/// </remarks>
public sealed class McpHttpResponse
{
    /// <summary>本实现唯一的响应媒体类型（不支持 SSE，故恒为 JSON）。</summary>
    internal const string JsonContentType = "application/json; charset=utf-8";

    /// <summary>
    /// 构造一条响应。
    /// </summary>
    /// <param name="statusCode">HTTP 状态码。</param>
    public McpHttpResponse(int statusCode) => StatusCode = statusCode;

    /// <summary>HTTP 状态码。</summary>
    public int StatusCode { get; set; }

    /// <summary>响应正文（<see langword="null"/> = 无正文，如 202 / 204）。</summary>
    public string? Body { get; set; }

    /// <summary>正文媒体类型（<see cref="Body"/> 非空时由处理器写为 <c>application/json; charset=utf-8</c>）。</summary>
    public string? ContentType { get; set; }

    /// <summary>响应头（含 <c>Mcp-Session-Id</c>、<c>Allow</c> 等）。</summary>
    public Dictionary<string, string> Headers { get; } = new(StringComparer.Ordinal);

    /// <summary>传输层拒绝：4xx + 可读原因（不伪装成 JSON-RPC 往返）。</summary>
    /// <param name="statusCode">HTTP 状态码。</param>
    /// <param name="reason">拒绝原因（进入响应正文，供宿主排障）。</param>
    /// <returns>响应。</returns>
    public static McpHttpResponse Reject(int statusCode, string reason)
        => new(statusCode)
        {
            Body = new JsonObject { ["error"] = reason }.ToWire(),
            ContentType = JsonContentType,
        };

    /// <summary>协议响应：200 + JSON-RPC 信封原文。</summary>
    /// <param name="jsonRpcBody">协议层产出的响应 JSON。</param>
    /// <returns>响应。</returns>
    public static McpHttpResponse Json(string jsonRpcBody)
        => new(200)
        {
            Body = jsonRpcBody,
            ContentType = JsonContentType,
        };

    /// <summary>已受理但无正文（通知）。</summary>
    /// <returns>202 响应。</returns>
    public static McpHttpResponse Accepted() => new(202);

    /// <summary>已处理且无正文（会话终止）。</summary>
    /// <returns>204 响应。</returns>
    public static McpHttpResponse NoContent() => new(204);
}
