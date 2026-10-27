// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Slides;

/// <summary>
/// 获取幻灯片页面截图响应体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Slides")]
public class ListSlideImagesResult
{
    /// <summary>
    /// <para>页面截图列表（data 为图片 Base64，体积可达兆级，不得直接回填模型上下文）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("slide_images")]
    public SlideImage[]? SlideImages { get; set; }

    /// <summary>
    /// <para>幻灯片总数量，用于分页统计场景（官方字段形态为字符串）</para>
    /// <para>必填：否</para>
    /// <para>示例值：10</para>
    /// </summary>
    [JsonPropertyName("total_count")]
    public string? TotalCount { get; set; }

    /// <summary>
    /// <para>演示文稿的版本标识，用于判断内容是否更新（官方字段形态为字符串）</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("revision")]
    public string? Revision { get; set; }
}
