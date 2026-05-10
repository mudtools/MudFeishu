// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Webhook.Configuration;

/// <summary>
/// 请求频率限制配置
/// </summary>
public class RateLimitOptions
{
    /// <summary>
    /// 是否启用请求频率限制
    /// </summary>
    public bool EnableRateLimit { get; set; } = false;

    /// <summary>
    /// 时间窗口大小（秒）
    /// </summary>
    public int WindowSizeSeconds { get; set; } = 60;

    /// <summary>
    /// 每个时间窗口内允许的最大请求数
    /// </summary>
    public int MaxRequestsPerWindow { get; set; } = 100;

    /// <summary>
    /// 是否基于 IP 限流
    /// </summary>
    public bool EnableIpRateLimit { get; set; } = true;

    /// <summary>
    /// 超出限制时的响应状态码
    /// </summary>
    public int TooManyRequestsStatusCode { get; set; } = 429;

    /// <summary>
    /// 超出限制时的响应消息
    /// </summary>
    public string TooManyRequestsMessage { get; set; } = "请求过于频繁，请稍后再试";

    /// <summary>
    /// 白名单 IP 列表（不参与限流）
    /// </summary>
    public HashSet<string> WhitelistIPs { get; set; } = [];

    /// <summary>
    /// 验证配置有效性
    /// </summary>
    /// <exception cref="InvalidOperationException">配置无效时抛出</exception>
    public void Validate()
    {
        if (EnableRateLimit)
        {
            if (WindowSizeSeconds < 1)
                throw new InvalidOperationException("WindowSizeSeconds 必须至少为 1 秒");

            if (MaxRequestsPerWindow < 1)
                throw new InvalidOperationException("MaxRequestsPerWindow 必须至少为 1");

            if (TooManyRequestsStatusCode < 400 || TooManyRequestsStatusCode > 599)
                throw new InvalidOperationException("TooManyRequestsStatusCode 必须在 400-599 之间");
        }
    }
}
