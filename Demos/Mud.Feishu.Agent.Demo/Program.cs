// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mud.Feishu.Abstractions.Conversations;
using Mud.Feishu.AI.Agents;
using Mud.Feishu.AI.Extensions;

namespace Mud.Feishu.Agent.Demo;

/// <summary>
/// 四个运行模式的入口分发（参数与模式开关**全部来自配置文件**，环境变量不再参与）。
/// </summary>
/// <remarks>
/// 模式判定顺序（见 <c>appsettings.json</c> 头注释）：
/// <list type="number">
///   <item><description><c>FeishuToolsDemo:Enabled</c> → 工具冒烟（全域只读工具 + 飞书客户端）；</description></item>
///   <item><description><c>FeishuImHandlerDemo:Enabled</c> → IM 会话处理器接入演示；</description></item>
///   <item><description><c>FeishuDocAgent:Enabled</c> → 文档业务智能体控制台；</description></item>
///   <item><description>均未启用 → Phase 0 裸模型一问一答（读 <c>FeishuDemo</c> 节）。</description></item>
/// </list>
/// </remarks>
public static class Program
{
    public static async Task Main()
    {
        // 配置根取输出目录（AppContext.BaseDirectory），appsettings.json → appsettings.local.json。
        var sources = DocAgentDemo.BuildConfiguration(AppContext.BaseDirectory);
        var configuration = sources.Configuration;

        // Phase 1/2 工具模式：全域只读工具 + 飞书客户端。
        if (configuration.GetValue<bool?>(ToolsDemoSettings.EnabledKey) is true)
        {
            await ToolsDemo.RunAsync(configuration);
            return;
        }

        // AI-FD-D12 P2D-5a 零自定义接入演示（内置 IM 会话事件处理器一行接入）。
        if (configuration.GetValue<bool?>(ImHandlerDemoSettings.EnabledKey) is true)
        {
            await ImConversationDemo.RunAsync(configuration);
            return;
        }

        // 文档业务智能体（写闭环 + 三道安全闸 + 剧本驱动），放置位置刻意在
        // 「事件模式之后、裸模型之前」，保持「能力由弱到强」的阅读顺序。
        if (DocAgentSettings.IsEnabledByConfiguration(configuration))
        {
            await DocAgentDemo.RunAsync(sources);
            return;
        }

        // Phase 0 裸模型（默认模式，无开关）。模型三参数统一读 FeishuDemo 节。
        var shared = FeishuDemoSettings.FromConfiguration(configuration);
        shared.Validate();
        var chat = shared.Model;

        var services = new ServiceCollection()
            .AddFeishuOpenAIChatClient("demo-model", chat.ModelId, chat.ApiKey, chat.Endpoint)
            .AddFeishuAgent(configure: options =>
            {
                options.ModelServiceKey = "demo-model";
                options.Name = "FeishuAgentDemo";
                options.Instructions = "你是嵌入在 .NET 服务里的飞书助手，用简洁中文回答。";
            })
            .BuildServiceProvider();

        var agent = services.GetRequiredService<FeishuAgent>();
        var store = services.GetRequiredService<IConversationStore>();

        // 单聊维度会话键：多轮对话共享同一会话（记忆）。
        var conversationKey = ConversationKeyBuilder.Build("demo-app", ConversationScope.P2P(), "ou_demo_user");
        var session = await agent.GetOrCreateSessionAsync(conversationKey);

        Console.WriteLine("裸模型一问一答（exit 退出）：");
        while (Console.ReadLine() is { } userText && userText.Length > 0 && !string.Equals(userText, "exit", StringComparison.OrdinalIgnoreCase))
        {
            var response = await agent.RunAsync(userText, session);
            Console.WriteLine($"Agent: {response.Text}");
            await agent.SaveSessionAsync(conversationKey, session);
        }
    }
}