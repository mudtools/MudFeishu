// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Approval;

/// <summary>
/// 获取单个审批实例详情响应体
/// </summary>
public class GetApprovalInstanceResult
{
    /// <summary>
    /// <para>审批名称</para>
    /// <para>必填：是</para>
    /// <para>示例值：Payment</para>
    /// </summary>
    [JsonPropertyName("approval_name")]
    public string ApprovalName { get; set; } = string.Empty;

    /// <summary>
    /// <para>审批创建时间，毫秒级时间戳。</para>
    /// <para>必填：否</para>
    /// <para>示例值：1564590532967</para>
    /// </summary>
    [JsonPropertyName("start_time")]
    public string? StartTime { get; set; }

    /// <summary>
    /// <para>审批完成时间，毫秒级时间戳。审批未完成时该参数值为 0。</para>
    /// <para>必填：是</para>
    /// <para>示例值：1564590532967</para>
    /// </summary>
    [JsonPropertyName("end_time")]
    public string EndTime { get; set; } = string.Empty;

    /// <summary>
    /// <para>发起审批的用户 user_id</para>
    /// <para>必填：是</para>
    /// <para>示例值：f3ta757q</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// <para>发起审批的用户 open_id</para>
    /// <para>必填：是</para>
    /// <para>示例值：ou_3cda9c969f737aaa05e6915dce306cb9</para>
    /// </summary>
    [JsonPropertyName("open_id")]
    public string OpenId { get; set; } = string.Empty;

    /// <summary>
    /// <para>审批单编号</para>
    /// <para>必填：是</para>
    /// <para>示例值：202102060002</para>
    /// </summary>
    [JsonPropertyName("serial_number")]
    public string SerialNumber { get; set; } = string.Empty;

    /// <summary>
    /// <para>发起审批用户所在部门的 ID</para>
    /// <para>必填：是</para>
    /// <para>示例值：od-8ec33ffec336c3a39a278bc25e931676</para>
    /// </summary>
    [JsonPropertyName("department_id")]
    public string DepartmentId { get; set; } = string.Empty;

    /// <summary>
    /// <para>审批实例状态</para>
    /// <para>必填：是</para>
    /// <para>示例值：PENDING</para>
    /// <para>可选值：<list type="bullet">
    /// <item>PENDING：审批中</item>
    /// <item>APPROVED：通过</item>
    /// <item>REJECTED：拒绝</item>
    /// <item>CANCELED：撤回</item>
    /// <item>DELETED：删除</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// <para>审批实例的唯一标识 id</para>
    /// <para>必填：是</para>
    /// <para>示例值：1234567</para>
    /// </summary>
    [JsonPropertyName("uuid")]
    public string Uuid { get; set; } = string.Empty;

    /// <summary>
    /// <para>审批表单控件 JSON 字符串，控件值详细说明参见本文下方 **控件值说明** 章节。</para>
    /// <para>必填：是</para>
    /// <para>示例值：[{\"id\": \"widget1\",\"custom_id\": \"user_info\",\"name\": \"Item application\",\"type\": \"textarea\"}]</para>
    /// </summary>
    [JsonPropertyName("form")]
    public string Form { get; set; } = string.Empty;

    /// <summary>
    /// <para>审批任务列表</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("task_list")]
    public InstanceTaskInfo[] TaskLists { get; set; } = [];

    /// <summary>
    /// <para>评论列表</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("comment_list")]
    public InstanceCommentInfo[] CommentLists { get; set; } = [];

    /// <summary>
    /// <para>审批动态</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("timeline")]
    public InstanceTimelineInfo[] Timelines { get; set; } = [];

    /// <summary>
    /// <para>修改的原实例 Code，仅在查询修改实例时显示该字段</para>
    /// <para>必填：否</para>
    /// <para>示例值：81D31358-93AF-92D6-7425-01A5D67C4E71</para>
    /// </summary>
    [JsonPropertyName("modified_instance_code")]
    public string? ModifiedInstanceCode { get; set; }

    /// <summary>
    /// <para>撤销的原实例 Code，仅在查询撤销实例时显示该字段</para>
    /// <para>必填：否</para>
    /// <para>示例值：81D31358-93AF-92D6-7425-01A5D67C4E71</para>
    /// </summary>
    [JsonPropertyName("reverted_instance_code")]
    public string? RevertedInstanceCode { get; set; }

    /// <summary>
    /// <para>审批定义 Code</para>
    /// <para>必填：是</para>
    /// <para>示例值：7C468A54-8745-2245-9675-08B7C63E7A85</para>
    /// </summary>
    [JsonPropertyName("approval_code")]
    public string ApprovalCode { get; set; } = string.Empty;

    /// <summary>
    /// <para>单据是否被撤销</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("reverted")]
    public bool? Reverted { get; set; }

    /// <summary>
    /// <para>审批实例 Code</para>
    /// <para>必填：是</para>
    /// <para>示例值：81D31358-93AF-92D6-7425-01A5D67C4E71</para>
    /// </summary>
    [JsonPropertyName("instance_code")]
    public string InstanceCode { get; set; } = string.Empty;
}
