// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ChatGroupMenu;


/// <summary>
/// <para>菜单项信息</para>
/// </summary>
public class ChatMenuItem
{
    /// <summary>
    /// <para>一级菜单类型</para>
    /// <para>**注意**：</para>
    /// <para>- 如果一级菜单有二级菜单，则此一级菜单的值必须为 `NONE`。</para>
    /// <para>- 菜单类型创建后不可更改。</para>
    /// <para>必填：是</para>
    /// <para>示例值：NONE</para>
    /// <para>可选值：<list type="bullet">
    /// <item>NONE：无类型，如果需要在一级菜单内添加二级菜单，则该一级菜单需要设置为 NONE。</item>
    /// <item>REDIRECT_LINK：跳转链接类型，取该值时需要设置对应的跳转链接（redirect_link）。</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("action_type")]
    public string ActionType { get; set; } = string.Empty;

    /// <summary>
    /// <para>一级菜单的跳转链接</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("redirect_link")]
    public ChatMenuItemRedirectLink? RedirectLink { get; set; }


    /// <summary>
    /// <para>一级菜单图标的 key 值。通过 [上传图片](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/im-v1/image/create) 接口上传 message 类型图片获取 image_key，并传入该值。</para>
    /// <para>**注意**：如果一级菜单有二级菜单，则此一级菜单不能设置图标。</para>
    /// <para>必填：否</para>
    /// <para>示例值：img_v2_b0fbe905-7988-4282-b882-82edd010336j</para>
    /// </summary>
    [JsonPropertyName("image_key")]
    public string? ImageKey { get; set; }

    /// <summary>
    /// <para>菜单名称</para>
    /// <para>**注意**：一级、二级菜单名称字符数要在 1 ~ 120 范围内。</para>
    /// <para>必填：是</para>
    /// <para>示例值：群聊</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// <para>菜单国际化名称</para>
    /// <para>**注意**：一级、二级菜单名称字符数要在 1 ~ 120 范围内。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("i18n_names")]
    public I18nName? I18nNames { get; set; }
}
