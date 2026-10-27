// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Mcp;

/// <summary>
/// MCP server 配置：<b>进程级租户上下文</b> + 服务端标识。
/// </summary>
/// <remarks>
/// <para>
/// <b>appKey 不从协议参数来</b>：一旦允许每次 <c>tools/call</c> 携带 appKey，就把"切换租户"的能力
/// 交给了 MCP 客户端（以及能影响客户端的模型/提示注入）——与"禁止默认应用兜底"同一根线。
/// 跨租户宿主的两条正确形态：<b>每租户一个 stdio 进程</b>（进程即租户边界），或 HTTP 传输由
/// <b>宿主认证层</b>逐请求给出身份（<see cref="FeishuMcpHttpOptions.TenantBoundPerRequest"/>）；
/// 两者都不给「客户端自报租户」留入口。
/// </para>
/// <para>
/// <b>为什么工具白名单不在这里</b>：暴露哪些工具的唯一真相源是
/// <see cref="FeishuAgentOptions.Tools"/> / <see cref="FeishuAgentOptions.WriteAllowList"/>
/// （装配期的注册表白名单）。本包<b>不新增</b>第二个开关——否则会出现"MCP 说没有、Agent 说有"
/// 的双真相源。
/// </para>
/// <para>
/// 配置绑定注意：本类型不得使用 <c>required</c>（源生成的配置绑定经 <c>new T()</c> 构造，
/// 会报 CS9035）；校验一律走 <see cref="Validate"/>。
/// </para>
/// </remarks>
public sealed class FeishuMcpServerOptions
{
    /// <summary>配置节名（<c>FeishuMcp</c>）。</summary>
    public const string SectionName = "FeishuMcp";

    /// <summary>工具执行上下文的应用标识（<b>必填</b>；多租户隔离的事实来源。HTTP 的逐请求绑租户是唯一豁免口，见 <see cref="Http"/>）。</summary>
    /// <remarks>缺失即 fail-closed：进程启动期抛可读异常，而不是运行期用"默认应用"兜底。</remarks>
    public string AppKey { get; set; } = string.Empty;

    /// <summary>
    /// 当前用户 ID（可空；user 身份工具<b>必需</b>）。
    /// </summary>
    /// <remarks>
    /// 与 <c>FeishuToolContext.UserId</c> 同义：user 身份工具（如 <c>task.list_my_tasks</c>、
    /// <c>attendance.query_my_flow</c>）在执行期用它作为<b>用户令牌缓存查找键</b>；
    /// 缺它时执行链以 <c>invalid_args</c>（指向 <c>FeishuToolContext.UserId</c>）拒绝，不会静默降级为租户令牌。
    /// </remarks>
    public string? UserId { get; set; }

    /// <summary>会话键（可空；仅用于审计/日志的可读标识，不参与租户隔离）。</summary>
    public string? ConversationKey { get; set; }

    /// <summary>聊天 ID（可空；个别工具从上下文取会话，而非模型传参）。</summary>
    public string? ChatId { get; set; }

    /// <summary>话题/线程 ID（可空；<c>im.reply_message</c> 的 <c>reply_in_thread</c> 从上下文取）。</summary>
    public string? ThreadId { get; set; }

    /// <summary>服务端名（<c>initialize.serverInfo.name</c>；默认 <c>mud-feishu</c>）。</summary>
    public string ServerName { get; set; } = "mud-feishu";

    /// <summary>
    /// 是否把"已启用域的 guidance（路由/避坑/示例）"作为 <c>initialize.instructions</c> 下发。
    /// </summary>
    /// <remarks>
    /// 默认 <c>true</c>：外部 Agent 拿到的是全量策展工具面（域数与工具数以 <c>tools/list</c>
    /// 实测为准——注释不写死规模数字，防止它与真相源漂移），缺了域路由资产就靠猜
    /// （这与 Agent 内部把 guidance 注入系统提示是同一份资产、同一份预算常量）。
    /// 关闭后只下发工具清单——留出"宿主自己写指令"的余地。
    /// </remarks>
    public bool IncludeGuidance { get; set; } = true;

    /// <summary>
    /// 是否暴露 <c>resources</c> 能力（默认 <c>true</c>）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 开启后：<c>initialize.capabilities</c> 增 <c>resources.listChanged=false</c>，
    /// 且 <c>resources/list</c> / <c>resources/read</c> 可用——把域 guidance / per-tool affordance /
    /// 域 scope / 错误契约<b>按需读取</b>（渐进披露），而不是全量灌进 instructions。
    /// </para>
    /// <para>
    /// <b>为什么可关</b>：老客户端（或不理解 <c>resources</c> 的客户端）在能力声明里看到它
    /// 不会有副作用，但宿主可能希望关闭该能力面（逐字节回归）——故留显式开关。
    /// 资源自身的数据源与 instructions <b>同源</b>（编译期常量），关闭只是"少一个入口"。
    /// </para>
    /// </remarks>
    public bool IncludeResources { get; set; } = true;

