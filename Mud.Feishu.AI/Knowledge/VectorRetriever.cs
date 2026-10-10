// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mud.Feishu.AI.Events;

namespace Mud.Feishu.AI.Knowledge;

/// <summary>
/// RAG-B 检索器：向量召回 → <b>预算裁剪</b> → <see cref="RetrievedChunk"/>。
/// </summary>
/// <remarks>
/// <para>
/// <b>与 RAG-A（Aily）的关系</b>：两者都实现 <see cref="IRetriever"/>，宿主<b>二选一</b>
/// （DI 用 <c>TryAddSingleton</c> ⇒ 先注册者生效，见 <c>AddFeishuVectorKnowledge</c>）。
/// RAG-B <b>不改变</b> RAG-A 的任何默认行为。
/// </para>
/// <para>
/// <b>为什么这里也做预算裁剪</b>：<see cref="KnowledgeContextAssembler"/> 有权威的三级闸，
/// 但它在<b>召回之后</b>才裁剪——若宿主接了个"什么都返回"的向量库，一轮就会把上万字塞进
/// 拼接过程。检索器侧按条数/单条/总长<b>先</b>裁一刀，是纵深防御（不是重复职责：两处闸的阈值与
/// 目的不同，检索器省的是"传输与拼接"，装配器守的是"进 prompt 的最终形态"）。
/// </para>
/// </remarks>
public sealed class VectorRetriever : IRetriever
{
    /// <summary>默认召回条数。</summary>
    public const int DefaultTopK = 5;

    private readonly IVectorStore _store;
    private readonly int _topK;
    private readonly int _maxChunkLength;
    private readonly int _maxTotalLength;

    /// <summary>
    /// 构造。
    /// </summary>
    /// <param name="store">向量存储（宿主实现，必填）。</param>
    /// <param name="topK">召回条数上限。</param>
    /// <param name="maxChunkLength">单条长度上限（默认取 <see cref="ContextBudgets.KnowledgeChunkPreviewLength"/>）。</param>
    /// <param name="maxTotalLength">总长度上限（默认取 <see cref="ContextBudgets.KnowledgeTotalLength"/>）。</param>
    /// <exception cref="ArgumentNullException"><paramref name="store"/> 为 null。</exception>
    /// <exception cref="ArgumentOutOfRangeException">阈值非法。</exception>
    public VectorRetriever(
        IVectorStore store,
        int topK = DefaultTopK,
        int maxChunkLength = ContextBudgets.KnowledgeChunkPreviewLength,
        int maxTotalLength = ContextBudgets.KnowledgeTotalLength)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));

        if (topK <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(topK), topK, "召回条数必须为正");
        }

        if (maxChunkLength <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxChunkLength), maxChunkLength, "单条长度上限必须为正");
        }

        if (maxTotalLength <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxTotalLength), maxTotalLength, "总长度上限必须为正");
        }

        _topK = topK;
        _maxChunkLength = maxChunkLength;
        _maxTotalLength = maxTotalLength;
    }

    /// <inheritdoc />
    /// <remarks>
    /// 空/空白查询<b>不触达</b>向量库（省一次往返，且避免"空向量"召回出一堆噪声）。
    /// </remarks>
    public async Task<IReadOnlyList<RetrievedChunk>> RetrieveAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var hits = await _store.QueryAsync(query, _topK, cancellationToken).ConfigureAwait(false);
        if (hits is null || hits.Count == 0)
        {
            return [];
        }

        var results = new List<RetrievedChunk>(Math.Min(hits.Count, _topK));
        var total = 0;

        foreach (var hit in hits)
        {
            if (results.Count >= _topK || total >= _maxTotalLength)
            {
                break;
            }

            if (hit is null || string.IsNullOrWhiteSpace(hit.Text))
            {
                continue;
            }

            var text = hit.Text.Length <= _maxChunkLength
                ? hit.Text
                : hit.Text.Substring(0, _maxChunkLength) + "…";

            total += text.Length;
            results.Add(hit with { Text = text });
        }

        return results;
    }
}

