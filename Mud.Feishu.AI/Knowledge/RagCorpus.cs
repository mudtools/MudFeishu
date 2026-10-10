// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Knowledge;

/// <summary>
/// 待索引语料文档（RAG-B 输入单元；由宿主侧 <see cref="ICorpusSource"/> 提供）。
/// </summary>
/// <param name="SourceToken">
/// <b>稳定来源标识</b>（如 wiki 节点 token / 文档 id）。切片必须逐块携带它——
/// 这是"答案可溯源"的唯一凭据，丢了就再也回不到原文。
/// </param>
/// <param name="Text">正文（Markdown 或纯文本）。</param>
/// <param name="Title">文档标题（可空；切片会据此推导标题层级路径）。</param>
/// <param name="Url">回链 URL（可空；宿主给出时切片逐块携带）。</param>
/// <param name="ScopeKey">
/// 检索范围键（可空，通常是租户/公司维度）。<b>向量库实现必须按它隔离</b>——
/// 与工具面"多租户禁止默认 appKey 兜底"同源：跨租户召回即为数据泄露。
/// </param>
public readonly record struct CorpusDocument(
    string SourceToken,
    string Text,
    string? Title = null,
    string? Url = null,
    string? ScopeKey = null);

/// <summary>
/// 语料切片（RAG-B 索引单元）。<b>回链三件套</b>（<see cref="SourceToken"/> / <see cref="TitlePath"/> /
/// <see cref="Url"/>）+ <see cref="ScopeKey"/> 缺一不可——它们是检索结果可溯源、且不跨租户的前提。
/// </summary>
/// <param name="SourceToken">来源标识（回链到文档/节点）。</param>
/// <param name="Text">切片正文。</param>
/// <param name="TitlePath">标题层级路径（如 <c>手册 / 部署 / 环境变量</c>）。</param>
/// <param name="Url">回链 URL（可空）。</param>
/// <param name="ScopeKey">检索范围键（可空；向量库按它隔离）。</param>
/// <param name="Index">该文档内的切片序号（0 起）。</param>
public sealed record CorpusChunk(
    string SourceToken,
    string Text,
    string? TitlePath = null,
    string? Url = null,
    string? ScopeKey = null,
    int Index = 0);

/// <summary>
/// 语料来源（<b>宿主契约</b>：拉取全量或变更流）。SDK 不实现——"语料在哪"是宿主知识。
/// </summary>
/// <remarks>
/// 未注册实现时 RAG-B 索引管线不可用（<see cref="CorpusIndexer"/> 需显式注入本接口），
/// 这与"域客户端缺席 → 该域工具不注册"是同一套软缺席纪律。
/// </remarks>
public interface ICorpusSource
{
    /// <summary>拉取待索引文档（实现方负责去重/增量与规模上限）。</summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>文档集合；空集合表示"无新增/无变更"。</returns>
    Task<IReadOnlyList<CorpusDocument>> PullAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// 向量存储（<b>宿主契约</b>：写入 + 相似检索）。SDK <b>不</b>引入向量库依赖（DP-C5-1 范围裁剪）。
/// </summary>
/// <remarks>
/// <para>
/// <b>实现方义务</b>：① <c>UpsertAsync</c> 按 <see cref="CorpusChunk.SourceToken"/> + <see cref="CorpusChunk.Index"/> 幂等覆盖
/// （同一文档重新切片后不残留旧块）；② <c>QueryAsync</c> <b>必须</b>按 <see cref="CorpusChunk.ScopeKey"/> 隔离，
/// 禁止跨范围召回；③ 返回的 <see cref="RetrievedChunk.Source"/> 应填回链（URL 优先，其次来源标识）。
/// </para>
/// <para>
/// 嵌入由宿主负责（SDK 无 Embedding API，也不该在 AOT 环境里拉模型）。
/// </para>
/// </remarks>
public interface IVectorStore
{
    /// <summary>写入/覆盖切片（幂等）。</summary>
    /// <param name="chunks">切片集合。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task UpsertAsync(IReadOnlyList<CorpusChunk> chunks, CancellationToken cancellationToken = default);

    /// <summary>相似检索。</summary>
    /// <param name="query">查询文本（嵌入由宿主完成）。</param>
    /// <param name="topK">返回条数上限。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>命中切片（按相关度降序；无可命中时返回空集合）。</returns>
    Task<IReadOnlyList<RetrievedChunk>> QueryAsync(string query, int topK, CancellationToken cancellationToken = default);
}