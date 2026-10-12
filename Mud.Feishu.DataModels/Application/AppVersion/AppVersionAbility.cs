// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Application;

/// <summary>
/// 应用版本的应用能力配置。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class AppVersionAbility
{
    /// <summary>
    /// <para>小程序能力配置</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("gadget")]
    public AppVersionAbilityGadget? Gadget { get; set; }

    /// <summary>
    /// <para>网页应用能力配置</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("web_app")]
    public AppVersionAbilityWebApp? WebApp { get; set; }

    /// <summary>
    /// <para>机器人能力配置</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("bot")]
    public AppVersionAbilityBot? Bot { get; set; }

    /// <summary>
    /// <para>小组件能力配置列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("workplace_widgets")]
    public List<AppVersionAbilityWorkplaceWidget>? WorkplaceWidgets { get; set; }

    /// <summary>
    /// <para>工作台导航样式配置</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("navigate")]
    public AppVersionAbilityNavigate? Navigate { get; set; }

    /// <summary>
    /// <para>云文档能力配置</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("cloud_doc")]
    public AppVersionAbilityCloudDoc? CloudDoc { get; set; }

    /// <summary>
    /// <para>文档块能力配置列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("docs_blocks")]
    public List<AppVersionAbilityDocsBlock>? DocsBlocks { get; set; }

    /// <summary>
    /// <para>消息操作（消息菜单）能力配置</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("message_action")]
    public AppVersionAbilityMessageAction? MessageAction { get; set; }

    /// <summary>
    /// <para>加号菜单能力配置</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("plus_menu")]
    public AppVersionAbilityPlusMenu? PlusMenu { get; set; }
}

/// <summary>
/// 应用版本的小程序能力配置。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class AppVersionAbilityGadget
{
    /// <summary>
    /// <para>桌面端小程序模式。0：未开启；1：开启</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("enable_pc_mode")]
    public int? EnablePcMode { get; set; }

    /// <summary>
    /// <para>小程序 PC 端适配的页面 schema 链接列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("schema_urls")]
    public List<string>? SchemaUrls { get; set; }

    /// <summary>
    /// <para>桌面端是否使用移动端版本。true：使用；false：不使用</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("pc_use_mobile_pkg")]
    public bool? PcUseMobilePkg { get; set; }

    /// <summary>
    /// <para>桌面端小程序版本号</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("pc_version")]
    public string? PcVersion { get; set; }

    /// <summary>
    /// <para>移动端小程序版本号</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("mobile_version")]
    public string? MobileVersion { get; set; }

    /// <summary>
    /// <para>移动端小程序最低兼容的飞书客户端版本</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("mobile_min_lark_version")]
    public string? MobileMinLarkVersion { get; set; }

    /// <summary>
    /// <para>桌面端小程序最低兼容的飞书客户端版本</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("pc_min_lark_version")]
    public string? PcMinLarkVersion { get; set; }
}

/// <summary>
/// 应用版本的网页应用能力配置。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class AppVersionAbilityWebApp
{
    /// <summary>
    /// <para>桌面端主页地址</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("pc_url")]
    public string? PcUrl { get; set; }

    /// <summary>
    /// <para>移动端主页地址</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("mobile_url")]
    public string? MobileUrl { get; set; }
}

/// <summary>
/// 应用版本的机器人能力配置。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class AppVersionAbilityBot
{
    /// <summary>
    /// <para>机器人消息卡片请求网址</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("card_request_url")]
    public string? CardRequestUrl { get; set; }

    /// <summary>
    /// <para>是否开启机器人菜单。true：开启；false：关闭</para>
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
}

/// <summary>
/// 应用版本的小组件能力配置。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class AppVersionAbilityWorkplaceWidget
{
    /// <summary>
    /// <para>小组件最低兼容的飞书客户端版本</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("min_lark_version")]
    public string? MinLarkVersion { get; set; }
}

/// <summary>
/// 应用版本的工作台导航样式配置。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class AppVersionAbilityNavigate
{
    /// <summary>
    /// <para>桌面端导航样式</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("pc")]
    public AppVersionAbilityNavigateItem? Pc { get; set; }

    /// <summary>
    /// <para>移动端导航样式</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("mobile")]
    public AppVersionAbilityNavigateItem? Mobile { get; set; }
}

