// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Application;

/// <summary>
/// 应用基础信息。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class ApplicationInfo
{
    /// <summary>
    /// <para>应用 App ID</para>
    /// <para>必填：是</para>
    /// <para>示例值：cli_9b445f5258795107</para>
    /// </summary>
    [JsonPropertyName("app_id")]
    public string? AppId { get; set; }

    /// <summary>
    /// <para>应用创建者的用户 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：ou_d317f090b7258ad0372aa53963cda70d</para>
    /// </summary>
    [JsonPropertyName("creator_id")]
    public string? CreatorId { get; set; }

    /// <summary>
    /// <para>应用状态。1：未启用；2：已启用</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("status")]
    public int? Status { get; set; }

    /// <summary>
    /// <para>应用场景类型。0：商店应用；1：企业自建应用</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("scene_type")]
    public int? SceneType { get; set; }

    /// <summary>
    /// <para>付费类型。0：免费；1：付费</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// </summary>
    [JsonPropertyName("payment_type")]
    public int? PaymentType { get; set; }

    /// <summary>
    /// <para>应用创建来源。可选值：unknown（未知）、developer_console（开发者后台）、base（Base）、app_engine（应用引擎）、bot_builder（机器人助手）、aily（Aily）</para>
    /// <para>必填：否</para>
    /// <para>示例值：base</para>
    /// </summary>
    [JsonPropertyName("create_source")]
    public string? CreateSource { get; set; }

    /// <summary>
    /// <para>重定向 URL 列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("redirect_urls")]
    public List<string>? RedirectUrls { get; set; }

    /// <summary>
    /// <para>线上版本 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：oav_d317f090b7258ad0372aa53963cda70d</para>
    /// </summary>
    [JsonPropertyName("online_version_id")]
    public string? OnlineVersionId { get; set; }

    /// <summary>
    /// <para>待审核版本 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：oav_d317f090b7258ad0372aa53963cda70d</para>
    /// </summary>
    [JsonPropertyName("unaudit_version_id")]
    public string? UnauditVersionId { get; set; }

    /// <summary>
    /// <para>应用名称</para>
    /// <para>必填：否</para>
    /// <para>示例值：应用名称</para>
    /// </summary>
    [JsonPropertyName("app_name")]
    public string? AppName { get; set; }

    /// <summary>
    /// <para>应用头像图片链接</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// <para>应用描述</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// <para>应用已申请的权限列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("scopes")]
    public List<AppScope>? Scopes { get; set; }

    /// <summary>
    /// <para>应用管理后台链接</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("back_home_url")]
    public string? BackHomeUrl { get; set; }

    /// <summary>
    /// <para>应用多语言名称与描述列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("i18n")]
    public List<AppI18nInfo>? I18n { get; set; }

    /// <summary>
    /// <para>应用主语言。可选值：zh_cn、en_us、ja_jp</para>
    /// <para>必填：否</para>
    /// <para>示例值：zh_cn</para>
    /// </summary>
    [JsonPropertyName("primary_language")]
    public string? PrimaryLanguage { get; set; }

    /// <summary>
    /// <para>应用管理后台的应用分类信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("common_categories")]
    public List<string>? CommonCategories { get; set; }

    /// <summary>
    /// <para>应用所有者信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("owner")]
    public ApplicationOwner? Owner { get; set; }

    /// <summary>
    /// <para>移动端默认应用能力。可选值：bot（机器人）、gadget（小程序）、web_app（网页应用）</para>
    /// <para>必填：否</para>
    /// <para>示例值：bot</para>
    /// </summary>
    [JsonPropertyName("mobile_default_ability")]
    public string? MobileDefaultAbility { get; set; }

    /// <summary>
    /// <para>桌面端默认应用能力。可选值：bot（机器人）、gadget（小程序）、web_app（网页应用）</para>
    /// <para>必填：否</para>
    /// <para>示例值：bot</para>
    /// </summary>
    [JsonPropertyName("pc_default_ability")]
    public string? PcDefaultAbility { get; set; }

    /// <summary>
    /// <para>应用凭据（App Secret）。仅在具备相应权限时返回，注意妥善保管，切勿记录到日志或暴露</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("secret")]
    public string? Secret { get; set; }

    /// <summary>
    /// <para>应用的事件订阅配置</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("event")]
    public ApplicationEventConfig? Event { get; set; }

    /// <summary>
    /// <para>应用的消息卡片回调配置</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("callback")]
    public ApplicationCallbackConfig? Callback { get; set; }

    /// <summary>
    /// <para>应用的事件与消息卡片加密配置</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("encryption")]
    public ApplicationEncryptionConfig? Encryption { get; set; }

    /// <summary>
    /// <para>应用的安全配置</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("security")]
    public ApplicationSecurityConfig? Security { get; set; }

    /// <summary>
    /// <para>是否允许刷新 user_access_token（refresh_token 免审续期）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("allow_refresh_token")]
    public bool? AllowRefreshToken { get; set; }

    /// <summary>
    /// <para>应用的回调订阅信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("callback_info")]
    public ApplicationCallbackInfo? CallbackInfo { get; set; }
}
