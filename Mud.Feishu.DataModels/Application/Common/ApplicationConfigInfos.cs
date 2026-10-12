// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Application;

/// <summary>
/// 应用的事件订阅配置。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class ApplicationEventConfig
{
    /// <summary>
    /// <para>订阅方式。webhook：将事件推送至开发者配置的地址；websocket：使用长连接接收事件</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("subscription_type")]
    public string? SubscriptionType { get; set; }

    /// <summary>
    /// <para>订阅事件的服务器地址</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("request_url")]
    public string? RequestUrl { get; set; }

    /// <summary>
    /// <para>已订阅的事件列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("subscribed_events")]
    public List<string>? SubscribedEvents { get; set; }
}

/// <summary>
/// 应用的消息卡片回调配置。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class ApplicationCallbackConfig
{
    /// <summary>
    /// <para>回调方式。webhook：将回调推送至开发者配置的地址；websocket：使用长连接接收回调</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("callback_type")]
    public string? CallbackType { get; set; }

    /// <summary>
    /// <para>消息卡片请求网址</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("request_url")]
    public string? RequestUrl { get; set; }

    /// <summary>
    /// <para>已订阅的回调列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("subscribed_callbacks")]
    public List<string>? SubscribedCallbacks { get; set; }
}

/// <summary>
/// 应用的事件与消息卡片加密配置。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class ApplicationEncryptionConfig
{
    /// <summary>
    /// <para>Encrypt Key（加密密钥）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("encryption_key")]
    public string? EncryptionKey { get; set; }

    /// <summary>
    /// <para>Verification Token（校验令牌）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("verification_token")]
    public string? VerificationToken { get; set; }
}

/// <summary>
/// 应用的安全配置（重定向 URL、IP 白名单、可信域名等）。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class ApplicationSecurityConfig
{
    /// <summary>
    /// <para>重定向 URL 列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("redirect_urls")]
    public List<string>? RedirectUrls { get; set; }

    /// <summary>
    /// <para>IP 白名单列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("allowed_ips")]
    public List<string>? AllowedIps { get; set; }

    /// <summary>
    /// <para>H5 可信域名列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("h5_trusted_domains")]
    public List<string>? H5TrustedDomains { get; set; }

    /// <summary>
    /// <para>网页应用可信域名列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("web_view_trusted_domains")]
    public List<string>? WebViewTrustedDomains { get; set; }

    /// <summary>
    /// <para>协议名白名单列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("allowed_schemas")]
    public List<string>? AllowedSchemas { get; set; }

    /// <summary>
    /// <para>服务端域名白名单列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("allowed_server_domains")]
    public List<string>? AllowedServerDomains { get; set; }
}

/// <summary>
/// 应用的回调订阅信息（区分 webhook 与长连接方式）。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class ApplicationCallbackInfo
{
    /// <summary>
    /// <para>回调方式。webhook：推送至服务器地址；websocket：使用长连接接收</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("callback_type")]
    public string? CallbackType { get; set; }

    /// <summary>
    /// <para>回调请求地址</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("request_url")]
    public string? RequestUrl { get; set; }

    /// <summary>
    /// <para>已订阅的回调列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("subscribed_callbacks")]
    public List<string>? SubscribedCallbacks { get; set; }
}
