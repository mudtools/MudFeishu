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

namespace Mud.Feishu.Agent.Demo;

/// <summary>
/// Phase 0 裸模型一问一答（默认模式，无开关）：消息文本 → 模型 → 回答，无工具、无飞书客户端。
/// </summary>
/// <remarks>
/// 模型三参数统一读统一配置节 <c>FeishuDemo</c>（<see cref="FeishuDemoSettings"/>）；
/// 会话记忆保存在单聊维度的会话键下（<see cref="ConversationKeyBuilder"/>）。
/// </remarks>
public static class BareModelDemo
{
    public static async Task RunAsync(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var shared = FeishuDemoSettings.FromConfiguration(configuration);
        shared.Validate();

        var services = new ServiceCollection()
            .AddDemoLogging(configuration)
            .AddDemoAgent(
                shared.Model,
                "FeishuAgentDemo",
                "你是嵌入在 .NET 服务里的飞书助手，用简洁中文回答。")
            .BuildServiceProvider();

        var agent = services.GetRequiredService<FeishuAgent>();

        // 单聊维度会话键：多轮对话共享同一会话（记忆）。
        var conversationKey = ConversationKeyBuilder.Build(
            DemoAgentDefaults.DefaultAppKey, ConversationScope.P2P(), DemoAgentDefaults.DemoUserId);
        var session = await agent.GetOrCreateSessionAsync(conversationKey);

        await DemoChatLoop.RunAsync("裸模型一问一答", async userText =>
        {
            var response = await agent.RunAsync(userText, session);
            Console.WriteLine($"Agent: {response.Text}");
            await agent.SaveSessionAsync(conversationKey, session);
        });
    }
}
