// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// Offer 申请表字段公式信息（字段配置子项）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class OfferApplyFormConfigFormulaInfo
{
    /// <summary>
    /// <para>计算公式，由薪资字段 ID 与运算符组成，如 "( [6872592813776914699] * 12 + 20 / 2 ) / [6872592813776914699] + 2000"，其中 6872592813776914699 为薪资字段 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("value")]
    public string? Value { get; set; }

    /// <summary>
    /// <para>计算结果显示格式：1 金额 / 2 数字 / 3 百分比</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("result")]
    public int? Result { get; set; }

    /// <summary>
    /// <para>公式字段信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("extra_map")]
    public OfferApplyFormFormulaExtraMapInfo[]? ExtraMap { get; set; }
}
