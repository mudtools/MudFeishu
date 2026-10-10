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
using Mud.Feishu.AI.AgentTools;
using Mud.Feishu.AI.Tools;

namespace Mud.Feishu.Agent.Demo;

/// <summary>
/// Phase 1/2 工具模式冒烟（端到端工具链——消息文本 → 模型 → 工具 tool_call → 执行链 → 回答；
/// Phase 2：可选流式回复 + 写工具白名单示例）。
/// </summary>
/// <remarks>
/// <para>
/// 运行前在统一配置节 <c>FeishuDemo</c>（<c>appsettings.local.json</c>）填写模型与飞书凭证，
/// 并把 <c>FeishuToolsDemo:Enabled</c> 置为 <c>true</c>（真实密钥只写本地覆盖文件，不落盘提交）：
/// <code>
/// "FeishuDemo": {
///   "ModelId": "glm-4-flash",
///   "ApiKey": "sk-xxxx",
///   "Endpoint": "https://open.bigmodel.cn/api/paas/v4/",
///   "AppId": "cli_xxx",
///   "AppSecret": "dsk_xxx"
/// },
/// "FeishuToolsDemo": {
///   "Enabled": true,
///   "StreamChatId": "oc_xxx"
/// }
/// </code>
/// <c>StreamChatId</c> 可选：提供目标群 chat_id 时经分片编辑通道流式回复（Phase 2）。
/// </para>
/// <para>
/// 工具执行链租户上下文固定为 <c>demo-app</c>（即下方注册的飞书应用 AppKey）；
/// 全部只读工具经只读白名单启用（<c>FeishuToolNames.ReadonlyAll</c>，生产由宿主按需裁剪，Phase 1 §3.3.4）。
/// 写工具默认不启用——示例展示 <c>WriteAllowList</c> 键控位（注释态），启用须同时注册
/// <c>IToolExecutionAuthorizer</c>（Phase 2 §3.3 安全默认）。
/// </para>
/// <para>
/// R4 起的两个装配约束（示例已按默认值体现）：
/// ① 只读面含 <c>task.list_my_tasks</c>（identity=user），须在 <c>AllowedIdentities</c> 放行 <c>user</c>，
/// 否则 <c>AddFeishuTools</c> 装配期 fail-fast；
/// ② 上传工具 <c>im.send_image</c>/<c>im.send_file</c> 还需宿主实现 <c>IFeishuAttachmentStager</c>（落盘 + 安全域）。
/// </para>
/// </remarks>
public static class ToolsDemo
{
    public static async Task RunAsync(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var settings = ToolsDemoSettings.FromConfiguration(configuration);
        settings.Validate();

        var model = settings.Model;
        var appKey = DemoAgentDefaults.DefaultAppKey;

        // 飞书多应用配置（进程内构造，不经文件；AppSecret 只从配置文件读，不落盘）。
        var feishuConfig = DemoAppConfig.CreateSingleApp(appKey, settings.AppId, settings.AppSecret);

        var streamChatId = settings.StreamChatId;

        var services = new ServiceCollection()
            .AddDemoLogging(configuration)
            .AddFeishuApp(feishuConfig, "FeishuApps")
            .AddFeishuServices(builder => builder
                .AddMessageApi()
                .AddBiTableApi()
                .AddDocxApi()
                .AddWikiApi()
                .AddSearchApi()
                .AddSpreadsheetsApi()
                // R4/WP5 新域：日历（AT-F04）与任务（AT-F17）——域客户端缺席时对应工具不进注册表。
                .AddCalendarApi()
                .AddTaskApi())
            .AddDemoAgent(model, "FeishuAgentToolsDemo", InstructionsText, options =>
            {
                // Demo 启用全部只读工具（ReadonlyAll，由生成器从 [FeishuTool] 派生）；生产由宿主按需裁剪白名单。
                options.Tools = [.. FeishuToolNames.ReadonlyAll];
                // 身份闭集（R4 WP2 / T2-4 决策 D-1 ⓑ）：只读面含 task.list_my_tasks（identity=user），
                // 必须在 AllowedIdentities 内显式放行，否则装配期 fail-fast（而不是运行期静默 policy_denied）。
                options.AllowedIdentities = ["tenant", "user"];
                // Phase 2 写工具示例（默认空=不启用）：显式键控 + 授权器注册后才真正放行。
                // options.WriteAllowList = [FeishuToolNames.ImSendMessage];
                // R4/WP7 上传工具（im.send_image / im.send_file）：Demo 参考落盘器已注册。
                // WP1（R5）：DemoAttachmentStager 提供域名白名单（HTTPS-only）+ 大小上限（25MB）+ 扩展名校验 + finally 清理。
                // 生产宿主应替换为自身安全域实现。
            })
            .AddFeishuTools();

        // WP1（R5）：注册 Demo 级附件落盘器，使 im.send_image / im.send_file 可用。
        services.AddHttpClient<DemoAttachmentStager>();
        services.AddSingleton<IFeishuAttachmentStager>(sp => sp.GetRequiredService<DemoAttachmentStager>());

        // Phase 2 流式演示：配置 StreamChatId 时注册分片编辑通道。
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
            : "流式回复：关（配置 FeishuToolsDemo:StreamChatId 开启 Phase 2 流式演示）");

        var conversationKey = ConversationKeyBuilder.Build(appKey, ConversationScope.P2P(), DemoAgentDefaults.DemoUserId);
        var session = await agent.GetOrCreateSessionAsync(conversationKey);

        await DemoChatLoop.RunAsync("工具模式问答", async userText =>
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
        });
    }

    /// <summary>工具冒烟模式的系统提示词（提取为常量以保持 <see cref="RunAsync"/> 装配代码可读）。</summary>
    private const string InstructionsText = "你是嵌入在 .NET 服务里的飞书助手。可使用只读工具查询多维表格、读取文档正文、" +
        "遍历知识库、搜索云文档、读取群历史消息与电子表格区域数据、查询日历忙闲与日程、列出我负责的任务；" +
        "两步链示例：wiki.get_node 解析链接 → docx.get_raw_content 读正文。用简洁中文回答，引用数据时说明来源工具。";
}
