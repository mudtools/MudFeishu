// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions;

/// <summary>
/// 飞书应用上下文切换接口：在配置了多个飞书应用时，切换当前使用的应用上下文。
/// </summary>
/// <remarks>
/// 本接口由飞书 HTTP 客户端 API 服务实现。除继承的带作用域切换能力外，它还显式声明三个
/// <b>无作用域</b>切换成员（切换后<b>不归还</b>上下文），以维持对外契约与全部生成实现类的兼容性。
/// <para>
/// <b>约束</b>：这三个成员的签名必须与上游 <c>IAppContextSwitcher</c> 的原签名逐字一致，
/// 否则生成器不再豁免、生成实现类将落入契约补全（<c>HTTPCLIENT024</c> 错误 + 占位实现）。
/// 签名在本大版本内不得改动。
/// </para>
/// <para>
/// 新代码应改用 <see cref="IAppScopeSwitcher.UseAppScope(string)"/> /
/// <see cref="IAppScopeSwitcher.UseDefaultAppScope"/>：守卫完全相同，且释放时自动归还上下文。
/// </para>
/// </remarks>
public interface IFeishuAppContextSwitcher : IAppContextSwitcher, IAppScopeSwitcher
{
    /// <summary>切换到指定应用上下文（<b>无作用域</b>：不会自动归还上下文）。</summary>
    /// <param name="appKey">应用标识。</param>
    /// <returns>切换后的应用上下文实例。</returns>
    [Obsolete("改用 IAppScopeSwitcher.UseAppScope(appKey)：守卫相同且释放时自动归还上下文。将在 4.0 移除。")]
    IMudAppContext UseApp(string appKey);

    /// <summary>切换到默认应用上下文（<b>无作用域</b>：不会自动归还上下文）。</summary>
    /// <returns>默认应用上下文实例。</returns>
    [Obsolete("改用 IAppScopeSwitcher.UseDefaultAppScope()：守卫相同且释放时自动归还上下文。将在 4.0 移除。")]
    IMudAppContext UseDefaultApp();

    /// <summary>切换到指定应用并以作用域自动归还上下文。</summary>
    /// <param name="appKey">应用标识。</param>
    /// <returns>释放时恢复之前上下文的作用域对象。</returns>
    [Obsolete("改用 IAppScopeSwitcher.UseAppScope(appKey)：与前者为同一实现，返回类型同为 IDisposable。将在 4.0 移除。")]
    IDisposable BeginScope(string appKey);
}
