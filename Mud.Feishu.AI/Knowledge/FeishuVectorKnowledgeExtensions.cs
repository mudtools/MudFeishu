// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mud.AI.Agent.Knowledge;

namespace Mud.Feishu.AI.Knowledge;

/// <summary>
/// RAG 自建检索面的 DI 入口（产品侧装配面）。
/// </summary>
/// <remarks>
/// <para>
/// <c>VectorRetriever</c> / <c>CorpusIndexer</c> / <c>ChunkingOptions</c> /
/// <c>TokenEstimator</c> 属 <c>Mud.AI.Agent.Knowledge</c>；本类只保留产品 DI 面
/// （<c>AddFeishuVectorKnowledge</c> / <c>AddFeishuCorpusIndexing</c> 是宿主可见的装配入口），
/// 内部注册中立类型。
/// </para>
/// <para>
/// <b>与托管问答（Aily）的互斥口径</b>：两者都用 <c>TryAddSingleton&lt;IRetriever&gt;</c> ⇒
/// <b>先注册者生效</b>。要换检索面就<b>先</b>注册新的一个（不必卸载 Aily）；
/// 同时注册两者不会报错，但只有先到的那个被解析到——让"换检索面"不需要卸载既有包。
/// </para>
/// </remarks>
public static class FeishuVectorKnowledgeExtensions
{
    /// <summary>
    /// 注册向量检索面：<see cref="IVectorStore"/>（宿主实现）+ <see cref="IRetriever"/>
    /// （<see cref="VectorRetriever"/>）。
    /// </summary>
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
