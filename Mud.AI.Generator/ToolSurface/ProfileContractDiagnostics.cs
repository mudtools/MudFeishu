// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;

namespace Mud.AI.ToolSurface;

/// <summary>
/// 工具面引擎的<b>固定诊断</b>（SDKT 档位）：ID 与 category 均不随剖面变化，
/// 区别于剖面注入的动态 <c>{prefix}{slot}</c> ID（后者由 <see cref="ToolSurfaceDiagnostics"/> 槽位表管理）。
/// </summary>
/// <remarks>
/// <para>
/// v2.3 自组件仓迁入时，把原 <c>Mud.HttpUtils</c> 根命名空间下 <c>Diagnostics</c>/<c>DiagnosticIds</c>
/// 两个静态类中的 SDKT 子集抽到此文件——组件仓那两个类的主体是 HTTP 客户端生成诊断，不随竖井搬迁。
/// </para>
/// <para>
/// 编号约定（ToolSurface 设计文档 §6.1）：级别为 Error 但<b>不带</b> <c>NotConfigurable</c>——
/// 本诊断属「使用者改一行即可修复」（补特性或补接口实现），加标签会连坐抑制同编译中的其他诊断。
/// </para>
/// </remarks>
internal static class ProfileContractDiagnostics
{
    /// <summary>SDKT001：<c>ISdkToolProfile</c> 与 <c>[SdkToolProfile]</c> 未成对出现。</summary>
    public const string SdkToolProfileContractViolationId = "SDKT001";

    /// <summary>SDKT002：剖面 <c>[SdkToolProfile]</c> 缺少引擎必需槽位（该剖面被跳过，不产出任何工具面）。</summary>
    public const string SdkToolProfileMissingRequiredSlotsId = "SDKT002";

    /// <summary>SDKT001：<c>ISdkToolProfile</c> 与 <c>[SdkToolProfile]</c> 未成对出现。</summary>
    public static readonly DiagnosticDescriptor SdkToolProfileContractViolation = new(
        id: SdkToolProfileContractViolationId,
        title: "工具剖面契约不成对：接口与特性必须同时出现",
        messageFormat: "类型 {0} {1}——ISdkToolProfile 与 [SdkToolProfile] 必须成对出现：实现接口即声明剖面，其全部数据由特性承载（生成器无法执行属性 getter）。请{2}。",
        category: "Mud.AI.ToolSurface",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "工具 Schema 生成引擎的剖面接缝契约：标记接口 ISdkToolProfile 声明「本类型是剖面」，"
            + "[SdkToolProfile] 承载全部编译期命名事实常量。二者缺一即剖面无法被引擎读取.");

    /// <summary>SDKT002：剖面特性缺少引擎运行必需的槽位（该剖面被跳过，不产出任何工具面）。</summary>
    public static readonly DiagnosticDescriptor SdkToolProfileMissingRequiredSlots = new(
        id: SdkToolProfileMissingRequiredSlotsId,
        title: "工具剖面缺少必需槽位",
        messageFormat: "剖面 {0} 的 [SdkToolProfile] 缺少必需槽位：{1}——该剖面本次编译被跳过，不产出任何工具面产物。请补全上述槽位。",
        category: "Mud.AI.ToolSurface",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "引擎固定档位：剖面数据完整性守卫。缺失槽位清单由 SdkToolProfileModel.RequiredSlotNames 定义.");
}
