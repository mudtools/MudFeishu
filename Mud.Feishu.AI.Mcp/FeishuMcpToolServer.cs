// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Reflection;
using System.Text.Encodings.Web;
using Mud.AI.Agent.Tools;
using Mud.AI.Agent.Tools.Errors;

namespace Mud.Feishu.AI.Mcp;

/// <summary>
/// 飞书工具面的 MCP 服务端（协议层）：把<b>白名单已启用</b>的注册表工具经 JSON-RPC 2.0 / MCP <c>tools</c> 能力暴露给进程外 Agent。
/// </summary>
/// <remarks>
/// <b>执行链复用（铁律）</b>：<c>tools/call</c> 只解析出 <see cref="Microsoft.Extensions.AI.AIFunction"/> 并调用它——授权门禁、租户上下文切换、
/// 出站净化、内容安全、审计、流式/截断口径<b>全部沿用既有执行链</b>；本包只依赖两条窄接缝（<see cref="IFeishuToolFunctionFactory"/> 构桥 +
/// <see cref="IToolErrorTextFormatter"/> 渲染错误文本），不 <c>new</c> 实现类型、不引用 SDK 强类型客户端（本目录不出现 <c>IFeishu*</c> 调用）。
/// <para>
/// <b>工具集真相源 = 注册表白名单</b>（<c>FeishuToolRegistry.EnabledTools</c>）：未启用的工具在 <c>tools/list</c> 不可见、调用只回"不存在或未启用"，
/// MCP <b>不提供第二条启用通道</b>。<b>不得</b>改用 <c>FeishuAgentToolSource.GetTools</c>——那条路径把写工具包进 MEAI 的
/// <c>ApprovalRequiredAIFunction</c>，而该类型<b>只是标记</b>，强制点在 MAF 的 <c>FunctionInvokingChatClient</c>（MCP 不走该管线）⇒ 标记被静默丢弃；
/// 真实门禁是执行链的授权门禁（写工具默认不在白名单，授权器返回 <c>NeedsUserConfirmation</c> 时拒绝且零调用下游）。
/// </para>
/// <para>
/// <b>租户上下文与资源能力</b>：每次调用前经 <see cref="IFeishuToolContextAccessor.Begin"/> 显式重建（MCP 调用不在事件流内，与续跑同构），
/// 作用域在 <c>finally</c> 释放，缺 appKey 构造期即抛（禁止默认应用兜底）；资源目录（<see cref="FeishuMcpResourceCatalog"/>）与工具面同一次装配、
/// 同一份白名单，内容全为编译期常量 ⇒ <c>resources/read</c> 无 IO、无租户依赖，instructions 只下发一行摘要。
/// </para>
/// <para>
/// <b>线程模型</b>：单连接顺序处理（<see cref="FeishuMcpStdioHost"/> 逐行 await），<c>_initialized</c> 标记无需加锁；宿主并发投递时须自行串行化。
/// </para>
/// </remarks>
public sealed class FeishuMcpToolServer
{
    /// <summary>线协议序列化选项：保留中文原文（与工具结果同一纪律，见 <c>ToolResultJson</c> 的注释）。</summary>
    /// <remarks>
    /// <c>JsonNode.ToJsonString()</c> 默认走 <c>JavaScriptEncoder.Default</c>，会把中文转义成
    /// <c>\uXXXX</c>——对模型/客户端而言既是 token 膨胀（约 6 倍）也是可读性灾难。
    /// </remarks>
    private static readonly JsonSerializerOptions WireOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false,
    };

    private readonly IServiceProvider _services;
    private readonly FeishuMcpServerOptions _options;
    private readonly IFeishuToolContextAccessor _contextAccessor;
    private readonly ILogger? _logger;

    /// <summary>
    /// 构桥接缝（<c>null</c> = 宿主未装配工具面 ⇒ <c>tools/list</c> 为空，与"注册表缺席"同语义）。
    /// </summary>
    private readonly IFeishuToolFunctionFactory? _functionFactory;

    /// <summary>
    /// 错误文案接缝（<c>null</c> 时退回纯文本原因——协议层不得因缺一个接缝而抛）。
    /// </summary>
    private readonly IToolErrorTextFormatter? _errorFormatter;
    private readonly List<McpToolInfo> _tools = [];
    private readonly Dictionary<string, AIFunction> _functions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, JsonNode> _inputSchemas = new(StringComparer.Ordinal);
    private readonly Dictionary<string, McpToolInfo> _byMcpName = new(StringComparer.Ordinal);

    /// <summary>
    /// 资源目录（<c>IncludeResources=false</c> 或工具面缺席时为 <see langword="null"/>）。
    /// </summary>
    /// <remarks>
    /// 与工具面<b>同一次装配</b>构建（域集合来自同一份白名单），故"资源可见性 == 工具可见性"
    /// 是结构事实而不是纪律。
    /// </remarks>
    private readonly FeishuMcpResourceCatalog? _resources;

    private bool _initialized;

    /// <summary>
    /// 初始化 <see cref="FeishuMcpToolServer"/>。
    /// </summary>
    /// <param name="services">根服务提供器（用于解析工具注册表与 guidance）。</param>
    /// <param name="options">MCP 配置（appKey 必填）。</param>
    /// <param name="contextAccessor">工具执行上下文访问器（每次调用前 Begin）。</param>
    /// <param name="logger">日志（可空）。</param>
    /// <exception cref="InvalidOperationException">配置非法，或工具名映射冲突/超长。</exception>
    public FeishuMcpToolServer(
        IServiceProvider services,
        IOptions<FeishuMcpServerOptions> options,
        IFeishuToolContextAccessor contextAccessor,
        ILogger<FeishuMcpToolServer>? logger = null)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
        _options = (options ?? throw new ArgumentNullException(nameof(options))).Value;

        // fail-closed：缺 appKey 的 server 没有任何可用语义（每次调用都会被执行链拒绝），
        // 故在构造期就抛——不留下"能起来但每次调用都失败"的空转形态。
        _options.Validate();

        _logger = logger;

        // 两条窄接缝从容器解析（与注册表同源，均由 AddFeishuToolInfrastructure 装配）。
        // 缺席（= 宿主没装配工具面）不抛：tools/list 返回空列表是合法状态，
        // 真正的装配错误（白名单含未注册工具等）已在注册表构建期 fail-fast。
        _functionFactory = _services.GetService<IFeishuToolFunctionFactory>();
        _errorFormatter = _services.GetService<IToolErrorTextFormatter>();

        Instructions = _options.IncludeGuidance ? BuildInstructions(_services) : string.Empty;
        BuildToolSurface();

        _resources = _options.IncludeResources && _tools.Count > 0
            ? BuildResources(_services)
            : null;
    }

    /// <summary>暴露给客户端的工具（<c>tools/list</c> 的内容来源）。</summary>
    public IReadOnlyList<McpToolInfo> Tools => _tools;

    /// <summary>暴露给客户端的资源（<c>resources/list</c> 的内容来源；未启用时为只读空集）。</summary>
    public IReadOnlyList<McpResourceInfo> Resources => _resources?.Resources ?? [];

    /// <summary>
    /// 合法资源 URI 形态清单（未知 URI 错误文案用）。
    /// </summary>
    /// <remarks>未启用 <c>resources</c> 时仍返回模板（错误文案一致性）；它只是"URI 长什么样"的说明。</remarks>
    public IReadOnlyList<string> ResourcesUriPrefixes
        => _resources?.UriPrefixes ?? FeishuMcpResourceCatalog.UriTemplates;

    /// <summary>
    /// 会话是否已初始化（收到 <c>initialize</c> 或 <c>notifications/initialized</c>）。
    /// </summary>
    public bool IsInitialized => _initialized;

    /// <summary>下发到 <c>initialize.instructions</c> 的域 guidance（<c>IncludeGuidance=false</c> 时为空串）。</summary>
    public string Instructions { get; }

    /// <summary>
    /// 处理一行 JSON-RPC 消息。
    /// </summary>
    /// <param name="json">请求 JSON（单行）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <param name="stateless">无状态会话（HTTP 形态）；不强制握手顺序。</param>
    /// <returns>响应 JSON；输入是<b>通知</b>（无 <c>id</c>）或空行时返回 <see langword="null"/>（不产生响应）。</returns>
    /// <remarks>本方法<b>不抛异常</b>（除取消外）：所有失败都归一为 JSON-RPC 错误对象，客户端不会收到"连接被打破"。</remarks>
    public async Task<string?> HandleMessageAsync(
        string? json,
        CancellationToken cancellationToken = default,
        bool stateless = false)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        JsonNode? parsed;
        try
        {
            parsed = JsonNode.Parse(json!);
        }
        catch (JsonException)
        {
            // 有意静默（守卫白名单）：异常被**转换**为 JSON-RPC parse error 回给客户端
            // （换了一条上报通道：客户端立刻能看到"这行不是合法 JSON"），不是吞掉。
            // 此处刻意不记日志：畸形报文可由客户端无限重放，逐条记日志会把日志通道变成放大器。
            return ErrorEnvelope(null, McpProtocol.ErrorParseError, "请求不是合法 JSON");
        }

        if (parsed is not JsonObject request)
        {
            return ErrorEnvelope(null, McpProtocol.ErrorInvalidRequest, "请求必须是 JSON-RPC 对象");
        }

        var id = request["id"];
        var isNotification = id is null;
        var method = request["method"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(method))
        {
            return isNotification
                ? null
                : ErrorEnvelope(id, McpProtocol.ErrorInvalidRequest, "请求缺少 method");
        }

        var parameters = request["params"] as JsonObject;

        // 初始化前只接受 initialize / ping（协议要求：其余**请求**回错误，而不是"看起来能用"）。
        // 通知不在此拦截：notifications/initialized 正是握手完成的信号，它本身必须能到达下面的分支。
        // stateless（HTTP）：不强制握手顺序——无状态会话下每个请求自包含。
        if (!stateless
            && !_initialized
            && !isNotification
            && !string.Equals(method, McpProtocol.MethodInitialize, StringComparison.Ordinal)
            && !string.Equals(method, McpProtocol.MethodPing, StringComparison.Ordinal))
        {
            return ErrorEnvelope(id, McpProtocol.ErrorServerNotInitialized, "会话尚未初始化：请先发送 initialize 请求");
        }

        try
        {
            switch (method)
            {
                case McpProtocol.MethodInitialize:
                    _initialized = true;
                    return isNotification ? null : ResultEnvelope(id, BuildInitializeResult(parameters));

                case McpProtocol.MethodInitialized:
                    _initialized = true;
                    return null;

                case McpProtocol.MethodPing:
                    return isNotification ? null : ResultEnvelope(id, new JsonObject());

                case McpProtocol.MethodToolsList:
                    return isNotification
                        ? null
                        : ResultEnvelope(id, BuildToolsListResult(parameters?["cursor"]?.GetValue<string>()));

                case McpProtocol.MethodToolsCall:
                    return isNotification ? null : await CallToolAsync(id!, parameters, cancellationToken).ConfigureAwait(false);

                case McpProtocol.MethodResourcesList:
                    return isNotification ? null : ResultEnvelope(id, BuildResourcesListResult());

                case McpProtocol.MethodResourcesRead:
                    return isNotification ? null : ReadResource(id!, parameters);

                default:
                    // 未知**通知**静默（协议允许扩展通知）；未知**请求**回方法不存在。
                    return isNotification
                        ? null
                        : ErrorEnvelope(id, McpProtocol.ErrorMethodNotFound, $"未知方法 '{method}'（本服务提供 tools/list、tools/call、resources/list 与 resources/read）");
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // 有意留痕（守卫白名单）：协议层不得把异常抛给"读一行写一行"的宿主循环——
            // 抛出去会终止会话，客户端只会看到"连接断了"，而真正的原因（工具/装配缺陷）无人可见。
            // 故归一为 JSON-RPC internal error 并记日志。
            _logger?.LogWarning(ex, "MCP 请求处理失败：method={Method}", method);
            return isNotification
                ? null
                : ErrorEnvelope(id, McpProtocol.ErrorInternalError, $"服务端处理失败：{ex.Message}");
        }
    }

    // ─────────────────────────── 协议载荷构造 ───────────────────────────

    private JsonObject BuildInitializeResult(JsonObject? parameters)
    {
        var requested = parameters?["protocolVersion"]?.GetValue<string>();


        var clientInfo = parameters?["clientInfo"] as JsonObject;
        var clientName = clientInfo?["name"]?.GetValue<string>();
        var clientVersion = clientInfo?["version"]?.GetValue<string>();


        var clientCapabilities = parameters?["capabilities"] as JsonObject;
        var clientSupportsResources = clientCapabilities is null
            || clientCapabilities["resources"] is JsonObject;

        if (!string.IsNullOrWhiteSpace(clientName))
        {
            _logger?.LogInformation(
                "MCP 客户端握手：name={Name} version={Version} protocol={Protocol} clientSupportsResources={Resources}",
                clientName,
                clientVersion,
                McpProtocol.Negotiate(requested),
                clientSupportsResources);
        }

        var result = new JsonObject
        {
            // 版本协商：闭集内回显，闭集外回落本服务默认版本（见 McpProtocol 的注释）。
            ["protocolVersion"] = McpProtocol.Negotiate(requested),
            ["capabilities"] = BuildCapabilities(clientSupportsResources),
            ["serverInfo"] = new JsonObject
            {
                ["name"] = _options.ServerName,
                ["version"] = ServerVersion(),
            },
        };

        if (!string.IsNullOrWhiteSpace(Instructions))
        {
            // 域资产（路由优先级 / 避坑 / 示例）与 Agent 内部注入的是同一份常量，
            // 故外部 Agent 的工具选择质量与进程内一致（而不是只拿到 163 条描述）。
            result["instructions"] = Instructions;
        }

        return result;
    }

    /// <summary>
    /// 能力声明：<c>tools</c> 恒在；<c>resources</c> 在启用且有可读资源时声明。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>为什么 <c>resources</c> 与"是否有资源"绑定</b>：声明了 <c>resources</c> 却让
    /// <c>resources/list</c> 恒返回空列表，会让客户端在 UI 上显示一个永远为空的资源面板
    /// （"能力声明"与"实际内容"不一致）。真实装配空集（无启用工具）时不声明该能力，
    /// 客户端据此也不会去问。
    /// </para>
    /// <para>
    /// <b>两个能力都是 <c>listChanged=false</c></b>：白名单与资产在装配期固定
    /// （进程生命周期内不变），无需通知客户端刷新——这也让"不发通知"成为契约而不是遗漏。
    /// </para>
    /// </remarks>
    /// <param name="clientSupportsResources">
    /// 客户端是否声明了 <c>resources</c> 能力（R-7/E 自适应：客户端不支持时不声明，避免其 UI 出现永远为空的资源面板）。
    /// </param>
    private JsonObject BuildCapabilities(bool clientSupportsResources)
    {
        var capabilities = new JsonObject
        {
            ["tools"] = new JsonObject { ["listChanged"] = false },

            // E-2 运行时自描述：客户端在握手期就能拿到「这个服务有多大、协议口径是哪版、工具是否只读聚合」，
            // 不必先 tools/list 再数一遍（探测/巡检脚本与 UI 均受益）。
            // 放在 experimental 而非 tools 内：后者是协议规定形态，加私有键会被严格客户端判为非法规格。
            ["experimental"] = BuildExperimentalCapabilities(),
        };

        if (_resources is { Resources.Count: > 0 } && clientSupportsResources)
        {
            capabilities["resources"] = new JsonObject { ["listChanged"] = false };
        }

        return capabilities;
    }

    /// <summary>非标准扩展面（<c>capabilities.experimental.feishu</c>）：规模与口径自描述。</summary>
    /// <remarks>
    /// <b>键的稳定性</b>：这些键是宿主探测脚本的输入契约，改名属于破坏性变更（守卫锁定键集合）；
    /// 取值一律来自本次装配产物或常量（工具数=实际暴露数，域数=工具名首段的去重数），不做估算。
    /// </remarks>
    /// <returns>扩展能力节点。</returns>
    private JsonObject BuildExperimentalCapabilities()
    {
        var domains = _tools
            .Select(static t => t.ContractName)
            .Where(static n => n.IndexOf('.') > 0)
            .Select(static n => n.Substring(0, n.IndexOf('.')))
            .Distinct(StringComparer.Ordinal)
            .Count();

        return new JsonObject
        {
            ["feishu"] = new JsonObject
            {
                ["protocolRevision"] = McpProtocol.DefaultVersion,
                ["toolCount"] = _tools.Count,
                ["domainCount"] = domains,
                ["writeToolCount"] = _tools.Count(static t => t.IsWrite),
                ["highRiskWriteToolCount"] = _tools.Count(static t => t.Risk == FeishuToolRisk.HighRiskWrite),
                ["resourceCount"] = Resources.Count,
                ["guidanceIncluded"] = !string.IsNullOrEmpty(Instructions),
            },
        };
    }

    /// <summary><c>resources/list</c> 结果（资源清单；不提供分页——清单规模远小于需要的阈值）。</summary>
    private JsonObject BuildResourcesListResult()
    {
        var resources = new JsonArray();

        foreach (var resource in Resources)
        {
            resources.AddNode(new JsonObject
            {
                ["uri"] = resource.Uri,
                ["name"] = resource.Name,
                ["description"] = resource.Description,
                ["mimeType"] = resource.MimeType,

                // 体积估算（防"客户端盲目全读"）——非协议标准字段，
                // 客户端可忽略；给得出来比"读之前不知道多大"更有用。
                ["sizeBytes"] = resource.SizeBytes,
            });
        }

        return new JsonObject { ["resources"] = resources };
    }

    /// <summary>
    /// <c>resources/read</c>：按 URI 取正文；未知 URI 回 <c>-32602</c> + <b>合法前缀清单</b>。
    /// </summary>
    /// <remarks>
    /// 未知 URI 的文案风格刻意与 <c>tools/call</c> 的"不存在或未启用"一致：
    /// <b>不回显已启用资源全集</b>（那等于给出一条枚举通道，与工具面的纪律相同），
    /// 只给出"URI 长什么样"的可修指引。
    /// </remarks>
    private string ReadResource(JsonNode id, JsonObject? parameters)
    {
        var uri = parameters?["uri"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(uri))
        {
            return ErrorEnvelope(
                id,
                McpProtocol.ErrorInvalidParams,
                "resources/read 缺少 params.uri");
        }

        if (_resources is null)
        {
            return ErrorEnvelope(
                id,
                McpProtocol.ErrorInvalidParams,
                "本服务未启用 resources 能力"
                + $"（{FeishuMcpServerOptions.SectionName}:{nameof(FeishuMcpServerOptions.IncludeResources)}）——请改用 instructions 与 tools");
        }

        if (!_resources.TryRead(uri, out var text))
        {
            return ErrorEnvelope(
                id,
                McpProtocol.ErrorInvalidParams,
                $"资源 '{uri}' 不存在或未启用——MCP 只暴露已启用工具所属域的资源。"
                + $"合法 URI 形态：{string.Join(" / ", _resources.UriPrefixes)}");
        }

        var contents = new JsonArray();
        contents.AddNode(new JsonObject
        {
            ["uri"] = uri,
            ["mimeType"] = "text/markdown",
            ["text"] = text,
        });

        return ResultEnvelope(id, new JsonObject { ["contents"] = contents });
    }

    /// <summary><c>tools/list</c> 结果（可选分页；默认关闭时全量返回）。</summary>
    /// <param name="cursor">上页返回的 <c>nextCursor</c>（空 = 首页）。</param>
    private JsonObject BuildToolsListResult(string? cursor)
    {
        var start = ResolveToolsCursor(cursor);
        var pageSize = _options.EnableToolsPagination ? Math.Max(1, _options.ToolsPageSize) : _tools.Count;
        var end = Math.Min(start + pageSize, _tools.Count);

        var tools = new JsonArray();
        for (var index = start; index < end; index++)
        {
            var info = _tools[index];
            tools.AddNode(new JsonObject
            {
                ["name"] = info.Name,

                // title 保留契约名：客户端 UI 上人看到的是《工具权限对照表》里的同一个名字，
                // 排查时不必再做一次"下划线/点"的心算。
                ["title"] = info.ContractName,
                ["description"] = info.Description,
                ["inputSchema"] = _inputSchemas[info.Name]?.DeepClone(),
                ["annotations"] = new JsonObject
                {
                    ["readOnlyHint"] = !info.IsWrite,
                    ["destructiveHint"] = info.Risk == FeishuToolRisk.HighRiskWrite,
                    ["openWorldHint"] = true,
                },
            });
        }

        var result = new JsonObject { ["tools"] = tools };

        // nextCursor 缺席即"已列完"（协议约定）；仅在还有下一页时给出。
        if (end < _tools.Count)
        {
            result["nextCursor"] = end.ToString(CultureInfo.InvariantCulture);
        }

        return result;
    }

    /// <summary>解析游标为起始索引（非法游标回落首页，让客户端自愈）。</summary>
    private int ResolveToolsCursor(string? cursor)
    {
        if (!_options.EnableToolsPagination || string.IsNullOrEmpty(cursor))
        {
            return 0;
        }

        return int.TryParse(cursor, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index) && index > 0
            ? Math.Min(index, _tools.Count)
            : 0;
    }

    private async Task<string> CallToolAsync(JsonNode id, JsonObject? parameters, CancellationToken cancellationToken)
    {
        var requestedName = parameters?["name"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(requestedName))
        {
            return ErrorEnvelope(id, McpProtocol.ErrorInvalidParams, "tools/call 缺少 params.name");
        }

        if (!TryResolveFunction(requestedName!, out var function, out var info))
        {
            // 不区分"不存在"与"未启用"之外的细节，也不回显已启用清单（那等于给出一条枚举通道）——
            // 但对"未启用"给出可修指引（宿主该改哪个配置）。
            return ErrorEnvelope(
                id,
                McpProtocol.ErrorInvalidParams,
                $"工具 '{requestedName}' 不存在或未启用——MCP 只暴露白名单已启用的飞书工具"
                + $"（{FeishuAgentOptions.SectionName}:{nameof(FeishuAgentOptions.Tools)} / "
                + $"{nameof(FeishuAgentOptions.WriteAllowList)}）");
        }

        // arguments 缺省 = 无参调用（合法）；给了但不是对象 = 客户端报文形态错误 ⇒ 协议层拒绝
        // （让它掉到工具层会被读成"参数缺失"，把客户端缺陷伪装成业务错误）。
        var argumentsNode = parameters?["arguments"];
        if (argumentsNode is not null && argumentsNode is not JsonObject)
        {
            return ErrorEnvelope(id, McpProtocol.ErrorInvalidParams, "tools/call 的 params.arguments 必须是 JSON 对象");
        }

        var arguments = ToArguments(argumentsNode as JsonObject);
        var (text, isError) = await InvokeAsync(function!, info!.ContractName, arguments, cancellationToken)
            .ConfigureAwait(false);

        var content = new JsonArray();
        content.AddNode(new JsonObject
        {
            ["type"] = "text",
            ["text"] = text,
        });

        return ResultEnvelope(id, new JsonObject
        {
            ["content"] = content,
            ["isError"] = isError,
        });
    }

    /// <summary>
    /// 执行一次工具调用：<b>重建租户上下文</b> → 走既有 <see cref="AIFunction"/>（内含执行链）→ 归一结果。
    /// </summary>
    /// <param name="function">工具桥（<c>FeishuToolAIFunction</c>）。</param>
    /// <param name="contractName">契约工具名（错误文案与审计对齐用）。</param>
    /// <param name="arguments">已归一化的入参。注意：<see cref="Microsoft.Extensions.AI.AIFunctionArguments"/> 会在此处被再次包装。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>结果文本 + 是否错误。</returns>
    private async Task<(string Text, bool IsError)> InvokeAsync(
        AIFunction function,
        string contractName,
        Dictionary<string, object?> arguments,
        CancellationToken cancellationToken)
    {
        // 显式重建租户上下文：MCP 调用不在事件流内，
        // 上下文不流动就会落到"缺 appKey ⇒ 工具桥结构化拒绝"上；作用域必须在 finally 释放，
        // 否则同进程的后续调用会带着前一个租户的 appKey（AsyncLocal 泄漏 = 跨租户串号）。
        using var scope = _contextAccessor.Begin(new FeishuToolContext(
            _options.AppKey,
            _options.ConversationKey,
            _options.ChatId,
            _options.UserId,
            _options.ThreadId));

        try
        {
            var result = await function
                .InvokeAsync(new AIFunctionArguments(arguments), cancellationToken)
                .ConfigureAwait(false);

            return result is ToolResult toolResult
                ? (toolResult.ToString(), toolResult.Error is not null)
                : (result?.ToString() ?? string.Empty, false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // 有意留痕（守卫白名单）：执行链已把可预期的下游失败归一为结构化错误文本，
            // 走到这里的是"未预期"的缺陷。此处必须回填可读错误而不是抛出——
            // 协议层抛出等于把整个 MCP 会话打断，客户端只看到连接断开，缺陷被降级为"网络问题"。
            _logger?.LogError(ex, "MCP 工具调用抛异常：tool={Tool}", contractName);
            return (
                // 经窄接缝渲染（与执行链同一份错误契约）；接缝缺席时退回纯文本原因
                // ——协议层不允许因为"少注册了一个服务"而把异常抛回宿主循环（那会中断会话）。
                _errorFormatter?.Format(contractName, "工具执行抛异常（详见宿主日志）")
                    ?? $"工具执行抛异常（详见宿主日志）：{contractName}",
                true);
        }
    }

    // ─────────────────────────── 工具面 ───────────────────────────

    /// <summary>
    /// 从注册表（白名单已启用集）构建 MCP 工具面。
    /// </summary>
    /// <remarks>
    /// 注册表缺席（宿主未调用 <c>AddFeishuTools</c> 等入口）不抛错：<c>tools/list</c> 返回空列表是
    /// 合法状态，客户端会看到"这个服务没有任何工具"。真正的装配错误（白名单含未注册工具、
    /// 身份未放行）在注册表构建期就已经 fail-fast。
    /// </remarks>
    private void BuildToolSurface()
    {
        var registry = _services.GetService<FeishuToolRegistry>();

        // 注册表缺席 = 宿主未装配工具面（tools/list 空是合法状态）；
        // 构桥接缝缺席 = 同一装配缺陷的另一面，一并按"无工具"处理而不是抛。
        if (registry is null || _functionFactory is null)
        {
            return;
        }

        // 先整体校验命名可映射性（冲突/超长都是一次性结构事实），再逐个构建。
        McpToolNames.EnsureMappable(registry.EnabledTools.Select(static d => d.Name));

        foreach (var definition in registry.EnabledTools)
        {
            if (!FeishuToolSchemas.SchemaByToolName.TryGetValue(definition.Name, out var schemaJson))
            {
                // 「注册名 ⊆ Schema 常量表」由装配期保证；此处兜底跳过而不是让服务起不来。
                continue;
            }

            var mcpName = McpToolNames.ToMcpName(definition.Name);

            // 经窄接缝构桥（不 new 实现类型；接缝内部复用 DI 的上下文访问器单例，
            // 与 InvokeAsync 里 Begin 的访问器是同一实例——这是"上下文可见"的前提）。
            var function = _functionFactory.Create(definition, schemaJson);

            // 描述**取自同一个 AIFunction 实例**（而非 definition.Description）。
            // 差别只在配了 per-tool affordance 的工具上：affordance 由 FeishuToolAIFunction 在构造期
            // 追加到 Description（`x-feishu` 刻意不进参数 Schema，故必须走描述），两链共用同一类型
            // ⇒ 进程内与 MCP 的工具描述逐字节一致，外部 Agent 也拿得到同一份决策指导。
            var info = new McpToolInfo(
                mcpName,
                definition.Name,
                function.Description,
                definition.IsWrite,
                definition.Risk,
                definition.Identity);

            _tools.Add(info);
            _byMcpName[mcpName] = info;
            _functions[mcpName] = function;

            // 入参 Schema 从 AIFunction 取（与 Agent 侧同一份编译期常量，经 ToolSchemaJson 提取为纯参数 Schema）。
            _inputSchemas[mcpName] = JsonNode.Parse(function.JsonSchema.GetRawText()) ?? new JsonObject();
        }
    }

    /// <summary>
    /// 按客户端给出的名字解析工具：先按 MCP 名精确匹配；未命中且名字含 <c>.</c> 时，
    /// 再按"契约名"重算一次映射（包容客户端把 <c>title</c> 当 <c>name</c> 回传的形态）。
    /// </summary>
    private bool TryResolveFunction(string requestedName, out AIFunction? function, out McpToolInfo? info)
    {
        if (_functions.TryGetValue(requestedName, out function) && _byMcpName.TryGetValue(requestedName, out info))
        {
            return true;
        }

        if (requestedName.IndexOf('.') >= 0)
        {
            var mapped = McpToolNames.ToMcpName(requestedName);
            if (_functions.TryGetValue(mapped, out function) && _byMcpName.TryGetValue(mapped, out info))
            {
                return true;
            }
        }

        function = null;
        info = null;
        return false;
    }

    /// <summary>
    /// 把 MCP 的 <c>arguments</c> 对象转成执行链的入参字典。
    /// </summary>
    /// <remarks>
    /// 值统一转成独立的 <see cref="JsonElement"/>：执行链的参数读取助手（<c>ToolArgs</c>）
    /// 对 <c>JsonElement</c> 的各 ValueKind 有显式处理，且该形态与"模型 tool_call 反序列化结果"一致
    /// （不引入第二套值语义）。<c>Clone()</c> 是必需的——<see cref="JsonDocument"/> 释放后
    /// 未克隆的 <c>JsonElement</c> 会抛 <c>ObjectDisposedException</c>。
    /// </remarks>
    private static Dictionary<string, object?> ToArguments(JsonObject? arguments)
    {
        var dictionary = new Dictionary<string, object?>(StringComparer.Ordinal);
        if (arguments is null)
        {
            return dictionary;
        }

        foreach (var pair in arguments)
        {
            if (pair.Value is null)
            {
                dictionary[pair.Key] = null;
                continue;
            }

            using var document = JsonDocument.Parse(pair.Value.ToJsonString());
            dictionary[pair.Key] = document.RootElement.Clone();
        }

        return dictionary;
    }

    /// <summary>
    /// 构建资源目录：域集合与工具面同源（同一份白名单的注册表条目）。
    /// </summary>
    private FeishuMcpResourceCatalog BuildResources(IServiceProvider services)
    {
        var registry = services.GetService<FeishuToolRegistry>();
        if (registry is null)
        {
            return FeishuMcpResourceCatalog.Build(
                [], new Dictionary<string, string>(StringComparer.Ordinal));
        }

        // 描述取自**已构桥的工具**（与 tools/list 逐字节同源）：affordance 是执行链在构造期
        // 追加到描述上的，故"资源里的 affordance" == "模型在 tools/list 里看到的那一段"。
        var descriptions = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var info in _tools)
        {
            descriptions[info.ContractName] = info.Description;
        }

        return FeishuMcpResourceCatalog.Build(registry.EnabledTools, descriptions);
    }

    /// <summary>
    /// 装配域 guidance（与 <c>FeishuAgent</c> 用同一份常量、同一预算装配器与<b>同一份摘要推导</b>）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>instructions 瘦身</b>：域集合与优先级顺序取自工具源（<see cref="FeishuAgentToolSource.GetGuidance"/>，
    /// 与进程内 Agent 完全一致的"工具数降序 + 域名序"），但<b>内容形态</b>由
    /// <see cref="FeishuMcpServerOptions.InstructionsMode"/> 决定——
    /// <c>routing</c>（默认）用与进程内同一个 <see cref="GuidanceRoutingSummary"/> 产出的一行摘要；
    /// <c>full</c> 用生成器发射的 L1 全文。
    /// </para>
    /// <para>
    /// <b>为什么内容重新取自 <c>FeishuToolGuidance</c> 而不是直接用源返回的块</b>：
    /// 源返回块的内容形态由 <see cref="FeishuAgentOptions.GuidanceMode"/> 决定（进程内链的开关），
    /// 而 MCP 链有<b>自己的</b>开关。两条链共用"域集合 + 顺序"（那份逻辑有白名单语义，必须同源），
    /// 各自决定"发摘要还是发全文"——否则宿主无法为两条链做不同取舍。
    /// </para>
    /// </remarks>
    private string BuildInstructions(IServiceProvider services)
    {
        var sourceBlocks = new List<GuidanceBlock>();
        foreach (var source in services.GetServices<FeishuAgentToolSource>())
        {
            if (source is null)
            {
                continue;
            }

            var guidance = source.GetGuidance(services);
            if (guidance is { Count: > 0 })
            {
                sourceBlocks.AddRange(guidance);
            }
        }

        var fullMode = string.Equals(
            _options.InstructionsMode, GuidanceModes.Full, StringComparison.Ordinal);

        var blocks = new List<GuidanceBlock>(sourceBlocks.Count);
        foreach (var block in sourceBlocks)
        {
            if (!FeishuToolGuidance.ByDomain.TryGetValue(block.Domain, out var full))
            {
                // 无 L1 资产的域（不应发生：装配契约要求"有工具必有资产"）——保持源内容，不丢域。
                blocks.Add(block);
                continue;
            }

            blocks.Add(new GuidanceBlock(
                block.Domain,
                fullMode ? full : GuidanceRoutingSummary.Create(block.Domain, full)));
        }

        return GuidanceComposer.Compose(null, blocks, FeishuGuidanceBudget.MaxGuidanceLength).Instructions;
    }

    private static string ServerVersion()
        => typeof(FeishuMcpToolServer).GetTypeInfo().Assembly.GetName().Version?.ToString() ?? "0.0.0";

    // ─────────────────────────── JSON-RPC 信封 ───────────────────────────

    private static string ResultEnvelope(JsonNode? id, JsonObject result)
        => new JsonObject
        {
            ["jsonrpc"] = "2.0",

            // DeepClone 必需：JsonNode 只能挂在一个父节点下，直接把请求里的 id 复用会抛
            // 「node already has a parent」。
            ["id"] = id?.DeepClone(),
            ["result"] = result,
        }.ToJsonString(WireOptions);

    private static string ErrorEnvelope(JsonNode? id, int code, string message)
        => new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id?.DeepClone(),
            ["error"] = new JsonObject
            {
                ["code"] = code,
                ["message"] = message,
            },
        }.ToJsonString(WireOptions);
}
