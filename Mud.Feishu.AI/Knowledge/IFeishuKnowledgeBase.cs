// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Knowledge;

/// <summary>
/// 单个召回知识切片（引用回链的最小单元，Phase 2 §3.4 RAG-A）。
/// </summary>
/// <param name="Text">切片文本（召回内容）。</param>
/// <param name="Source">来源标识（Aily 数据知识 ID / 文档标题等，可空；RAG-B 为 wiki 回链 URL）。</param>
/// <param name="Score">相关性得分（可空；Aily 托管问答不返回得分）。</param>
public sealed record RetrievedChunk(string Text, string? Source = null, double? Score = null);

/// <summary>
/// 知识问答结果：答案文本 + 召回切片（供「带引用的回答」注入 Prompt 与回传 sources）。
/// </summary>
/// <param name="Question">原始问题。</param>
/// <param name="AnswerText">答案文本（可空——<see cref="HasAnswer"/> 为 <see langword="false"/> 时无答案）。</param>
/// <param name="HasAnswer">是否有结果（false 表示未命中配置知识，答案不可信）。</param>
/// <param name="Chunks">召回切片（引用回链来源）。</param>
public sealed record KnowledgeAnswer(
    string Question,
    string? AnswerText,
    bool HasAnswer,
    IReadOnlyList<RetrievedChunk> Chunks)
{
    /// <summary>构造未命中结果（HasAnswer=false）。</summary>
    public static KnowledgeAnswer NoAnswer(string question) => new(question, null, false, []);
}

/// <summary>
/// 知识库数据面门面（路线图主线③）：宿主经此做问答与数据同步，不感知托管（RAG-A）/自建（RAG-B）差异。
/// </summary>
/// <remarks>
/// Phase 2 交付 RAG-A（Aily 托管问答）；RAG-B 自建检索为条件交付（已决策⑨，Phase 3），
/// 届时新增第二实现并存（<c>IRetriever</c> 检索面正交）。
/// </remarks>
public interface IFeishuKnowledgeBase
{
    /// <summary>
    /// 执行知识问答（RAG-A：Aily 数据知识问答，SSE 流式）。
    /// </summary>
    /// <param name="query">用户问题。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>答案 + 召回切片（引用回链）。</returns>
    Task<KnowledgeAnswer> AskAsync(string query, CancellationToken cancellationToken = default);
}

/// <summary>
/// 知识库检索面（与数据面正交）：把问题检索为切片列表，供 Agent 组装「召回块 + 引用」上下文。
/// </summary>
public interface IRetriever
{
    /// <summary>
    /// 检索与问题相关的知识切片。
    /// </summary>
    /// <param name="query">用户问题。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>召回切片（按相关性排序）。</returns>
    Task<IReadOnlyList<RetrievedChunk>> RetrieveAsync(string query, CancellationToken cancellationToken = default);
}
