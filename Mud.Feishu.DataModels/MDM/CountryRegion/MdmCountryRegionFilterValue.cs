// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.MDM;

/// <summary>
/// 国家/地区查询过滤条件的字段值（value）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "MDM")]
public class MdmCountryRegionFilterValue
{
    /// <summary>
    /// <para>字符串值</para>
    /// <para>必填：否</para>
    /// <para>示例值：安道尔</para>
    /// </summary>
    [JsonPropertyName("string_value")]
    public string? StringValue { get; set; }

    /// <summary>
    /// <para>布尔值</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("bool_value")]
    public bool? BoolValue { get; set; }

    /// <summary>
    /// <para>整型值</para>
    /// <para>必填：否</para>
    /// <para>示例值：111</para>
    /// </summary>
    [JsonPropertyName("int_value")]
    public string? IntValue { get; set; }

    /// <summary>
    /// <para>字符串列表值</para>
    /// <para>必填：否</para>
    /// <para>示例值：["1"]</para>
    /// </summary>
    [JsonPropertyName("string_list_value")]
    public string[]? StringListValue { get; set; }

    /// <summary>
    /// <para>整型列表值</para>
    /// <para>必填：否</para>
    /// <para>示例值：["1"]</para>
    /// </summary>
    [JsonPropertyName("int_list_value")]
    public string[]? IntListValue { get; set; }
}
