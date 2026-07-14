// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.AI;

/// <summary>
/// <para>识别出的实体类型</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "AI")]
public class VehicleEntity
{
    /// <summary>
    /// <para>识别的字段种类</para>
    /// <para>必填：否</para>
    /// <para>示例值：vehicle_type</para>
    /// <para>可选值：<list type="bullet">
    /// <item>plate_number：号牌号码</item>
    /// <item>vehicle_type：车辆类型</item>
    /// <item>owner：所有人</item>
    /// <item>address：住址</item>
    /// <item>use_character：使用性质</item>
    /// <item>model：品牌型号</item>
    /// <item>vin：车辆识别代号</item>
    /// <item>engine_number：发动机号码</item>
    /// <item>register_date：注册日期</item>
    /// <item>issue_date：发证日期</item>
    /// <item>license_issuing_authority：发证机关</item>
    /// <item>document_id：档案编号</item>
    /// <item>approved_passengers_capacity：核定载人数</item>
    /// <item>total_mass：总质量</item>
    /// <item>curb_weight：整备质量</item>
    /// <item>ratified_load_capacity：核定载质量</item>
    /// <item>gabarite：外廓尺寸</item>
    /// <item>traction_mass：准牵引总质量</item>
    /// <item>remarks：备注</item>
    /// <item>inspection_record：检验记录</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// <para>识别出字段的文本信息</para>
    /// <para>必填：否</para>
    /// <para>示例值：小型普通客车</para>
    /// </summary>
    [JsonPropertyName("value")]
    public string? Value { get; set; }
}
