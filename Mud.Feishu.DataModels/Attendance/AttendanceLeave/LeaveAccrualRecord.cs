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
public class LeaveAccrualRecord
{
    /// <summary>
    /// <para>发放记录唯一ID</para>
    /// <para>必填：是</para>
    /// <para>示例值：6893014062142064135</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// <para>员工ID，类型对应user_id_type</para>
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
    /// <para>发放来源，1：系统发放；2：手动发放；3：外部系统发放</para>
    /// <para>必填：是</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("granted_by")]
    public int GrantedBy { get; set; }

    /// <summary>
    /// <para>发放原因</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("reason")]
    public LangText[] Reasons { get; set; } = [];

    /// <summary>
    /// <para>发放记录的创建时间，unix时间戳</para>
    /// <para>必填：是</para>
    /// <para>示例值：1687428000</para>
    /// </summary>
    [JsonPropertyName("created_at")]
    public string CreatedAt { get; set; } = string.Empty;

    /// <summary>
    /// <para>发放记录的创建人的ID，类型对应user_id_type</para>
    /// <para>必填：是</para>
    /// <para>示例值：6982509313466189342</para>
    /// </summary>
    [JsonPropertyName("created_by")]
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// <para>发放记录的更新时间，unix时间戳</para>
    /// <para>必填：是</para>
    /// <para>示例值：1687428000</para>
    /// </summary>
    [JsonPropertyName("updated_at")]
    public string UpdatedAt { get; set; } = string.Empty;

    /// <summary>
    /// <para>发放记录的更新人的ID，类型对应user_id_type</para>
    /// <para>必填：是</para>
    /// <para>示例值：6982509313466189342</para>
    /// </summary>
    [JsonPropertyName("updated_by")]
    public string UpdatedBy { get; set; } = string.Empty;
}
