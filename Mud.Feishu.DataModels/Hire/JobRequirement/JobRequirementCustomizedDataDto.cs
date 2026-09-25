// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 招聘需求自定义字段信息（招聘需求响应子项）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class JobRequirementCustomizedDataDto
{
    /// <summary>
    /// <para>自定义字段 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("object_id")]
    public string? ObjectId { get; set; }

    /// <summary>
    /// <para>字段名称</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("name")]
    public I18nName? Name { get; set; }

    /// <summary>
    /// <para>字段类型：1 单行文本 / 2 多行文本 / 3 单选 / 4 多选 / 5 日期 / 6 月份选择 / 7 年份选择 / 8 时间段 / 9 数字 / 10 默认字段 / 11 模块</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("object_type")]
    public int? ObjectType { get; set; }

    /// <summary>
    /// <para>自定义字段值</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("value")]
    public JobRequirementCustomizedValue? Value { get; set; }
}
