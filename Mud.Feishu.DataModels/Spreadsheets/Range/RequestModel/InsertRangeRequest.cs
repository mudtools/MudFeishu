// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Spreadsheets;

/// <summary>
/// 插入行列请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Spreadsheets")]
public class InsertRangeRequest
{
    /// <summary>
    /// <para>需要插入行列的维度信息</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("dimension")]
    public SheetRangeDimension Dimension { get; set; } = new();


    /// <summary>
    /// <para>插入的空白行或列是否继承表中的单元格样式。不填或设置为空即不继承任何样式，为默认空白样式。可选值：</para>
    /// <para>- `BEFORE`：继承起始位置的单元格的样式</para>
    /// <para>- `AFTER`：继承结束位置的单元格的样式</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("inheritStyle")]
    public string? InheritStyle { get; set; }
}
