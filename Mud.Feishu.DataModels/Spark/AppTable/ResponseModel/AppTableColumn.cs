// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Spark;

/// <summary>
/// 妙搭数据表列
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Spark")]
public class AppTableColumn
{
    /// <summary>
    /// <para>列名</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>列描述</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// <para>数据库数据类型</para>
    /// <para>必填：否</para>
    /// <para>示例值：varchar</para>
    /// </summary>
    [JsonPropertyName("data_type")]
    public string? DataType { get; set; }

    /// <summary>
    /// <para>是否是主键</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("is_primary_key")]
    public bool? IsPrimaryKey { get; set; }

    /// <summary>
    /// <para>是否唯一</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("is_unique")]
    public bool? IsUnique { get; set; }

    /// <summary>
    /// <para>是否是自增</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("is_auto_increment")]
    public bool? IsAutoIncrement { get; set; }

    /// <summary>
    /// <para>是否是数组类型</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("is_array")]
    public bool? IsArray { get; set; }

    /// <summary>
    /// <para>是否允许为空</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("is_allow_null")]
    public bool? IsAllowNull { get; set; }

    /// <summary>
    /// <para>默认值</para>
    /// <para>必填：否</para>
    /// <para>示例值：默认值</para>
    /// </summary>
    [JsonPropertyName("default_value")]
    public string? DefaultValue { get; set; }
}
