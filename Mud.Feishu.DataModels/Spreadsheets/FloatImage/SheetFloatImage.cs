// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Spreadsheets;

/// <summary>
/// 电子表格浮动图片
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Spreadsheets")]
public class SheetFloatImage
{
    /// <summary>
    /// <para>浮动图片的 token。通过[上传素材]上传图片至表格，获得素材的 `file_token`，即为 float_image_token。</para>
    /// <para>**注意**：</para>
    /// <para>该参数必填，请忽略左侧必填列的”否”。</para>
    /// <para>必填：否</para>
    /// <para>示例值：boxcnrHpsg1QDqXAAAyachabcef</para>
    /// </summary>
    [JsonPropertyName("float_image_token")]
    public string? FloatImageToken { get; set; }

    /// <summary>
    /// <para>浮动图片左上角所在单元格位置，只允许单个单元格的形式，如 "ahgsch!A1:A1"。</para>
    /// <para>**注意**：</para>
    /// <para>该参数必填，请忽略左侧必填列的”否”。</para>
    /// <para>必填：否</para>
    /// <para>示例值：ahgsch!A1:A1</para>
    /// </summary>
    [JsonPropertyName("range")]
    public string? Range { get; set; }

    /// <summary>
    /// <para>浮动图片的宽度，单位为像素。不传会默认采用图片实际宽度，如果传则需要大于等于 20 像素。</para>
    /// <para>必填：否</para>
    /// <para>示例值：100</para>
    /// </summary>
    [JsonPropertyName("width")]
    public float? Width { get; set; }

    /// <summary>
    /// <para>浮动图片的高度，单位为像素。不传会默认采用图片实际高度，如果传则需要大于等于 20 像素。</para>
    /// <para>必填：否</para>
    /// <para>示例值：100</para>
    /// </summary>
    [JsonPropertyName("height")]
    public float? Height { get; set; }

    /// <summary>
    /// <para>浮动图片左上角距离所在单元格左上角的横向偏移，单位为像素，默认为 0，设置的值需要大于等于 0、小于浮动图片左上角所在单元格的宽度。</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// </summary>
    [JsonPropertyName("offset_x")]
    public float? OffsetX { get; set; }

    /// <summary>
    /// <para>浮动图片左上角距离所在单元格左上角的纵向偏移，单位为像素，默认为 0。设置的值需要大于等于 0、小于浮动图片左上角所在单元格的高度。</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// </summary>
    [JsonPropertyName("offset_y")]
    public float? OffsetY { get; set; }
}

/// <summary>
/// 电子表格浮动图片数据
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Spreadsheets")]
public class SheetFloatImageData : SheetFloatImage
{
    /// <summary>
    /// <para>工作表内浮动图片的唯一标识。可不传由系统自动生成，也可选择自定义。</para>
    /// <para>**数据校验规则**：</para>
    /// <para>长度为 10，由 0-9、a-z、A-Z 组合而成。</para>
    /// <para>必填：否</para>
    /// <para>示例值：ye06SS14ph</para>
    /// </summary>
    [JsonPropertyName("float_image_id")]
    public string? FloatImageId { get; set; }
}
