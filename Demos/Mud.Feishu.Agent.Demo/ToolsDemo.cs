// -----------------------------------------------------------------------
//  作者:Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mud.Feishu.Abstractions.Conversations;
using Mud.Feishu.AI.Agents;
using Mud.Feishu.AI.Channels;
using Mud.Feishu.AI.Extensions;
using Mud.Feishu.AI.FeishuTools;
using Mud.Feishu.AI.Tools;

namespace Mud.Feishu.Agent.Demo;

/// <summary>
/// Phase 1/2 工具模式冒烟（端到端工具链——消息文本 → 模型 → 工具 tool_call → 执行链 → 回答；
/// Phase 2：可选流式回复 + 写工具白名单示例）。
/// </summary>
/// <remarks>
/// <para>
/// 运行前设置环境变量（Key 绝不写进代码/配置文件提交）：
/// <code>
/// set FEISHU_DEMO_TOOLS=1
/// set FEISHU_AI_MODEL_KEY=glm-4-flash
/// set FEISHU_AI_API_KEY=sk-xxxx
/// set FEISHU_AI_ENDPOINT=https://open.bigmodel.cn/api/paas/v4/
/// set FEISHU_APP_ID=cli_xxx
/// set FEISHU_APP_SECRET=dsk_xxx
/// rem Phase 2 流式演示（可选）：提供目标群 chat_id 时经分片编辑通道流式回复
/// set FEISHU_DEMO_CHAT_ID=oc_xxx
/// dotnet run --project Demos/Mud.Feishu.Agent.Demo
/// </code>
/// </para>
/// <para>
/// 工具执行链租户上下文固定为 <c>demo-app</c>（即下方注册的飞书应用 AppKey）；
/// 10 个只读工具经只读白名单启用（生产由宿主按需裁剪，Phase 1 §3.3.4）。
/// 写工具默认不启用——示例展示 <c>WriteAllowList</c> 键控位（注释态），启用须同时注册
/// <c>IToolExecutionAuthorizer</c>（Phase 2 §3.3 安全默认）。
/// </para>
/// </remarks>
public static class ToolsDemo
{
    public static async Task RunAsync()
    {
        var modelId = Environment.GetEnvironmentVariable("FEISHU_AI_MODEL_KEY")
            ?? throw new InvalidOperationException("请先设置 FEISHU_AI_MODEL_KEY");
        var apiKey = Environment.GetEnvironmentVariable("FEISHU_AI_API_KEY")
            ?? throw new InvalidOperationException("请先设置 FEISHU_AI_API_KEY");
        var endpoint = Environment.GetEnvironmentVariable("FEISHU_AI_ENDPOINT");
        var appId = Environment.GetEnvironmentVariable("FEISHU_APP_ID")
            ?? throw new InvalidOperationException("请先设置 FEISHU_APP_ID");
        var appSecret = Environment.GetEnvironmentVariable("FEISHU_APP_SECRET")
            ?? throw new InvalidOperationException("请先设置 FEISHU_APP_SECRET");
        var streamChatId = Environment.GetEnvironmentVariable("FEISHU_DEMO_CHAT_ID");

        const string appKey = "demo-app";

        // 飞书多应用配置（进程内构造，不经文件；AppSecret 走环境变量，不落盘）。
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeishuApps:0:AppKey"] = appKey,
                ["FeishuApps:0:AppId"] = appId,
                ["FeishuApps:0:AppSecret"] = appSecret,
                ["FeishuApps:0:IsDefault"] = "true",
            })
            .Build();

        var services = new ServiceCollection()
            .AddFeishuApp(configuration, "FeishuApps")
            .AddFeishuServices(builder => builder
                .AddMessageApi()
                .AddBiTableApi()
                .AddDocxApi()
                .AddWikiApi()
                .AddSearchApi()
                .AddSpreadsheetsApi())
            .AddFeishuOpenAIChatClient("demo-model", modelId, apiKey, endpoint)
            .AddFeishuAgent(configure: options =>
            {
                options.ModelServiceKey = "demo-model";
                options.Name = "FeishuAgentToolsDemo";
                options.Instructions = "你是嵌入在 .NET 服务里的飞书助手。可使用只读工具查询多维表格、读取文档正文、" +
                    "遍历知识库、搜索云文档、读取群历史消息与电子表格区域数据；两步链示例：wiki.get_node 解析链接 → " +
                    "docx.get_raw_content 读正文。用简洁中文回答，引用数据时说明来源工具。";
                // Demo 启用全部 10 个只读工具；生产由宿主按需裁剪白名单。
                options.Tools = [.. FeishuToolNames.ReadonlyAll];
                // Phase 2 写工具示例（默认空=不启用）：显式键控 + 授权器注册后才真正放行。
                // options.WriteAllowList = [FeishuToolNames.ImSendMessage];
            })
            .AddFeishuTools();

        // Phase 2 流式演示：提供 FEISHU_DEMO_CHAT_ID 时注册分片编辑通道。
        if (!string.IsNullOrEmpty(streamChatId))
        {
            services.AddFeishuEditMessageChannel();
        }

        var provider = services.BuildServiceProvider();

        var agent = provider.GetRequiredService<FeishuAgent>();
        var toolContextAccessor = provider.GetRequiredService<IFeishuToolContextAccessor>();
        var registry = provider.GetRequiredService<FeishuToolRegistry>();
        var messageChannel = provider.GetService<IMessageChannel>();

        Console.WriteLine($"已启用工具 {registry.EnabledTools.Count} 个：{string.Join("、", registry.EnabledTools.Select(t => t.Name))}");
        Console.WriteLine(messageChannel is not null
            ? $"流式回复：开（分片编辑 → chat {streamChatId}）"
            : "流式回复：关（设置 FEISHU_DEMO_CHAT_ID 开启 Phase 2 流式演示）");

        var conversationKey = ConversationKeyBuilder.Build(appKey, ConversationScope.P2P(), "ou_demo_user");
        var session = await agent.GetOrCreateSessionAsync(conversationKey);

        Console.WriteLine("工具模式问答（exit 退出）：");
        while (Console.ReadLine() is { } userText
               && userText.Length > 0
               && !string.Equals(userText, "exit", StringComparison.OrdinalIgnoreCase))
        {
            // 工具执行上下文沿异步流注入（多租户隔离事实来源；生产由 ConversationalFeishuEventHandler 注入）。
            using var toolScope = toolContextAccessor.Begin(new FeishuToolContext(appKey, conversationKey));

            if (messageChannel is not null)
            {
                // Phase 2 流式路径：Begin 占位 → 增量写入（通道内分片编辑）→ Flush 收尾。
                var messageId = await messageChannel.BeginAsync(appKey, streamChatId!);
                Console.Write("Agent: ");
                await foreach (var update in agent.RunStreamingAsync(userText, session))
                {
                    if (!string.IsNullOrEmpty(update.Text))
                    {
                        await messageChannel.WriteStreamAsync(appKey, streamChatId!, messageId, update.Text);
                        Console.Write(update.Text);
                    }
                }

                await messageChannel.FlushAsync(appKey, streamChatId!, messageId);
                Console.WriteLine("（已流式送达飞书）");
            }
            else
            {
                var response = await agent.RunAsync(userText, session);
                Console.WriteLine($"Agent: {response.Text}");
            }

            await agent.SaveSessionAsync(conversationKey, session);
        }
    }
}
