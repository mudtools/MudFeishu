// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Performance;

/// <summary>
/// 指标库指标的字段信息
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Performance")]
public class PerformanceMetricFieldInLibrary
{
    /// <summary>
    /// <para>指标字段 ID，详情可查看获取指标字段详情接口</para>
    /// <para>示例值：7296701873237786643</para>
    /// </summary>
    [JsonPropertyName("field_id")]
    public string? FieldId { get; set; }

    /// <summary>
    /// <para>指标字段填写设置类型：admin（管理员统一配置）/ data_source_inputter（数据源录入人填写）/ reviewee（指标制定人填写）</para>
    /// <para>示例值：admin</para>
    /// </summary>
    [JsonPropertyName("input_setting")]
    public string? InputSetting { get; set; }

    /// <summary>
    /// <para>指标字段值</para>
    /// <para>示例值：90</para>
    /// </summary>
    [JsonPropertyName("field_value")]
    public string? FieldValue { get; set; }

    /// <summary>
    /// <para>字段值，当字段为人员信息时有值</para>
    /// </summary>
    [JsonPropertyName("field_value_person")]
    public UserIdInfo? FieldValuePerson { get; set; }
}
