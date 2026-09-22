// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.WebSocket;

/// <summary>
/// 时间跨度钳制工具（架构不变量 I16 的载体）。
/// </summary>
/// <remarks>
/// <b>为什么需要</b>：<see cref="CancellationTokenSource"/> 的延时构造函数会把 <see cref="TimeSpan"/>
/// 折算为整数毫秒并校验上界，超过时**抛 <see cref="ArgumentOutOfRangeException"/>**。
/// 若调用方把配置值直接透传（例如 <c>Reconnect.TotalBudget</c> 被配成 60 天），
/// 异常会在**异步路径的 catch 里**被吞掉，表现为"该轮自动重连完全不执行"——
/// 这类失败既难定位又需要在生产上付出长时断连的代价。
/// <para>
/// <b>上界的真实取值（实测结论，勿照抄旧注释）</b>：
/// <list type="bullet">
/// <item><b>.NET Core / .NET 5+</b>：<c>MaxSupportedTimeout = uint.MaxValue - 1</c> 毫秒（约 <b>49.7 天</b>）。
/// 实测 <c>new CancellationTokenSource(TimeSpan.FromDays(30))</c> **不会**抛（曾按"24.8 天"推断，属误判）；</item>
/// <item><b>.NET Framework / netstandard2.0 宿主</b>：旧实现以 <c>int</c> 承载，上界为 <c>int.MaxValue - 1</c> 毫秒（约 <b>24.8 天</b>）。</item>
/// </list>
/// 本类取**二者较小值**（<c>int.MaxValue - 1</c>）作为 <see cref="MaxCancellationTokenDelayMs"/>，
/// 使钳制结果在四个目标框架上语义一致——代价是 .NET Core 上牺牲了 24.8~49.7 天区间的"可观测灵敏度"，
/// 而该区间内的配置值本身就是运维错误（配置面另有 7 天上界的 fail-fast）。
/// </para>
/// <para>
/// <b>不变量 I16</b>：全模块任何 <c>new CancellationTokenSource(&lt;TimeSpan&gt;)</c> / <c>Task.Delay(&lt;TimeSpan&gt;)</c>
/// 的调用点都必须先经过本类钳制；配置面同时必须做**双向**（下界 + 上界）校验。
/// </para>
/// <para>
/// <b>兼容性</b>：<c>Math.Clamp</c> 需要 <c>netstandard2.1</c>，本模块最低目标为
/// <c>netstandard2.0</c>，故内部一律使用 <c>Math.Min</c> / <c>Math.Max</c>。
/// </para>
/// <para>
/// 本类型为纯 BCL 数值运算，不涉及反射/JSON，**AOT 与裁剪安全**，无需
/// <c>RequiresUnreferencedCode</c>/<c>RequiresDynamicCode</c> 标注。
/// </para>
/// </remarks>
internal static class TimeSpanGuards
{
    /// <summary>
    /// <see cref="CancellationTokenSource"/>(<see cref="TimeSpan"/>) 可取的最大总毫秒数（跨 TFM 的保守上界）。
    /// </summary>
    /// <remarks>
    /// 取值 = <c>int.MaxValue - 1</c>（约 24.8 天）：这是 .NET Framework / <c>netstandard2.0</c> 宿主的真实上界；
    /// .NET Core / .NET 5+ 放宽到 <c>uint.MaxValue - 1</c>（约 49.7 天，实测确认）。
    /// 取较小值以保证"钳制后一定被 BCL 接受"这一结论在四个目标框架上**都成立**
    /// （钳制工具的正确性不能依赖运行宿主）。
    /// </remarks>
    internal const int MaxCancellationTokenDelayMs = int.MaxValue - 1;

    /// <summary>
    /// <see cref="Task.Delay(TimeSpan, CancellationToken)"/> 可取的最大总毫秒数。
    /// </summary>
    internal const long MaxTaskDelayMs = (long)uint.MaxValue - 1;

    /// <summary>
    /// 把时间跨度钳制到 <see cref="CancellationTokenSource"/>(<see cref="TimeSpan"/>) 可接受的区间。
    /// </summary>
    /// <param name="value">原始时间跨度</param>
    /// <returns>
    /// 落在 <c>[TimeSpan.Zero, MaxCancellationTokenDelayMs 毫秒]</c> 内的值；
    /// 负值钳制为 <see cref="TimeSpan.Zero"/>（关闭定时器而非"立即取消"是不可区分的，调用方须自行决定是否接受 0）。
    /// </returns>
    /// <remarks>
    /// <b>有意不回退默认值</b>：回退会掩盖配置错误（用户配了 100 天，静默按 30 分钟跑更容易误导）。
    /// 配置越界应由 <see cref="FeishuWebSocketOptions.Validate"/> 在**启动期** fail-fast；
    /// 本方法只保证"运行期即使被绕过校验也不会抛异常"。
    /// </remarks>
    internal static TimeSpan ClampToCancellationTokenRange(TimeSpan value)
    {
        if (value <= TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        var max = TimeSpan.FromMilliseconds(MaxCancellationTokenDelayMs);
        return value > max ? max : value;
    }

    /// <summary>
    /// 把时间跨度钳制到 <see cref="Task.Delay(TimeSpan, CancellationToken)"/> 可接受的区间。
    /// </summary>
    /// <param name="value">原始时间跨度</param>
    /// <returns>落在 <c>[TimeSpan.Zero, MaxTaskDelayMs 毫秒]</c> 内的值。</returns>
    internal static TimeSpan ClampToTaskDelayRange(TimeSpan value)
    {
        if (value <= TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        var max = TimeSpan.FromMilliseconds(MaxTaskDelayMs);
        return value > max ? max : value;
    }

    /// <summary>
    /// 判断给定时间跨度能否直接用于 <see cref="CancellationTokenSource"/>(<see cref="TimeSpan"/>)。
    /// </summary>
    /// <param name="value">待判断的时间跨度</param>
    /// <returns>处于合法区间（含 0）时返回 <c>true</c>。</returns>
    internal static bool IsValidCancellationTokenDelay(TimeSpan value)
        => value >= TimeSpan.Zero && value <= TimeSpan.FromMilliseconds(MaxCancellationTokenDelayMs);
}
