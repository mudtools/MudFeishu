// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.AI.Agent.Tools;
using Mud.Feishu.DataModels;

namespace Mud.Feishu.AI.Tools.Results;

/// <summary>
/// 飞书侧分页便捷重载（v2.3 P1-D）：<c>where TPage : ApiPageListResult</c> 约束版——
/// <b>按 v2.2 架构裁决留飞书适配层</b>：<c>ApiPageListResult</c> 是核心 SDK 分页契约
/// （<c>Mud.Feishu.Abstractions</c>），中立骨架不得反向依赖它。
/// </summary>
/// <remarks>
/// 约束版把"页状态怎么读"固化为唯一真相（<c>HasMore</c>/<c>PageToken</c>），
/// 于是 22 个工具都不必各自写一遍读法（写错一处就是"永远只翻一页"的静默缺陷）；
/// 翻页契约字段名与基类不一致的历史 DTO 走骨架的委托式无约束重载
/// （<c>ToolExecutor.FromPagedApiAsync(readPageState: …)</c>）。
/// </remarks>
internal static class ToolExecutorPagination
{
    /// <summary>统一"单页 / 自动翻页"出口（约束版）：<paramref name="project"/> 与 <paramref name="fetchPage"/> 同时服务两条路径。</summary>
    /// <typeparam name="TPage">单页结果类型（必须携带翻页契约）。</typeparam>
    /// <param name="executor">执行器骨架（工具名 + 截断上限已绑定）。</param>
    /// <param name="fetchPage">单次分页调用（<c>page_token</c> 为 <see langword="null"/> 表示首页）。</param>
    /// <param name="project"><b>唯一</b>的字段白名单投影（两条路径共用）。</param>
    /// <param name="fetchAll">模型传入的 <c>fetch_all</c>。</param>
    /// <param name="maxItems">模型传入的 <c>max_items</c>（可空 ⇒ 用宿主默认）。</param>
    /// <param name="pageToken">模型传入的 <c>page_token</c>（首页为 <see langword="null"/>）。</param>
    /// <param name="optionsDefaultMaxItems">宿主默认条目预算。</param>
    /// <param name="optionsDefaultMaxPages">宿主默认页数预算。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public static Task<ToolResult> FromPagedApiAsync<TPage>(
        this ToolExecutor executor,
        Func<string?, CancellationToken, Task<ToolApiOutcome<TPage>>> fetchPage,
        Func<TPage, JsonObject> project,
        bool? fetchAll,
        int? maxItems,
        string? pageToken,
        int optionsDefaultMaxItems,
        int optionsDefaultMaxPages,
        CancellationToken cancellationToken)
        where TPage : ApiPageListResult
        => executor.FromPagedApiAsync(
            fetchPage,
            project,
            static page => (page.HasMore, page.PageToken),
            fetchAll,
            maxItems,
            pageToken,
            optionsDefaultMaxItems,
            optionsDefaultMaxPages,
            cancellationToken);
}
