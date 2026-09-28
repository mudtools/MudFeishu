// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Okr;

/// <summary>
/// 上传进展记录图片响应体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Okr")]
public class UploadProgressRecordImageResult
{
    /// <summary>
    /// <para>图片 token</para>
    /// <para>示例值：boxbcc6DmPfgi4rNXIaGfptc9HX</para>
    /// </summary>
    [JsonPropertyName("file_token")]
    public string? FileToken { get; set; }

    /// <summary>
    /// <para>图片下载链接</para>
    /// <para>示例值：https://internal-****.cn/stream/api/downloadFile/?file_token=boxbcc6DmPfgi4rNXIaGfptc9HX&amp;ticket=...</para>
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }
}
