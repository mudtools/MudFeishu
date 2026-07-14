// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Docx;

/// <summary>
/// <para>文档封面</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Docx")]
public class DocumentCoverInfo
{
    /// <summary>
    /// <para>图片 token</para>
    /// <para>必填：是</para>
    /// <para>示例值：O9E7bhebQooOzMx7yc7cSabcdef</para>
    /// <para>最大长度：27</para>
    /// <para>最小长度：27</para>
    /// </summary>
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// <para>展示视图在水平方向的偏移比例。其值为距离原图中心的水平方向偏移值 px / 原图宽度 px。 视图在原图中心时，该值为 0； 视图在原图右部分时，该值为正数； 视图在原图左部分时，改值为负数。</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// <para>默认值：0</para>
    /// </summary>
    [JsonPropertyName("offset_ratio_x")]
    public float? OffsetRatioX { get; set; }

    /// <summary>
    /// <para>展示视图在垂直方向的偏移比例。其值为距离原图中心的垂直方向偏移值 px / 原图高度 px。 视图在原图中心时，该值为 0； 视图在原图上部分时，该值为正数； 视图在原图下部分时，改值为负数。</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// <para>默认值：0</para>
    /// </summary>
    [JsonPropertyName("offset_ratio_y")]
    public float? OffsetRatioY { get; set; }
}
