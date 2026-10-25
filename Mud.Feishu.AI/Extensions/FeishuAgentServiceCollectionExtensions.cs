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
using Mud.AI.Abstractions.Observability;
using Mud.Feishu.AI.Agents;
using Mud.Feishu.AI.Channels;
using Mud.Feishu.AI.Conversations;
using Mud.Feishu.AI.Events;
using Mud.Feishu.AI.Knowledge;
using Mud.Feishu.AI.AgentTools;
using Mud.Feishu.Abstractions.Configuration;
using Mud.Feishu.Abstractions.Conversations;
using OpenAI;
using System.ClientModel;

namespace Mud.Feishu.AI.Extensions;

/// <summary>
/// <see cref="FeishuAgent"/> 服务注册扩展（一行启动：<c>AddFeishuOpenAIChatClient</c> + <c>AddFeishuAgent</c>），
/// 以及**飞书集成面**装配入口（流式通道 / IM 会话处理器 / Aily 知识问答）。
/// </summary>
public static class FeishuAgentServiceCollectionExtensions
{
    /// <summary>
    /// 注册键控 <see cref="IChatClient"/>（OpenAI-compatible 端点）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 首个落地模型走 OpenAI-compatible 协议；私有化/第三方端点经
    /// <paramref name="endpoint"/> 指定。API Key 绝不落日志。
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
    /// 模型客户端，解析即校验，不透传 null）。
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
            // 源生成绑定：Bind 调用点可被配置绑定源生成器拦截（net8+ 由根 props 启用）。
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

        // 默认进程内会话闸门（同键串行，防并发 Run 丢历史）；
        // 多实例部署经 Mud.Feishu.Redis 的 AddFeishuRedisConversationGate 替换本注册。
        // 注册顺序 = KeyedConversationGate（内层）→ GlobalConcurrencyGate（外层装饰器，
        // 先取全局信号量再委托内层），事件入口代码零改动（仍注入 IConversationGate）。
        services.TryAddSingleton<KeyedConversationGate>();
        services.TryAddSingleton<IConversationGate>(sp =>
        {
            var inner = sp.GetRequiredService<KeyedConversationGate>();
            var options = sp.GetRequiredService<IOptions<FeishuAgentOptions>>().Value;
            return new GlobalConcurrencyGate(
                inner,
                options.MaxConcurrentTurns,
                options.TurnQueueTimeoutSeconds);
        });

        // 待确认快照的进程内实现（软缺席契约的默认实现）——单进程部署下宿主重进进程仍能列出待办；
        // 多实例部署由宿主替换为分布式实现（键必须含 AppKey 以隔离租户）。
        services.TryAddSingleton<IPendingApprovalStore, InMemoryPendingApprovalStore>();

        // 遥测端口默认接产品实现——宿主可替换；替换后回合管线 / 摘要器 /
        // 通道（含降级链）全部走宿主实现。端口未注册时中立侧落 Noop（零开销）。
        // 工具面端口（IAgentToolTelemetry）与主端口**同一实例**：接口拆分是能力面，不是第二个实现。
        services.TryAddSingleton<Mud.Feishu.AI.Diagnostics.FeishuAgentTelemetry>();
        services.TryAddSingleton<IAgentTelemetry>(static sp =>
            sp.GetRequiredService<Mud.Feishu.AI.Diagnostics.FeishuAgentTelemetry>());
        services.TryAddSingleton<IAgentToolTelemetry>(static sp =>
            sp.GetRequiredService<Mud.Feishu.AI.Diagnostics.FeishuAgentTelemetry>());

