// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Acs;

/// <summary>
/// 门禁通行记录创建
/// <para>门禁设备成功识别用户后，将会推送此事件。</para>
/// <para>事件类型:acs.access_record.created_v1</para>
/// <para>使用时请继承：<see cref="AcsAccessRecordCreatedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/server-docs/acs-v1/access_record/events/created</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.AcsAccessRecordCreated, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "Acs")]
public class AcsAccessRecordCreatedResult : IEventResult
{
    /// <summary>
    /// <para>通行记录 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("access_record_id")]
    public string? AccessRecordId { get; set; }

    /// <summary>
    /// <para>识别到的用户 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public UserIdInfo? UserId { get; set; }

    /// <summary>
    /// <para>门禁设备 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("device_id")]
    public string? DeviceId { get; set; }

    /// <summary>
    /// <para>用户是否打卡</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("is_clock_in")]
    public bool? IsClockIn { get; set; }

    /// <summary>
    /// <para>是否开门</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("is_door_open")]
    public bool? IsDoorOpen { get; set; }

    /// <summary>
    /// <para>识别时间，单位为秒</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("access_time")]
    public string? AccessTime { get; set; }
}
