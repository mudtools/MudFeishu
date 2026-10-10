// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Mcp;

/// <summary>
/// MCP（Model Context Protocol）线协议常量：方法名、协议版本与 JSON-RPC 2.0 错误码。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么把版本协商做成"闭集常量"而不是回显客户端值</b>：回显会让"服务端实际支持的语法"
/// 与"声明的版本"脱钩——客户端按新版本发来的新参数我们会当未知字段静默忽略，失败方式不可观测。
/// 本服务只用到 <c>tools</c> 能力（且 <c>listChanged=false</c>），三个已发布修订的
/// <c>initialize</c>/<c>tools/list</c>/<c>tools/call</c> 形态一致，故显式声明支持的闭集，
/// 客户端请求了闭集外的版本时返回<b>本服务支持的默认版本</b>（协议规定由客户端决定是否继续）。
/// </para>
/// <para>
/// 本类型是 <b>internal</b>：协议常量不是宿主契约（宿主只需配置 <see cref="FeishuMcpServerOptions"/>）。
/// </para>
/// </remarks>
internal static class McpProtocol
{
    /// <summary>协议修订 2024-11-05（首个正式修订）。</summary>
    public const string Version20241105 = "2024-11-05";

    /// <summary>协议修订 2025-03-26（引入 Streamable HTTP；stdio 形态不变）。</summary>
    public const string Version20250326 = "2025-03-26";

    /// <summary>协议修订 2025-06-18（引入结构化工具输出与 <c>title</c>/annotations 细化）。</summary>
    public const string Version20250618 = "2025-06-18";

    /// <summary>默认（也是声明支持的最高）协议版本。</summary>
    public const string DefaultVersion = Version20250618;

    /// <summary><c>initialize</c>：会话握手（客户端第一条请求）。</summary>
    public const string MethodInitialize = "initialize";

    /// <summary><c>notifications/initialized</c>：客户端握手完成通知（无 id，不需响应）。</summary>
    public const string MethodInitialized = "notifications/initialized";

    /// <summary><c>ping</c>：存活探测（协议要求服务端以空结果响应）。</summary>
    public const string MethodPing = "ping";

    /// <summary><c>tools/list</c>：列出可调用工具。</summary>
    public const string MethodToolsList = "tools/list";

    /// <summary><c>tools/call</c>：调用工具。</summary>
    public const string MethodToolsCall = "tools/call";

    /// <summary>JSON-RPC 2.0：请求体不是合法 JSON。</summary>
    public const int ErrorParseError = -32700;

    /// <summary>JSON-RPC 2.0：不是合法请求对象（缺 method 等）。</summary>
    public const int ErrorInvalidRequest = -32600;

    /// <summary>JSON-RPC 2.0：方法不存在。</summary>
    public const int ErrorMethodNotFound = -32601;

    /// <summary>JSON-RPC 2.0：参数非法（含"工具不存在/未启用"）。</summary>
    public const int ErrorInvalidParams = -32602;

    /// <summary>JSON-RPC 2.0：服务端内部错误。</summary>
    public const int ErrorInternalError = -32603;

    /// <summary>MCP 自定义：会话尚未初始化（未 `initialize` 就发请求）。</summary>
    public const int ErrorServerNotInitialized = -32002;

    /// <summary>客户端请求的协议版本是否在支持闭集内。</summary>
    /// <param name="requested">客户端在 <c>initialize.params.protocolVersion</c> 中给出的版本（可空）。</param>
    /// <returns>是否支持。</returns>
    public static bool IsSupported(string? requested)
        => string.Equals(requested, Version20241105, StringComparison.Ordinal)
           || string.Equals(requested, Version20250326, StringComparison.Ordinal)
           || string.Equals(requested, Version20250618, StringComparison.Ordinal);

    /// <summary>协商结果：支持则回显请求版本，否则回落到 <see cref="DefaultVersion"/>。</summary>
    /// <param name="requested">客户端请求版本（可空）。</param>
    /// <returns>本服务在本次会话中使用的协议版本。</returns>
    public static string Negotiate(string? requested)
        => IsSupported(requested) ? requested! : DefaultVersion;
}

/// <summary>
/// 工具名映射：MCP 客户端（Claude 系）对工具名有 <c>^[a-zA-Z0-9_-]{1,64}$</c> 约束，
/// 而飞书工具契约名含 <c>.</c>（如 <c>bitable.query_records</c>）。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么必须映射而不是原样暴露</b>：不映射时名称里的 <c>.</c> 会被严格客户端在<b>客户端侧</b>
/// 拒绝（工具列表为空或报错），而服务端毫无信号——这是"服务端看起来正常、客户端一个工具都收不到"
/// 的静默失败形态。
/// </para>
/// <para>
/// <b>映射是单向且可判定冲突的</b>：<c>.</c> → <c>_</c>；契约名 <c>a.b_c</c> 与 <c>a_b.c</c> 会映射到
/// 同一名字——这类冲突在服务端<b>构造期 fail-fast</b>（见 <c>FeishuMcpToolServer</c>），
/// 不做"后者覆盖前者"的静默降级。契约名本身经 <c>tools/list</c> 的 <c>title</c> 字段原样透出
/// （人可读、且与《工具权限对照表》一致）。
/// </para>
/// </remarks>
internal static class McpToolNames
{
    /// <summary>严格客户端（Anthropic 工具名规范）允许的最大长度。</summary>
    public const int MaxNameLength = 64;

    /// <summary>契约名 → MCP 工具名（<c>.</c> → <c>_</c>）。</summary>
    /// <param name="contractName">契约工具名（如 <c>bitable.query_records</c>）。</param>
    /// <returns>MCP 工具名（如 <c>bitable_query_records</c>）。</returns>
    public static string ToMcpName(string contractName)
    {
        if (contractName is null)
            throw new ArgumentNullException(nameof(contractName));

        return contractName.Replace('.', '_');
    }

    /// <summary>
    /// 校验一批契约名"映射后可直接下发给严格客户端"：无冲突、长度合规。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 抽成<b>纯函数</b>而不是写在服务端构造函数里：这样它既能被真实工具面驱动
    /// （守卫跑 <c>FeishuToolNames.All</c>），也能被合成输入自证（<c>a.b_c</c> vs <c>a_b.c</c> 必须报红）——
    /// 否则"冲突检测"只是一段没有证据的防御代码。
    /// </para>
    /// </remarks>
    /// <param name="contractNames">契约工具名集合。</param>
    /// <exception cref="InvalidOperationException">存在映射冲突，或映射后名字超出客户端上限。</exception>
    public static void EnsureMappable(IEnumerable<string> contractNames)
    {
        if (contractNames is null)
            throw new ArgumentNullException(nameof(contractNames));

        var seen = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var contractName in contractNames)
        {
            var mcpName = ToMcpName(contractName);

            if (mcpName.Length > MaxNameLength)
            {
                throw new InvalidOperationException(
                    $"工具 '{contractName}' 映射后的 MCP 名 '{mcpName}' 超过 "
                    + $"{MaxNameLength.ToString(CultureInfo.InvariantCulture)} 字符——"
                    + "严格客户端（Anthropic 工具名规范）会拒绝整个工具列表，故在此 fail-fast");
            }

            if (seen.TryGetValue(mcpName, out var existing))
            {
                throw new InvalidOperationException(
                    $"工具名映射冲突：'{contractName}' 与 '{existing}' 都映射为 MCP 名 '{mcpName}'"
                    + "（契约名的 '.' 与 '_' 在映射后无法区分）——请调整工具命名");
            }

            seen[mcpName] = contractName;
        }
    }
}
