// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Hire;

/// <summary>
/// 投递阶段变更
/// <para>当申请的投递阶段（stage）发生变更时触发此事件。</para>
/// <para>事件类型:hire.application.stage_changed_v1</para>
/// <para>使用时请继承：<see cref="HireApplicationStageChangedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/hire-v1/candidate-management/delivery-process-management/application/events/stage_changed</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.HireApplicationStageChanged, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class HireApplicationStageChangedResult : IEventResult
{
    /// <summary>
    /// <para>投递 ID，可参考「获取投递信息」接口</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("application_id")]
    public string? ApplicationId { get; set; }

    /// <summary>
    /// <para>变更前的投递阶段 ID（投递阶段转移前）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("origin_stage_id")]
    public string? OriginStageId { get; set; }

    /// <summary>
    /// <para>变更后的投递阶段 ID（投递阶段转移后）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("target_stage_id")]
    public string? TargetStageId { get; set; }

    /// <summary>
    /// <para>投递阶段变更时间戳（单位：毫秒）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("update_time")]
    public long? UpdateTime { get; set; }
}
