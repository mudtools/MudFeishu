// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Slides;

/// <summary>
/// 获取幻灯片页面截图请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Slides")]
public class ListSlideImagesRequest
{
    /// <summary>
    /// <para>幻灯片页面 short ID 列表</para>
    /// <para>必填：否（slide_ids、slide_numbers 至少提供一个）</para>
    /// </summary>
    [JsonPropertyName("slide_ids")]
    public string[]? SlideIds { get; set; }

    /// <summary>
    /// <para>幻灯片页号列表</para>
    /// <para>必填：否（slide_ids、slide_numbers 至少提供一个）</para>
    /// </summary>
    [JsonPropertyName("slide_numbers")]
    public int[]? SlideNumbers { get; set; }
}
