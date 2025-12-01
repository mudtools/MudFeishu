// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ChatGroupMenu;

/// <summary>
/// <para>菜单跳转链接</para>
/// </summary>
public class ChatMenuItemRedirectLink
{
    /// <summary>
    /// <para>公用跳转链接，必须以 http/https 开头。</para>
    /// <para>必填：否</para>
    /// <para>示例值：https://open.feishu.cn/</para>
    /// </summary>
    [JsonPropertyName("common_url")]
    public string? CommonUrl { get; set; }

    /// <summary>
    /// <para>iOS 端跳转链接，当该字段不设置时，iOS 端默认使用 `common_url` 值。必须以 http/https 开头。</para>
    /// <para>必填：否</para>
    /// <para>示例值：https://open.feishu.cn/</para>
    /// </summary>
    [JsonPropertyName("ios_url")]
    public string? IosUrl { get; set; }

    /// <summary>
    /// <para>Android 端跳转链接，当该字段不设置时，Android 端默认使用 `common_url` 值。必须以 http/https 开头。</para>
    /// <para>必填：否</para>
    /// <para>示例值：https://open.feishu.cn/</para>
    /// </summary>
    [JsonPropertyName("android_url")]
    public string? AndroidUrl { get; set; }

    /// <summary>
    /// <para>PC 端跳转链接，当该字段不设置时，PC 端默认使用 `common_url` 值。必须以 http/https 开头。</para>
    /// <para>**使用说明**：在 PC 端点击群菜单后，如果需要 url 对应的页面在飞书侧边栏展开，可以在 url 前加上 `https://applink.feishu.cn/client/web_url/open?mode=sidebar-semi&amp;url=`，例如 `https://applink.feishu.cn/client/web_url/open?mode=sidebar-semi&amp;url=https://open.feishu.cn/`</para>
    /// <para>必填：否</para>
    /// <para>示例值：https://open.feishu.cn/</para>
    /// </summary>
    [JsonPropertyName("pc_url")]
    public string? PcUrl { get; set; }

    /// <summary>
    /// <para>Web 端跳转链接，当该字段不设置时，Web 端默认使用 `common_url` 值。必须以 http/https 开头。</para>
    /// <para>必填：否</para>
    /// <para>示例值：https://open.feishu.cn/</para>
    /// </summary>
    [JsonPropertyName("web_url")]
    public string? WebUrl { get; set; }
}
