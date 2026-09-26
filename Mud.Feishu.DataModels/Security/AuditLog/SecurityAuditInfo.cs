// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Security;

/// <summary>
/// 行为审计日志条目（audit_info）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Security")]
public class SecurityAuditInfo
{
    /// <summary>
    /// <para>事件 ID，不唯一，可用作聚合</para>
    /// <para>示例值：7254062411181719572</para>
    /// </summary>
    [JsonPropertyName("event_id")]
    public string? EventId { get; set; }

    /// <summary>
    /// <para>事件唯一 ID，可以用于去重，倾向使用该字段识别用户的行为</para>
    /// <para>示例值：7254062413199179796</para>
    /// </summary>
    [JsonPropertyName("unique_id")]
    public string? UniqueId { get; set; }

    /// <summary>
    /// <para>事件名称，字段详情见飞书枚举值列表附录</para>
    /// <para>示例值：space_edit_doc</para>
    /// </summary>
    [JsonPropertyName("event_name")]
    public string? EventName { get; set; }

    /// <summary>
    /// <para>用户所属部门的 ID 列表</para>
    /// </summary>
    [JsonPropertyName("department_ids")]
    public string[]? DepartmentIds { get; set; }

    /// <summary>
    /// <para>事件模块，字段详情见飞书枚举值列表附录</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("event_module")]
    public int? EventModule { get; set; }

    /// <summary>
    /// <para>操作人类型：1（组织内成员）/ 12（机器人）/ 1001（组织外成员）</para>
    /// </summary>
    [JsonPropertyName("operator_type")]
    public int? OperatorType { get; set; }

    /// <summary>
    /// <para>操作人 ID，当 operator_type 是 1001 时，该项为脱敏后的值</para>
    /// <para>示例值：4a3b8541</para>
    /// </summary>
    [JsonPropertyName("operator_value")]
    public string? OperatorValue { get; set; }

    /// <summary>
    /// <para>操作对象列表</para>
    /// </summary>
    [JsonPropertyName("objects")]
    public SecurityAuditObjectEntity[]? Objects { get; set; }

    /// <summary>
    /// <para>接收者对象列表</para>
    /// </summary>
    [JsonPropertyName("recipients")]
    public SecurityAuditRecipientEntity[]? Recipients { get; set; }

    /// <summary>
    /// <para>事件时间，秒级时间戳</para>
    /// <para>示例值：1688968015</para>
    /// </summary>
    [JsonPropertyName("event_time")]
    public int? EventTime { get; set; }

    /// <summary>
    /// <para>ip 信息</para>
    /// </summary>
    [JsonPropertyName("ip")]
    public string? Ip { get; set; }

    /// <summary>
    /// <para>第三方 isv ID</para>
    /// </summary>
    [JsonPropertyName("operator_app")]
    public string? OperatorApp { get; set; }

    /// <summary>
    /// <para>环境信息</para>
    /// </summary>
    [JsonPropertyName("audit_context")]
    public SecurityAuditContext? AuditContext { get; set; }

    /// <summary>
    /// <para>事件扩展字段，参考 common_drawers 中的信息即可</para>
    /// </summary>
    [JsonPropertyName("extend")]
    public SecurityAuditEventExtend? Extend { get; set; }

    /// <summary>
    /// <para>第三方 isv 名称</para>
    /// </summary>
    [JsonPropertyName("operator_app_name")]
    public string? OperatorAppName { get; set; }

    /// <summary>
    /// <para>事件扩展字段，字段详情见飞书枚举值列表附录</para>
    /// </summary>
    [JsonPropertyName("common_drawers")]
    public SecurityAuditCommonDrawers? CommonDrawers { get; set; }

    /// <summary>
    /// <para>设备信息：city（ip 位置，城市名称）、device_model（设备型号）、mc（Mac 地址）、os（操作系统）</para>
    /// </summary>
    [JsonPropertyName("audit_detail")]
    public SecurityAuditDetail? AuditDetail { get; set; }

    /// <summary>
    /// <para>操作人所在企业编号</para>
    /// <para>示例值：F686619755</para>
    /// </summary>
    [JsonPropertyName("operator_tenant")]
    public string? OperatorTenant { get; set; }
}
