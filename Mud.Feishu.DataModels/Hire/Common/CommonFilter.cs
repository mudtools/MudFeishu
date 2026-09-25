// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 通用筛选项（相同的 Key 仅可传一次）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class CommonFilter
{
    /// <summary>
    /// <para>筛选项 key，使用筛选项查询时必填</para>
    /// <para>必填：是</para>
    /// <para>示例值：cooperation_status</para>
    /// </summary>
    [JsonPropertyName("key")]
    public string? Key { get; set; }

    /// <summary>
    /// <para>筛选项值类型：1 值筛选（填 value_list）/ 2 范围筛选（填 range_filter），使用筛选项查询时必填</para>
    /// <para>必填：是</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("value_type")]
    public int? ValueType { get; set; }

    /// <summary>
    /// <para>筛选项值列表，当 value_type 为 1 时必填</para>
    /// <para>必填：否</para>
    /// <para>示例值：["1","2"]</para>
    /// </summary>
    [JsonPropertyName("value_list")]
    public string[]? ValueList { get; set; }

    /// <summary>
    /// <para>范围筛选，当 value_type 为 2 时必填</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("range_filter")]
    public RangeFilter? RangeFilter { get; set; }

    /// <summary>
    /// <para>用户 ID 筛选，需与入参 user_id_type 类型一致，当 value_type 为 3 时必填</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("user_id_list")]
    public string[]? UserIdList { get; set; }
}
