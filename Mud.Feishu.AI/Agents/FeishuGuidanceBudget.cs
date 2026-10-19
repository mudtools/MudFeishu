// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Agents;

/// <summary>
/// 飞书域 guidance 资产的预算常量（<b>产品语义</b>，故留在飞书侧；装配机制在
/// <see cref="Mud.AI.Agent.Guidance.GuidanceComposer"/>）。
/// </summary>
/// <remarks>
/// 预算数值是<b>产品语义</b>，故留在飞书侧；装配器只接收 <c>maxGuidanceLength</c> 参数。
/// 三条约束（改动前先读 ADR-0002 的复算表）：
/// <list type="number">
/// <item>超限策略是<b>整域丢弃</b>且失败静默（仅装配结果的 <c>Truncated</c>/<c>OmittedDomains</c> 可查）
/// ⇒ 常量必须对当前全部域资产留有余量，否则尾部域"深化了内容却一域都进不了 prompt"。</item>
/// <item><b>只计域 guidance 本体</b>（含块间分隔符），不含宿主 <c>Instructions</c>——
/// 宿主指令长度不该决定域资产是否被注入。</item>
/// <item>恒定流程：<b>内容加长 → 先复算总量 → 再抬常量</b>。由守卫
/// <c>GuidanceAssetContractGuards</c> 的「全域拼装零丢弃」用例锁死。</item>
/// </list>
/// 当前取值与依据（对 23 域 ≈25,209 字符留 ≈30% 余量）：
/// documents/AIAgent/decisions/ADR-0002-guidance-budget.md
/// </remarks>
public static class FeishuGuidanceBudget
{
    /// <summary>域 guidance 总长硬上限（字符数；超限按域顺序整域丢弃）。</summary>
    public const int MaxGuidanceLength = 32768;
}
