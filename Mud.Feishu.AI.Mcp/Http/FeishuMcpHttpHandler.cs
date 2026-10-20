// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Mcp;

/// <summary>
/// MCP Streamable HTTP 传输适配器：把一条 HTTP 请求映射到既有协议层
/// （<see cref="FeishuMcpToolServer"/>），再把响应映射回 <see cref="McpHttpResponse"/>。
/// </summary>
/// <remarks>
/// <para>
/// <b>执行链复用（铁律不变）</b>：本类型只做传输与会话，<b>不</b>触碰工具面、授权、净化、审计——
/// 那些仍在 <c>HandleMessageAsync</c> 之下的既有执行链里。传输层新增的判据全部是<b>拒绝</b>形态：
/// 宁可回 4xx，也不把一条不合格的请求交给执行链。
/// </para>
/// <para>
/// <b>租户身份的唯一来源是宿主认证层</b>（<see cref="McpHttpRequest.AppKey"/>）：本类型绝不从 URL、
/// 正文或自定义头推导租户，理由见 <see cref="McpHttpRequest"/>。会话一经 <c>initialize</c> 绑定租户，
/// 后续请求必须给出<b>同一个</b>身份，否则 403——会话标识因此不是切租户的凭据。
/// </para>
/// <para>
/// 只做 JSON 响应（无服务端推送 ⇒ <c>GET</c> 一律 405、批量 400）、并发形态与 stdio 顺序处理的差异、
/// 以及逐请求绑租户的裁定，见 documents/AIAgent/decisions/ADR-0008-mcp-http-transport.md。
/// </para>
/// </remarks>
public sealed class FeishuMcpHttpHandler
{
    private readonly FeishuMcpToolServer _server;
    private readonly FeishuMcpServerOptions _options;
    private readonly FeishuMcpHttpOptions _http;
    private readonly FeishuMcpSessionDirectory _sessions;
    private readonly ILogger? _logger;

    /// <summary>初始化 HTTP 适配器。</summary>
    /// <param name="server">协议层服务端（与 stdio 宿主共用同一实例即可）。</param>
    /// <param name="options">MCP 配置（含 <c>Http</c> 节）。</param>
    /// <param name="logger">日志（可空）。</param>
    /// <exception cref="ArgumentNullException"><paramref name="server"/> 或 <paramref name="options"/> 为空。</exception>
    /// <exception cref="InvalidOperationException"><c>Http</c> 节配置非法。</exception>
    public FeishuMcpHttpHandler(
        FeishuMcpToolServer server,
        IOptions<FeishuMcpServerOptions> options,
        ILogger<FeishuMcpHttpHandler>? logger = null)
        : this(
            server,
            (options ?? throw new ArgumentNullException(nameof(options))).Value,
            sessions: null,
            utcNow: null,
            logger: logger)
    {
    }

    internal FeishuMcpHttpHandler(
        FeishuMcpToolServer server,
        FeishuMcpServerOptions options,
        FeishuMcpSessionDirectory? sessions,
        Func<DateTimeOffset>? utcNow,
        ILogger? logger)
    {
        _server = server ?? throw new ArgumentNullException(nameof(server));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _http = options.Http;
        _http.Validate();
        _sessions = sessions ?? new FeishuMcpSessionDirectory(_http, utcNow);
        _logger = logger;
    }

    /// <summary>驻留会话数（观测面；用于宿主健康检查与用例断言）。</summary>
    public int SessionCount => _sessions.Count;

    /// <summary>
    /// 处理一条 MCP HTTP 请求。
    /// </summary>
    /// <param name="request">请求快照（租户身份须已由宿主认证层填写）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>响应快照。本方法<b>不抛</b>业务异常：传输不合格一律回 4xx。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> 为空。</exception>
    public async Task<McpHttpResponse> HandleAsync(McpHttpRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        // OPTIONS 预检（CORS）归宿主中间件；到这里说明没接住，按不支持处理。
        if (IsMethod(request.HttpMethod, "POST"))
        {
            return await HandlePostAsync(request, cancellationToken).ConfigureAwait(false);
        }

        if (IsMethod(request.HttpMethod, "DELETE"))
        {
            return HandleDelete(request);
        }

        // GET 在 Streamable HTTP 里只用于服务端推送流，本实现不提供（见类型注释）。
        return RejectWithAllow(405, $"HTTP 方法 '{request.HttpMethod}' 不受支持：本端点接受 POST（JSON-RPC）与 DELETE（终止会话）");
    }

    // ─────────────────────────── 分支 ───────────────────────────

