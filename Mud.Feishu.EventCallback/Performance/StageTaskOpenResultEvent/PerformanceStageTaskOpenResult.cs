// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Performance;

/// <summary>
/// 绩效结果开通
/// <para>用于订阅绩效结果的开通事件，当绩效结果开通时触发此事件。</para>
/// <para>事件类型:performance.stage_task.open_result_v2</para>
/// <para>使用时请继承：<see cref="PerformanceStageTaskOpenEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/performance-v1/stage_task/events/open_result</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.PerformanceStageTaskOpenResult, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "Performance")]
public class PerformanceStageTaskOpenResult : IEventResult
{
    /// <summary>
    /// <para>新开通的绩效结果被评估人列表。长度范围：0 ~ 9999999</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("items")]
    public PerformanceOpenResultItem[]? Items { get; set; }
}
