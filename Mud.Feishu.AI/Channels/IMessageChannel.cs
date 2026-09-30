// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Channels;

/// <summary>
/// 流式回复通道抽象（Phase 2 §3.1）：把模型增量 token 桥接给飞书消息面。
/// </summary>
/// <remarks>
/// <para>
/// SDK 不预设消息通道——实现方决定「占位消息怎么建、增量怎么落」（首版降级实现为
/// <c>EditMessageAsync</c> 分片编辑，落点在 <c>Mud.Feishu.AI.FeishuTools</c> 包；
/// 消息流卡片 <c>IFeishuTenantV2AppCardMessageStream</c> 为后续通道实现切换）。
/// </para>
/// <para>
/// 失败语义（Phase 2 §3.1，对齐 I1 事件派发异常隔离）：<b>单次增量写入失败不中断模型流</b>——
/// 实现方记日志后继续；<see cref="BeginAsync"/> 失败由调用方回退非流式路径（模型尚未调用，零重复成本）。
/// </para>
/// <para>
/// <b>并发契约（R2-11，明确写入契约面）</b>：同一 <c>messageId</c> 上的
/// <see cref="WriteStreamAsync"/> 与 <see cref="FlushAsync"/> <b>必须由调用方顺序 <c>await</c></b>——
/// 并发调用<b>不在契约内</b>（实现方为无锁乐观路径：缓冲摘除后仍持引用的并发写会永久滞留半截文本）。
/// 事件处理器即按顺序 <c>await</c> 驱动（<c>ConversationalFeishuEventHandler.RunConversationAsync</c>），
/// 故该约束在生产链路上恒成立；此声明把"实现依赖的隐含前提"变成契约的一部分。
/// </para>
/// <para>
/// <b>状态生命周期契约（R2-02）</b>：实现方若为 <c>Singleton</c>，其<b>per-messageId 状态必须在
/// <see cref="FlushAsync"/> 内清理</b>（基类钩子见 <c>BufferedMessageChannel.OnFlushed</c>）——
/// 否则字典键随会话单调新增而永不重复，形成确定性常驻内存增长。
/// </para>
/// </remarks>
public interface IMessageChannel
{
    /// <summary>
    /// 创建占位消息（流式回复的载体），返回消息 ID。
    /// </summary>
    /// <param name="appKey">应用唯一标识（租户上下文切换由实现方经 <c>BeginScope</c> 承担）。</param>
    /// <param name="chatId">目标会话 ID（chat_id）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>占位消息 ID。</returns>
    Task<string> BeginAsync(string appKey, string chatId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 写入一段模型增量文本（实现方按需缓冲/分片落盘；实现必须自带失败隔离，不向上抛业务异常）。
    /// </summary>
    /// <param name="appKey">应用唯一标识。</param>
    /// <param name="chatId">目标会话 ID。</param>
    /// <param name="messageId"><see cref="BeginAsync"/> 返回的占位消息 ID。</param>
    /// <param name="delta">模型增量文本（可能为空串）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task WriteStreamAsync(string appKey, string chatId, string messageId, string delta, CancellationToken cancellationToken = default);

    /// <summary>
    /// 结束流式回复：落地最终完整文本/结束态（卡片收尾等）。
    /// </summary>
    /// <param name="appKey">应用唯一标识。</param>
    /// <param name="chatId">目标会话 ID。</param>
    /// <param name="messageId"><see cref="BeginAsync"/> 返回的占位消息 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task FlushAsync(string appKey, string chatId, string messageId, CancellationToken cancellationToken = default);
}
