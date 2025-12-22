// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Webhook.Demo.Services;

namespace Mud.Feishu.Webhook.Demo.Models;

/// <summary>
/// Webhook状态响应
/// </summary>
public class WebhookStatus
{
    public bool IsConfigured { get; init; }
    public WebhookConfiguration Configuration { get; init; } = null!;
    public EventStatistics Statistics { get; init; } = null!;
    public DateTime ServerTime { get; init; }
}

/// <summary>
/// Webhook配置信息
/// </summary>
public class WebhookConfiguration
{
    public string RoutePrefix { get; init; } = string.Empty;
    public bool AutoRegisterEndpoint { get; init; }
    public bool EnableRequestLogging { get; init; }
    public bool EnableExceptionHandling { get; init; }
    public int EventHandlingTimeoutMs { get; init; }
    public int MaxConcurrentEvents { get; init; }
    public string[] AllowedHttpMethods { get; init; } = Array.Empty<string>();
    public long MaxRequestBodySize { get; init; }
    public bool ValidateSourceIP { get; init; }
    public string[] AllowedSourceIPs { get; init; } = Array.Empty<string>();
}

/// <summary>
/// 模拟事件请求
/// </summary>
public class MockEventRequest
{
    public string EventType { get; init; } = string.Empty;
    public Dictionary<string, object> Data { get; init; } = new();
    public string? UserId { get; init; }
    public string? TenantKey { get; init; }
    public string? AppId { get; init; }
}

/// <summary>
/// Webhook事件测试请求
/// </summary>
public class WebhookTestRequest
{
    public string EventType { get; init; } = string.Empty;
    public string TestMessage { get; init; } = string.Empty;
    public Dictionary<string, object>? CustomData { get; init; }
}

/// <summary>
/// 验证配置请求
/// </summary>
public class VerifyConfigurationRequest
{
    public string VerificationToken { get; init; } = string.Empty;
    public string EncryptKey { get; init; } = string.Empty;
    public string RoutePrefix { get; init; } = "feishu/Webhook";
}

/// <summary>
/// Webhook测试结果
/// </summary>
public class WebhookTestResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public object? Data { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public long ProcessingTimeMs { get; init; }
}

/// <summary>
/// 事件处理统计信息
/// </summary>
public class EventProcessingStatistics
{
    /// <summary>
    /// 总事件处理数
    /// </summary>
    public int TotalEvents { get; set; }

    /// <summary>
    /// 成功处理的事件数
    /// </summary>
    public int SuccessfulEvents { get; set; }

    /// <summary>
    /// 失败的事件数
    /// </summary>
    public int FailedEvents { get; set; }

    /// <summary>
    /// 按事件类型统计
    /// </summary>
    public Dictionary<string, int> EventTypeCounts { get; set; } = new();

    /// <summary>
    /// 平均处理时间（毫秒）
    /// </summary>
    public double AverageProcessingTimeMs { get; set; }

    /// <summary>
    /// 最后处理时间
    /// </summary>
    public DateTime? LastProcessTime { get; set; }

    /// <summary>
    /// 最近处理的事件列表
    /// </summary>
    public List<RecentEvent> RecentEvents { get; set; } = new();
}

/// <summary>
/// 最近事件记录
/// </summary>
public class RecentEvent
{
    public string EventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTime ProcessTime { get; set; }
    public bool Success { get; set; }
    public long ProcessingTimeMs { get; set; }
    public string? ErrorMessage { get; set; }
}