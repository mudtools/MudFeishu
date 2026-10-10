// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text;
using Mud.Feishu.AI.Tools.Tools;
using Mud.Feishu.AI.Knowledge;

namespace Mud.Feishu.AI.Tools.Internal;

/// <summary>
/// Knowledge 单工具执行器（<c>knowledge.search</c>，AI-FD-D12 P2D-4b）：绑定
/// <see cref="IRetriever"/> 检索门面（Aily 数据知识），回填 = 编号切片列表（每条截断，超限截断标记）。
/// </summary>
/// <remarks>
/// <para>
/// appKey 异步流缺失时由 <c>AilyKnowledgeProvider</c> 抛结构化失败（多租户隔离禁止默认应用兜底，
/// TMA2-20），经执行链异常归一为 <c>[tool_error]</c> 回填——与只读门禁语义一致。
/// </para>
/// <para>执行骨架（catch/回填）由 <see cref="ToolExecutor"/> 承担（WP3）；本类只保留参数校验与投影语义。</para>
/// </remarks>
internal sealed class KnowledgeSearchTools(IRetriever retriever, IOptions<FeishuAgentOptions> options)
{
    /// <summary>单条切片回填截断长度（编号列表可读性优先，逐条再截断）。</summary>
    private const int ChunkPreviewLength = 500;

    private readonly IRetriever _retriever = retriever ?? throw new ArgumentNullException(nameof(retriever));
    private readonly int _maxResultLength = (options ?? throw new ArgumentNullException(nameof(options))).Value.MaxToolResultLength;

    /// <summary>knowledge.search：知识检索（编号切片回填）。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantKnowledgeSearchTool))]
    public Task<FeishuToolResult> SearchAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.KnowledgeSearch, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = KnowledgeSearchArgs.Unpack(arguments);

            var chunks = await _retriever.RetrieveAsync(args.Query, cancellationToken).ConfigureAwait(false);
            if (chunks.Count == 0)
            {
                return FeishuToolResult.FromText($"{executor.ToolName}: 知识库未检索到与问题相关的内容（has_answer=false）——请基于既有上下文作答");
            }

            var builder = new StringBuilder();
            for (var i = 0; i < chunks.Count; i++)
            {
                builder.Append('[').Append((i + 1).ToString(CultureInfo.InvariantCulture)).Append("] ")
                    .AppendLine(ToolResultText.Truncate(chunks[i].Text, ChunkPreviewLength));
                if (!string.IsNullOrEmpty(chunks[i].Source))
                {
                    builder.Append("    来源: ").AppendLine(chunks[i].Source);
                }
            }

            // R-1：出站唯一出口（B-1 一类）。
            return ToolResultPipeline.Ok(builder.ToString(), _maxResultLength);
        });
    }
}
