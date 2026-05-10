// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Webhook;

/// <summary>
/// 安全审计服务接口
/// </summary>
public interface ISecurityAuditService
{
    /// <summary>
    /// 记录安全验证失败事件
    /// </summary>
    /// <param name="eventType">安全事件类型</param>
    /// <param name="clientIp">客户端IP地址</param>
    /// <param name="requestPath">请求路径</param>
    /// <param name="details">详细信息</param>
    /// <param name="requestId">请求ID</param>
    /// <param name="appKey">应用标识（可选，多应用模式下使用）</param>
    Task LogSecurityFailureAsync(
        SecurityEventType eventType,
        string clientIp,
        string requestPath,
        string details,
        string? requestId = null,
        string? appKey = null);

    /// <summary>
    /// 记录安全验证成功事件
    /// </summary>
    /// <param name="eventType">安全事件类型</param>
    /// <param name="clientIp">客户端IP地址</param>
    /// <param name="requestPath">请求路径</param>
    /// <param name="details">详细信息</param>
    /// <param name="requestId">请求ID</param>
    /// <param name="appKey">应用标识（可选，多应用模式下使用）</param>
    Task LogSecuritySuccessAsync(
        SecurityEventType eventType,
        string clientIp,
        string requestPath,
        string details,
        string? requestId = null,
        string? appKey = null);
}

/// <summary>
/// 安全事件类型
/// </summary>
public enum SecurityEventType
{
    /// <summary>
    /// 签名验证
    /// </summary>
    SignatureValidation,

    /// <summary>
    /// 时间戳验证
    /// </summary>
    TimestampValidation,

    /// <summary>
    /// IP验证
    /// </summary>
    IpValidation,

    /// <summary>
    /// 订阅验证
    /// </summary>
    SubscriptionValidation,

    /// <summary>
    /// Content-Type 验证
    /// </summary>
    InvalidContentType,

    /// <summary>
    /// 请求频率限制
    /// </summary>
    RateLimitExceeded,

    /// <summary>
    /// 请求大小限制
    /// </summary>
    RequestSizeLimit,

    /// <summary>
    /// 威胁检测
    /// </summary>
    ThreatDetection,

    /// <summary>
    /// 其他安全事件
    /// </summary>
    Other
}