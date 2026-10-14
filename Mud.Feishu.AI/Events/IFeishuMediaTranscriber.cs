// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Events;

/// <summary>
/// 飞书侧媒体转写契约的<b>派生命名门面</b>（N1 中立化后的 ④ 式保留）。
/// </summary>
/// <remarks>
/// 契约本体（含"不得把原始 base64 / 转写文本写日志或 Metrics tag"的安全约束）已上移
/// <see cref="Mud.AI.Abstractions.Media.IMediaTranscriber"/>；本接口只保留产品命名，
/// 使宿主既有实现（如基于飞书 OCR/ASR 的转写器）与 DI 注册点零改动。
/// <b>⚠️ 不得新增成员</b>（同 <see cref="AgentTools.IFeishuToolApprovalChannel"/> 的理由）。
/// </remarks>
public interface IFeishuMediaTranscriber : Mud.AI.Abstractions.Media.IMediaTranscriber
{
}
