// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.CoreHrV2;

/// <summary>
/// CoreHrV2OnboardingFlowChange
/// <para>入职流程状态变更</para>
/// <para>- 当流程状态无变更，仅有任务状态变更时，此字段的会返回空</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "CoreHrV2")]
public class CoreHrV2OnboardingFlowChange
{
    /// <summary>
    /// <para>入职流程状态变更</para>
    /// <para>**可选值有**：</para>
    /// <para>- not_started：未开始</para>
    /// <para>- in_progress：进行中</para>
    /// <para>- completed：已完成（所有任务节点均已完成）</para>
    /// <para>- withdrawn：已撤销（撤销待入职）</para>
    /// <para>- others：其他(异常情况)</para>
    /// <para>- expired：已失效(回退至Offer沟通阶段)</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("after_status")]
    public string? AfterStatus { get; set; }
}
