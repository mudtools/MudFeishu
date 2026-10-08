// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools;

/// <summary>
/// 工具结果整形钩子（AI-FD-D12 P1D-2a）：对白名单投影 + 截断后的最终结果做宿主侧整形
/// （摘要化裁剪、字段二次白名单等），在回填模型前调用。
/// </summary>
/// <remarks>
/// <para>
/// SDK 内建默认 = <b>无实现</b>（维持默认裁剪行为）；宿主按需注册（DI 注册任一实现即生效，
/// 仅取首个）。执行链内单实现失败记日志并回退默认整形结果（异常隔离，绝不中断执行链）。
/// </para>
/// </remarks>
public interface IToolResultShaper
{
    /// <summary>对下游白名单投影结果做最终整形（返回 null 表示维持默认裁剪行为）。</summary>
    /// <param name="toolName">工具名（契约表名，供按工具差异化整形）。</param>
    /// <param name="projectedResult">白名单投影 + 默认截断后的结果文本。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>整形后的文本；<see langword="null"/> 维持默认结果。</returns>
    Task<string?> ShapeAsync(string toolName, string projectedResult, CancellationToken cancellationToken = default);
}
