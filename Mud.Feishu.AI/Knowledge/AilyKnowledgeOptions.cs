// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Knowledge;

/// <summary>
/// Aily 托管知识问答（RAG-A）配置项（配置节 <see cref="SectionName"/>，Phase 2 §3.4）。
/// </summary>
/// <remarks>
/// <para>
/// 配置面治理（R4/R5）：每个属性都有真实消费点（<c>AilyKnowledgeProvider</c>）；
/// 配置 DTO 不使用 <c>required</c>（源生成配置绑定器限制），合法性在 <see cref="Validate"/> 校验；
/// Aily 应用 ID 是 Aily 平台资源标识而非飞书应用凭据，故落独立配置节而非 <c>FeishuAppConfig</c>
/// （租户维度的知识库范围治理归 Phase 4 多租户 AI 治理线）。
/// </para>
/// </remarks>
public sealed class AilyKnowledgeOptions
{
    /// <summary>配置节名称（<c>FeishuAilyKnowledge</c>）。</summary>
    public const string SectionName = "FeishuAilyKnowledge";

    /// <summary>
    /// Aily 应用 ID（形如 <c>spring_5862e4fea8__c</c>；消费点：
    /// <c>AskDataKnowledgeAsync</c> 的 <c>app_id</c> 路径参数）。
    /// </summary>
    public string AppId { get; set; } = string.Empty;

    /// <summary>
    /// 限定问答依据的数据知识 ID 范围（可空=不限制；消费点：
    /// <c>AskDataKnowledgeRequest.DataAssetIds</c>）。
    /// </summary>
    public string[]? DataAssetIds { get; set; }

    /// <summary>
    /// 限定问答依据的数据知识分类 ID 范围（可空=不限制；消费点：
    /// <c>AskDataKnowledgeRequest.DataAssetTagIds</c>）。
    /// </summary>
    public string[]? DataAssetTagIds { get; set; }

    /// <summary>
    /// 校验配置合法性（注册时 fail-fast）。
    /// </summary>
    /// <exception cref="InvalidOperationException">存在非法取值。</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(AppId))
            throw new InvalidOperationException(
                $"FeishuAilyKnowledge:{nameof(AppId)} 不能为空——Aily 数据知识问答必须指定应用 ID（spring_xxx__c）");
    }
}
