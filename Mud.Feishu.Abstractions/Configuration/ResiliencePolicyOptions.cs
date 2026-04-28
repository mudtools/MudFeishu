// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions;

/// <summary>
/// 弹性策略配置选项
/// </summary>
/// <remarks>
/// 定义飞书 API 调用的弹性策略配置，包括熔断器等高级策略。
/// 重试和超时策略由 FeishuAppConfig 的 RetryCount、RetryDelayMs、TimeOut 属性直接控制。
/// </remarks>
public class ResiliencePolicyOptions
{
    /// <summary>
    /// 是否启用熔断策略
    /// </summary>
    /// <remarks>
    /// 默认值: false
    /// 当设置为 true 时，在连续失败达到阈值后将触发熔断，暂时阻止请求以保护下游服务。
    /// </remarks>
    public bool CircuitBreakerEnabled { get; set; } = false;

    /// <summary>
    /// 触发熔断的连续失败阈值
    /// </summary>
    /// <remarks>
    /// 默认值: 5
    /// 范围: 2-100
    /// 当连续失败的请求数达到此阈值时，熔断器将打开，阻止后续请求。
    /// </remarks>
    public int CircuitBreakerThreshold { get; set; } = 5;

    /// <summary>
    /// 熔断持续时间（秒）
    /// </summary>
    /// <remarks>
    /// 默认值: 30
    /// 范围: 5-300
    /// 熔断器打开后，在此时间内所有请求将被直接拒绝。
    /// 熔断时间结束后，熔断器进入半开状态，允许少量请求通过以测试服务是否恢复。
    /// </remarks>
    public int CircuitBreakerDurationSeconds { get; set; } = 30;
}
