// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.AgentTools;

/// <summary>
/// 租户上下文作用域工厂：切换 <c>appKey</c> 的唯一入口（TMA2-20：租户 = 应用上下文）。
/// </summary>
/// <remarks>
/// <para>
/// 生成的 HTTP 客户端共享单例 <c>IAppContextHolder</c>（默认 <c>AsyncLocalAppContextSwitcher</c>），
/// 任意一个客户端切换环境应用上下文都会在异步流内<b>对所有客户端同时生效</b>——故本抽象只需
/// 「切到目标 appKey」这一个动作，不需要知道调用方接下来用哪个业务域客户端。
/// </para>
/// <para>
/// <b>接口归属（R3-1 步骤 0）</b>：本抽象只依赖 <see cref="IDisposable"/>，与任何飞书业务域无关，
/// 因此归 AI 底座包（<c>Mud.Feishu.AI</c>）；具体实现（依赖 <c>IAppContextHolder</c> /
/// <c>IFeishuAppManager</c>）留在工具包 <c>Mud.Feishu.AI.Tools</c>——
/// 依赖方向保持 FeishuTools → AI <b>单向</b>，基类因此得以在回复前主动切租户（R3-1/2）。
/// </para>
/// <para>
/// <b>fail-closed</b>：作用域无法建立时不得回退默认应用（多租户隔离禁止默认应用兜底）。
/// </para>
/// </remarks>
public interface IFeishuAppContextScopeFactory
{
    /// <summary>
    /// 切换环境应用上下文到 <paramref name="appKey"/>；返回的作用域释放时恢复原上下文。
    /// </summary>
    /// <param name="appKey">应用唯一标识（非空且格式合法，否则实现必须显式失败）。</param>
    /// <returns>作用域（<see cref="IDisposable.Dispose"/> 恢复原上下文）。</returns>
    IDisposable BeginScope(string appKey);
}