// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.VideoConferencing;



/// <summary>
/// <para>素材列表</para>
/// </summary>
public class RoomDigitalSignageMaterial
{
    /// <summary>
    /// <para>素材ID，当设置新素材时，无需传递该字段</para>
    /// <para>必填：否</para>
    /// <para>示例值：7847784676276</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>素材名称</para>
    /// <para>必填：否</para>
    /// <para>示例值：name</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>素材类型</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// <para>可选值：<list type="bullet">
    /// <item>1：图片</item>
    /// <item>2：视频</item>
    /// <item>3：GIF</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("material_type")]
    public int? MaterialType { get; set; }

    /// <summary>
    /// <para>素材url</para>
    /// <para>必填：否</para>
    /// <para>示例值：url</para>
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// <para>播放时长（单位sec），取值1~43200</para>
    /// <para>必填：否</para>
    /// <para>示例值：15</para>
    /// </summary>
    [JsonPropertyName("duration")]
    public int? Duration { get; set; }

    /// <summary>
    /// <para>素材封面url</para>
    /// <para>必填：否</para>
    /// <para>示例值：url</para>
    /// </summary>
    [JsonPropertyName("cover")]
    public string? Cover { get; set; }

    /// <summary>
    /// <para>素材文件md5</para>
    /// <para>必填：否</para>
    /// <para>示例值：md5</para>
    /// </summary>
    [JsonPropertyName("md5")]
    public string? Md5 { get; set; }

    /// <summary>
    /// <para>素材文件vid</para>
    /// <para>必填：否</para>
    /// <para>示例值：vid</para>
    /// </summary>
    [JsonPropertyName("vid")]
    public string? Vid { get; set; }

    /// <summary>
    /// <para>素材文件大小（单位byte）</para>
    /// <para>必填：否</para>
    /// <para>示例值：100</para>
    /// </summary>
    [JsonPropertyName("size")]
    public string? Size { get; set; }
}