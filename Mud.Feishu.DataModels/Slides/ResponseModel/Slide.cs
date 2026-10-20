// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Slides;

/// <summary>
/// 幻灯片页面内容
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Slides")]
public class Slide
{
    /// <summary>
    /// <para>幻灯片页面的 short ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：sld_xxx</para>
    /// </summary>
    [JsonPropertyName("slide_id")]
    public string? SlideId { get; set; }

    /// <summary>
    /// <para>幻灯片页面的 XML 内容</para>
    /// <para>必填：否（创建页面时必填）</para>
    /// <para>示例值：&lt;slide xmlns="http://www.larkoffice.com/sml/2.0"&gt;&lt;data&gt;&lt;/data&gt;&lt;/slide&gt;</para>
    /// </summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }
}