/// <summary>
/// RAG-B 索引管线：<see cref="ICorpusSource"/> → <see cref="DocumentChunker"/> → <see cref="IVectorStore"/>。
/// </summary>
/// <remarks>
/// <b>为什么提供它</b>：只有契约而没有编排，等于把"拉取→切片→写入"的顺序与幂等语义
/// 留给每个宿主各写一遍（且大概率写出三种）。本类把这三步的<b>确定顺序</b>与
/// "空语料不写库"的语义固定下来。
/// </remarks>
public sealed class CorpusIndexer(
    ICorpusSource source,
    IVectorStore store,
    ChunkingOptions? options = null)
{
    private readonly ICorpusSource _source = source ?? throw new ArgumentNullException(nameof(source));
    private readonly IVectorStore _store = store ?? throw new ArgumentNullException(nameof(store));

    /// <summary>
    /// 执行一次索引（拉取 → 切片 → 写入）。
    /// </summary>
    /// <returns>写入的切片总数（空语料返回 0，且<b>不</b>调用向量库）。</returns>
    public async Task<int> IndexAsync(CancellationToken cancellationToken = default)
    {
        var documents = await _source.PullAsync(cancellationToken).ConfigureAwait(false);
        if (documents is null || documents.Count == 0)
        {
            // 空语料不写库：否则会触发一次"把所有旧向量标记为陈旧"的无效写入。
            return 0;
        }

        var chunks = new List<CorpusChunk>();
        foreach (var document in documents)
        {
            if (string.IsNullOrWhiteSpace(document.SourceToken))
            {
                // 与切片器同一口径：缺来源标识的文档不可回链 ⇒ 不入索引（由切片器抛错，此处先行过滤以成批处理）。
                continue;
            }

            chunks.AddRange(DocumentChunker.Chunk(document, options));
        }

        if (chunks.Count == 0)
        {
            return 0;
        }

        await _store.UpsertAsync(chunks, cancellationToken).ConfigureAwait(false);
        return chunks.Count;
    }
}

/// <summary>
/// RAG-B 的 DI 入口。
/// </summary>
public static class FeishuVectorKnowledgeExtensions
{
    /// <summary>
    /// 注册向量检索面：<see cref="IVectorStore"/>（宿主实现）+ <see cref="IRetriever"/>
    /// （<see cref="VectorRetriever"/>）。
    /// </summary>
    /// <remarks>
    /// <b>与 Aily 的互斥口径</b>：两者都用 <c>TryAddSingleton&lt;IRetriever&gt;</c> ⇒
    /// <b>先注册者生效</b>。要换检索面就<b>先</b>注册新的一个（不必卸载 Aily）；
    /// 同时注册两者不会报错，但只有先到的那个被解析到——这是刻意选择：让"换检索面"不需要卸载既有包。
    /// </remarks>
    /// <param name="services">服务集合。</param>
    /// <param name="store">宿主实现的向量存储。</param>
    /// <param name="configure">切片/检索参数覆盖（可空）。</param>
    public static IServiceCollection AddFeishuVectorKnowledge(
        this IServiceCollection services,
        IVectorStore store,
        Action<ChunkingOptions>? configure = null)
    {
        // ⚠️ 不用 ArgumentNullException.ThrowIfNull：netstandard2.0 目标上没有该 API。
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (store is null)
        {
            throw new ArgumentNullException(nameof(store));
        }

        var options = new ChunkingOptions();
        configure?.Invoke(options);
        options.Validate();

        services.TryAddSingleton(store);
        services.TryAddSingleton<IRetriever>(sp => new VectorRetriever(
            sp.GetRequiredService<IVectorStore>()));

        return services;
    }

    /// <summary>
    /// 注册索引管线（<see cref="CorpusIndexer"/>）：需要宿主同时提供语料来源与向量存储。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <param name="source">宿主实现的语料来源。</param>
    public static IServiceCollection AddFeishuCorpusIndexing(
        this IServiceCollection services,
        ICorpusSource source)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        services.TryAddSingleton(source);
        services.TryAddSingleton<CorpusIndexer>();

        return services;
    }
}