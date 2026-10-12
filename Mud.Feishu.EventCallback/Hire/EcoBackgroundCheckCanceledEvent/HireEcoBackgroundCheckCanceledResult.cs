// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Hire;

/// <summary>
/// 背调取消
/// <para>招聘系统客户终止背调后，系统向对应背调服务商推送「停止背调」事件。</para>
/// <para>事件类型:hire.eco_background_check.canceled_v1</para>
/// <para>使用时请继承：<see cref="HireEcoBackgroundCheckCanceledEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/server-docs/hire-v1/ecological-docking/eco_background_check/events/canceled</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.HireEcoBackgroundCheckCanceled, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class HireEcoBackgroundCheckCanceledResult : IEventResult
{
    /// <summary>
    /// <para>背调 ID，在招聘系统中唯一，可通过「创建背调」事件获取</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("background_check_id")]
    public string? BackgroundCheckId { get; set; }

    /// <summary>
    /// <para>终止原因（例如："主动拒绝此次背调"）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("termination_reason")]
    public string? TerminationReason { get; set; }
}
