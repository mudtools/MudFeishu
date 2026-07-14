// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json.Serialization;

namespace Mud.Feishu.Webhook.Models;

/// <summary>
/// Webhook 错误响应 DTO
/// </summary>
public class WebhookErrorResponse
{
    /// <summary>
    /// 是否成功
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// 请求 ID
    /// </summary>
    [JsonPropertyName("request_id")]
    public string? RequestId { get; set; }

    /// <summary>
    /// 错误详情
    /// </summary>
    [JsonPropertyName("error")]
    public WebhookErrorDetail? Error { get; set; }
}

/// <summary>
/// Webhook 错误详情
/// </summary>
public class WebhookErrorDetail
{
    /// <summary>
    /// HTTP 状态码
    /// </summary>
    [JsonPropertyName("code")]
    public int Code { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Webhook 空成功响应 DTO（用于替代匿名 `new { }`）
/// </summary>
public class WebhookEmptyResponse
{
    /// <summary>
    /// 是否成功
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; } = true;
}