// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Mud.Feishu.AI.Mcp;

/// <summary>
/// MCP server 装配入口（可选包 <c>Mud.Feishu.AI.Mcp</c>）。
/// </summary>
/// <remarks>
/// <para>
/// <b>前置</b>：宿主已装配飞书工具面（<c>AddFeishuTools</c> 或逐域入口）与
/// <c>FeishuAgentOptions</c>（<c>AddFeishuAgent</c>）——本入口<b>不</b>替你装配工具面
/// （否则会隐式拉全量域，与"宿主显式装配"的既有契约冲突）。
/// </para>
/// <para>
/// <b>白名单同源</b>：暴露给 MCP 客户端的工具集 = <c>FeishuAgent:Tools</c> +
/// <c>FeishuAgent:WriteAllowList</c>（装配期注册表白名单），本包<b>不新增</b>任何开关。
/// </para>
/// <para>
/// 最小可用示例（宿主进程 <c>Program.cs</c>）：
/// <code>
/// var builder = Host.CreateApplicationBuilder(args);
/// builder.Services.AddFeishuAgent(...);                 // 飞书 Agent 运行时
/// builder.Services.AddFeishuTools();                    // 工具面（全部域，默认不启用）
/// builder.Services.AddFeishuMcpServer(o =&gt;               // 本包
/// {
///     o.AppKey = "cli_xxx";                            // 必填：进程级租户
///     // o.UserId = "ou_xxx";                          // 用到 user 身份工具时必填
/// });
/// await builder.Build().RunFeishuMcpStdioAsync();       // 阻塞直到客户端关闭 stdin
/// </code>
/// 并在 <c>appsettings.json</c> 里给工具面白名单（MCP 与进程内 Agent 共用同一份配置）：
/// <code>
/// "FeishuAgent": { "Tools": [ "bitable.query_records", "docx.get_raw_content" ],
///                  "WriteAllowList": [ "im.send_message" ] },
/// "FeishuMcp": { "AppKey": "cli_xxx" }
/// </code>
/// </para>
/// <para>
/// <b>stdio 与日志</b>：stdio 传输下 <b>stdout 是协议通道</b>，日志必须写 stderr 或文件，
/// 否则日志行会被客户端当成非法 JSON-RPC 消息（服务端会回 parse error，客户端会看到噪声）。
/// </para>
/// </remarks>
public static class McpServiceCollectionExtensions
{
    /// <summary>
    /// 注册 MCP server（协议层 + stdio 宿主）。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <param name="configure">配置回调（可空；也可由宿主的配置系统绑定 <c>FeishuMcp</c> 节）。</param>
    /// <returns>服务集合。</returns>
    public static IServiceCollection AddFeishuMcpServer(
        this IServiceCollection services,
        Action<FeishuMcpServerOptions>? configure = null)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        // 上下文访问器由工具面包提供（AddFeishuTools 已 TryAdd）；此处再 TryAdd 一次，
        // 使"只装配单域 + 本包"的宿主也能工作，且不产生第二个实例（AsyncLocal 必须唯一）。
        services.TryAddSingleton<IFeishuToolContextAccessor, FeishuToolContextAccessor>();

        var optionsBuilder = services.AddOptions<FeishuMcpServerOptions>();
        if (configure is not null)
        {
            optionsBuilder.Configure(configure);
        }

        services.TryAddSingleton<FeishuMcpToolServer>();
        services.TryAddSingleton(sp => new FeishuMcpStdioHost(
            sp.GetRequiredService<FeishuMcpToolServer>(),
            Console.In,
            Console.Out,
            sp.GetService<ILogger<FeishuMcpStdioHost>>()));

        return services;
    }

    /// <summary>
    /// 解析 MCP server 并以 stdio 协议运行到输入结束。
    /// </summary>
    /// <param name="services">服务提供器（须已调用 <see cref="AddFeishuMcpServer"/>）。</param>
    /// <param name="input">输入流（可空 ⇒ <c>Console.In</c>；测试可注入字符串读取器）。</param>
    /// <param name="output">输出流（可空 ⇒ <c>Console.Out</c>）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>已处理的请求数。</returns>
    /// <exception cref="InvalidOperationException">配置非法（如缺 appKey）——在解析时 fail-fast。</exception>
    public static Task<int> RunFeishuMcpStdioAsync(
        this IServiceProvider services,
        TextReader? input = null,
        TextWriter? output = null,
        CancellationToken cancellationToken = default)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        // 解析服务器即触发配置校验（缺 appKey 在这里响亮失败，而不是等第一次工具调用）。
        var server = services.GetRequiredService<FeishuMcpToolServer>();
        var host = new FeishuMcpStdioHost(
            server,
            input ?? Console.In,
            output ?? Console.Out,
            services.GetService<ILogger<FeishuMcpStdioHost>>());

        return host.RunAsync(cancellationToken);
    }
}