    private McpHttpResponse HandleDelete(McpHttpRequest request)
    {
        var identity = CheckIdentity(request);
        if (identity is not null)
        {
            return identity;
        }

        if (string.IsNullOrWhiteSpace(request.SessionId))
        {
            return McpHttpResponse.Reject(400, $"终止会话必须携带 {McpHttpHeaders.SessionId} 头");
        }

        var session = _sessions.Find(request.SessionId);
        if (session is null)
        {
            return McpHttpResponse.Reject(404, "会话不存在或已过期");
        }

        // 只有绑定该租户的身份能终止它——否则泄漏一个会话标识就变成一条跨租户的注销通道。
        var owner = CheckSessionOwner(session, request);
        if (owner is not null)
        {
            return owner;
        }

        _sessions.Remove(request.SessionId);
        _logger?.LogInformation("MCP HTTP 会话已终止：tenant={AppKey}", session.AppKey);
        return McpHttpResponse.NoContent();
    }

    private async Task<McpHttpResponse> HandlePostAsync(McpHttpRequest request, CancellationToken cancellationToken)
    {
        var header = CheckTransportHeaders(request);
        if (header is not null)
        {
            return header;
        }

        var identity = CheckIdentity(request);
        if (identity is not null)
        {
            return identity;
        }

        if (string.IsNullOrWhiteSpace(request.Body))
        {
            return McpHttpResponse.Reject(400, "请求正文为空");
        }

        JsonNode? parsed;
        try
        {
            parsed = JsonNode.Parse(request.Body!);
        }
        catch (JsonException)
        {
            // 有意留痕（守卫白名单）：畸形正文转成 400 回给客户端（换了一条上报通道），不是吞掉。
            // 刻意不逐条记日志——畸形报文可无限重放，记日志等于把日志通道变成放大器。
            return McpHttpResponse.Reject(400, "请求正文不是合法 JSON");
        }

        if (parsed is not JsonObject message)
        {
            return McpHttpResponse.Reject(400, "请求必须是 JSON-RPC 对象");
        }

        var method = message.ReadOptionalString("method");
        var isInitialize = string.Equals(method, McpProtocol.MethodInitialize, StringComparison.Ordinal);

        return isInitialize
            ? await InitializeAsync(request, message, cancellationToken).ConfigureAwait(false)
            : await RelayAsync(request, method, cancellationToken).ConfigureAwait(false);
    }

    private async Task<McpHttpResponse> InitializeAsync(McpHttpRequest request, JsonObject message, CancellationToken cancellationToken)
    {
        var requested = (message["params"] as JsonObject).ReadOptionalString("protocolVersion");
        var session = _sessions.Create(request.AppKey!, request.UserId, McpProtocol.Negotiate(requested));
        if (session is null)
        {
            return McpHttpResponse.Reject(
                503,
                $"并发会话已达上限 {_options.Http.MaxSessions.ToString(CultureInfo.InvariantCulture)}，请稍后重连");
        }

        var body = await _server.HandleMessageAsync(request.Body, session, cancellationToken).ConfigureAwait(false);
        return WithSession(McpHttpResponse.Json(body ?? "{}"), session.Id);
    }

