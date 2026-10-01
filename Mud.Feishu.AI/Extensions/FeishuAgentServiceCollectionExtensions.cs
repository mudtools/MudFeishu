// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Mud.Feishu.AI.Agents;
using Mud.Feishu.AI.Conversations;
using Mud.Feishu.AI.Tools;
using Mud.Feishu.Abstractions.Configuration;
using Mud.Feishu.Abstractions.Conversations;
using OpenAI;
using System.ClientModel;

namespace Mud.Feishu.AI.Extensions;

/// <summary>
/// <see cref="FeishuAgent"/> 服务注册扩展（一行启动：<c>AddFeishuOpenAIChatClient</c> + <c>AddFeishuAgent</c>）。
/// </summary>
public static class FeishuAgentServiceCollectionExtensions
{
    /// <summary>
    /// 注册键控 <see cref="IChatClient"/>（OpenAI-compatible 端点）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 首个落地模型走 OpenAI-compatible 协议（路线图已决策①）；私有化/第三方端点经
    /// <paramref name="endpoint"/> 指定。API Key 绝不落日志（安全红线 §7.2）。
    /// </para>
    /// <para>
    /// 端点白名单：仅接受 HTTPS（环回地址例外），对齐「BaseUrl 白名单」安全默认。
    /// </para>
    /// </remarks>
    /// <param name="services">服务集合。</param>
    /// <param name="serviceKey">键控服务键（与 <see cref="FeishuAgentOptions.ModelServiceKey"/> 对应）。</param>
    /// <param name="modelId">模型 ID（如 <c>glm-4-flash</c>）。</param>
    /// <param name="apiKey">模型 API Key（由调用方从安全配置取出，不落日志）。</param>
    /// <param name="endpoint">OpenAI-compatible 端点（可空；为空走 OpenAI 官方默认端点）。</param>
    /// <returns>服务集合。</returns>
    /// <exception cref="ArgumentException">服务键/模型 ID/API Key 为空。</exception>
    /// <exception cref="InvalidOperationException">端点非 HTTPS 且非环回地址。</exception>
    public static IServiceCollection AddFeishuOpenAIChatClient(
        this IServiceCollection services,
        string serviceKey,
        string modelId,
        string apiKey,
        string? endpoint = null)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));
        if (string.IsNullOrWhiteSpace(serviceKey))
            throw new ArgumentException("服务键不能为空", nameof(serviceKey));
        if (string.IsNullOrWhiteSpace(modelId))
            throw new ArgumentException("模型 ID 不能为空", nameof(modelId));
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("API Key 不能为空", nameof(apiKey));

        var clientOptions = new OpenAIClientOptions();
        if (!string.IsNullOrWhiteSpace(endpoint))
        {
            var uri = new Uri(endpoint);
            EnsureHttpsEndpoint(uri);
            clientOptions.Endpoint = uri;
        }

        services.TryAddKeyedSingleton<IChatClient>(serviceKey, (sp, key) =>
        {
            var openAiClient = new OpenAIClient(new ApiKeyCredential(apiKey), clientOptions);
            return openAiClient.GetChatClient(modelId).AsIChatClient();
        });

        return services;
    }

    /// <summary>
    /// 注册 <see cref="FeishuAgent"/>（默认内存会话存储）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 注册内容：<see cref="FeishuAgentOptions"/>（配置节 <see cref="FeishuAgentOptions.SectionName"/>，
    /// 经 <paramref name="configuration"/> 绑定）、<see cref="IValidateOptions{TOptions}"/> 启动期校验
    /// （net6+ ValidateOnStart）、默认 <see cref="MemoryConversationStore"/>（可被 Redis 实现替换）、
    /// <see cref="FeishuAgent"/> 工厂（按 <see cref="FeishuAgentOptions.ModelServiceKey"/> 解析键控
    /// 模型客户端，解析即校验，不透传 null——Phase 0 §8）。
    /// </para>
    /// </remarks>
    /// <param name="services">服务集合。</param>
    /// <param name="configuration">应用配置（可空；提供时绑定 <c>FeishuAgent</c> 节）。</param>
    /// <param name="configure">编程式配置覆盖（可空；在配置绑定之后执行）。</param>
    /// <returns>服务集合。</returns>
    public static IServiceCollection AddFeishuAgent(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        Action<FeishuAgentOptions>? configure = null)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        var builder = services.AddOptions<FeishuAgentOptions>();
        if (configuration is not null)
        {
            // 源生成绑定（AOT-3）：Bind 调用点可被配置绑定源生成器拦截（net8+ 由根 props 启用）。
            var section = configuration.GetSection(FeishuAgentOptions.SectionName);
            builder.Configure(options => section.Bind(options));

            // 会话存储配置（FeishuConversationOptions 在 Abstractions，Memory/Redis 双后端单一阈值源）。
            var conversationSection = configuration.GetSection(FeishuConversationOptions.SectionName);
            services.AddOptions<FeishuConversationOptions>()
                .Configure(options => conversationSection.Bind(options));
        }

        if (configure is not null)
        {
            builder.Configure(configure);
        }

        services.TryAddSingleton<IValidateOptions<FeishuAgentOptions>, FeishuAgentOptionsValidator>();
        services.TryAddSingleton<IValidateOptions<FeishuConversationOptions>, FeishuConversationOptionsValidator>();
