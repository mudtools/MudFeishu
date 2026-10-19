// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.AgentTools;

/// <summary>
/// 飞书侧工具人工确认（HITL）宿主批准通道的<b>派生命名契约</b>。
/// </summary>
/// <remarks>
/// <para>
/// 契约本体（方法、语义、fail-closed 纪律、宿主续跑义务）位于中立接口
/// <see cref="Mud.AI.Abstractions.Approvals.IToolApprovalChannel"/>——本接口是
/// 「<b>共享结构、不共享语义</b>」定式里的<b>门面保留</b>手段：
/// 飞书侧的诊断、工具面（<c>Mud.Feishu.AI.AgentTools</c> 消费方）、Demo 与测试仍按
/// <c>IFeishuToolApprovalChannel</c> 这一产品命名解析，避免为了改一个 using 而改动
/// 下游 ~10 处调用点与 61 个工具面守卫文件。
/// </para>
/// <para>
/// <b>⚠️ 本接口不得新增任何成员</b>：它必须<b>逐字节等价</b>于其基线。
/// 一旦这里出现"飞书特有的额外方法"，中立管线（只认识基线接口）将无法在多生态下泛化，
/// 等于用一个空壳把产品语义偷渡回中立层。多出的能力应由<b>另立的扩展接口</b>承载
/// （如 <c>IFeishuXxxExtension : IToolApprovalChannel</c>），并显式做能力探测。
/// </para>
/// </remarks>
public interface IFeishuToolApprovalChannel : Mud.AI.Abstractions.Approvals.IToolApprovalChannel
{
}
