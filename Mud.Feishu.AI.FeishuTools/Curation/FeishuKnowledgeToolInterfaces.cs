// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.FeishuTools.Curation;

// <summary>
// Knowledge 域只读工具接口（AI-FD-D12 P2D-4b：RAG-A 工具化——模型按需检索，省 token、可控）。
// </summary>

/// <summary>工具接口：knowledge.search（绑定 <c>IRetriever</c> 门面，Aily 数据知识问答）。</summary>
[FeishuTool("knowledge.search",
    Description = "检索飞书知识库（Aily 数据知识），返回与问题相关的知识切片；当需要企业知识/文档内容作答时使用。只读。",
    RequiredScopes = ["aily:knowledge:readonly"])]
public interface IFeishuTenantKnowledgeSearchTool
{
    /// <summary>知识检索（切片编号列表回填）。</summary>
    /// <returns>编号切片文本（每条截断，超限截断标记），超长截断并标记 truncated。</returns>
    Task<string> SearchAsync(
        [ToolParameter("query", "检索问题（用完整的自然语言问句）", Required = true)] string query,
        CancellationToken cancellationToken = default);
}
