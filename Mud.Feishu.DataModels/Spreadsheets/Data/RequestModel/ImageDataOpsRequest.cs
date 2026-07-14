// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Spreadsheets;

/// <summary>
/// 写入图片请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Spreadsheets")]
public class ImageDataOpsRequest
{
    /// <summary>
    /// <para>指定写入图片的单元格。格式为`&lt;sheetId&gt;!&lt;开始单元格&gt;:&lt;结束单元格&gt;`。其中：</para>
    /// <para>- `sheetId` 为工作表 ID</para>
    /// <para>- `&lt;开始单元格&gt;:&lt;结束单元格&gt;` 用于指定单元格，开始单元格与结束单元格需保持一致，如：`A1:A1`。其中，数字表示行索引，字母表示列索引。如 `A1:A1` 表示该工作表第 1 行 A 列的单元格。</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("range")]
    public string Range { get; set; } = string.Empty;

    /// <summary>
    /// <para>需要写入的图片的二进制流，支持 "PNG"、"JPEG"、"JPG"、"GIF"、"BMP"、"JFIF"、"EXIF"、 "TIFF"、"BPG"、"HEIC" 等图片格式。</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("image")]
    public byte[] Image { get; set; } = [];

    /// <summary>
    /// <para>写入的图片名称。</para>
    /// <para>**注意**：该参数需加后缀名，如 `test.png`。支持的后缀名有："PNG"、"JPEG"、"JPG"、"GIF"、"BMP"、"JFIF"、"EXIF"、 "TIFF"、"BPG"、"HEIC"。不区分大小写。</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