    /// <summary>
    /// <c>initialize.instructions</c> 的形态（取值与 <see cref="GuidanceModes"/> 一致，默认 <c>routing</c>）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>routing</c>（默认）：每域<b>一行路由摘要</b>（≤256 字符/域），全文经
    /// <c>resources/read feishu-guidance://{domain}</c> 按需读取——总量恒定、不随域数增长。
    /// <c>full</c>：每域 L1 全文拼接（域多时会按预算整域丢弃）。
    /// </para>
    /// <para>
    /// <b>与 <see cref="FeishuAgentOptions.GuidanceMode"/> 的关系</b>：两者取值闭集相同、语义相同，
    /// 但作用域不同——前者管<b>进程内 Agent</b> 的系统提示，后者管 <b>MCP 协议层</b>下发的
    /// <c>initialize.instructions</c>（同一宿主可能同时跑两条链，各自需要不同的形态：
    /// 进程内没有翻页成本、MCP 客户端有工具列表开销）。
    /// 两条链<b>共用同一份摘要推导实现</b>（<c>GuidanceRoutingSummary</c>），故形态不同但内容同源。
    /// </para>
    /// </remarks>
    public string InstructionsMode { get; set; } = GuidanceModes.Routing;

    /// <summary>Streamable HTTP 传输配置（<c>FeishuMcp:Http</c> 节；判据与理由见 <see cref="FeishuMcpHttpOptions"/>）。</summary>
    public FeishuMcpHttpOptions Http { get; set; } = new();

    /// <summary>是否启用 <c>tools/list</c> 分页（默认 false——不传 cursor 的客户端静默少工具比报错更危险）。</summary>
    public bool EnableToolsPagination { get; set; }

    /// <summary>分页每页工具数（默认 50；仅 <see cref="EnableToolsPagination"/> 为 true 时生效）。</summary>
    public int ToolsPageSize { get; set; } = 50;

    /// <summary>单条请求的最大字节数（默认 1 MiB）；超限回 <c>-32600</c> 并继续（不断连）。</summary>
    public int MaxRequestBytes { get; set; } = 1_048_576;

    /// <summary>
    /// 校验配置；非法即抛（装配期/启动期 fail-fast，不留到运行期）。
    /// </summary>
    /// <exception cref="InvalidOperationException">缺少 appKey，或服务端标识、HTTP 节非法。</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(AppKey) && !Http.TenantBoundPerRequest)
        {
            throw new InvalidOperationException(
                $"{SectionName}:{nameof(AppKey)} 必填——MCP 调用不在事件流内，租户上下文必须由宿主显式提供；"
                + "多租户隔离禁止默认应用兜底（跨租户宿主请为每个租户起一个 stdio 进程，"
                + $"或经 HTTP 走 {SectionName}:{nameof(Http)}.{nameof(Http.TenantBoundPerRequest)}=true）");
        }

        if (string.IsNullOrWhiteSpace(ServerName))
        {
            throw new InvalidOperationException(
                $"{SectionName}:{nameof(ServerName)} 不能为空（它是 initialize.serverInfo.name，客户端据此标识本服务）");
        }

        // 形态闭集校验（拼写错误会静默退回全文拼接——域数增长后表现为整域丢弃）。
        if (!GuidanceModes.IsValid(InstructionsMode))
        {
            throw new InvalidOperationException(
                $"{SectionName}:{nameof(InstructionsMode)} 取值非法: '{InstructionsMode}'——"
                + $"合法值为 {GuidanceModes.AllowedValuesText}（与 FeishuAgent:{nameof(FeishuAgentOptions.GuidanceMode)} 同闭集）");
        }

        Http.Validate();

        if (ToolsPageSize < 1)
        {
            throw new InvalidOperationException(
                $"{SectionName}:{nameof(ToolsPageSize)} 必须 >= 1（当前 {ToolsPageSize}）");
        }

        if (MaxRequestBytes < 1024)
        {
            throw new InvalidOperationException(
                $"{SectionName}:{nameof(MaxRequestBytes)} 至少 1024 字节（当前 {MaxRequestBytes}）");
        }

    }
}