        services.TryAddSingleton(static sp =>
        {
            var options = sp.GetRequiredService<IOptions<FeishuAgentOptions>>().Value;
            options.Validate();

            IChatClient chatClient = string.IsNullOrWhiteSpace(options.ModelServiceKey)
                ? sp.GetRequiredService<IChatClient>()
                : sp.GetRequiredKeyedService<IChatClient>(options.ModelServiceKey);

            // 工具来源：容器内全部 FeishuAgentToolSource（如 Mud.Feishu.AI.Tools 的白名单桥）。
            // 未注册工具包时为空集——保持裸模型行为。
            var sources = sp.GetServices<FeishuAgentToolSource>().ToArray();
            var tools = sources
                .SelectMany(source => source.GetTools(sp))
                .ToArray();

            // 域 guidance：与工具同一来源、同一"已启用"口径——只注入已启用工具所属域的资产。
            var guidance = sources
                .SelectMany(source => source.GetGuidance(sp))
                .ToArray();

            // 不把根 IServiceProvider 钉进单例（Captive Dependency 面）。
            // 工具与 guidance 已在上面**立即**解析为实例，会话历史由构造期显式装配的
            // InMemoryChatHistoryProvider 提供——MAF 侧无需再经容器解析任何依赖，故 services 传 null。
            // 宿主确需注入容器（如注册 ChatHistoryProviderFactory）时，可自行
            // new FeishuAgent(..., services: <scope factory 派生>) 装配，并遵守
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

        // IPv6 字面量经 System.Uri 规范化后 Host **带方括号且为零压缩的完整形态**
        // （http://[::1]:8000/v1 ⇒ "[0000:0000:0000:0000:0000:0000:0000:0001]"），
        // 故不得用 "::1" 字面量比较、也不得直接 IPAddress.TryParse("[::1]")——
        // 那会让「环回地址例外」在 IPv6 写法下被静默破坏（fail-closed 方向，纯可用性缺陷）。
        // 先剥方括号再 TryParse，同时覆盖 "[::1]" 与 "[0:0:0:0:0:0:0:1]" 两种书写。
        var host = endpoint.Host.Trim('[', ']');
        var isLoopback = string.Equals(endpoint.Host, "localhost", StringComparison.OrdinalIgnoreCase)
            || (System.Net.IPAddress.TryParse(host, out var address)
                && System.Net.IPAddress.IsLoopback(address));

        if (!isLoopback)
            throw new InvalidOperationException(
                "模型端点必须为 HTTPS（环回地址例外）——对齐 BaseUrl 白名单安全默认，实际: " + endpoint.Host);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // 以下 5 个入口原在 Mud.Feishu.AI.Tools 的 FeishuToolsServiceCollectionExtensions，
    // 随集成面（Channels / Events / Knowledge）一并并入本工程：它们注册的是集成面实现
    // （流式通道、会话事件处理器、知识 Provider），留在工具包会迫使只想接集成面的宿主
    // 引用工具包（连带事件 DTO + 工具面依赖）。
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 注册分片编辑流式通道（兼容入口）：仅注册 <see cref="EditMessageChannel"/> 单通道
    /// （不走卡片流降级链）。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <returns>服务集合。</returns>
    public static IServiceCollection AddFeishuEditMessageChannel(this IServiceCollection services)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        services.TryAddSingleton<EditMessageChannel>();
        services.TryAddSingleton<IMessageChannel>(static sp => sp.GetRequiredService<EditMessageChannel>());
        return services;
    }

    /// <summary>
    /// 注册流式通道降级链：首选 CardKit 会话内流式通道
    /// （<see cref="CardKitStreamMessageChannel"/>，群聊/单聊通用），Begin 失败自动降级
    /// <see cref="EditMessageChannel"/>（分片编辑）——事件处理器零感知。
    /// </summary>
    /// <remarks>
    /// 要求宿主已注册 <c>IFeishuTenantV1Message</c>（编辑通道与气泡投放共用）；CardKit 客户端
    /// （<c>IFeishuTenantV1Card</c> / <c>IFeishuTenantV1CardElements</c>）任一缺席时链内仅剩编辑通道（注册期软缺席）。
    /// </remarks>
    /// <param name="services">服务集合。</param>
    /// <returns>服务集合。</returns>
    public static IServiceCollection AddFeishuStreamingChannel(this IServiceCollection services)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        // CardKit 客户端缺席 → 通道缺席（链内仅编辑通道），与写执行器同一软缺席模式。
        // 两条通道均接遥测端口（产品子类覆写 OnUpdateDegraded 不走基类默认路径，
        // 接线只为中立机制自身的观测面，行为零漂移）。
        services.TryAddSingleton(static sp => sp.GetService<Mud.Feishu.IFeishuTenantV1Card>() is { } card
            && sp.GetService<Mud.Feishu.IFeishuTenantV1CardElements>() is { } elements
            ? new CardKitStreamMessageChannel(
                card,
                elements,
                sp.GetRequiredService<Mud.Feishu.IFeishuTenantV1Message>(),
                sp.GetRequiredService<IFeishuAppContextScopeFactory>(),
                sp.GetRequiredService<IOptions<FeishuAgentOptions>>(),
                sp.GetService<ILogger<CardKitStreamMessageChannel>>(),
                telemetry: sp.GetService<IAgentTelemetry>())
            : null!);
        services.TryAddSingleton(static sp => new EditMessageChannel(
            sp.GetRequiredService<Mud.Feishu.IFeishuTenantV1Message>(),
            sp.GetRequiredService<IFeishuAppContextScopeFactory>(),
            sp.GetRequiredService<IOptions<FeishuAgentOptions>>(),
            sp.GetService<ILogger<EditMessageChannel>>(),
            telemetry: sp.GetService<IAgentTelemetry>()));

        services.TryAddSingleton<IMessageChannel>(static sp =>
        {
            var card = sp.GetService<CardKitStreamMessageChannel>();
            var edit = sp.GetRequiredService<EditMessageChannel>();
            return card is not null
                ? new StreamingChannelChain(sp.GetService<ILogger<StreamingChannelChain>>(), card, edit)
                : new StreamingChannelChain(sp.GetService<ILogger<StreamingChannelChain>>(), edit);
        });

        return services;
    }