    private async Task<McpHttpResponse> RelayAsync(McpHttpRequest request, string? method, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SessionId))
        {
            return McpHttpResponse.Reject(400, $"缺少 {McpHttpHeaders.SessionId} 头：请先 initialize 取得会话标识");
        }

        var session = _sessions.Find(request.SessionId);
        if (session is null)
        {
            return McpHttpResponse.Reject(404, "会话不存在或已过期：请重新 initialize");
        }

        var owner = CheckSessionOwner(session, request);
        if (owner is not null)
        {
            return owner;
        }

        var version = CheckProtocolVersion(request, session);
        if (version is not null)
        {
            return version;
        }

        var body = await _server.HandleMessageAsync(request.Body, session, cancellationToken).ConfigureAwait(false);

        // 通知（无 id）按协议不产生响应 ⇒ 202 空正文，而不是"看起来像一次成功的往返"。
        return body is null ? McpHttpResponse.Accepted() : McpHttpResponse.Json(body);
    }

    // ─────────────────────────── 传输判据 ───────────────────────────

    /// <summary>Origin / Content-Type / Accept 三项传输判据（顺序即优先级：先拒跨源，再拒形态不合格）。</summary>
    private McpHttpResponse? CheckTransportHeaders(McpHttpRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.Origin) && !IsOriginAllowed(request.Origin!))
        {
            // 不点名允许的清单：那是把「猜哪个来源能过」变成一次可观测的反馈。
            return McpHttpResponse.Reject(403, "Origin 不在允许清单内（本端点默认只服务不发 Origin 的客户端）");
        }

        if (!IsJson(request.ContentType))
        {
            return McpHttpResponse.Reject(415, "Content-Type 必须是 application/json");
        }

        if (!AcceptsJson(request.Accept))
        {
            return McpHttpResponse.Reject(406, "Accept 必须能接受 application/json（本实现不提供 text/event-stream）");
        }

        return null;
    }

    /// <summary>租户身份判据：必须由宿主认证层给出，且落在进程配置/名单允许的范围内。</summary>
    private McpHttpResponse? CheckIdentity(McpHttpRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.AppKey))
        {
            return McpHttpResponse.Reject(
                401,
                $"缺少租户身份：{nameof(McpHttpRequest.AppKey)} 必须由宿主认证层填写——HTTP 通道禁止默认应用兜底");
        }

        if (_http.AllowedAppKeys.Count > 0
            && !Contains(_http.AllowedAppKeys, request.AppKey!))
        {
            return McpHttpResponse.Reject(403, "该租户不在允许的名单内");
        }

        // 未显式声明「逐请求绑租户」时，进程只服务配置里那一个租户：
        // 换一个 appKey 进来 = 用 A 租户装配的端点读 B 租户的数据。
        if (!_http.TenantBoundPerRequest
            && !string.Equals(request.AppKey, _options.AppKey, StringComparison.Ordinal))
        {
            return McpHttpResponse.Reject(
                403,
                $"本进程按单租户装配（{nameof(FeishuMcpServerOptions.AppKey)}），请求身份与之不符；"
                + $"多租户部署请显式设置 {FeishuMcpServerOptions.SectionName}:{nameof(FeishuMcpServerOptions.Http)}"
                + $":{nameof(FeishuMcpHttpOptions.TenantBoundPerRequest)}=true");
        }

        return null;
    }

    private McpHttpResponse? CheckSessionOwner(FeishuMcpSession session, McpHttpRequest request)
        => string.Equals(session.AppKey, request.AppKey, StringComparison.Ordinal)
            ? null
            : McpHttpResponse.Reject(403, "会话与本次认证的租户不一致：该会话标识不可用于当前身份");

    private McpHttpResponse? CheckProtocolVersion(McpHttpRequest request, FeishuMcpSession session)
    {
        if (string.IsNullOrWhiteSpace(request.ProtocolVersion))
        {
            // 老客户端（2025-03-26 形态）不带该头：按会话握手时协商的版本处理，不因此拒服务。
            return null;
        }

        if (!McpProtocol.IsSupported(request.ProtocolVersion))
        {
            return McpHttpResponse.Reject(
                400,
                $"不支持的协议版本 '{request.ProtocolVersion}'（本服务支持 {string.Join(", ", McpProtocol.SupportedVersions)}）");
        }

        if (!string.Equals(request.ProtocolVersion, session.ProtocolVersion, StringComparison.Ordinal))
        {
            return McpHttpResponse.Reject(
                400,
                $"协议版本与会话握手结果不一致：本会话协商为 '{session.ProtocolVersion}'");
        }

        return null;
    }

    private static McpHttpResponse WithSession(McpHttpResponse response, string sessionId)
    {
        response.Headers[McpHttpHeaders.SessionId] = sessionId;
        return response;
    }

    private static McpHttpResponse RejectWithAllow(int statusCode, string reason)
    {
        var response = McpHttpResponse.Reject(statusCode, reason);
        response.Headers[McpHttpHeaders.Allow] = McpHttpHeaders.AllowedMethods;
        return response;
    }

    private static bool IsMethod(string? httpMethod, string expected)
        => string.Equals(httpMethod, expected, StringComparison.OrdinalIgnoreCase);

    private static bool IsJson(string? contentType)
        => contentType is not null
            && contentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase);

    private static bool AcceptsJson(string? accept)
        => string.IsNullOrWhiteSpace(accept)
            || ContainsText(accept, "application/json")
            || ContainsText(accept, "text/event-stream")
            || ContainsText(accept, "*/*");

    private static bool ContainsText(string? value, string needle)
        => value?.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;

    private bool IsOriginAllowed(string origin) => Contains(_http.AllowedOrigins, origin);

    private static bool Contains(List<string> values, string value)
    {
        foreach (var candidate in values)
        {
            if (string.Equals(candidate, value, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
