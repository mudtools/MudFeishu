// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;
using Microsoft.Extensions.Logging;
using Mud.Feishu.AI.Agents;
using Mud.Feishu.AI.Knowledge;
using Mud.Feishu.DataModels.Aily;

namespace Mud.Feishu.AI.FeishuTools.Knowledge;

/// <summary>
/// Aily 托管知识问答提供器（RAG-A 默认路径，Phase 2 §3.4）：包装
/// <c>IFeishuTenantV1AilyDataKnowledge.AskDataKnowledgeAsync</c>（SSE），产出
/// 答案 + 召回切片（<see cref="KnowledgeAnswer.Chunks"/>，供带引用的回答与 sources 回传）。
/// </summary>
/// <remarks>
/// <para>
/// <b>租户上下文</b>：与工具执行链同一事实来源——经 <see cref="IFeishuToolContextAccessor"/>
/// 异步流读取当前 <see cref="FeishuToolContext.AppKey"/>（事件入口注入），缺失即失败
/// （多租户隔离禁止默认应用兜底，TMA2-20）；每次调用经
/// <see cref="IFeishuAppContextScopeFactory.BeginScope"/> 切换，finally 释放。
/// </para>
/// <para>
/// <b>AOT 约束</b>：SSE 事件投影走 <see cref="JsonDocument"/> 手工白名单（DataModels 的
/// <c>AilyJsonContext</c> 为 internal 且仅 net8+ 编译，跨包不可用——对齐 Phase 1 投影偏差决策），
/// 全程无反射序列化。SSE 载荷（问答事件）一次性读入内存按行解析——体量小，且规避
/// netstandard2.0 缺失 <c>ReadAsStreamAsync(ct)</c>/<c>ReadLineAsync(ct)</c> 重载的 TFM 分叉。
/// </para>
/// <para>
/// <b>错误语义</b>：非 2xx 由 HTTP 执行器抛 <c>ApiException</c>；飞书残余风险「HTTP 200 +
/// JSON 错误体」（2700033 执行失败 / 2700034 问答被禁用 / 2700035 执行超时，见
/// <c>documents/ErrorHandling.md</c>）按 Content-Type 自检后转可读异常，不在答案中静默吞掉。
/// </para>
/// </remarks>
public sealed class AilyKnowledgeProvider : IFeishuKnowledgeBase, IRetriever
{
    private const string StatusFinished = "finished";

    /// <summary>未限定数据资产范围时的默认来源标注（P2D-4c 弱引用）。</summary>
    private const string DefaultChunkSource = "aily:data-knowledge";

    private readonly Mud.Feishu.IFeishuTenantV1AilyDataKnowledge _knowledgeClient;
    private readonly IFeishuAppContextScopeFactory _scopeFactory;
    private readonly IFeishuToolContextAccessor? _contextAccessor;
    private readonly AilyKnowledgeOptions _options;
    private readonly ILogger? _logger;

