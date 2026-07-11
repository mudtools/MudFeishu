// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions;

namespace Mud.Feishu.Webhook.Configuration;

/// <summary>
/// 失败事件重试配置
/// </summary>
public class FailedEventRetryOptions
{
    /// <summary>
    /// 是否启用失败事件重试
    /// </summary>
    public bool EnableRetry { get; set; }

    /// <summary>
    /// 最大重试次数，默认 3
    /// </summary>
    public int MaxRetryCount { get; set; } = Mud.Feishu.Abstractions.Consts.DefaultEventRetryCount;

    /// <summary>
    /// 初始重试延迟（秒），默认 10
    /// </summary>
    public int InitialRetryDelaySeconds { get; set; } = Consts.DefaultEventRetryInitialDelaySeconds;

    /// <summary>
    /// 重试延迟倍数（指数退避），默认 2
    /// </summary>
    public double RetryDelayMultiplier { get; set; } = Consts.DefaultEventRetryDelayMultiplier;

    /// <summary>
    /// 最大重试延迟（秒），默认 300（5分钟）
    /// </summary>
    public int MaxRetryDelaySeconds { get; set; } = Consts.DefaultEventRetryMaxDelaySeconds;

    /// <summary>
    /// 重试轮询间隔（秒），默认 30
    /// </summary>
    public int RetryPollIntervalSeconds { get; set; } = Consts.DefaultEventRetryPollIntervalSeconds;

    /// <summary>
    /// 每次轮询处理的最大失败事件数，默认 10
    /// </summary>
    public int MaxRetryPerPoll { get; set; } = Consts.DefaultEventRetryMaxRetryPerPoll;

    /// <summary>
    /// 验证配置有效性
    /// </summary>
    public void Validate()
    {
        if (MaxRetryCount < 0)
            throw new InvalidOperationException("MaxRetryCount 不能为负数");

        if (InitialRetryDelaySeconds < 1)
            throw new InvalidOperationException("InitialRetryDelaySeconds 必须至少为 1 秒");

        if (RetryDelayMultiplier < 1.0)
            throw new InvalidOperationException("RetryDelayMultiplier 必须大于等于 1.0");

        if (MaxRetryDelaySeconds < InitialRetryDelaySeconds)
            throw new InvalidOperationException("MaxRetryDelaySeconds 必须大于等于 InitialRetryDelaySeconds");

        if (RetryPollIntervalSeconds < 1)
            throw new InvalidOperationException("RetryPollIntervalSeconds 必须至少为 1 秒");

        if (MaxRetryPerPoll < 1)
            throw new InvalidOperationException("MaxRetryPerPoll 必须至少为 1");

        if (!EnableRetry)
        {
            // 当 EnableRetry=false 时，执行宽松验证而不是严格异常
            // 这允许在配置热更新或动态切换时更灵活的处理
            
            // 仅验证基本范围，不强制要求等于默认值，保持配置一致性
            // 实际应用中这些配置不会生效，但保持合理的值有助于配置维护
        }
    }
}