#if NET6_0_OR_GREATER
        // netstandard2.0 无 ValidateOnStart，由 IValidateOptions 在首次解析时触发。
        services.AddOptions<FeishuAgentOptions>().ValidateOnStart();
        services.AddOptions<FeishuConversationOptions>().ValidateOnStart();
#endif

        // 默认内存会话存储：TTL 单一阈值源 = FeishuConversationOptions.SessionTtl。
        // 分布式部署经 Mud.Feishu.Redis 的 AddFeishuRedisConversationStore 替换本注册。
        services.TryAddSingleton<IConversationStore>(static sp => new MemoryConversationStore(
            sp.GetRequiredService<IOptions<FeishuConversationOptions>>().Value.SessionTtl));

        // 默认进程内会话闸门（P2D-1：同键串行，防并发 Run 丢历史）；
        // 多实例部署经 Mud.Feishu.Redis 的 AddFeishuRedisConversationGate 替换本注册。
        services.TryAddSingleton<IConversationGate, KeyedConversationGate>();

        services.TryAddSingleton(static sp =>
        {
            var options = sp.GetRequiredService<IOptions<FeishuAgentOptions>>().Value;
            options.Validate();

            IChatClient chatClient = string.IsNullOrWhiteSpace(options.ModelServiceKey)
                ? sp.GetRequiredService<IChatClient>()
                : sp.GetRequiredKeyedService<IChatClient>(options.ModelServiceKey);

            // 工具来源：容器内全部 FeishuAgentToolSource（如 Mud.Feishu.AI.FeishuTools 的白名单桥）。
            // 未注册工具包时为空集——保持 Phase 0 裸模型行为。
            var sources = sp.GetServices<FeishuAgentToolSource>().ToArray();
            var tools = sources
                .SelectMany(source => source.GetTools(sp))
                .ToArray();

            // 域 guidance（WP6）：与工具同一来源、同一"已启用"口径——只注入已启用工具所属域的资产。
            var guidance = sources
                .SelectMany(source => source.GetGuidance(sp))
                .ToArray();

            // R4-6：不把根 IServiceProvider 钉进单例（Captive Dependency 面 / TMA-13）。
            // 工具与 guidance 已在上面**立即**解析为实例（source.GetTools(sp) / GetGuidance(sp)），
            // 会话历史由构造期显式装配的 InMemoryChatHistoryProvider 提供——MAF 侧无需再经容器解析任何
            // 依赖，故 services 传 null。宿主确需注入容器（如注册 ChatHistoryProviderFactory）时，
            // 可自行 new FeishuAgent(..., services: <scope factory 派生>) 装配，并遵守
            // "依赖必须 Singleton/Transient"约束（见 Mud.Feishu.AI/Readme.md）。
            return new FeishuAgent(
                chatClient,
                options,
                sp.GetRequiredService<IConversationStore>(),
                sp.GetService<ILoggerFactory>(),
                services: null,
                tools,
                guidance);
        });
        services.TryAddSingleton<AIAgent>(static sp => sp.GetRequiredService<FeishuAgent>());

        return services;
    }

    private static void EnsureHttpsEndpoint(Uri endpoint)
    {
        if (string.Equals(endpoint.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            return;

        var isLoopback = string.Equals(endpoint.Host, "localhost", StringComparison.OrdinalIgnoreCase)
            || string.Equals(endpoint.Host, "::1", StringComparison.OrdinalIgnoreCase)
            || (System.Net.IPAddress.TryParse(endpoint.Host, out var address)
                && System.Net.IPAddress.IsLoopback(address));

        if (!isLoopback)
            throw new InvalidOperationException(
                "模型端点必须为 HTTPS（环回地址例外）——对齐 BaseUrl 白名单安全默认，实际: " + endpoint.Host);
    }
}
