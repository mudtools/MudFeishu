// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Results;

/// <summary>
/// 长正文窗口切片（逐字稿、演示文稿 XML 等「整体长度远超上下文」的文本共用同一口径）。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么是窗口而不是截断</b>：「截断 + 布尔标记」的口径下模型<b>永远拿不到</b>后半段
/// （没有任何参数能把它取回来）。窗口化配合 <see cref="ContentWindow.NextOffset"/> 续读游标，
/// 模型可据 <c>has_more</c> 自行分段读全量。
/// </para>
/// <para>
/// <b>越界不抛错</b>：按上一轮游标恰好读到末尾是正常路径，返回空窗口 +
/// <see cref="ContentWindow.OffsetOutOfRange"/> 如实回填；抛错会让模型把「已读尽」误判为「工具故障」。
/// </para>
/// </remarks>
internal static class ToolContentWindow
{
    /// <summary>默认窗口长度（字符）。</summary>
    public const int DefaultLength = 10000;

    /// <summary>窗口长度硬上限：再大就失去"分窗口"的意义（一次灌爆上下文）。</summary>
    public const int MaxLength = 50000;

    /// <summary>
    /// 校验窗口参数（必须在<b>下游调用之前</b>执行：非法参数不消耗限频配额）。
    /// </summary>
    /// <param name="offset">起始偏移（可选，默认 0；负数拒绝）。</param>
    /// <param name="limit">窗口长度（可选，默认 <see cref="DefaultLength"/>；超出 1~<see cref="MaxLength"/> 拒绝）。</param>
    /// <param name="offsetParameter">偏移入参在模型侧的名字（错误文案须指名道姓，否则模型不知道改哪个参数）。</param>
    /// <param name="limitParameter">长度入参在模型侧的名字。</param>
    /// <exception cref="ArgumentException">参数越界。</exception>
    internal static void Validate(int? offset, int? limit, string offsetParameter, string limitParameter)
    {
        if ((offset ?? 0) < 0)
        {
            throw new ArgumentException($"参数 {offsetParameter} 不能为负数（它表示从第几个字符开始读）");
        }

        var windowLength = limit ?? DefaultLength;
        if (windowLength < 1 || windowLength > MaxLength)
        {
            throw new ArgumentException(
                $"参数 {limitParameter} 必须在 1~{MaxLength.ToString(CultureInfo.InvariantCulture)} 之间，"
                + $"实际 {windowLength.ToString(CultureInfo.InvariantCulture)}");
        }
    }

    /// <summary>
    /// 计算窗口切片（<b>纯函数</b>，便于独立单测；无 IO、无反射、AOT 友好）。
    /// </summary>
    /// <param name="content">完整正文（可为 null ⇒ 视为空）。</param>
    /// <param name="offset">起始偏移（可选，默认 0）。</param>
    /// <param name="limit">窗口长度（可选，默认 <see cref="DefaultLength"/>）。</param>
    /// <param name="offsetParameter">偏移入参在模型侧的名字。</param>
    /// <param name="limitParameter">长度入参在模型侧的名字。</param>
    /// <exception cref="ArgumentException">参数越界（执行器骨架转为 <c>validation/invalid_args</c> 结构化错误）。</exception>
    internal static ContentWindow Compute(string? content, int? offset, int? limit, string offsetParameter, string limitParameter)
    {
        Validate(offset, limit, offsetParameter, limitParameter);

        var text = content ?? string.Empty;
        var start = offset ?? 0;
        var windowLength = limit ?? DefaultLength;

        var total = text.Length;

        // 偏移超过总长 ⇒ 空窗口（不抛错：模型可能按上一轮的 next_offset 恰好读到末尾）。
        var effectiveStart = start < total ? start : total;
        var length = Math.Min(windowLength, total - effectiveStart);
        var slice = length > 0 ? text.Substring(effectiveStart, length) : string.Empty;
        var hasMore = effectiveStart + length < total;

        return new ContentWindow(
            Text: slice,
            TotalLength: total,
            Offset: effectiveStart,
            Limit: windowLength,
            HasMore: hasMore,
            NextOffset: hasMore ? effectiveStart + length : null,
            OffsetOutOfRange: start > total);
    }
}

/// <summary>长正文窗口切片结果（<see cref="ToolContentWindow.Compute"/> 的产物）。</summary>
/// <param name="Text">窗口内的文本。</param>
/// <param name="TotalLength">正文总长度（模型据此判断"还剩多少"）。</param>
/// <param name="Offset">实际生效的起始偏移（超过总长时收敛为总长）。</param>
/// <param name="Limit">实际生效的窗口长度。</param>
/// <param name="HasMore">是否还有后续内容（<paramref name="NextOffset"/> 非空当且仅当它为 true）。</param>
/// <param name="NextOffset">续读游标；无后续时为 null。</param>
/// <param name="OffsetOutOfRange">调用方传入的 offset 是否超过正文总长（如实回填，避免"空串"被误读为"没有内容"）。</param>
internal readonly record struct ContentWindow(
    string Text,
    int TotalLength,
    int Offset,
    int Limit,
    bool HasMore,
    int? NextOffset,
    bool OffsetOutOfRange);
