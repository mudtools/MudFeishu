// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.Configuration;

/// <summary>
/// HTTP 熔断嵌套配置（C2/R3）。
/// </summary>
/// <remarks>
/// 默认值引用 <see cref="Mud.Feishu.Abstractions.Consts"/> 对应常量。无 <c>required</c> 成员（AOT 绑定）。
/// </remarks>
public class CircuitBreakerOptions
{
    /// <summary>是否启用熔断，默认 true</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>失败率阈值（百分比），默认 20</summary>
    public int FailureThreshold { get; set; } = Consts.DefaultCircuitBreakerFailureThreshold;

    /// <summary>采样窗口（秒），默认 60</summary>
    public int SamplingDurationSeconds { get; set; } = Consts.DefaultCircuitBreakerSamplingDurationSeconds;

    /// <summary>熔断持续（秒），默认 60</summary>
    public int BreakDurationSeconds { get; set; } = Consts.DefaultCircuitBreakerBreakDurationSeconds;

    /// <summary>最小吞吐量，默认 10</summary>
    public int MinimumThroughput { get; set; } = Consts.DefaultCircuitBreakerMinimumThroughput;
}

/// <summary>
/// HTTP 重试嵌套配置（C2/R3）。
/// </summary>
public class HttpRetryOptions
{
    /// <summary>最大重试次数，默认 3</summary>
    public int MaxAttempts { get; set; } = Consts.DefaultHttpRetryCount;

    /// <summary>重试延迟（毫秒），默认 1000</summary>
    public int DelayMs { get; set; } = Consts.DefaultRetryDelayMs;
}
