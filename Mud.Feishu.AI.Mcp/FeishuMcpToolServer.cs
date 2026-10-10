// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Reflection;
using System.Text.Encodings.Web;

namespace Mud.Feishu.AI.Mcp;

/// <summary>
/// 飞书工具面的 MCP 服务端（协议层）：把<b>白名单已启用</b>的注册表工具经
/// JSON-RPC 2.0 / MCP <c>tools</c> 能力暴露给进程外 Agent。
/// </summary>
/// <remarks>
/// <para>
/// <b>执行链复用（DP-C6-1 铁律）</b>：<c>tools/call</c> 只解析出 <see cref="Microsoft.Extensions.AI.AIFunction"/>
/// 并调用它——授权门禁、租户上下文切换（<c>BeginScope</c>）、出站净化、内容安全、审计、
/// 流式/截断口径<b>全部沿用既有执行链</b>（经 <see cref="IFeishuToolFunctionFactory"/> 构造的桥 → 执行链类型）。
/// <b>R-7 起该桥与执行链类型已 internal</b>：本包只依赖两条<b>窄接缝</b>
/// （<see cref="IFeishuToolFunctionFactory"/> 构桥 + <see cref="IToolErrorTextFormatter"/> 渲染错误文本），
/// 不再 <c>new</c> 实现类型。
/// 本包<b>不</b>引用 SDK 强类型客户端，因此结构上无法旁路（守卫断言本目录不出现 <c>IFeishu*</c> 调用）。
/// </para>
/// <para>
/// <b>工具集真相源 = 注册表白名单</b>（<c>FeishuToolRegistry.EnabledTools</c>，即
/// <c>FeishuAgent:Tools</c> + <c>FeishuAgent:WriteAllowList</c> 应用后的结果）。
/// 未启用的工具在 <c>tools/list</c> 中<b>不可见</b>，调用时也只回"不存在或未启用"——
/// MCP 不提供第二条启用通道。
/// </para>
/// <para>
/// <b>为什么不用 <c>FeishuAgentToolSource.GetTools</c> 的列表</b>：那条路径会把写工具包进
/// MEAI 的 <c>ApprovalRequiredAIFunction</c>，而该类型<b>只是标记</b>——它的强制点在同一条管线里的
/// <c>FunctionInvokingChatClient</c>（MCP 不走 MAF 管线）。若照搬那条列表，写工具的"需要人工确认"
/// 标记会被静默丢弃。此处改用注册表 + <see cref="IFeishuToolFunctionFactory"/> 构造的桥：
/// 真实门禁是执行链的授权门禁（写工具默认不在白名单、授权器返回
/// <c>NeedsUserConfirmation</c> 时拒绝且零调用下游——由用例锁定），因此本差别是<b>可测的</b>而不是隐含的。
/// </para>
/// <para>
/// <b>租户上下文</b>：每次调用前经 <see cref="IFeishuToolContextAccessor.Begin"/> 显式重建
/// （MCP 调用不在事件流内，与续跑同构），作用域在 <c>finally</c> 内释放；
/// 缺 appKey 时构造期即抛（fail-closed，禁止默认应用兜底）。
/// </para>
/// <para>
/// <b>线程模型</b>：单连接顺序处理（<see cref="FeishuMcpStdioHost"/> 逐行 await），
/// <c>_initialized</c> 标记无需加锁；若宿主自行并发投递，请自行串行化。
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
    /// R-7：构桥接缝（<c>null</c> = 宿主未装配工具面 ⇒ <c>tools/list</c> 为空，与"注册表缺席"同语义）。
    /// </summary>
    private readonly IFeishuToolFunctionFactory? _functionFactory;

    /// <summary>
    /// R-7：错误文案接缝（<c>null</c> 时退回纯文本原因——协议层不得因缺一个接缝而抛）。
    /// </summary>
    private readonly IToolErrorTextFormatter? _errorFormatter;
    private readonly List<McpToolInfo> _tools = [];
    private readonly Dictionary<string, AIFunction> _functions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, JsonNode> _inputSchemas = new(StringComparer.Ordinal);
    private readonly Dictionary<string, McpToolInfo> _byMcpName = new(StringComparer.Ordinal);

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

        // R-7：两条窄接缝从容器解析（与注册表同源，均由 AddFeishuToolInfrastructure 装配）。
        // 缺席（= 宿主没装配工具面）不抛：tools/list 返回空列表是合法状态，
        // 真正的装配错误（白名单含未注册工具等）已在注册表构建期 fail-fast。
        _functionFactory = _services.GetService<IFeishuToolFunctionFactory>();
        _errorFormatter = _services.GetService<IToolErrorTextFormatter>();

        Instructions = _options.IncludeGuidance ? BuildInstructions(_services) : string.Empty;
        BuildToolSurface();
    }

    /// <summary>暴露给客户端的工具（<c>tools/list</c> 的内容来源）。</summary>
    public IReadOnlyList<McpToolInfo> Tools => _tools;

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
    /// <returns>响应 JSON；输入是<b>通知</b>（无 <c>id</c>）或空行时返回 <see langword="null"/>（不产生响应）。</returns>
    /// <remarks>本方法<b>不抛异常</b>（除取消外）：所有失败都归一为 JSON-RPC 错误对象，客户端不会收到"连接被打破"。</remarks>
    public async Task<string?> HandleMessageAsync(string? json, CancellationToken cancellationToken = default)
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
        if (!_initialized
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
                    return isNotification ? null : ResultEnvelope(id, BuildToolsListResult());

                case McpProtocol.MethodToolsCall:
                    return isNotification ? null : await CallToolAsync(id!, parameters, cancellationToken).ConfigureAwait(false);

                default:
                    // 未知**通知**静默（协议允许扩展通知）；未知**请求**回方法不存在。
                    return isNotification
                        ? null
                        : ErrorEnvelope(id, McpProtocol.ErrorMethodNotFound, $"未知方法 '{method}'（本服务提供 tools/list 与 tools/call）");
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
        var result = new JsonObject
        {
            // 版本协商：闭集内回显，闭集外回落本服务默认版本（见 McpProtocol 的注释）。
            ["protocolVersion"] = McpProtocol.Negotiate(requested),
            ["capabilities"] = new JsonObject
            {
                // listChanged=false：白名单在装配期固定（进程生命周期内不变），无需通知客户端刷新。
                ["tools"] = new JsonObject { ["listChanged"] = false },
            },
            ["serverInfo"] = new JsonObject
            {
                ["name"] = _options.ServerName,
                ["version"] = ServerVersion(),
            },
        };

        if (!string.IsNullOrWhiteSpace(Instructions))
        {
            // R7/A4~A6 的域资产（路由优先级 / 避坑 / 示例）与 Agent 内部注入的是同一份常量，
            // 故外部 Agent 的工具选择质量与进程内一致（而不是只拿到 163 条描述）。
            result["instructions"] = Instructions;
        }

        return result;
    }

    private JsonObject BuildToolsListResult()
    {
        var tools = new JsonArray();
        foreach (var info in _tools)
        {
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

        return new JsonObject { ["tools"] = tools };
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
        // 显式重建租户上下文（与 R5-2 续跑同构的教训）：MCP 调用不在事件流内，
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

            return result is FeishuToolResult toolResult
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
                // R-7：经窄接缝渲染（与执行链同一份错误契约）；接缝缺席时退回纯文本原因
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

        // 先整体校验命名可映射性（冲突/超长都是一次性结构事实），再逐个构建——
        // 校验是纯函数（McpToolNames.EnsureMappable），因此"冲突会 fail-fast"这句话有可自证的证据。
        McpToolNames.EnsureMappable(registry.EnabledTools.Select(static d => d.Name));

        foreach (var definition in registry.EnabledTools)
        {
            if (!FeishuToolSchemas.SchemaByToolName.TryGetValue(definition.Name, out var schemaJson))
            {
                // 契约守卫保证「注册名 ⊆ Schema 常量表」；此处兜底跳过而不是让服务起不来。
                continue;
            }

            var mcpName = McpToolNames.ToMcpName(definition.Name);

            // R-7：经窄接缝构桥（不再 new 实现类型；接缝内部复用 DI 的上下文访问器单例，
            // 与 InvokeAsync 里 Begin 的访问器是同一实例——这是"上下文可见"的前提）。
            var function = _functionFactory.Create(definition, schemaJson);
            var info = new McpToolInfo(
                mcpName,
                definition.Name,
                definition.Description,
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

    /// <summary>装配域 guidance（与 <c>FeishuAgent</c> 用同一份常量与同一预算装配器）。</summary>
    private static string BuildInstructions(IServiceProvider services)
    {
        var blocks = new List<FeishuGuidanceBlock>();
        foreach (var source in services.GetServices<FeishuAgentToolSource>())
        {
            if (source is null)
            {
                continue;
            }

            var guidance = source.GetGuidance(services);
            if (guidance is { Count: > 0 })
            {
                blocks.AddRange(guidance);
            }
        }

        return FeishuGuidanceComposer.Compose(null, blocks).Instructions;
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
