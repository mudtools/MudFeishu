// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.DataModels.Approval;


/// <summary>
/// 外出审批事件体
/// <para>审批定义的表单包含 外出控件组 时，该定义下的审批实例在 通过 或者 通过并撤销 时，会触发该事件。</para>
/// <para>事件类型:out_approval</para>
/// <para>使用时请继承：<see cref="OutApprovalEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/server-docs/approval-v4/event/special-event/out-of-office</para>
/// </summary>
[EventHandler(EventType = FeishuEventTypes.OutApproval, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom)]
public class OutApprovalResult : IEventResult
{

    /// <summary>
    /// 应用的 App ID。可调用获取应用信息接口查询应用详细信息。
    /// </summary>
    [JsonPropertyName("app_id")]
    public string? AppId { get; set; }

    /// <summary>
    /// 外出类型选项的国际化文案。
    /// </summary>
    [JsonPropertyName("i18n_resources")]
    public string[]? I18nResources { get; set; }

    /// <summary>
    /// 审批实例 Code。可调用获取单个审批实例详情接口查询审批实例详情。
    /// </summary>
    [JsonPropertyName("instance_code")]
    public string? InstanceCode { get; set; }

    /// <summary>
    /// 审批实例 Code。可调用获取单个审批实例详情接口查询审批实例详情。
    /// </summary>
    [JsonPropertyName("approval_code")]
    public string? ApprovalCode { get; set; }

    /// <summary>
    /// 外出拍照的图片。
    /// </summary>
    [JsonPropertyName("out_image")]
    public string? OutImage { get; set; }

    /// <summary>
    /// 外出时长。单位：秒
    /// </summary>
    [JsonPropertyName("out_interval")]
    public int? OutInterval { get; set; }

    /// <summary>
    /// 外出类型，需要根据该参数返回的数值，从 i18n_resources 参数中获取对应的外出类型文案。
    /// </summary>
    [JsonPropertyName("out_name")]
    public string? OutName { get; set; }

    /// <summary>
    /// 外出事由。
    /// </summary>
    [JsonPropertyName("out_reason")]
    public string? OutReason { get; set; }

    /// <summary>
    /// 外出开始时间。示例格式：2025-01-14 19:00:00
    /// </summary>
    [JsonPropertyName("out_start_time")]
    public string? OutStartTime { get; set; }

    /// <summary>
    /// 外出结束时间。示例格式：2025-01-14 19:00:00
    /// </summary>
    [JsonPropertyName("out_end_time")]
    public string? OutEndTime { get; set; }

    /// <summary>
    /// <para>外出时长的单位，该单位对应填写表单时显示的时长单位。例如表单的外出时长单位是小时，则这里取值 HOUR。</para>
    /// <para> - HOUR：小时</para>
    /// <para> - DAY：天</para>
    /// <para> - HALR_DAY：半天</para>
    /// </summary>
    [JsonPropertyName("out_unit")]
    public string? OutUnit { get; set; }

    /// <summary>
    /// 审批开始时间，秒级时间戳。
    /// </summary>
    [JsonPropertyName("start_time")]
    public int? StartTime { get; set; }

    /// <summary>
    /// 审批结束时间，秒级时间戳。
    /// </summary>
    [JsonPropertyName("end_time")]
    public int? EndTime { get; set; }

    /// <summary>
    /// 租户 Key，是企业的唯一标识。
    /// </summary>
    [JsonPropertyName("tenant_key")]
    public string? TenantKey { get; set; }

    /// <summary>
    /// 事件类型。固定值 out_approval
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// 审批发起人的 open_id。你可以调用获取单个用户信息接口，通过 open_id 获取用户信息。
    /// </summary>
    [JsonPropertyName("open_id")]
    public string? OpenId { get; set; }

    /// <summary>
    /// 审批发起人的 user_id。你可以调用获取单个用户信息接口，通过 user_id 获取用户信息。
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }
}