    /// <summary>
    /// 初始化 <see cref="AilyKnowledgeProvider"/>。
    /// </summary>
    /// <param name="knowledgeClient">Aily 数据知识客户端（Tenant 身份）。</param>
    /// <param name="scopeFactory">租户上下文作用域工厂。</param>
    /// <param name="options">RAG-A 配置（<see cref="AilyKnowledgeOptions"/>）。</param>
    /// <param name="contextAccessor">工具执行上下文访问器（appKey 事实来源；可空=执行期结构化失败）。</param>
    /// <param name="logger">日志（可空）。</param>
    public AilyKnowledgeProvider(
        Mud.Feishu.IFeishuTenantV1AilyDataKnowledge knowledgeClient,
        IFeishuAppContextScopeFactory scopeFactory,
        IOptions<AilyKnowledgeOptions> options,
        IFeishuToolContextAccessor? contextAccessor = null,
        ILogger<AilyKnowledgeProvider>? logger = null)
    {
        _knowledgeClient = knowledgeClient ?? throw new ArgumentNullException(nameof(knowledgeClient));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _options = (options ?? throw new ArgumentNullException(nameof(options))).Value;
        _contextAccessor = contextAccessor;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<KnowledgeAnswer> AskAsync(string query, CancellationToken cancellationToken = default)
    {
        var outcome = await AskCoreAsync(query, cancellationToken).ConfigureAwait(false);
        return new KnowledgeAnswer(query, outcome.AnswerText, outcome.HasAnswer, outcome.Chunks);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RetrievedChunk>> RetrieveAsync(string query, CancellationToken cancellationToken = default)
    {
        var outcome = await AskCoreAsync(query, cancellationToken).ConfigureAwait(false);
        if (!outcome.HasAnswer)
        {
            return [];
        }

        // FAQ 命中无 chunks 时，答案文本本身即知识单元（标准问答对）。
        if (outcome.Chunks.Count == 0)
        {
            return [new RetrievedChunk(outcome.AnswerText ?? string.Empty, Source: "aily:faq")];
        }

        // 引用回链（P2D-4c）：Aily ask SSE 的 chunks 为纯文本（不携带来源元数据）——
        // 配置了 DataAssetIds 限定范围时标注对应数据资产（弱引用），否则标注 aily:data-knowledge。
        var assetIds = _options.DataAssetIds;
        if (assetIds is { Length: > 0 })
        {
            var assetSource = "aily:data-knowledge:" + string.Join("|", assetIds);
            return [.. outcome.Chunks.Select(chunk => chunk with { Source = assetSource })];
        }

        return [.. outcome.Chunks.Select(static chunk => chunk with { Source = DefaultChunkSource })];
    }

    /// <summary>SSE 问答执行：返回（答案文本, hasAnswer, 召回切片）。</summary>
    private async Task<(string? AnswerText, bool HasAnswer, IReadOnlyList<RetrievedChunk> Chunks)> AskCoreAsync(
        string query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("query 不能为空", nameof(query));

        var appKey = _contextAccessor?.Current?.AppKey;
        if (string.IsNullOrWhiteSpace(appKey))
        {
            throw new InvalidOperationException(
                "Aily 知识问答缺少 appKey 执行上下文——请经 ConversationalFeishuEventHandler 或 IFeishuToolContextAccessor.Begin 注入（多租户隔离禁止默认应用兜底，TMA2-20）");
        }

        // netstandard2.0 的 BCL 不带 IsNullOrWhiteSpace 的 NotNullWhen 注解，需显式断言（对齐既有风格）。
        using var scope = _scopeFactory.BeginScope(appKey!);
        using var response = await _knowledgeClient
            .AskDataKnowledgeAsync(_options.AppId, BuildRequest(query), cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Aily 数据知识问答无响应（result 为空）");

#if NET5_0_OR_GREATER
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
#else
        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif

        if (IsJsonBody(response))
        {
            // 残余风险：HTTP 200 + JSON 错误体（飞书 Aily 业务错误不抛 HTTP 异常）。
            throw new InvalidOperationException($"Aily 数据知识问答返回错误: {DescribeErrorBody(body)}");
        }

        var parsed = ParseSseBody(body);
        _logger?.LogInformation(
            "Aily 知识问答完成（hasAnswer: {HasAnswer}, chunks: {ChunkCount}）", parsed.HasAnswer, parsed.Chunks.Count);
        return parsed;
    }

    private AskDataKnowledgeRequest BuildRequest(string query)
        => new()
        {
            Message = { Content = query },
            DataAssetIds = _options.DataAssetIds,
            DataAssetTagIds = _options.DataAssetTagIds,
        };

    private static bool IsJsonBody(HttpResponseMessage response)
        => string.Equals(
            response.Content.Headers.ContentType?.MediaType,
            "application/json",
            StringComparison.OrdinalIgnoreCase);

    /// <summary>解析 SSE 载荷（<c>data:</c> 行逐事件投影；QA 答案取最后一次出现的非空 content）。</summary>
    private static (string? AnswerText, bool HasAnswer, IReadOnlyList<RetrievedChunk> Chunks) ParseSseBody(string body)
    {
        string? answerText = null;
        string? faqAnswer = null;
        var hasAnswer = false;
        var chunks = new List<RetrievedChunk>();

        foreach (var rawLine in body.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            if (!TryParseDataLine(line, out var payload))
            {
                continue;
            }

            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;

            var status = root.TryGetProperty("status", out var statusElement) ? statusElement.GetString() : null;
            hasAnswer = root.TryGetProperty("has_answer", out var hasAnswerElement)
                && hasAnswerElement.ValueKind == JsonValueKind.True;

            if (root.TryGetProperty("message", out var messageElement)
                && messageElement.ValueKind == JsonValueKind.Object
                && messageElement.TryGetProperty("content", out var contentElement)
                && contentElement.ValueKind == JsonValueKind.String)
            {
                // processing 中间事件也带 message，以最后出现的非空内容为准（SSE 顺序到达）。
                var content = contentElement.GetString();
                if (!string.IsNullOrEmpty(content))
                {
                    answerText = content;
                }
            }

            if (root.TryGetProperty("process_data", out var processDataElement)
                && processDataElement.ValueKind == JsonValueKind.Object
                && processDataElement.TryGetProperty("chunks", out var chunksElement)
                && chunksElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var chunk in chunksElement.EnumerateArray())
                {
                    // R3-6：非字符串 chunk（数字/布尔/对象/数组）降级为文本而非抛异常——
                    // JsonElement.GetString() 对非 String 会抛 InvalidOperationException，
                    // 一条脏数据会让**整次召回**（chunks + answer）失败。
                    var text = chunk.ValueKind switch
                    {
                        JsonValueKind.String => chunk.GetString(),
                        JsonValueKind.Null or JsonValueKind.Undefined => null,
                        _ => chunk.GetRawText(),
                    };
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        chunks.Add(new RetrievedChunk(text!));
                    }
                }
            }

            if (root.TryGetProperty("faq_result", out var faqElement) && faqElement.ValueKind == JsonValueKind.Object)
            {
                faqAnswer = faqElement.TryGetProperty("answer", out var answerElement)
                    && answerElement.ValueKind == JsonValueKind.String
                        ? answerElement.GetString()
                        : null;
            }

            if (string.Equals(status, StatusFinished, StringComparison.Ordinal))
            {
                break;
            }
        }

        var finalText = !string.IsNullOrWhiteSpace(answerText) ? answerText : faqAnswer;
        return (finalText, hasAnswer && !string.IsNullOrWhiteSpace(finalText), chunks);
    }

    private static bool TryParseDataLine(string line, out string payload)
    {
        const string DataPrefix = "data:";
        if (line.Length <= DataPrefix.Length || !line.StartsWith(DataPrefix, StringComparison.Ordinal))
        {
            payload = string.Empty;
            return false;
        }

        payload = line.Substring(DataPrefix.Length).Trim();
        return payload.Length > 0;
    }

    /// <summary>HTTP 200 + JSON 错误体的可读描述（code/msg 提取失败时降级为原文截断）。</summary>
    private static string DescribeErrorBody(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            var code = root.TryGetProperty("code", out var codeElement) ? codeElement.GetRawText() : "(无 code)";
            var message = root.TryGetProperty("msg", out var msgElement) ? msgElement.GetString() ?? string.Empty : string.Empty;
            return $"code={code}, msg={message}";
        }
        catch (JsonException)
        {
            // 有意静默（守卫白名单）：本方法是"把错误体转成可读文本"的纯函数（无 logger），
            // 解析失败时**输出仍然确定**（截断原文），调用方会把结果包进 InvalidOperationException 上报。
            return body.Length > 200 ? body.Substring(0, 200) : body;
        }
    }
}
