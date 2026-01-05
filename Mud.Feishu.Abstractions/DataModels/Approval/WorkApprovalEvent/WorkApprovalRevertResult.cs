// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.DataModels.Approval;


/// <summary>
/// 加班审批通过并撤销事件体
/// <para>审批定义的表单包含 加班控件组 时，该定义下的审批实例在 通过 或者 通过并撤销 时，会触发该事件。</para>
/// <para>事件类型:work_approval_revert</para>
/// <para>使用时请继承：<see cref="WorkApprovalRevertEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/server-docs/approval-v4/event/common-event/approval-task-event</para>
/// </summary>
[EventHandler(EventType = FeishuEventTypes.WorkApprovalRevert, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom)]
public class WorkApprovalRevertResult : IEventResult
{

    /// <summary>
    /// <para>是否存在多时段。可能值有：</para>
    /// <para> - 1：存在多时段</para>
    /// <para> - 0：不存在多时段</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("allow_multi_time_range")]
    public int? AllowMultiTimeRange { get; set; }

    /// <summary>
    /// <para>应用的 App ID。可调用获取应用信息接口查询应用详细信息。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("app_id")]
    public string? AppId { get; set; }

    /// <summary>
    /// <para>审批定义 Code。可调用查看指定审批定义接口查询审批定义详情。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("approval_code")]
    public string? ApprovalCode { get; set; }

    /// <summary>
    /// <para>审批实例 Code。可调用获取单个审批实例详情接口查询审批实例详情。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("instance_code")]
    public string? InstanceCode { get; set; }

    /// <summary>
    /// <para>审批提交人的 user_id。你可以调用获取单个用户信息接口，通过 user_id 获取用户信息。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("employee_id")]
    public string? EmployeeId { get; set; }

    /// <summary>
    /// <para>审批提交人的 open_id。你可以调用获取单个用户信息接口，通过 user_id 获取用户信息。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("open_id")]
    public string? OpenId { get; set; }

    /// <summary>
    /// <para>审批结束时间，秒级时间戳。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("end_time")]
    public int? EndTime { get; set; }

    /// <summary>
    /// <para>审批开始时间，秒级时间戳。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("start_time")]
    public int? StartTime { get; set; }

    /// <summary>
    /// <para>租户 Key，是企业的唯一标识。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("tenant_key")]
    public string? TenantKey { get; set; }

    /// <summary>
    /// <para>多时段信息，参数 allow_multi_time_range 取值为 1 时该字段有返回值。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("time_range")]
    public TimeRangeSuffix[]? TimeRange { get; set; }

    /// <summary>
    /// <para>事件类型。固定值 work_approval</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// <para>每日加班明细。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("work_detail")]
    public WorkDetailSuffix[]? WorkDetail { get; set; }


    /// <summary>
    /// <para>加班开始时间。示例格式：2018-12-01 12:00:00</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("work_start_time")]
    public string? WorkStartTime { get; set; }

    /// <summary>
    /// <para>加班结束时间。示例格式：2018-12-02 12:00:00</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("work_end_time")]
    public string? WorkEndTime { get; set; }

    /// <summary>
    /// <para>加班时长。单位：秒</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("work_interval")]
    public int? WorkInterval { get; set; }

    /// <summary>
    /// <para>代多人提交用户列表。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("work_multi_imitate_users")]
    public object[]? WorkMultiImitateUsers { get; set; }

    /// <summary>
    /// <para>加班事由。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("work_reason")]
    public string? WorkReason { get; set; }

    /// <summary>
    /// <para>加班类型。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("work_type")]
    public string? WorkType { get; set; }

}
