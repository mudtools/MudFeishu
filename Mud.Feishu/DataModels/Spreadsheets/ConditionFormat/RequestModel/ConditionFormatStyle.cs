// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Spreadsheets;

/// <summary>条件格式的样式</summary>
public class ConditionFormatStyle
{
    /// <summary>
    /// <para>符合条件的数据的字体样式</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("font")]
    public ConditionFormatFont? Font { get; set; }

    /// <summary>
    /// <para>文本装饰。为文本设置下划线或删除线。可选值：</para>
    /// <para>- 0：无下划线和删除线</para>
    /// <para>- 1：下划线</para>
    /// <para>- 2：删除线</para>
    /// <para>- 3：同时设置下划线和删除线</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("text_decoration")]
    public int? TextDecoration { get; set; }

    /// <summary>
    /// <para>设置字体颜色。需填写字体颜色的十六进制代码。如 #faf1d1。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("fore_color")]
    public string? ForeColor { get; set; }

    /// <summary>
    /// <para>设置背景颜色。需填写背景颜色的十六进制代码。如 #faf1d1。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("back_color")]
    public string? BackColor { get; set; }
}