// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json.Nodes;
using Mud.Feishu.AI.Channels;
using Mud.Feishu.AI.Conversations;
using Mud.Feishu.AI.Events;
using Mud.Feishu.AI.Knowledge;

namespace Mud.Feishu.AI.FeishuTools.Events;

/// <summary>
/// 内置 IM 会话事件处理器可选装配的上下文装配器集（AI-FD-D12 P2D-5b 位标记）。
/// </summary>
/// <remarks>
/// <c>AddFeishuImConversationHandler</c> 默认只启用 <see cref="ImConversationAssemblers.SenderInfo"/>（零外部调用）；
/// <see cref="QuoteMessage"/> 依赖 P1D-1a 路由修复后的消息内容回查（失败自动跳过）；
/// <see cref="Knowledge"/> 依赖宿主已注册 <c>IRetriever</c>（如 <c>AddFeishuAilyKnowledge</c>）。
/// </remarks>
[Flags]
public enum ImConversationAssemblers
{
    /// <summary>不装配任何内置装配器。</summary>
    None = 0,

    /// <summary>发送者信息装配器（默认；零外部调用，仅 ID 形态脱敏输出）。</summary>
    SenderInfo = 1,

    /// <summary>引用消息装配器（一次消息内容回查调用，失败跳过）。</summary>
    QuoteMessage = 2,

    /// <summary>知识检索装配器（每轮检索注入；省 token 请改用 knowledge.search 工具模式）。</summary>
    Knowledge = 4,
}

/// <summary>
/// 发送者信息装配器（P2D-5b）：发送者 ID 形态注入 Prompt。
/// </summary>
/// <remarks>
/// 脱敏边界：只输出 ID 形态（open_id/user_id），<b>不取昵称原文</b>（防 PII 入模型上下文）。
/// 零外部调用。
/// </remarks>
public sealed class SenderInfoContextAssembler : IContextAssembler
{
    /// <summary>默认装配顺序（问题文本之前的基础事实）。</summary>
    public const int DefaultOrder = 10;

    /// <inheritdoc />
    public int Order => DefaultOrder;

    /// <inheritdoc />
    public Task<string?> AssembleAsync(ConversationRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null || string.IsNullOrEmpty(request.SenderId))
        {
            return Task.FromResult<string?>(null);
        }

        var scopeText = request.Scope.IsGroup ? "群聊" : "单聊";
        return Task.FromResult<string?>($"[发送者] 用户 ID: {request.SenderId}（{scopeText}会话）");
    }
}

/// <summary>
/// 引用消息上下文装配器（P2D-5b）：请求引用的父消息 → <c>im.get_message_content</c> 取内容预览
/// （截断 300 字）。失败跳过（异常隔离由事件层承接，本装配器自身亦不抛业务异常）。
/// </summary>
/// <remarks>依赖 P1D-1a 修复后的 <c>GetContentListByMessageIdAsync</c> 路由。</remarks>
public sealed class QuoteMessageContextAssembler : IContextAssembler
{
    /// <summary>引用内容预览截断长度。</summary>
    public const int QuotePreviewLength = 300;

    /// <summary>默认装配顺序（发送者信息之后、问题文本之前）。</summary>
    public const int DefaultOrder = 20;

    private readonly Mud.Feishu.IFeishuTenantV1Message _messageClient;
    private readonly IFeishuAppContextScopeFactory _scopeFactory;
    private readonly ILogger? _logger;

    /// <summary>
    /// 初始化 <see cref="QuoteMessageContextAssembler"/>。
    /// </summary>
    /// <param name="messageClient">消息客户端（Tenant 身份）。</param>
    /// <param name="scopeFactory">租户上下文作用域工厂。</param>
    /// <param name="logger">日志（可空）。</param>
    public QuoteMessageContextAssembler(
        Mud.Feishu.IFeishuTenantV1Message messageClient,
        IFeishuAppContextScopeFactory scopeFactory,
        ILogger<QuoteMessageContextAssembler>? logger = null)
    {
        _messageClient = messageClient ?? throw new ArgumentNullException(nameof(messageClient));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger;
    }

    /// <inheritdoc />
    public int Order => DefaultOrder;

    /// <inheritdoc />
    public async Task<string?> AssembleAsync(ConversationRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null || string.IsNullOrEmpty(request.ParentId))
        {
            return null;
        }

        try
        {
            using var scope = _scopeFactory.BeginScope(request.AppKey);
            var outcome = FeishuApiResultReader.Read(await _messageClient
                .GetContentListByMessageIdAsync(request.ParentId!, cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            if (!outcome.Ok)
            {
                // 引用消息可能已删除/无权限——跳过即可，不阻断对话。
                _logger?.LogInformation("引用消息内容回查未成功（parentId: {ParentId}）: {Error}——跳过装配",
                    request.ParentId, outcome.ErrorText);
                return null;
            }

            var content = outcome.Data!.Items?.FirstOrDefault()?.Body?.Content;
            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            // content 为类型化 JSON（如 {"text":"…"}），尽量还原纯文本预览（ns2.0 流转注解缺失，显式断言）。
            var preview = ExtractPreview(content!);
            var trimmed = preview.Length <= QuotePreviewLength ? preview : preview.Substring(0, QuotePreviewLength) + "…";
            return $"[引用消息] {trimmed}";
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogInformation(ex, "引用消息装配失败（parentId: {ParentId}）——跳过", request.ParentId);
            return null;
        }
    }

    /// <summary>text 类型 content 还原纯文本；其余类型按原文截断展示。</summary>
    private static string ExtractPreview(string content)
    {
        try
        {
            if (JsonNode.Parse(content) is JsonObject obj
                && obj["text"] is System.Text.Json.Nodes.JsonValue value
                && value.TryGetValue<string>(out var text))
            {
                return text;
            }
        }
        catch (System.Text.Json.JsonException)
        {
            // 非法 JSON 按原文展示。
            // 有意静默（守卫白名单）：降级路径本身有确定输出（原文），且引用内容来自平台回调——
            // 记日志的收益（噪声）小于成本；解析失败是"消息不是 text 形态"的正常分支。
        }

        return content;
    }
}
