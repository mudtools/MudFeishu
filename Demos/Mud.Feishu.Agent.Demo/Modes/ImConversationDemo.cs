// -----------------------------------------------------------------------
//  作者:Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Mud.Feishu.Abstractions.Conversations;
using Mud.Feishu.AI.Agents;
using Mud.Feishu.AI.Extensions;
using Mud.Feishu.AI.Tools;
using Mud.Feishu.AI.Tools.Events;

namespace Mud.Feishu.Agent.Demo;

/// <summary>
/// AI-FD-D12 P2D-5a「零自定义接入」演示：内置 IM 会话事件处理器一行接入——
/// 消息 → 会话 → 模型 → 回复/流式（Bot 自激过滤、群聊 @ 过滤安全内建）。
/// </summary>
/// <remarks>
/// 运行前在统一配置节 <c>FeishuDemo</c> 填写模型三项，并把
/// <c>FeishuImHandlerDemo:Enabled</c> 置为 <c>true</c>（飞书多应用由宿主在 WebSocket/Webhook 通道另行配置）：
/// <code>
/// "FeishuDemo": {
///   "ModelId": "glm-4-flash",
///   "ApiKey": "sk-xxxx",
///   "Endpoint": ""
/// },
/// "FeishuImHandlerDemo": {
///   "Enabled": true
/// }
/// </code>
/// 事件订阅侧由 WebSocket 通道挂载内置处理器（<c>AddHandler&lt;ImMessageConversationalEventHandler&gt;()</c>，
/// 已决策④：不改变事件接入方式）。多实例部署时再追加
/// Mud.Feishu.Redis 的 <c>AddFeishuRedisConversationGate()</c>（会话串行化跨实例生效）。
/// </remarks>
public static class ImConversationDemo
{
    public static async Task RunAsync(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var settings = ImHandlerDemoSettings.FromConfiguration(configuration);
        settings.Validate();

        var services = new ServiceCollection()
            .AddDemoLogging(configuration)
            .AddDemoAgent(
                settings.Model,
                "FeishuImConversationDemo",
                "你是嵌入在 .NET 服务里的飞书助手，用简洁中文回答。")
            // ── 一行接入（P2D-5a）：事件规范化 → 会话 → 模型 → 回复/流式 ──
            // 默认启用 SenderInfo 装配器；可选 QuoteMessage（引用消息）/ Knowledge（知识注入）。
            .AddFeishuImConversationHandler(configure: options =>
            {
                options.RequireMentionInGroup = true; // 群聊仅响应 @Bot（默认 true）。
                options.AllowP2pConversation = true;  // 单聊会话开关（默认 true）。
            })
            .BuildServiceProvider();

        var agent = services.GetRequiredService<FeishuAgent>();
        var conversationKey = ConversationKeyBuilder.Build(
            DemoAgentDefaults.DefaultAppKey, ConversationScope.P2P(), DemoAgentDefaults.DemoUserId);
        var session = await agent.GetOrCreateSessionAsync(conversationKey);
        _ = session; // 会话管线由 ImMessageConversationalEventHandler 驱动，此处仅演示 Agent 可解析。

        Console.WriteLine("AddFeishuImConversationHandler 注册完成——宿主侧在 WebSocket/Webhook 通道上执行：");
        Console.WriteLine("    CreateFeishuWebSocketServiceBuilder(configuration, \"default\")");
        Console.WriteLine("        .AddHandler<ImMessageConversationalEventHandler>()");
        Console.WriteLine("        .Build();");
        Console.WriteLine("（本演示为注册面冒烟；连通真实飞书需配置多应用 appKey/appSecret 并启动通道。）");
        await Task.CompletedTask;
    }
}
