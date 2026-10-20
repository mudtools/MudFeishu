// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Slides;

/// <summary>
/// 创建幻灯片页面请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Slides")]
public class CreateSlideRequest
{
    /// <summary>
    /// <para>页面内容（content 为单个完整 &lt;slide&gt; XML 文档）</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("slide")]
    public Slide? Slide { get; set; }

    /// <summary>
    /// <para>插入到指定页面之前；不传时追加到末尾。空字符串会被判为未知页面，故不得传空值</para>
    /// <para>必填：否</para>
    /// <para>示例值：slidecn20m4XJ1hJXXxXXX</para>
    /// </summary>
    [JsonPropertyName("before_slide_id")]
    public string? BeforeSlideId { get; set; }
}
