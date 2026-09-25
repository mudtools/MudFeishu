// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 关联维度设置（维度关联配置子项）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class RelatedDimensionSetting
{
    /// <summary>
    /// <para>关联维度 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("dimension_id")]
    public string? DimensionId { get; set; }

    /// <summary>
    /// <para>关联运算符：1 等于 / 2 不等于 / 3 包含任意 / 4 不包含 / 5 为空 / 6 不为空</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("related_operator_type")]
    public int? RelatedOperatorType { get; set; }

    /// <summary>
    /// <para>关联维度选项 ID 列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("dimension_option_ids")]
    public string[]? DimensionOptionIds { get; set; }
}
