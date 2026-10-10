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
/// <b>为什么 appKey 只从配置来、不从协议参数来</b>：一旦允许每次 <c>tools/call</c> 携带 appKey，
/// 就等于把"切换租户"的能力交给了 MCP 客户端（以及能影响客户端的模型/提示注入）——
/// 这与"多租户隔离禁止默认应用兜底"（TMA2-20）同一根线，只是换了个入口。
/// 跨租户宿主的正确形态是<b>每个租户一个 stdio 进程</b>（进程即租户边界），
/// 而不是在协议层传租户标识。
/// </para>
/// <para>
/// <b>为什么工具白名单不在这里</b>：暴露哪些工具的唯一真相源是
/// <see cref="FeishuAgentOptions.Tools"/> / <see cref="FeishuAgentOptions.WriteAllowList"/>
/// （装配期的注册表白名单）。本包<b>不新增</b>第二个开关——否则会出现"MCP 说没有、Agent 说有"
/// 的双真相源（DP-C6-1）。
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

    /// <summary>
    /// 工具执行上下文的应用标识（<b>必填</b>；多租户隔离的事实来源）。
    /// </summary>
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
    /// 默认 <c>true</c>：外部 Agent 拿到的工具面有 160+ 个工具，缺了域路由资产就靠猜
    /// （这与 Agent 内部把 guidance 注入系统提示是同一份资产、同一份预算常量）。
    /// 关闭后只下发工具清单——留出"宿主自己写指令"的余地。
    /// </remarks>
    public bool IncludeGuidance { get; set; } = true;

    /// <summary>
    /// 校验配置；非法即抛（装配期/启动期 fail-fast，不留到运行期）。
    /// </summary>
    /// <exception cref="InvalidOperationException">缺少 appKey，或服务端标识非法。</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(AppKey))
        {
            throw new InvalidOperationException(
                $"{SectionName}:{nameof(AppKey)} 必填——MCP 调用不在事件流内，租户上下文必须由宿主显式提供；"
                + "多租户隔离禁止默认应用兜底（跨租户宿主请为每个租户起一个 stdio 进程）");
        }

        if (string.IsNullOrWhiteSpace(ServerName))
        {
            throw new InvalidOperationException(
                $"{SectionName}:{nameof(ServerName)} 不能为空（它是 initialize.serverInfo.name，客户端据此标识本服务）");
        }
    }
}
