// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;

namespace Mud.AI.ToolSurface;

/// <summary>
/// 输出路径统一异常兜底（泛化上游 <c>FeishuToolSchemaGenerator.Guard&lt;T&gt;</c>，设计文档 §1.2-6）。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么是包装器而不是逐回调 try/catch</b>（根因 R-C）：行为用例的覆盖面 = 你想到的路径数，
/// 结构包装的覆盖面 = 代码里的实际路径数。把「每条注册路径都必须经本包装器」做成结构断言
/// （引擎测试侧元守卫），新增路径自动被覆盖。
/// </para>
/// <para>
/// <b><see cref="OperationCanceledException"/> 必须放行</b>：编译被取消不是生成器故障；
/// 上报兜底诊断（Error + 零容忍）会把「用户取消构建」变成「构建失败」。
/// </para>
/// <para>
/// <b>兜底诊断 ID 不再是静态 <c>MUDFT026</c>，而是 <c>{profile.DiagnosticPrefix}026</c></b>
/// （profile 注入的动态档位，经 <see cref="ToolSurfaceDiagnostics.Factory"/> 取描述符）。
/// 多 profile 扇出下，<c>{prefix}026</c> 的上报在入口的<b>逐剖面循环</b>内完成
/// （单剖面异常不阻断其余剖面）；<see cref="WrapUnchecked{T}"/> 是外层统一兜底，
/// 并覆盖「缺可信前缀无法拼 026」的 SDKT002 路径（退化为日志）。
/// </para>
/// </remarks>
internal static class Guard
{
    /// <summary>
    /// 入口管线的外层兜底：多剖面扇出路径的 <c>{prefix}026</c> 上报在
    /// <see cref="ToolSurfaceSourceGenerator"/> 的逐剖面循环内完成（单剖面异常不阻断其余剖面），
    /// 外层只需把「循环自身逸出」的异常记日志；SDKT002 上报路径缺可信前缀（缺
    /// <c>DiagnosticPrefix</c> 正是其主题），同样走本兜底——退化为日志 + 构建继续，
    /// 不叠加第二条噪声诊断。
    /// </summary>
    public static Action<SourceProductionContext, T> WrapUnchecked<T>(
        string product,
        Action<SourceProductionContext, T> emit)
        => (spc, input) =>
        {
            try
            {
                emit(spc, input);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                GeneratorDebugLogger.LogError(product, ex);
            }
        };
}