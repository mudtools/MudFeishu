// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
// -----------------------------------------------------------------------

using Mud.Feishu.AI.CapabilityDemo.Common;

namespace Mud.Feishu.AI.CapabilityDemo.Host;

/// <summary>
/// 一幕的装配差异（白名单 / 客户端域 / 模型形态 / 宿主钩子）。
/// </summary>
/// <remarks>
/// <b>白名单只在这里出现一次</b>：<c>--verify</c> 直接消费这些数组，因此"演示用的名单"
/// 与"被检查的名单"结构上不可能分叉。
/// </remarks>
internal sealed record ActProfile(
    string Id,
    string AgentName,
    string Instructions,
    string[] ReadTools,
    string[] WriteTools,
    Action<FeishuServiceBuilder> ConfigureClients,
    IChatClient? ScriptedClient = null,
    Action<FeishuAgentOptions>? ConfigureOptions = null,
    Action<IServiceCollection>? ConfigureHooks = null);

internal static class ActProfiles
{
    private const string BaseInstructions = """
        你是飞书能力面的演示助手。规则：
        1) 先拿真实 id（table_id / record_id）再谈修改，不要编造；
        2) 修改前说明改哪条、改什么；不确定时先 dry_run；
        3) 没有启用的工具就是不存在的工具，不要重试、不要绕过；
        4) 回答里引用你实际调用的工具名。
        """;

    public static ActProfile A1(DemoSettings settings) => new(
        Id: "a1",
        AgentName: "cap-a1-hello",
        Instructions: BaseInstructions,
        ReadTools: ToolSets.A1Read,
        WriteTools: [],
        ConfigureClients: static b => b.AddBiTableApi());

    public static ActProfile A3(DemoSettings settings) => new(
        Id: "a3",
        AgentName: "cap-a3-meta",
        Instructions: BaseInstructions + """

            当你不确定有没有某个能力时：先调 feishu.tool_search（按关键词检索策展工具），
            再用 feishu.guidance_read 读该域的路由/避坑/示例；不要猜一个不存在的工具名。
            """,
        ReadTools: [.. ToolSets.Meta4, .. ToolSets.MinimalRead],
        WriteTools: [],
        ConfigureClients: static b => b.AddBiTableApi());

    public static ActProfile A5(DemoSettings settings, bool includeDelete) => new(
        Id: "a5",
        AgentName: "cap-a5-bitable",
        Instructions: BaseInstructions + $"""

            目标多维表格：app_token={settings.BitTableAppToken}，table_id={settings.BitTableTableId}。
            用户的写操作会先停在人工审批；你无法自批，也不要假装已执行。
            """,
        ReadTools: ToolSets.BitableRead6,
        WriteTools: ToolSets.WritesForA5(includeDelete),
        ConfigureClients: static b => b.AddBiTableApi());

    public static ActProfile A8(DemoSettings settings) => new(
        Id: "a8",
        AgentName: "cap-a8-tenants",
        Instructions: BaseInstructions,
        ReadTools: ToolSets.MinimalRead,
        WriteTools: [],
        ConfigureClients: static b => b.AddBiTableApi());

    public static ActProfile A6(DemoSettings settings) => new(
        Id: "a6",
        AgentName: "cap-a6-mcp",
        Instructions: "MCP 演示（进程内不跑模型）。",
        ReadTools: ToolSets.McpRead,
        WriteTools: [],
        ConfigureClients: static b => b.AddBiTableApi());
}
