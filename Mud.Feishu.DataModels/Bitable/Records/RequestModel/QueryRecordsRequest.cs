// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Bitable;

/// <summary>
/// 查询记录请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Bitable")]
public class QueryRecordsRequest
{
    /// <summary>
    /// <para>多维表格中视图的唯一标识。</para>
    /// <para>**注意**：</para>
    /// <para>当 filter 参数 或 sort 参数不为空时，请求视为对数据表中的全部数据做条件过滤，指定的 view_id 会被忽略。</para>
    /// <para>必填：否</para>
    /// <para>示例值：vewqhz51lk</para>
    /// <para>最大长度：50</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("view_id")]
    public string? ViewId { get; set; }

    /// <summary>
    /// <para>字段名称，用于指定本次查询返回记录中包含的字段</para>
    /// <para>必填：否</para>
    /// <para>最大长度：200</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("field_names")]
    public string[]? FieldNames { get; set; }

    /// <summary>
    /// <para>排序条件</para>
    /// <para>必填：否</para>
    /// <para>最大长度：100</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("sort")]
    public RecordQuerySort[]? Sorts { get; set; }


    /// <summary>
    /// <para>包含条件筛选信息的对象。了解 filter 填写指南和使用示例（如怎样同时使用 `and` 和 `or` 逻辑链接词），参考[记录筛选参数填写指南](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/bitable-v1/app-table-record/record-filter-guide)。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("filter")]
    public RecordQueryFilterInfo? Filter { get; set; }

    /// <summary>
    /// <para>是否自动计算并返回创建时间（created_time）、修改时间（last_modified_time）、创建人（created_by）、修改人（last_modified_by）这四类字段。默认为 false，表示不返回。</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("automatic_fields")]
    public bool? AutomaticFields { get; set; }
}
