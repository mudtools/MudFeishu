// -----------------------------------------------------------------------
//  作者:Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Agents.AI;
using Microsoft.Extensions.DependencyInjection;
using Mud.Feishu.Abstractions.Conversations;
using Mud.Feishu.AI.Agents;
using Mud.Feishu.AI.Extensions;

namespace Mud.Feishu.Agent.Demo;

/// <summary>
/// Phase 0 裸模型一问一答冒烟（DoD：AddFeishuOpenAIChatClient + AddFeishuAgent 一行启动）。
/// </summary>
/// <remarks>
/// 运行前设置环境变量（Key 绝不写进代码/配置文件提交）：
/// <code>
/// set FEISHU_AI_MODEL_KEY=glm-4-flash
/// set FEISHU_AI_API_KEY=sk-xxxx
/// set FEISHU_AI_ENDPOINT=https://open.bigmodel.cn/api/paas/v4/
/// dotnet run --project Demos/Mud.Feishu.Agent.Demo
/// </code>
/// </remarks>
public static class Program
{
    public static async Task Main()
    {
        // Phase 1 工具模式：FEISHU_DEMO_TOOLS=1 时启用 10 个只读工具 + 飞书客户端。
        if (string.Equals(Environment.GetEnvironmentVariable("FEISHU_DEMO_TOOLS"), "1", StringComparison.Ordinal))
        {
            await ToolsDemo.RunAsync();
            return;
        }

        // AI-FD-D12 P2D-5a 零自定义接入演示：FEISHU_DEMO_IM_HANDLER=1。
        if (string.Equals(Environment.GetEnvironmentVariable("FEISHU_DEMO_IM_HANDLER"), "1", StringComparison.Ordinal))
        {
            await ImConversationDemo.RunAsync();
            return;
        }

        // Phase 3 文档业务智能体（写闭环 + 三道安全闸 + 剧本驱动）：FEISHU_DEMO_DOC_AGENT=1 时启用。
        // 放置位置刻意在「事件模式之后、裸模型之前」，保持「能力由弱到强」的阅读顺序。
        if (string.Equals(Environment.GetEnvironmentVariable("FEISHU_DEMO_DOC_AGENT"), "1", StringComparison.Ordinal))
        {
            await DocAgentDemo.RunAsync();
            return;
        }

        var modelId = Environment.GetEnvironmentVariable("FEISHU_AI_MODEL_KEY")
            ?? throw new InvalidOperationException("请先设置 FEISHU_AI_MODEL_KEY");
        var apiKey = Environment.GetEnvironmentVariable("FEISHU_AI_API_KEY")
            ?? throw new InvalidOperationException("请先设置 FEISHU_AI_API_KEY");
        var endpoint = Environment.GetEnvironmentVariable("FEISHU_AI_ENDPOINT");

        var services = new ServiceCollection()
            .AddFeishuOpenAIChatClient("demo-model", modelId, apiKey, endpoint)
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
