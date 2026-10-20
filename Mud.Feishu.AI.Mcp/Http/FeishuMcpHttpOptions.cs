// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Mcp;

/// <summary>
/// MCP Streamable HTTP 传输配置（<c>FeishuMcp:Http</c> 节）。
/// </summary>
/// <remarks>
/// <para>
/// <b>本节点不含「启用开关」</b>：HTTP 通道是否对外提供由宿主「映射不映射那个端点」决定，
/// 再加一个布尔开关就出现「配置说开着、实际没接线」的第二真相源。
/// </para>
/// <para>
/// <b>租户身份不在这里</b>：stdio 的租户边界是进程（<see cref="FeishuMcpServerOptions.AppKey"/>）；
/// HTTP 一个进程服务多个远端调用方，租户身份必须由宿主<b>认证层</b>逐请求给出
/// （见 <see cref="McpHttpRequest.AppKey"/>）。<see cref="AllowedAppKeys"/> 只是纵深防御的第二道
/// 名单，不是身份来源——把它当身份来源就等于允许客户端自报租户。
/// </para>
/// <para>
/// 配置绑定注意：本类型不得使用 <c>required</c>（源生成绑定经 <c>new T()</c> 构造）；校验走 <see cref="Validate"/>。
/// </para>
/// </remarks>
public sealed class FeishuMcpHttpOptions
{
    /// <summary>配置节名（<c>Http</c>，挂在 <see cref="FeishuMcpServerOptions.SectionName"/> 下）。</summary>
    public const string SectionName = "Http";

    /// <summary>默认会话空闲上限（秒）。</summary>
    public const int DefaultSessionIdleTimeoutSeconds = 900;

    /// <summary>默认并发会话上限。</summary>
    public const int DefaultMaxSessions = 64;

    /// <summary>
    /// 允许携带 <c>Origin</c> 头的来源清单（<c>scheme://host[:port]</c>，逐字比较）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>空清单 = 拒绝一切带 Origin 的请求</b>（fail-closed）：Origin 只由浏览器发出，
    /// 服务端 Agent/CLI 不发，因此默认形态对非浏览器客户端零影响，而 DNS 重绑定/跨源页面
    /// 打到一个「能读飞书全量数据」的端点时必然带 Origin，被这一条挡下。
    /// 确需浏览器客户端接入时把来源逐条加进来。
    /// </para>
    /// </remarks>
    public List<string> AllowedOrigins { get; set; } = [];

    /// <summary>
    /// 允许的租户标识名单（可空 ⇒ 不设第二道名单，完全以宿主认证层为准）。
    /// </summary>
    /// <remarks>非空时，认证给出的 appKey 必须在名单内，否则拒绝——用于「认证服务被误配成放行全部」时的兜底。</remarks>
    public List<string> AllowedAppKeys { get; set; } = [];

    /// <summary>会话空闲回收阈值（秒；默认 <see cref="DefaultSessionIdleTimeoutSeconds"/>）。</summary>
    /// <remarks>超过该时长未被访问的会话在<b>下一次访问或计数检查时</b>惰性回收，不起后台线程。</remarks>
    public int SessionIdleTimeoutSeconds { get; set; } = DefaultSessionIdleTimeoutSeconds;

    /// <summary>并发驻留会话上限（默认 <see cref="DefaultMaxSessions"/>）。</summary>
    /// <remarks>
    /// 每个会话持有一份协议会话状态；会话标识由服务端随机生成，故上限只是防御
    /// 「客户端崩溃重连风暴 / 恶意刷握手」把内存吃掉，不是业务配额。
    /// </remarks>
    public int MaxSessions { get; set; } = DefaultMaxSessions;

    /// <summary>
    /// 是否允许「一个进程服务多个租户」（默认 <see langword="false"/>）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>默认关闭的理由</b>：stdio 的租户边界是进程，<see cref="FeishuMcpServerOptions.AppKey"/>
    /// 因此必填。HTTP 让一个进程面对多个远端调用方，逐请求身份成为必需，但「允许逐请求换租户」
    /// 是把跨租户读写的可能性交给认证层——这是一个需要宿主明确承担的决定，不该由「接了个端点」隐式发生。
    /// </para>
    /// <para>
    /// <c>false</c>：HTTP 请求给出的身份必须<b>等于</b>配置的 <see cref="FeishuMcpServerOptions.AppKey"/>
    /// （单租户 HTTP 部署，与 stdio 同一形态）。<c>true</c>：<see cref="FeishuMcpServerOptions.AppKey"/>
    /// 允许留空，身份完全由认证层逐请求给出（每条会话在握手时绑定、逐请求校验一致）。
    /// 打开本项即要求认证层已经把租户判对——<see cref="AllowedAppKeys"/> 是它的兜底名单，不是替代。
    /// </para>
    /// </remarks>
    public bool TenantBoundPerRequest { get; set; }

    /// <summary>空闲会话回收阈值（<see cref="SessionIdleTimeoutSeconds"/> 的 <see cref="TimeSpan"/> 形态）。</summary>
    internal TimeSpan SessionIdleTimeout => TimeSpan.FromSeconds(SessionIdleTimeoutSeconds);

    /// <summary>
    /// 校验配置；非法即抛（装配期 fail-fast）。由 <see cref="FeishuMcpServerOptions.Validate"/> 调用。
    /// </summary>
    /// <exception cref="InvalidOperationException">数值越界，或名单含空白/畸形条目。</exception>
    public void Validate()
    {
        if (SessionIdleTimeoutSeconds < 1)
        {
            throw new InvalidOperationException(
                $"{SectionName}:{nameof(SessionIdleTimeoutSeconds)} 必须 ≥ 1（当前 {SessionIdleTimeoutSeconds.ToString(CultureInfo.InvariantCulture)}）"
                + "——0 或负值意味着每个会话在下一次访问即被回收，握手成功后第一条请求就会 404");
        }

        if (MaxSessions < 1)
        {
            throw new InvalidOperationException(
                $"{SectionName}:{nameof(MaxSessions)} 必须 ≥ 1（当前 {MaxSessions.ToString(CultureInfo.InvariantCulture)}）");
        }

        EnsureNoBlank(AllowedOrigins, nameof(AllowedOrigins));
        EnsureNoBlank(AllowedAppKeys, nameof(AllowedAppKeys));
    }

    private static void EnsureNoBlank(List<string> values, string name)
    {
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(
                    $"{SectionName}:{name} 含空白条目——空白项会让逐字比较退化为「空字符串也算命中」");
            }
        }
    }
}
