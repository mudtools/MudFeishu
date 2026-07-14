// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Bitable;


/// <summary>
/// 创建多维表格数据表的响应结果
/// <para>多维表格数据表的 ID、默认表格视图的 ID、数据表初始字段的 ID 列表</para>
/// </summary>
public class CreateTableResult
{
    /// <summary>
    /// <para>多维表格数据表的 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：tbl1AybU4ogJYXKA</para>
    /// </summary>
    [JsonPropertyName("table_id")]
    public string? TableId { get; set; }

    /// <summary>
    /// <para>默认表格视图的 ID。该字段仅在请求参数中填写了`default_view_name` 或 `fields` 字段才会返回</para>
    /// <para>必填：否</para>
    /// <para>示例值：vew3y6oFgo</para>
    /// </summary>
    [JsonPropertyName("default_view_id")]
    public string? DefaultViewId { get; set; }

    /// <summary>
    /// <para>数据表初始字段的 ID 列表，该字段仅在请求参数中填写了 `fields` 才会返回</para>
    /// <para>必填：否</para>
    /// <para>示例值：["fldO1Q5uD2"]</para>
    /// </summary>
    [JsonPropertyName("field_id_list")]
    public string[]? FieldIdList { get; set; }
}
