// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.AI.Agent.Agents;

/// <summary>
/// 内层 <see cref="ChatClientAgent"/> 的装配工厂：把「Agent 名 / 指令 / 工具 / 历史裁剪窗」四项
/// 宿主事实组装成 <see cref="ChatClientAgentOptions"/> 并构造内层 Agent。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么这一层必须中立</b>：<c>Name</c> / <c>ChatOptions{Instructions, Tools}</c> /
/// <c>ChatHistoryProvider</c> 三件套的构造与产品无关；产品侧只负责喂产品事实
/// （名字、指令、历史状态键、裁剪窗上限）与配置 fail-fast。
/// </para>
/// <para>
/// <b>为什么事实经参数传入</b>：历史状态键与裁剪窗上限都是产品事实，中立层不得硬编码任何产品命名面
/// （中立侧零状态键前缀字面量，由产品侧守卫机械锁定）。装配器只做"事实 → MAF 类型"的翻译。
/// </para>
/// <para>
/// <b>调用方保证</b>：<c>chatClient</c> 非空、<c>instructions</c> 非空——
/// 空值守卫与配置校验留在产品侧（fail-fast 的归属是产品配置面），本层不重复校验。
/// </para>
/// </remarks>
public static class AgentAssemblyFactory
{
    /// <summary>
    /// 构造内层 Agent：指令装入 <see cref="ChatOptions.Instructions"/>，工具装入
    /// <see cref="ChatOptions.Tools"/>（空集与 <see langword="null"/> 同义，保持裸模型行为），
    /// 历史由 <see cref="InMemoryChatHistoryProvider"/> 按状态键读写、按
    /// <paramref name="maxHistoryMessages"/> 施加裁剪窗。
    /// </summary>
    /// <param name="chatClient">模型客户端。</param>
    /// <param name="name">Agent 展示名（可空：MAF 允许无名字的 Agent）。</param>
    /// <param name="instructions">已组装完成的系统指令（含宿主指令与域 guidance）。</param>
    /// <param name="tools">暴露给模型的工具（可空/空集 → 不设置 <see cref="ChatOptions.Tools"/>）。</param>
    /// <param name="historyStateKey">会话历史在 MAF 状态袋内的键（产品事实，由调用方注入）。</param>
    /// <param name="maxHistoryMessages">历史裁剪窗上限（源产品配置）。</param>
    /// <param name="loggerFactory">日志工厂（可空，MAF 内部日志兜底关闭）。</param>
    /// <param name="services">服务提供器（可空；调用方须保证不解析 Scoped 服务）。</param>
    public static ChatClientAgent Create(
        IChatClient chatClient,
        string? name,
        string instructions,
        IReadOnlyList<AIFunction>? tools,
        string historyStateKey,
        int maxHistoryMessages,
        ILoggerFactory? loggerFactory = null,
        IServiceProvider? services = null)
    {
        var options = new ChatClientAgentOptions
        {
            Name = name,
            ChatOptions = new ChatOptions
            {
                Instructions = instructions,
                Tools = tools is { Count: > 0 } ? [.. tools] : null,
            },
            ChatHistoryProvider = new InMemoryChatHistoryProvider(new InMemoryChatHistoryProviderOptions
            {
                StateKey = historyStateKey,
                ChatReducer = new MessageCountingChatReducer(maxHistoryMessages),
            }),
        };

        return new ChatClientAgent(chatClient, options, loggerFactory, services);
    }
}
