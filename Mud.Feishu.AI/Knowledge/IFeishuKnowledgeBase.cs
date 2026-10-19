// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Knowledge;

/// <summary>
/// 飞书侧知识库数据面门面的<b>派生命名契约</b>。
/// </summary>
/// <remarks>
/// <para>
/// 数据面（<c>AskAsync</c>）、检索面（<c>IRetriever</c>）与值类型
/// （<c>RetrievedChunk</c>/<c>KnowledgeAnswer</c>）由 <see cref="Mud.AI.Abstractions.Knowledge.IKnowledgeBase"/>
/// 承载；本接口保留产品命名，
/// 使「飞书托管知识（Aily）」的宿主装配点与既有消费方零改动。
/// </para>
/// <para>
/// <b>⚠️ 不得新增成员</b>——产品特有的能力（如 Aily 专有的流式事件形态）应由
/// <c>AilyKnowledgeProvider</c> 自身的公开面承载，而不是把这个门面撑成"飞书专用接口"。
/// </para>
/// </remarks>
public interface IFeishuKnowledgeBase : Mud.AI.Abstractions.Knowledge.IKnowledgeBase
{
}
