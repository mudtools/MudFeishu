// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.SecurityCompliance;

/// <summary>
/// 设备申报事件
/// <para>订阅此事件后，成员提交设备自主申报后会收到通知，通知包含申报设备的参数以及申报人等信息</para>
/// <para>事件类型:security_and_compliance.device_apply_record.device_apply_event_v2</para>
/// <para>使用时请继承：<see cref="SecurityComplianceDeviceAppliedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/security_and_compliance-v2/device_apply_record/events/device_apply_event</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.SecurityComplianceDeviceApplied, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "SecurityCompliance")]
public class SecurityComplianceDeviceAppliedResult : IEventResult
{
    /// <summary>
    /// <para>设备申报记录ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("device_apply_record_id")]
    public string? DeviceApplyRecordId { get; set; }

    /// <summary>
    /// <para>设备记录</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("device_record")]
    public SecurityComplianceDeviceRecordEvent? DeviceRecord { get; set; }

    /// <summary>
    /// <para>申报时间</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("apply_time")]
    public string? ApplyTime { get; set; }

    /// <summary>
    /// <para>申报状态</para>
    /// <para>**可选值有**：</para>
    /// <para>- 0：未知</para>
    /// <para>- 1：待审核</para>
    /// <para>- 2：审核通过</para>
    /// <para>- 3：已驳回</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("apply_status")]
    public int? ApplyStatus { get; set; }

    /// <summary>
    /// <para>用户 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("operator")]
    public UserIdInfo? Operator { get; set; }

    /// <summary>
    /// <para>申报设备归属</para>
    /// <para>**可选值有**：</para>
    /// <para>- 0：未知设备</para>
    /// <para>- 1：个人设备</para>
    /// <para>- 2：企业设备</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("apply_device_ownership")]
    public int? ApplyDeviceOwnership { get; set; }
}
