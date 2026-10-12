// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Application;

/// <summary>
/// 机器人菜单项。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class BotMenu
{
    /// <summary>
    /// <para>菜单 ID，更新菜单时必填</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("menu_id")]
    public string? MenuId { get; set; }

    /// <summary>
    /// <para>父级菜单 ID，仅一级菜单需要填</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("parent_menu_id")]
    public string? ParentMenuId { get; set; }

    /// <summary>
    /// <para>菜单排序，同层级内取值 0～99</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("sort")]
    public int? Sort { get; set; }

    /// <summary>
    /// <para>菜单默认名称</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("default_name")]
    public string? DefaultName { get; set; }

    /// <summary>
    /// <para>菜单多语言名称，键为语言标签（如 zh_cn、en_us）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("i18n_name")]
    public Dictionary<string, string>? I18nName { get; set; }

    /// <summary>
    /// <para>菜单跳转链接</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("redirect_link")]
    public BotMenuRedirectLink? RedirectLink { get; set; }

    /// <summary>
    /// <para>菜单事件 key</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("event_key")]
    public string? EventKey { get; set; }

    /// <summary>
    /// <para>菜单图标 file_key</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("icon_file_key")]
    public string? IconFileKey { get; set; }

    /// <summary>
    /// <para>菜单 UD 图标配置</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("ud_icon")]
    public UdIcon? UdIcon { get; set; }

    /// <summary>
    /// <para>菜单内容类型</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("menu_content_type")]
    public int? MenuContentType { get; set; }
}

/// <summary>
/// 机器人菜单跳转链接。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class BotMenuRedirectLink
{
    /// <summary>
    /// <para>桌面端跳转链接</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("pc_url")]
    public string? PcUrl { get; set; }

    /// <summary>
    /// <para>移动端跳转链接</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("mobile_url")]
    public string? MobileUrl { get; set; }
}

/// <summary>
/// 机器人菜单 UD 图标配置。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class UdIcon
{
    /// <summary>
    /// <para>图标 token</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("token")]
    public string? Token { get; set; }

    /// <summary>
    /// <para>图标颜色</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("color")]
    public string? Color { get; set; }
}
