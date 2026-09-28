// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Okr;

/// <summary>
/// 进展记录图片元素
/// </summary>
public class ContentImageItem
{
    /// <summary>
    /// <para>图片 token，通过上传进展记录图片接口获取</para>
    /// <para>示例值：boxcnOj88GDkmWGm2zsTyCBqoLb</para>
    /// </summary>
    [JsonPropertyName("fileToken")]
    public string? FileToken { get; set; }

    /// <summary>
    /// <para>图片链接</para>
    /// <para>示例值：https://example.cn/drive/home/</para>
    /// </summary>
    [JsonPropertyName("src")]
    public string? Src { get; set; }

    /// <summary>
    /// <para>图片宽，单位 px</para>
    /// <para>示例值：458</para>
    /// </summary>
    [JsonPropertyName("width")]
    public double? Width { get; set; }

    /// <summary>
    /// <para>图片高，单位 px</para>
    /// <para>示例值：372</para>
    /// </summary>
    [JsonPropertyName("height")]
    public double? Height { get; set; }
}
