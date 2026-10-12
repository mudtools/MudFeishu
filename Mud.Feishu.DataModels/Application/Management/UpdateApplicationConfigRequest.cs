// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Application;

/// <summary>
/// 更新应用开发配置请求体（通讯录、安全、可见性等）。未传入的参数保持不变，仅更新传入的参数。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class UpdateApplicationConfigRequest
{
    /// <summary>
    /// <para>权限配置</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("scope")]
    public AppConfigScope? Scope { get; set; }

    /// <summary>
    /// <para>事件配置</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("event")]
    public AppConfigEvent? Event { get; set; }

    /// <summary>
    /// <para>安全配置</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("security")]
    public AppConfigSecurity? Security { get; set; }

    /// <summary>
    /// <para>可用范围配置</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("visibility")]
    public AppConfigVisibility? Visibility { get; set; }

    /// <summary>
    /// <para>通讯录权限范围配置</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("contacts")]
    public AppConfigContactsRange? Contacts { get; set; }

    /// <summary>
    /// <para>事件与回调加密策略配置</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("event_and_callback_encrypt_strategy")]
    public EventAndCallbackEncryptStrategy? EventAndCallbackEncryptStrategy { get; set; }

    /// <summary>
    /// <para>回调配置</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("callback")]
    public AppConfigCallback? Callback { get; set; }
}

/// <summary>
/// 应用权限（scope）增删配置。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class AppConfigScope
{
    /// <summary>
    /// <para>新增的权限列表，长度范围：0 ～ 100</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("add_scopes")]
    public List<AppConfigScopeItem>? AddScopes { get; set; }

    /// <summary>
    /// <para>移除的权限列表，长度范围：0 ～ 100</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("remove_scopes")]
    public List<AppConfigScopeItem>? RemoveScopes { get; set; }
}

/// <summary>
/// 应用权限条目。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class AppConfigScopeItem
{
    /// <summary>
    /// <para>权限名称</para>
    /// <para>必填：是</para>
    /// <para>示例值：contact:user.base:readonly</para>
    /// </summary>
    [JsonPropertyName("scope_name")]
    public string? ScopeName { get; set; }

    /// <summary>
    /// <para>权限的令牌类型。user：用户身份申请；tenant：应用身份申请</para>
    /// <para>必填：是</para>
    /// <para>示例值：tenant</para>
    /// </summary>
    [JsonPropertyName("token_type")]
    public string? TokenType { get; set; }
}

/// <summary>
/// 应用事件订阅配置。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class AppConfigEvent
{
    /// <summary>
    /// <para>订阅方式。webhook：将事件推送至开发者配置的地址；websocket：使用长连接接收事件</para>
    /// <para>必填：是</para>
    /// <para>示例值：websocket</para>
    /// </summary>
    [JsonPropertyName("subscription_type")]
    public string? SubscriptionType { get; set; }

    /// <summary>
    /// <para>订阅事件的服务器地址，订阅方式为 webhook 时必填</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("request_url")]
    public string? RequestUrl { get; set; }

    /// <summary>
    /// <para>新增订阅的事件列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("add_events")]
    public List<string>? AddEvents { get; set; }

    /// <summary>
    /// <para>移除订阅的事件列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("remove_events")]
    public List<string>? RemoveEvents { get; set; }
}

/// <summary>
/// 应用安全配置（新增/移除重定向 URL、IP 白名单、可信域名等）。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class AppConfigSecurity
{
    /// <summary>
    /// <para>新增的安全配置项</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("add")]
    public AppConfigSecurityItem? Add { get; set; }

    /// <summary>
    /// <para>移除的安全配置项</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("remove")]
    public AppConfigSecurityItem? Remove { get; set; }

    /// <summary>
    /// <para>是否允许刷新 user_access_token（refresh_token 免审续期）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("allow_refresh_token")]
    public bool? AllowRefreshToken { get; set; }
}

/// <summary>
/// 应用安全配置项。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class AppConfigSecurityItem
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
/// 应用可用范围配置。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class AppConfigVisibility
{
    /// <summary>
    /// <para>是否对租户内全员可用</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("is_visible_to_all")]
    public bool? IsVisibleToAll { get; set; }

    /// <summary>
    /// <para>可用范围成员列表，is_visible_to_all 为 false 时生效</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("visible_list")]
    public AppVisibleList? VisibleList { get; set; }
}

/// <summary>
/// 应用通讯录权限范围配置。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class AppConfigContactsRange
{
    /// <summary>
    /// <para>通讯录权限范围类型</para>
    /// <para>必填：是</para>
    /// <para>可选值：equal_to_availability（与应用可用范围一致）、some（部分成员）、all（全部成员）</para>
    /// <para>示例值：some</para>
    /// </summary>
    [JsonPropertyName("contacts_range_type")]
    public string? ContactsRangeType { get; set; }

    /// <summary>
    /// <para>可见范围成员列表，contacts_range_type 为 some 时生效</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("visible_list")]
    public AppVisibleList? VisibleList { get; set; }
}

/// <summary>
/// 事件与回调加密策略配置。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class EventAndCallbackEncryptStrategy
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
/// 应用消息卡片回调配置。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class AppConfigCallback
{
    /// <summary>
    /// <para>回调方式。webhook：将回调推送至开发者配置的地址；websocket：使用长连接接收回调</para>
    /// <para>必填：是</para>
    /// <para>示例值：websocket</para>
    /// </summary>
    [JsonPropertyName("callback_type")]
    public string? CallbackType { get; set; }

    /// <summary>
    /// <para>消息卡片请求网址，回调方式为 webhook 时必填</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("request_url")]
    public string? RequestUrl { get; set; }

    /// <summary>
    /// <para>新增订阅的回调列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("add_callbacks")]
    public List<string>? AddCallbacks { get; set; }

    /// <summary>
    /// <para>移除订阅的回调列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("remove_callbacks")]
    public List<string>? RemoveCallbacks { get; set; }
}
