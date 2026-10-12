// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Application;

/// <summary>
/// 更新应用能力请求体。未传入的参数保持不变，仅更新传入的参数。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class UpdateApplicationAbilityRequest
{
    /// <summary>
    /// <para>网页应用能力配置</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("web_app")]
    public AppAbilityWebApp? WebApp { get; set; }

    /// <summary>
    /// <para>机器人能力配置</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("bot")]
    public AppAbilityBot? Bot { get; set; }
}

/// <summary>
/// 应用网页应用能力配置。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class AppAbilityWebApp
{
    /// <summary>
    /// <para>是否开启网页应用能力</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("enable")]
    public bool? Enable { get; set; }

    /// <summary>
    /// <para>桌面端主页地址</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("pc_url")]
    public string? PcUrl { get; set; }

    /// <summary>
    /// <para>桌面端链接打开方式。new_tab：新开标签页；browser：新开浏览器窗口</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("pc_new_page_open_mode")]
    public string? PcNewPageOpenMode { get; set; }

    /// <summary>
    /// <para>移动端主页地址</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("mobile_url")]
    public string? MobileUrl { get; set; }
}

/// <summary>
/// 应用机器人能力配置。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class AppAbilityBot
{
    /// <summary>
    /// <para>是否开启机器人能力</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("enable")]
    public bool? Enable { get; set; }

    /// <summary>
    /// <para>机器人消息卡片请求网址</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("message_card_callback_url")]
    public string? MessageCardCallbackUrl { get; set; }

    /// <summary>
    /// <para>机器人开启引导语多语言配置</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("i18ns")]
    public List<AppAbilityBotI18nInfo>? I18ns { get; set; }

    /// <summary>
    /// <para>是否开启机器人菜单</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("bot_menu_enable")]
    public bool? BotMenuEnable { get; set; }

    /// <summary>
    /// <para>机器人菜单列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("bot_menus")]
    public List<BotMenu>? BotMenus { get; set; }

    /// <summary>
    /// <para>机器人菜单展示策略。1：菜单与聊天页面平铺；2：菜单折叠收起</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("bot_menu_display_strategy")]
    public int? BotMenuDisplayStrategy { get; set; }

    /// <summary>
    /// <para>是否允许其他应用将机器人添加进群</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("allow_invited_to_group_by_other_app_switch_open")]
    public bool? AllowInvitedToGroupByOtherAppSwitchOpen { get; set; }
}

/// <summary>
/// 机器人开启引导语多语言配置。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class AppAbilityBotI18nInfo
{
    /// <summary>
    /// <para>语言标签，如 zh_cn、en_us、ja_jp</para>
    /// <para>必填：是</para>
    /// <para>示例值：zh_cn</para>
    /// </summary>
    [JsonPropertyName("i18n_key")]
    public string? I18nKey { get; set; }

    /// <summary>
    /// <para>机器人开启引导语</para>
    /// <para>必填：是</para>
    /// <para>示例值：如何使用机器人</para>
    /// </summary>
    [JsonPropertyName("get_started_desc")]
    public string? GetStartedDesc { get; set; }
}