/// <summary>
/// 应用版本的工作台导航样式配置项。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class AppVersionAbilityNavigateItem
{
    /// <summary>
    /// <para>导航样式版本</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    /// <summary>
    /// <para>导航图片链接</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("image_url")]
    public string? ImageUrl { get; set; }

    /// <summary>
    /// <para>悬浮态导航图片链接</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("hover_image_url")]
    public string? HoverImageUrl { get; set; }
}

/// <summary>
/// 应用版本的云文档能力配置。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class AppVersionAbilityCloudDoc
{
    /// <summary>
    /// <para>云空间文档的地址</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("space_url")]
    public string? SpaceUrl { get; set; }

    /// <summary>
    /// <para>云文档多语言信息列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("i18n")]
    public List<AppVersionAbilityCloudDocI18n>? I18n { get; set; }

    /// <summary>
    /// <para>云文档图标链接</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("icon_url")]
    public string? IconUrl { get; set; }

    /// <summary>
    /// <para>云文档模式</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("mode")]
    public int? Mode { get; set; }
}

/// <summary>
/// 应用版本云文档能力的多语言信息。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class AppVersionAbilityCloudDocI18n
{
    /// <summary>
    /// <para>语言标签</para>
    /// <para>必填：是</para>
    /// <para>示例值：zh_cn</para>
    /// </summary>
    [JsonPropertyName("i18n_key")]
    public string? I18nKey { get; set; }

    /// <summary>
    /// <para>名称</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>使用说明</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("read_description")]
    public string? ReadDescription { get; set; }

    /// <summary>
    /// <para>写入说明</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("write_description")]
    public string? WriteDescription { get; set; }
}

/// <summary>
/// 应用版本的文档块能力配置。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class AppVersionAbilityDocsBlock
{
    /// <summary>
    /// <para>文档块类型 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("block_type_id")]
    public string? BlockTypeId { get; set; }

    /// <summary>
    /// <para>文档块多语言信息列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("i18n")]
    public List<AppVersionAbilityDocsBlockI18n>? I18n { get; set; }

    /// <summary>
    /// <para>移动端图标链接</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("mobile_icon_url")]
    public string? MobileIconUrl { get; set; }

    /// <summary>
    /// <para>桌面端图标链接</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("pc_icon_url")]
    public string? PcIconUrl { get; set; }
}

/// <summary>
/// 应用版本文档块能力的多语言信息。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class AppVersionAbilityDocsBlockI18n
{
    /// <summary>
    /// <para>语言标签</para>
    /// <para>必填：否</para>
    /// <para>示例值：zh_cn</para>
    /// </summary>
    [JsonPropertyName("i18n_key")]
    public string? I18nKey { get; set; }

    /// <summary>
    /// <para>名称</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

/// <summary>
/// 应用版本的消息操作（消息菜单）能力配置。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class AppVersionAbilityMessageAction
{
    /// <summary>
    /// <para>桌面端应用链接</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("pc_app_link")]
    public string? PcAppLink { get; set; }

    /// <summary>
    /// <para>移动端应用链接</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("mobile_app_link")]
    public string? MobileAppLink { get; set; }

    /// <summary>
    /// <para>消息操作多语言信息列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("i18n")]
    public List<AppVersionAbilityMessageActionI18n>? I18n { get; set; }
}

/// <summary>
/// 应用版本消息操作能力的多语言信息。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class AppVersionAbilityMessageActionI18n
{
    /// <summary>
    /// <para>语言标签</para>
    /// <para>必填：否</para>
    /// <para>示例值：zh_cn</para>
    /// </summary>
    [JsonPropertyName("i18n_key")]
    public string? I18nKey { get; set; }

    /// <summary>
    /// <para>名称</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

/// <summary>
/// 应用版本的加号菜单能力配置。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class AppVersionAbilityPlusMenu
{
    /// <summary>
    /// <para>桌面端应用链接</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("pc_app_link")]
    public string? PcAppLink { get; set; }

    /// <summary>
    /// <para>移动端应用链接</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("mobile_app_link")]
    public string? MobileAppLink { get; set; }
}
