// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.AI.Abstractions.Configuration;

/// <summary>
/// 中立 Agent 运行期配置：中立机制（会话摘要、流式通道、管线、闸门）的<b>唯一</b>配置依赖面。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么不直接消费产品 Options</b>：中立实现若读产品配置类，就形成了
/// 「<c>Mud.AI.*</c> → 产品配置」的隐式依赖（编译期不报，P3 上游化时才爆炸）。
/// 本类型是「产品事实参数化」定式的配置面形态——产品侧按投影构造它，宿主<b>不</b>直接配置。
/// </para>
/// <para>
/// <b>投影而非继承</b>：产品 Options 的公开面（配置节名、属性名、异常文案）是宿主可见面，
/// 一个字节都不能变；本类型只承载其中<b>与产品语义无关</b>的运行时旋钮。
/// 投影函数由产品侧提供（见产品的 <c>ToRuntimeOptions()</c>）。
/// </para>
/// <para>
/// <b>校验归属</b>：本类型不重复实现产品侧的配置校验——投影的<b>来源</b>已在校验后
/// （产品的 Options 校验器 + 构造期 <c>Validate()</c> 双触发）。此处只提供
/// <see cref="Validate"/> 作为「中立侧独立构造时」的兜底防线，避免第二份校验逻辑
/// （两份校验会漂移，且漂移无法被编译器发现）。
/// </para>
/// </remarks>
public sealed class AgentRuntimeOptions
{
    /// <summary>会话历史裁剪窗（非 system 消息条数上限）。</summary>
    public int MaxHistoryMessages { get; set; } = 50;

    /// <summary>触发渐进式会话摘要的消息数阈值（0 = 禁用摘要）。</summary>
    public int SummaryThreshold { get; set; } = 30;

    /// <summary>触发摘要的 token 上限（0 = 不启用，仅按条数阈值判定）。</summary>
    public int MaxHistoryTokens { get; set; } = 8000;

    /// <summary>流式回复的分片编辑阈值（字符数）。</summary>
    public int MaxStreamChunkLength { get; set; } = 200;

    /// <summary>单轮工具调用预算上限（0 = 不限制）。</summary>
    public int MaxToolCallsPerTurn { get; set; } = 10;

    /// <summary>全局最大并发轮数（进程内闸门）。</summary>
    public int MaxConcurrentTurns { get; set; } = 32;

    /// <summary>全局闸门队列等待超时（秒；0 = 不限等待）。</summary>
    public int TurnQueueTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// 流式文本抑制：为 <see langword="true"/> 时缓存本轮全部文本增量，
    /// 仅在本轮无工具调用时一次性推送；出现工具调用则该轮文本并入终态。
    /// </summary>
    public bool DeferTextUntilTurnComplete { get; set; }

    /// <summary>工具重试策略。</summary>
    public ToolRetryOptions ToolRetry { get; set; } = new();

    /// <summary>
    /// 校验中立运行期配置（中立侧独立构造时的兜底；正常路径由产品侧的投影保证）。
    /// </summary>
    /// <exception cref="InvalidOperationException">存在非法取值。</exception>
    public void Validate()
    {
        if (MaxHistoryMessages is < 1 or > 10_000)
            throw new InvalidOperationException(
                $"AgentRuntime:{nameof(MaxHistoryMessages)} 须在 1..10000 之间，实际值: {MaxHistoryMessages}");

        if (SummaryThreshold is < 0 or (> 0 and < 4) or > 10_000)
            throw new InvalidOperationException(
                $"AgentRuntime:{nameof(SummaryThreshold)} 为 0（禁用）或 4..10000，实际值: {SummaryThreshold}");

        if (MaxHistoryTokens is < 0 or > 1_000_000)
            throw new InvalidOperationException(
                $"AgentRuntime:{nameof(MaxHistoryTokens)} 为 0（不启用）或 1..1000000，实际值: {MaxHistoryTokens}");

        if (MaxStreamChunkLength is < 1 or > 100_000)
            throw new InvalidOperationException(
                $"AgentRuntime:{nameof(MaxStreamChunkLength)} 须在 1..100000 之间，实际值: {MaxStreamChunkLength}");

        if (MaxToolCallsPerTurn is < 0 or > 100)
            throw new InvalidOperationException(
                $"AgentRuntime:{nameof(MaxToolCallsPerTurn)} 为 0（不限制）或 1..100，实际值: {MaxToolCallsPerTurn}");

        if (MaxConcurrentTurns is < 1 or > 1024)
            throw new InvalidOperationException(
                $"AgentRuntime:{nameof(MaxConcurrentTurns)} 须在 1..1024 之间，实际值: {MaxConcurrentTurns}");

        if (TurnQueueTimeoutSeconds is < 0 or > 600)
            throw new InvalidOperationException(
                $"AgentRuntime:{nameof(TurnQueueTimeoutSeconds)} 为 0（不限等待）或 1..600，实际值: {TurnQueueTimeoutSeconds}");
    }
}
