// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.DataModels.Approval;


/// <summary>
/// 请假审批事件体
/// <para>审批定义的表单包含 请假控件组 时，该定义下的审批实例在 通过 或者 通过并撤销 时，会触发该事件。</para>
/// <para>事件类型:leave_approvalV2</para>
/// <para>使用时请继承：<see cref="LeaveApprovalV2EventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/server-docs/approval-v4/event/special-event/leave</para>
/// </summary>
[EventHandler(EventType = FeishuEventTypes.LeaveApprovalV2, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom)]
public class LeaveApprovalV2Result : IEventResult
{
    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("app_id")]
    public string? AppId { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("tenant_key")]
    public string? TenantKey { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// 审批实例Code
    /// </summary>
    [JsonPropertyName("instance_code")]
    public string? InstanceCode { get; set; }

    /// <summary>
    /// 用户id
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// 用户open_id
    /// </summary>
    [JsonPropertyName("open_id")]
    public string? OpenId { get; set; }

    /// <summary>
    /// 销假单关联的原始单据
    /// </summary>
    [JsonPropertyName("origin_instance_code")]
    public string? OriginInstanceCode { get; set; }

    /// <summary>
    /// 审批发起时间，单位：秒
    /// </summary>
    [JsonPropertyName("start_time")]
    public long StartTime { get; set; }

    /// <summary>
    /// 审批结束时间，单位：秒
    /// </summary>
    [JsonPropertyName("end_time")]
    public long EndTime { get; set; }

    /// <summary>
    /// 上班晚到（哺乳假相关）
    /// </summary>
    [JsonPropertyName("leave_feeding_arrive_late")]
    public int LeaveFeedingArriveLate { get; set; }

    /// <summary>
    /// 下班早走（哺乳假相关）
    /// </summary>
    [JsonPropertyName("leave_feeding_leave_early")]
    public int LeaveFeedingLeaveEarly { get; set; }

    /// <summary>
    /// 每日休息（哺乳假相关）
    /// </summary>
    [JsonPropertyName("leave_feeding_rest_daily")]
    public int LeaveFeedingRestDaily { get; set; }

    /// <summary>
    /// 假期名称
    /// </summary>
    [JsonPropertyName("leave_name")]
    public string? LeaveName { get; set; }

    /// <summary>
    /// 请假最小时长
    /// </summary>
    [JsonPropertyName("leave_unit")]
    public string? LeaveUnit { get; set; }

    /// <summary>
    /// 请假开始时间
    /// </summary>
    [JsonPropertyName("leave_start_time")]
    public string? LeaveStartTime { get; set; }

    /// <summary>
    /// 请假结束时间
    /// </summary>
    [JsonPropertyName("leave_end_time")]
    public string? LeaveEndTime { get; set; }

    /// <summary>
    /// 具体的请假明细时间
    /// </summary>
    [JsonPropertyName("leave_detail")]
    public string[][]? LeaveDetail { get; set; }

    /// <summary>
    /// 具体的请假时间范围
    /// </summary>
    [JsonPropertyName("leave_range")]
    public string[][]? LeaveRange { get; set; }

    /// <summary>
    /// 请假时长，单位（秒）
    /// </summary>
    [JsonPropertyName("leave_interval")]
    public int LeaveInterval { get; set; }

    /// <summary>
    /// 请假事由
    /// </summary>
    [JsonPropertyName("leave_reason")]
    public string? LeaveReason { get; set; }

    /// <summary>
    /// 国际化文案
    /// </summary>
    [JsonPropertyName("i18n_resources")]
    public I18nResource[]? I18nResources { get; set; }

}
