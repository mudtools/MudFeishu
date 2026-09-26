// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Approval;

/// <summary>
/// 获取单个审批实例详情（以用户身份查询）响应体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Approval")]
public class InstanceDetailResult
{
    /// <summary>
    /// <para>审批名称</para>
    /// </summary>
    [JsonPropertyName("definition_name")]
    public string? DefinitionName { get; set; }

    /// <summary>
    /// <para>审批创建时间，毫秒时间戳</para>
    /// </summary>
    [JsonPropertyName("start_time")]
    public string? StartTime { get; set; }

    /// <summary>
    /// <para>审批完成时间，毫秒时间戳，未完成为 0</para>
    /// </summary>
    [JsonPropertyName("end_time")]
    public string? EndTime { get; set; }

    /// <summary>
    /// <para>发起审批用户</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// <para>审批单编号</para>
    /// </summary>
    [JsonPropertyName("serial_number")]
    public string? SerialNumber { get; set; }

    /// <summary>
    /// <para>发起审批用户所在部门</para>
    /// </summary>
    [JsonPropertyName("department_id")]
    public string? DepartmentId { get; set; }

    /// <summary>
    /// <para>审批实例状态</para>
    /// <para>可选值：<list type="bullet">
    /// <item>PENDING：审批中</item>
    /// <item>APPROVED：通过</item>
    /// <item>REJECTED：拒绝</item>
    /// <item>CANCELED：撤回</item>
    /// <item>DELETED：删除</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// <para>json 字符串，控件值</para>
    /// </summary>
    [JsonPropertyName("form")]
    public string? Form { get; set; }

    /// <summary>
    /// <para>审批任务列表</para>
    /// </summary>
    [JsonPropertyName("tasks")]
    public InstanceDetailTaskInfo[]? Tasks { get; set; }

    /// <summary>
    /// <para>评论列表</para>
    /// </summary>
    [JsonPropertyName("comments")]
    public InstanceDetailCommentInfo[]? Comments { get; set; }

    /// <summary>
    /// <para>审批动态</para>
    /// </summary>
    [JsonPropertyName("operation_records")]
    public InstanceDetailTimelineInfo[]? OperationRecords { get; set; }

    /// <summary>
    /// <para>审批定义 Code</para>
    /// </summary>
    [JsonPropertyName("definition_code")]
    public string? DefinitionCode { get; set; }

    /// <summary>
    /// <para>单据是否被撤销</para>
    /// </summary>
    [JsonPropertyName("reverted")]
    public bool? Reverted { get; set; }

    /// <summary>
    /// <para>审批实例 Code</para>
    /// </summary>
    [JsonPropertyName("instance_code")]
    public string? InstanceCode { get; set; }

    /// <summary>
    /// <para>当前审批节点</para>
    /// </summary>
    [JsonPropertyName("current_nodes")]
    public InstanceCurrentNodeInfo[]? CurrentNodes { get; set; }
}
