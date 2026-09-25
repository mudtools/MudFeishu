// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// Offer 申请表字段显示条件配置（字段配置子项）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class OfferApplyFormObjectDisplayConfigInfo
{
    /// <summary>
    /// <para>显示条件类型：1 全部满足 / 2 任意满足</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("display_condition")]
    public int? DisplayCondition { get; set; }

    /// <summary>
    /// <para>条件列表，由字段 ID、运算符、字段值组合成一个条件</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("pre_object_config_list")]
    public OfferApplyFormPreObjectConfigInfo[]? PreObjectConfigList { get; set; }
}
