// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.SecurityCompliance;

/// <summary>
/// 设备信息变更事件
/// <para>使用该接口，可以订阅接收设备管理变更记录通知，包含设备新增、设备删除、设备归属变更、可信状态变更、设备特征如生产序列号、硬盘序列号等相关信息发生变化时事件通知</para>
/// <para>事件类型:security_and_compliance.device_record.device_change_event_v2</para>
/// <para>使用时请继承：<see cref="SecurityComplianceDeviceChangedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/security_and_compliance-v2/device_record/events/device_change_event</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.SecurityComplianceDeviceChanged, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "SecurityCompliance")]
public class SecurityComplianceDeviceChangedResult : IEventResult
{
    /// <summary>
    /// <para>设备认证编码</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("device_record_id")]
    public string? DeviceRecordId { get; set; }

    /// <summary>
    /// <para>版本号</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    /// <summary>
    /// <para>变更类型</para>
    /// <para>**可选值有**：</para>
    /// <para>- 1：插入</para>
    /// <para>- 2：更新</para>
    /// <para>- 3：删除</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("change_type")]
    public int? ChangeType { get; set; }

    /// <summary>
    /// <para>变更前数据</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("before")]
    public SecurityComplianceDeviceChangeEvent? Before { get; set; }

    /// <summary>
    /// <para>变更后数据</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("after")]
    public SecurityComplianceDeviceChangeEvent? After { get; set; }
}
