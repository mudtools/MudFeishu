// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.AttendanceLeave;

/// <summary>
/// <para>员工过期日期的发放记录</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Attendance")]
public class LeaveEmployExpireRecord
{
    /// <summary>
    /// <para>发放记录ID</para>
    /// <para>必填：是</para>
    /// <para>示例值：6893014062142064135</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// <para>员工ID，与user_id_type保持一致</para>
    /// <para>必填：是</para>
    /// <para>示例值：6982509313466189342</para>
    /// </summary>
    [JsonPropertyName("employment_id")]
    public string EmploymentId { get; set; } = string.Empty;

    /// <summary>
    /// <para>假期类型ID</para>
    /// <para>必填：是</para>
    /// <para>示例值：6893014062142064135</para>
    /// </summary>
    [JsonPropertyName("leave_type_id")]
    public string LeaveTypeId { get; set; } = string.Empty;

    /// <summary>
    /// <para>发放数量</para>
    /// <para>必填：是</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("granting_quantity")]
    public string GrantingQuantity { get; set; } = string.Empty;

    /// <summary>
    /// <para>剩余数量</para>
    /// <para>必填：是</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("left_granting_quantity")]
    public string LeftGrantingQuantity { get; set; } = string.Empty;

    /// <summary>
    /// <para>发放单位，1表示天，2表示小时</para>
    /// <para>必填：是</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("granting_unit")]
    public int GrantingUnit { get; set; }

    /// <summary>
    /// <para>生效日期，格式为yyyy-MM-dd</para>
    /// <para>必填：是</para>
    /// <para>示例值：2020-01-01</para>
    /// </summary>
    [JsonPropertyName("effective_date")]
    public string EffectiveDate { get; set; } = string.Empty;

    /// <summary>
    /// <para>失效日期，格式为yyyy-MM-dd</para>
    /// <para>必填：是</para>
    /// <para>示例值：2020-01-01</para>
    /// </summary>
    [JsonPropertyName("expiration_date")]
    public string ExpirationDate { get; set; } = string.Empty;

    /// <summary>
    /// <para>发放原因</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("reason")]
    public LangText[] Reasons { get; set; } = [];


    /// <summary>
    /// <para>是否已经被外部系统更改过</para>
    /// <para>必填：是</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("is_update_by_external")]
    public bool IsUpdateByExternal { get; set; }

    /// <summary>
    /// <para>发放来源</para>
    /// <para>必填：是</para>
    /// <para>示例值：1</para>
    /// <para>可选值：<list type="bullet">
    /// <item>1：系统发放</item>
    /// <item>2：人工发放</item>
    /// <item>3：外部系统发放</item>
    /// <item>4：虚拟发放</item>
    /// <item>5：旧系统导入</item>
    /// <item>6：加班转入</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("accrual_source")]
    public int AccrualSource { get; set; }

    /// <summary>
    /// <para>假期子类型id</para>
    /// <para>必填：是</para>
    /// <para>示例值：6893014062142064135</para>
    /// </summary>
    [JsonPropertyName("leave_sub_type_id")]
    public string LeaveSubTypeId { get; set; } = string.Empty;

    /// <summary>
    /// <para>是否参与折算（1不参与折算，2参与折算）。默认不折算</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("section_type")]
    public int? SectionType { get; set; }
}
