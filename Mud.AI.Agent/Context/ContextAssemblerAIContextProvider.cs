// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.AI.Abstractions.Context;
using Mud.AI.Abstractions.Conversations;

namespace Mud.AI.Agent.Context;

/// <summary>
/// <see cref="IContextAssembler"/> → MAF <c>AIContextProvider</c> 的<b>桥接适配器</b>（N4 产出物，方案 §1）：
/// 让已写好的装配器族可被 MAF 原生 <c>AIContextProvider</c> 组合消费——宿主可二选一：
/// 自有管线（<see cref="ConversationalTurnPipeline"/>，装配器在用户消息内联拼装），
/// 或 MAF 原生组合（本桥接器 + <c>ChatClientAgentOptions.AIContextProvider</c>）。
/// </summary>
/// <remarks>
/// <para>
/// <b>桥接语义</b>：每次模型调用前（<c>ProvideAIContextAsync</c>），取本轮最后一条 user 消息文本，
/// 经<b>请求工厂</b>投影为 <see cref="ConversationRequest"/>（缺省工厂 = 仅 <c>MentionedText</c>，
/// 其余字段为空），交给装配器族装配；产出文本作为一条附加 user 消息并入本轮上下文。
/// 两种姿态的注入位置不同（自有管线 = 问题文本同段；MAF 组合 = 独立上下文消息），宿主按需选择。
/// </para>
/// <para>
/// <b>失败隔离</b>：单个装配器失败记 Warning 后跳过（对齐回合管线内建装配的隔离纪律），
/// 绝不让装配故障中断模型调用。
/// </para>
/// <para>
/// <b>为什么是「每调用投影」而不是缓存请求</b>：<c>AIContextProvider</c> 是跨会话复用的单例；
/// 请求是<b>每轮</b>事实，由工厂从本轮输入即时投影（宿主也可注入自定义工厂携带租户/会话维度）。
/// </para>
/// </remarks>
public sealed class ContextAssemblerAIContextProvider : AIContextProvider
{
    private readonly IReadOnlyList<IContextAssembler> _assemblers;
    private readonly Func<ChatMessage?, ConversationRequest> _requestFactory;
    private readonly ILogger? _logger;

    /// <summary>
    /// 初始化桥接适配器。
    /// </summary>
    /// <param name="assemblers">装配器族（可空 = 空集合）。</param>
    /// <param name="requestFactory">
    /// 输入消息 → 会话请求的投影工厂（可空 = 缺省投影：最后一条 user 消息文本 → <c>MentionedText</c>，
    /// 其余字段为空）。宿主需要租户/会话维度时注入自有工厂。
    /// </param>
    /// <param name="logger">日志（可空）。</param>
    public ContextAssemblerAIContextProvider(
        IReadOnlyList<IContextAssembler>? assemblers = null,
        Func<ChatMessage?, ConversationRequest>? requestFactory = null,
        ILogger? logger = null)
    {
        _assemblers = assemblers ?? [];
        _requestFactory = requestFactory ?? DefaultRequestFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async ValueTask<AIContext?> ProvideAIContextAsync(
        InvokingContext context, CancellationToken cancellationToken = default)
    {
        var request = _requestFactory(LastUserMessage(context));
        var fragments = new List<string>();
        foreach (var assembler in _assemblers.OrderBy(a => a.Order))
        {
            try
            {
                var fragment = await assembler.AssembleAsync(request, cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(fragment))
                {
                    // netstandard2.0 的 BCL 不带 IsNullOrWhiteSpace 的 NotNullWhen 注解，需显式断言。
                    fragments.Add(fragment!);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // 单个装配器失败不中断模型调用（异常隔离，对齐回合管线内建装配纪律）。
                _logger?.LogWarning(ex, "上下文装配器 {Assembler} 失败，已跳过", assembler.GetType().Name);
            }
        }

        if (fragments.Count == 0)
        {
            // 无附加上下文：返回空 AIContext（零消息 = 无并入）。基类 InvokingCoreAsync 对
            // null 返回值不稳妥（1.20.0 实测 NRE），空上下文语义等价且对基类安全。
            return new AIContext { Messages = [] };
        }

        return new AIContext
        {
            Messages = [new ChatMessage(ChatRole.User, string.Join("\n", fragments))],
        };
    }

    private static ChatMessage? LastUserMessage(InvokingContext context)
    {
        ChatMessage? last = null;
        var messages = context.AIContext?.Messages;
        if (messages is not null)
        {
            foreach (var message in messages)
            {
                if (message.Role == ChatRole.User)
                {
                    last = message;
                }
            }
        }

        return last;
    }

    private static ConversationRequest DefaultRequestFactory(ChatMessage? message)
        => new(
            AppKey: string.Empty,
            Scope: ConversationScope.P2P(),
            SubjectId: string.Empty,
            SenderId: string.Empty,
            MessageId: string.Empty,
            MentionedText: string.IsNullOrWhiteSpace(message?.Text) ? null : message!.Text);
}
