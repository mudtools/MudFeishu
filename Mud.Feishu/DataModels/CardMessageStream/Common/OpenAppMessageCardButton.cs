// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.CardMessageStream;


/// <summary>
/// <para>按钮组合</para>
/// </summary>
public class OpenAppMessageCardButton
{
    /// <summary>
    /// <para>跳转 URL（仅支持 https 协议）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("multi_url")]
    public OpenAppMessageCardUrl? MultiUrl { get; set; }

    /// <summary>
    /// <para>交互类型（按钮交互方式可配置跳转 URL 页面，也可配置 webhook 回调）</para>
    /// <para>必填：是</para>
    /// <para>示例值：url_page</para>
    /// <para>可选值：<list type="bullet">
    /// <item>url_page：URL 页面</item>
    /// <item>webhook：回调</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("action_type")]
    public string ActionType { get; set; } = string.Empty;

    /// <summary>
    /// <para>文字</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("text")]
    public OpenAppMessageCardText Text { get; set; } = new();

    /// <summary>
    /// <para>按钮类型</para>
    /// <para>必填：否</para>
    /// <para>示例值：default</para>
    /// <para>可选值：<list type="bullet">
    /// <item>default：默认</item>
    /// <item>primary：主要</item>
    /// <item>success：成功</item>
    /// </list></para>
    /// <para>默认值：default</para>
    /// </summary>
    [JsonPropertyName("button_type")]
    public string? ButtonType { get; set; }

    /// <summary>
    /// <para>action 字典</para>
    /// <para>必填：否</para>
    /// <para>示例值：{"foo": "bar"}</para>
    /// </summary>
    [JsonPropertyName("action_map")]
    public Dictionary<string, string>? ActionMap { get; set; }
}