    /// <summary>
    /// 注册知识检索上下文装配器（注入模式）：每轮把 <see cref="IRetriever"/>
    /// 召回切片注入 Prompt。宿主<b>显式装配</b>（不默认开启，省 token）——工具模式见
    /// <c>AddFeishuKnowledgeTools</c>；并用合法（注入给背景、工具给深挖）。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <param name="order">装配顺序（缺省 100：默认装配器之后，问题文本先于知识注入）。</param>
    /// <returns>服务集合。</returns>
    public static IServiceCollection AddFeishuKnowledgeContext(this IServiceCollection services, int order = KnowledgeContextAssembler.DefaultOrder)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IContextAssembler>(sp =>
            new KnowledgeContextAssembler(
                sp.GetRequiredService<IRetriever>(),
                order)));
        return services;
    }

    /// <summary>
    /// 一行接入内置 IM 会话事件处理器：消息 → 会话 → 模型 → 回复/流式。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 注册 <see cref="ImConversationOptions"/>（配置节 <c>FeishuAgent:ImConversation</c>）与
    /// <see cref="ImMessageConversationalEventHandler"/>（含 Bot 自激过滤、群聊 @ 过滤安全内建）。
    /// 内置上下文装配器集默认只启用 <see cref="ImConversationAssemblers.SenderInfo"/>（零外部调用）；
    /// 其余经 <paramref name="assemblers"/> 显式装配。事件订阅侧由宿主在
    /// WebSocket/Webhook 通道上 <c>AddHandler&lt;ImMessageConversationalEventHandler&gt;()</c>
    /// （通道工厂从自身注册表解析，不读全局 <c>IFeishuEventHandler</c> 注册）。
    /// </para>
    /// </remarks>
    /// <param name="services">服务集合。</param>
    /// <param name="configuration">应用配置（可空；提供时绑定 <c>FeishuAgent:ImConversation</c> 节）。</param>
    /// <param name="configure">编程式配置覆盖（可空；在配置绑定之后执行）。</param>
    /// <param name="assemblers">内置装配器集（默认仅 SenderInfo；QuoteMessage 需 Message 客户端、Knowledge 需 IRetriever）。</param>
    /// <returns>服务集合。</returns>
    public static IServiceCollection AddFeishuImConversationHandler(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        Action<ImConversationOptions>? configure = null,
        ImConversationAssemblers assemblers = ImConversationAssemblers.SenderInfo)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        var builder = services.AddOptions<ImConversationOptions>();
        if (configuration is not null)
        {
            // 源生成绑定：Bind 调用点可被配置绑定源生成器拦截（net8+ 由根 props 启用）。
            var section = configuration.GetSection(ImConversationOptions.SectionName);
            builder.Configure(options => section.Bind(options));
        }

        if (configure is not null)
        {
            builder.Configure(configure);
        }

        services.TryAddSingleton(static sp =>
        {
            var options = sp.GetRequiredService<IOptions<ImConversationOptions>>().Value;
            options.Validate();
            return Options.Create(options);
        });

        // 内置装配器集：默认只启用 SenderInfo（零外部调用）；其余宿主显式装配。
        if (assemblers.HasFlag(ImConversationAssemblers.SenderInfo))
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IContextAssembler, SenderInfoContextAssembler>());
        }

        if (assemblers.HasFlag(ImConversationAssemblers.QuoteMessage))
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IContextAssembler>(static sp =>
                new QuoteMessageContextAssembler(
                    sp.GetRequiredService<Mud.Feishu.IFeishuTenantV1Message>(),
                    sp.GetRequiredService<IFeishuAppContextScopeFactory>(),
                    sp.GetService<ILogger<QuoteMessageContextAssembler>>())));
        }

        if (assemblers.HasFlag(ImConversationAssemblers.Knowledge))
        {
            // 注入模式：要求宿主已注册 IRetriever（如 AddFeishuAilyKnowledge）。
            services.AddFeishuKnowledgeContext();
        }

        if (assemblers.HasFlag(ImConversationAssemblers.GroupHistory))
        {
            // 群聊历史装配器——直调 SDK GetHistoryMessageAsync，不经工具面。
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IContextAssembler>(static sp =>
                new GroupHistoryContextAssembler(
                    sp.GetRequiredService<IFeishuTenantV1Message>(),
                    sp.GetRequiredService<IFeishuAppContextScopeFactory>(),
                    sp.GetService<ILogger<GroupHistoryContextAssembler>>())));
        }

        if (assemblers.HasFlag(ImConversationAssemblers.MediaMessage))
        {
            // 媒体消息装配器——图片/音频/文件转写为文本。
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IContextAssembler>(static sp =>
                new MediaMessageContextAssembler(
                    sp.GetRequiredService<IFeishuTenantV1Message>(),
                    sp.GetRequiredService<IFeishuAppContextScopeFactory>(),
                    sp.GetService<IFeishuMediaTranscriber>(),
                    sp.GetRequiredService<IOptions<MediaInputOptions>>().Value,
                    sp.GetService<ILogger<MediaMessageContextAssembler>>())));
        }

        // 回复前切租户依赖作用域工厂——本入口是 IM 会话处理器的「一行接入」装配点，
        // 若此处不注册，多应用宿主会在回复时 fail-closed 抛错（有 appKey 无工厂）。
        services.TryAddSingleton<IFeishuAppContextScopeFactory, FeishuAppContextScopeFactory>();

        // 处理器本体：宿主经通道 AddHandler<ImMessageConversationalEventHandler>() 挂载（AddScoped 语义由通道补齐）。
        services.TryAddScoped<ImMessageConversationalEventHandler>();

        return services;
    }

    /// <summary>
    /// 注册 Aily 托管知识问答（RAG 默认路径）：
    /// <see cref="IFeishuKnowledgeBase"/> + <see cref="IRetriever"/> 单实现双接口。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <param name="configuration">应用配置（可空；提供时绑定 <c>FeishuAilyKnowledge</c> 节）。</param>
    /// <param name="configure">编程式配置覆盖（可空；在配置绑定之后执行）。</param>
    /// <returns>服务集合。</returns>
    public static IServiceCollection AddFeishuAilyKnowledge(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        Action<AilyKnowledgeOptions>? configure = null)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        var builder = services.AddOptions<AilyKnowledgeOptions>();
        if (configuration is not null)
        {
            // 源生成绑定：Bind 调用点可被配置绑定源生成器拦截（net8+ 由根 props 启用）。
            var section = configuration.GetSection(AilyKnowledgeOptions.SectionName);
            builder.Configure(options => section.Bind(options));
        }

        if (configure is not null)
        {
            builder.Configure(configure);
        }

        // 与 FeishuAgentOptions 同等对待——Options 管线接入 Validate()，
        // 使"AppId 未配置"在**启动期**响亮失败，而不是等第一次知识提问时在 Provider 工厂里炸。
        services.TryAddSingleton<IValidateOptions<AilyKnowledgeOptions>, AilyKnowledgeOptionsValidator>();
#if NET6_0_OR_GREATER
        services.AddOptions<AilyKnowledgeOptions>().ValidateOnStart();
#endif

        // 注册幂等：避免重复调用产生多条描述符。
        services.TryAddSingleton<AilyKnowledgeProvider>(static sp =>
        {
            var options = sp.GetRequiredService<IOptions<AilyKnowledgeOptions>>().Value;
            options.Validate();

            return new AilyKnowledgeProvider(
                sp.GetRequiredService<Mud.Feishu.IFeishuTenantV1AilyDataKnowledge>(),
                sp.GetRequiredService<IFeishuAppContextScopeFactory>(),
                Options.Create(options),
                sp.GetService<IFeishuToolContextAccessor>(),
                sp.GetService<ILogger<AilyKnowledgeProvider>>());
        });
        services.TryAddSingleton<IFeishuKnowledgeBase>(static sp => sp.GetRequiredService<AilyKnowledgeProvider>());
        services.TryAddSingleton<IRetriever>(static sp => sp.GetRequiredService<AilyKnowledgeProvider>());

        return services;
    }
}
