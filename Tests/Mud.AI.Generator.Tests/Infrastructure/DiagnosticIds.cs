// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.AI.Generator.Tests;

/// <summary>
/// 测试用例引用的<b>诊断 ID 常量</b>（字符串形态，便于与 <c>Diagnostic.Id</c> 直接比对）。
/// </summary>
/// <remarks>
/// v2.3 P1-B：随引擎迁入。常量<b>转发</b>仓内引擎的真相源
/// （<see cref="Mud.AI.ToolSurface.ProfileContractDiagnostics"/>），不复制字面量——
/// 组件仓与飞书仓的 ID 形态差异（MUDFT* / SDKT*）集中在引擎一处定义，
/// 测试侧不会出现第二份真相源。
/// </remarks>
internal static class DiagnosticIds
{
    /// <summary>剖面契约违例（缺必填槽 / 槽值非法）。</summary>
    public const string SdkToolProfileContractViolation =
        Mud.AI.ToolSurface.ProfileContractDiagnostics.SdkToolProfileContractViolationId;

    /// <summary>剖面缺必填槽。</summary>
    public const string SdkToolProfileMissingRequiredSlots =
        Mud.AI.ToolSurface.ProfileContractDiagnostics.SdkToolProfileMissingRequiredSlotsId;
}
