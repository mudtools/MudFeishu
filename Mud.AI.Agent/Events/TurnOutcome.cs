// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.AI.Agent.Events;

/// <summary>
/// 一轮会话回合的<b>结构化结果</b>（<see cref="ConversationalTurnPipeline.RunTurnAsync"/> 的返回值）。
/// </summary>
/// <param name="ResponseText">
/// 模型最终回答文本（可空：本轮被前置判定/空消息守卫/空回复守卫短路时为 <see langword="null"/>）。
/// </param>
/// <param name="Streamed">
/// 回复是否已由流式通道分片段送达（<see langword="true"/> 时不再有整段下发）。
/// </param>
/// <param name="Replayed">
/// 本轮是否命中「上一轮待补发回复」——未重跑模型直接补发（P4-4 outbox）。
/// </param>
/// <remarks>
/// 结果是<b>观测面</b>：回复下发由管线经 <see cref="IConversationReplySink"/> 完成，
/// 调用方（产品薄壳）只消费结果做日志/诊断，不承担控制流。
/// </remarks>
public sealed record TurnOutcome(string? ResponseText, bool Streamed, bool Replayed)
{
    /// <summary>空结果（本轮被前置守卫短路，未产生模型文本）。</summary>
    public static TurnOutcome Empty { get; } = new(null, false, false);
}
