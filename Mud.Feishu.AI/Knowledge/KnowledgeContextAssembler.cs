// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.Events;

namespace Mud.Feishu.AI.Knowledge;

/// <summary>
/// 知识检索上下文装配器（AI-FD-D12 P2D-4a，注入模式）：把 <see cref="IRetriever"/> 召回切片
/// 以编号参考块形式注入 Prompt——「检索→Prompt 注入桥」，宿主无需手写检索注入。
/// </summary>
/// <remarks>
/// <para>
/// <b>两种姿态</b>（写入文档，宿主二选一或并用）：
/// 工具模式 = 只注册 <c>knowledge.search</c>（模型按需检索，省 token、可控，默认推荐）；
/// 注入模式 = 注册本装配器（每轮强制检索注入，适合「必须基于知识库作答」的垂直 Bot）；
/// 并用合法（注入给背景、工具给深挖）。本装配器经 <c>AddFeishuKnowledgeContext()</c> 宿主
/// <b>显式装配</b>，不默认开启（省 token）。
/// </para>
/// <para>
/// <b>失败隔离</b>：装配器自身失败由事件层既有异常隔离承接（记日志跳过，不阻断对话）；
/// 检索无结果时返回空片段（本装配器对本次事件无贡献）。
/// </para>
/// <para>
/// <see cref="IContextAssembler.Order"/> = 100：默认装配器之后，问题文本先于知识注入。
/// </para>
/// </remarks>
public sealed class KnowledgeContextAssembler : IContextAssembler
{
    /// <summary>默认装配顺序（默认装配器之后）。</summary>
    public const int DefaultOrder = 100;

    /// <summary>单条切片注入截断长度。</summary>
    public const int ChunkPreviewLength = 500;

    private const string Header = "[参考知识｜来自飞书知识库检索，引用请注明编号]";
    private const string Footer = "（若与问题无关请忽略本节）";

    private readonly IRetriever _retriever;
    private readonly int _order;

    /// <summary>
    /// 初始化 <see cref="KnowledgeContextAssembler"/>。
    /// </summary>
    /// <param name="retriever">知识检索门面（Aily 托管/RAG-B 自建均可）。</param>
    /// <param name="order">装配顺序（缺省 <see cref="DefaultOrder"/>）。</param>
    public KnowledgeContextAssembler(IRetriever retriever, int order = DefaultOrder)
    {
        _retriever = retriever ?? throw new ArgumentNullException(nameof(retriever));
        _order = order;
    }

    /// <inheritdoc />
    public int Order => _order;

    /// <inheritdoc />
    public async Task<string?> AssembleAsync(ConversationRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        // 用户问题取 @提及指令文本（可空时本轮不检索——无问题即无检索语义）。
        var query = request.MentionedText;
        if (string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        var chunks = await _retriever.RetrieveAsync(query!, cancellationToken).ConfigureAwait(false);
        if (chunks.Count == 0)
        {
            return null;
        }

        var builder = new System.Text.StringBuilder();
        builder.AppendLine(Header);
        for (var i = 0; i < chunks.Count; i++)
        {
            var text = chunks[i].Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            builder.Append('[').Append((i + 1).ToString(CultureInfo.InvariantCulture)).Append("] ")
                .AppendLine(TruncateChunk(text));
        }

        builder.Append(Footer);
        return builder.ToString();
    }

    private static string TruncateChunk(string text)
        => text.Length <= ChunkPreviewLength ? text : text.Substring(0, ChunkPreviewLength) + "…";
}
