// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Spreadsheets;


/// <summary>
/// <para>工作表列表</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Spreadsheets")]
public class SheetDetailInfo
{
    /// <summary>
    /// <para>工作表 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：sxj5ws</para>
    /// </summary>
    [JsonPropertyName("sheet_id")]
    public string? SheetId { get; set; }

    /// <summary>
    /// <para>工作表标题</para>
    /// <para>必填：否</para>
    /// <para>示例值：title</para>
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// <para>工作表索引位置，索引从 0 开始计数。</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// </summary>
    [JsonPropertyName("index")]
    public int? Index { get; set; }

    /// <summary>
    /// <para>工作表是否被隐藏</para>
    /// <para>- `true`：被隐藏</para>
    /// <para>- `false`：未被隐藏</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("hidden")]
    public bool? Hidden { get; set; }

    /// <summary>
    /// <para>单元格属性，仅当 `resource_type` 为 `sheet` 即工作表类型为电子表格时返回。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("grid_properties")]
    public SheetGridProperties? GridProperties { get; set; }



    /// <summary>
    /// <para>工作表类型</para>
    /// <para>- `sheet`：工作表</para>
    /// <para>- `bitable`：多维表格。</para>
    /// <para>- `#UNSUPPORTED_TYPE`：不支持的类型</para>
    /// <para>必填：否</para>
    /// <para>示例值：sheet</para>
    /// </summary>
    [JsonPropertyName("resource_type")]
    public string? ResourceType { get; set; }

    /// <summary>
    /// <para>合并单元格的相关信息。没有合并单元格则不返回。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("merges")]
    public SheetMergeRange[]? Merges { get; set; }


}
