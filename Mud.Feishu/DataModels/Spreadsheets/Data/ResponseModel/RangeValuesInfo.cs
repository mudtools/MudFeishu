// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Spreadsheets;

/// <summary>
/// <para>读取的值与范围</para>
/// </summary>
public class RangeValuesInfo
{
    /// <summary>
    /// <para>返回的 values 数组中数据的呈现维度。固定取值 ROWS，即数据为从左到右、从上到下的读取顺序。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("majorDimension")]
    public string? MajorDimension { get; set; }

    /// <summary>
    /// <para>读取的范围。为空时表示查询范围没有数据。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("range")]
    public string? Range { get; set; }

    /// <summary>
    /// <para>工作表的版本号。从 0 开始计数，更新一次版本号加一。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("revision")]
    public int? Revision { get; set; }

    /// <summary>
    /// <para>指定范围中的数据</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("values")]
    public object[][]? Values { get; set; }
}