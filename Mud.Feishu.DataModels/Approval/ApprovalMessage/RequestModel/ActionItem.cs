// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ApprovalMessage;

/// <summary>
/// 操作区，最多可设置 2 个操作按钮。
/// </summary>
public class ActionItem
{
    /// <summary>
    /// 操作类型。示例值：@i18n@7
    /// <list type="bullet">
    /// <item>第一个按钮的 action_name 固定取值 DETAIL。</item>
    /// <item>第二个按钮的 action_name 自定义配置。这里传入的是国际化文案 Key（即 i18n_resources.texts 参数中的 Key），还需要在 i18n_resources.texts 参数中以 Key:Value 格式进行赋值。Key 需要以 @i18n@ 开头。</item>
    /// </list>
    /// </summary>
    [JsonPropertyName("action_name")]
    public string ActionName { get; set; } = string.Empty;

    /// <summary>
    /// 默认链接，不同的端配置不同的操作跳转 url，链接必须包含 schema 才能生效。
    /// </summary>
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Android 端的跳转链接。
    /// </summary>
    [JsonPropertyName("android_url")]
    public string AndroidUrl { get; set; } = string.Empty;

    /// <summary>
    /// iOS 端的跳转链接。
    /// </summary>
    [JsonPropertyName("ios_url")]
    public string IosUrl { get; set; } = string.Empty;

    /// <summary>
    /// PC 端的跳转链接。
    /// </summary>
    [JsonPropertyName("pc_url")]
    public string PcUrl { get; set; } = string.Empty;
}
