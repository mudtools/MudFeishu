// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using Mud.Feishu.Abstractions;
using Mud.Feishu.AI.Tools.Generated;
using Mud.Feishu.AI.Tools.Tools;

namespace Mud.Feishu.AI.Tools.Internal;

/// <summary>
/// 万能兜底调用执行器（<c>feishu.api_call</c>，R6 / S4-S5）。
/// </summary>
/// <remarks>
/// <para>
/// <b>它解决什么</b>：SDK 有 1228 个方法，策展工具面只覆盖其中一部分。未策展的方法对模型完全
/// 不可见 —— <c>feishu.schema_read</c> 让模型查到签名，本工具让它<b>真的能调</b>。
/// </para>
/// <para>
/// <b>调度方式：HTTP 层调度，不是反射</b>。用编译期目录（<see cref="FeishuToolMethodCatalog"/>）
/// 拿到 http/route/参数名，把 <c>{param}</c> 替换为实际值、拼上查询串，再经
/// <c>appContext.HttpClient.SendRawAsync</c> 发出。选这条路的原因有二：
/// ① 反射 <c>MethodInfo.Invoke</c> 会打破本仓库的 AOT 纪律（<c>IL2026/IL3050</c> 必须为 0）；
/// ② <c>SendRawAsync</c> 内部走 <c>SendCoreAsync</c>（由生成的子类重写以注入令牌与重试），
/// 并且会先经 <c>ValidateRequestAsync</c> 做 URL 白名单 / SSRF 复验 —— 兜底通道因此
/// <b>不会绕过</b>鉴权与出站治理。
/// </para>
/// <para>
/// <b>四条 fail-closed 边界（本工具的安全面就在这里）</b>：
/// <list type="number">
/// <item><b>用户态方法拒绝</b>：<c>token_kind=user</c> 的方法不经兜底。
/// 本工具无 <c>Source</c>，其身份轴被派定为 <c>tenant</c>，用租户令牌调用户态接口会
/// <b>静默造成身份错配</b>（表面上成功，实际以错误主体执行）。</item>
/// <item><b>高危写拒绝</b>：<c>risk=high-risk-write</c>（目录由方法名危险词在编译期派生）的方法
/// 一律拒绝，要求走策展工具或先策展。兜底的存在意义是"补未策展的常规能力"，
/// 不是"绕过策展的高危闸门"。</item>
/// <item><b>路径参数按路由模板强校验</b>：路由里的 <c>{x}</c> 占位符必须<b>恰好</b>被
/// <c>path_params</c> 覆盖（缺一个就把带 <c>{x}</c> 的字面量发出去，多一个说明模型搞错了对象）。</item>
/// <item><b>默认预演</b>：<c>dry_run</c> 缺省为 <c>true</c>，只回调用形态不触下游。</item>
/// </list>
/// </para>
/// <para>
/// <b>已策展的方法</b>：预览与响应里都会提示对应的策展工具名（<c>curated_tool</c>）——
/// 有专用工具时应优先用专用工具（它有投影、截断、幂等键与更精确的风险分级）。
/// </para>
/// </remarks>
internal sealed class GenericApiTools(
    IOptions<FeishuAgentOptions> options,
    IFeishuAppManager? appManager = null)
{
    /// <summary>路由模板里的路径参数占位符（如 <c>{meeting_id}</c>）。</summary>
    private static readonly Regex RoutePlaceholder = new(@"\{([^}]+)\}", RegexOptions.Compiled, TimeSpan.FromSeconds(2));

    /// <summary>高危写（目录 <c>risk</c> 字段取值）。</summary>
    private const int HighRisk = 2;

    private readonly int _maxResultLength = (options ?? throw new ArgumentNullException(nameof(options))).Value.MaxToolResultLength;
    private readonly IFeishuAppManager? _appManager = appManager;

    /// <summary>feishu.api_call：万能兜底调用（默认只预演）。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantApiCallTool))]
    public Task<FeishuToolResult> ApiCallAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.FeishuApiCall, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var method = ToolArgs.RequireString(arguments, "method");
            var dryRun = ToolArgs.OptionalBool(arguments, "dry_run") ?? true;

            if (!FeishuToolMethodCatalog.ByQualifiedName.TryGetValue(method, out var entry))
            {
                throw new ArgumentException(
                    $"方法 '{method}' 不在 SDK 方法目录中。可用 feishu.schema_read 按关键字查询"
                    + $"（当前目录共 {FeishuToolMethodCatalog.SdkMethodCount} 个方法）。");
            }

            if (string.IsNullOrWhiteSpace(entry.Route))
            {
                throw new ArgumentException(
                    $"方法 '{method}' 在目录中没有 HTTP 路由（可能是模块注册器一类的伪方法），不可经兜底调用。");
            }

            if (string.Equals(entry.TokenKind, "user", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    $"方法 '{method}' 是**用户态**接口（token_kind=user），兜底通道不提供它："
                    + "本工具的身份轴派定为 tenant，用租户令牌调用户态接口会造成静默的身份错配。"
                    + "请改用对应的策展工具（先用 feishu.schema_read 看 curated_tool），或由宿主策展该动作。");
            }

            if (entry.Risk >= HighRisk)
            {
                throw new ArgumentException(
                    $"方法 '{method}' 是 **high-risk-write**（目录 risk=2），兜底通道不提供它——"
                    + "高危动作必须走策展工具（有授权门禁、dry_run 摘要与白名单键控）。"
                    + "若确实需要，请按 R6 方案把该动作策展为工具。");
            }

            var pathParams = ParseJsonObject(arguments, "path_params", method);
            var queryParams = ParseJsonObject(arguments, "query_params", method);
            var bodyJson = ToolArgs.OptionalString(arguments, "body");

            var route = SubstitutePathParams(entry, pathParams);
            var url = AppendQueryParams(route, queryParams);

            if (dryRun)
            {
                return FeishuToolResult.FromText(ToolResultText.TruncateJson(
                    ToolResultJson.ToText(BuildPreview(entry, url, pathParams, queryParams, bodyJson)),
                    _maxResultLength));
            }

            if (_appManager is null)
            {
                throw new ArgumentException(
                    "feishu.api_call 的 dry_run=false 需要 IFeishuAppManager——宿主须注册飞书应用管理器。"
                    + "当前仅 dry_run=true 可用（预演不需要任何下游依赖）。");
            }

            // 令牌、重试、URL 白名单/SSRF 复验都在 SendRawAsync → SendCoreAsync 链内完成，
            // 本工具**不自行拼 Authorization 头**（重复实现认证链会绕开出站治理）。
            var appContext = _appManager.GetDefaultApp();
            var baseUrl = appContext.Config.BaseUrl?.TrimEnd('/') ?? string.Empty;
            var fullUrl = baseUrl + url;

            using var request = new HttpRequestMessage(new HttpMethod(entry.Http), fullUrl);
            if (!string.IsNullOrWhiteSpace(bodyJson))
            {
                request.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");
            }

            using var response = await appContext.HttpClient
                .SendRawAsync(request, cancellationToken)
                .ConfigureAwait(false);
            var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            return FeishuToolResult.FromText(ToolResultText.TruncateJson(
                ToolResultJson.ToText(BuildResponse(entry, fullUrl, response, responseBody)),
                _maxResultLength));
        });
    }

    // ────────── 结果构造 ──────────

    private static JsonObject BuildPreview(
        FeishuToolMethodCatalog.Entry entry,
        string url,
        JsonObject pathParams,
        JsonObject queryParams,
        string? bodyJson)
    {
        var preview = new JsonObject
        {
            ["dry_run"] = true,
            ["method"] = entry.InterfaceName + "." + entry.Method,
            ["http"] = entry.Http,
            ["route_template"] = entry.Route,
            ["url"] = url,
            ["token_kind"] = entry.TokenKind,
            ["module"] = entry.Module,
            ["risk"] = RiskLabel(entry.Risk),
            ["path_params"] = pathParams.DeepClone(),
            ["query_params"] = queryParams.DeepClone(),
            // 键名刻意用 request_body 而不是 body：本字段是**请求体预览**（api_call 的入参），
            // 不是消息正文。`ToolExecutorSkeletonGuards` 按 `["body"]`/`["content"]` 键判定
            // "消息正文回填必须预截断"，改名是为了语义准确而非规避——预览整体由外层
            // TruncateJson 统一按 MaxToolResultLength 截断，不构成无界回填。
            ["request_body"] = bodyJson is null ? null : ParseJsonSafely(bodyJson),
            ["curated"] = entry.Curated,
            ["curated_tool"] = entry.CuratedTool,
            ["note"] = entry.Curated
                ? $"已有策展工具 '{entry.CuratedTool}' —— 优先用它（带投影/截断/幂等键与更精确的风险分级）。"
                : "未策展方法。以 dry_run=false 重放同一参数才会实际发起 HTTP 请求。",
        };

        // 目录未登记的查询参数**不拒绝**（目录可能不完整），但要点出来——否则模型会以为它生效了。
        var unlisted = queryParams
            .Select(static p => p.Key)
            .Where(name => !entry.QueryParams.Contains(name, StringComparer.Ordinal))
            .ToArray();
        if (unlisted.Length > 0)
        {
            // 逐条 AddNode（本仓的 AOT 安全惯用法）：netstandard2.0 目标上
            // `new JsonArray([.. xs.Select(x => JsonValue.Create(x))])` 会因 JsonValue.Create
            // 的重载集合而无法推断 Select 的类型参数（CS0411）。
            var unlistedArray = new JsonArray();
            foreach (var name in unlisted)
            {
                unlistedArray.AddNode(JsonValue.Create(name));
            }

            preview["unlisted_query_params"] = unlistedArray;
        }

        return preview;
    }

    private static JsonObject BuildResponse(
        FeishuToolMethodCatalog.Entry entry,
        string fullUrl,
        HttpResponseMessage response,
        string responseBody)
    {
        var parsed = ParseJsonSafely(responseBody);
        var result = new JsonObject
        {
            ["status_code"] = (int)response.StatusCode,
            ["method"] = entry.InterfaceName + "." + entry.Method,
            ["http"] = entry.Http,
            ["url"] = fullUrl,
            ["risk"] = RiskLabel(entry.Risk),
            ["response"] = parsed,
        };

        // 飞书的业务错误在 200 里带 code/msg：抽到顶层，模型不必自己翻响应体。
        if (parsed is JsonObject envelope)
        {
            if (envelope.TryGetPropertyValue("code", out var code))
            {
                result["code"] = code?.DeepClone();
            }

            if (envelope.TryGetPropertyValue("msg", out var msg))
            {
                result["msg"] = msg?.DeepClone();
            }

            if (envelope.TryGetPropertyValue("data", out var data))
            {
                result["data"] = data?.DeepClone();
            }
        }

        return result;
    }

    private static string RiskLabel(int risk) => risk switch
    {
        0 => "read",
        1 => "write",
        2 => "high-risk-write",
        _ => $"unknown({risk})",
    };

    // ────────── 参数构造（严格校验，不做静默降级） ──────────

    /// <summary>把路由模板中的 <c>{param}</c> 替换为实际值；缺失或多传一律报错。</summary>
    /// <remarks>
    /// <b>为什么判据取自路由模板而不是目录的 <c>path_params</c></b>：模板里的占位符就是
    /// "必须有值"的权威事实，且不依赖目录完整度。缺失时若静默放行，发出去的是带
    /// <c>{meeting_id}</c> 字面量的 URL——平台会回一个难以定位的 404，比本地拒绝差得多。
    /// </remarks>
    private static string SubstitutePathParams(FeishuToolMethodCatalog.Entry entry, JsonObject pathParams)
    {
        // `.Cast<Match>()` 不可省：netstandard2.0 上 MatchCollection 只实现非泛型 IEnumerable
        // ⇒ 直接 `Matches(...).Select(...)` 的类型推断会失败（CS0411）。本仓既有守卫同样这么写。
        var required = RoutePlaceholder.Matches(entry.Route)
            .Cast<Match>()
            .Select(static m => m.Groups[1].Value)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var missing = required.Where(name => !pathParams.ContainsKey(name)).ToArray();
        if (missing.Length > 0)
        {
            throw new ArgumentException(
                $"path_params 缺少路由要求的参数：{string.Join(", ", missing)}"
                + $"（路由模板 {entry.Route} 需要 {string.Join(", ", required)}）。");
        }

        var extra = pathParams
            .Select(static p => p.Key)
            .Where(name => !required.Contains(name, StringComparer.Ordinal))
            .ToArray();
        if (extra.Length > 0)
        {
            throw new ArgumentException(
                $"path_params 含路由模板未声明的参数：{string.Join(", ", extra)}"
                + $"（该路由只接受 {string.Join(", ", required)}）——大概率是调错了方法或参数名写错。");
        }

        var result = entry.Route;
        foreach (var pair in pathParams)
        {
            // 用两参重载：`Replace(string, string, StringComparison)` 在 netstandard2.0 上不存在（CS1501），
            // 而两参重载本来就是 ordinal 语义（本仓支持的 TFM 集合含 netstandard2.0）。
            result = result.Replace("{" + pair.Key + "}", Uri.EscapeDataString(ValueText(pair.Value)));
        }

        return result;
    }

    /// <summary>把查询参数追加到 URL（值按 <c>ToString</c> 后 URL 转义）。</summary>
    private static string AppendQueryParams(string route, JsonObject queryParams)
    {
        var effective = queryParams.Where(static p => p.Value is not null).ToArray();
        if (effective.Length == 0)
        {
            return route;
        }

        var builder = new StringBuilder(route).Append('?');
        for (var i = 0; i < effective.Length; i++)
        {
            if (i > 0)
            {
                builder.Append('&');
            }

            builder.Append(Uri.EscapeDataString(effective[i].Key))
                .Append('=')
                .Append(Uri.EscapeDataString(ValueText(effective[i].Value)));
        }

        return builder.ToString();
    }

    /// <summary>JSON 标量 → 查询/路径文本（字符串不带引号，布尔用 <c>true/false</c>）。</summary>
    private static string ValueText(JsonNode? value)
        => value switch
        {
            null => string.Empty,
            JsonValue v when v.TryGetValue<string>(out var s) => s,
            _ => value.ToJsonString().Trim('"'),
        };

    /// <summary>
    /// 严格解析参数 JSON 对象。<b>非法 JSON 一律报错</b>：静默降级为空对象会让带
    /// <c>{placeholder}</c> 的 URL 被真的发出去（"看起来成功、实际调错对象"）。
    /// </summary>
    /// <exception cref="ArgumentException">不是合法 JSON 对象。</exception>
    private static JsonObject ParseJsonObject(
        IReadOnlyDictionary<string, object?> arguments,
        string name,
        string method)
    {
        var raw = ToolArgs.OptionalString(arguments, name);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        JsonNode? node;
        try
        {
            node = JsonNode.Parse(raw);
        }
        catch (System.Text.Json.JsonException ex)
        {
            throw new ArgumentException($"{name} 不是合法 JSON（方法 {method}）：{ex.Message}");
        }

        return node as JsonObject
            ?? throw new ArgumentException($"{name} 需为 JSON 对象（形如 {{\"id\":\"xxx\"}}），实际: {raw}");
    }

    /// <summary>安全解析 JSON；失败时按纯文本回填（响应可能是非 JSON 内容）。</summary>
    private static JsonNode? ParseJsonSafely(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonNode.Parse(json);
        }
        catch (System.Text.Json.JsonException)
        {
            // 有意静默（守卫白名单）：本方法是 internal 纯函数（无 logger 面），且**输出确定**——
            // 解析失败即"响应体不是 JSON"，此时按纯文本回填正是要表达的事实（二进制/HTML 错误页
            // 也是合法响应）。当成异常上报会把"平台返回非 JSON 错误页"变成未处理异常，
            // 而模型需要看到的恰恰是那页错误文本。HTTP 状态码与 code/msg 已在同层显式回填。
            return JsonValue.Create(json);
        }
    }
}
