// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions;

/// <summary>
/// 飞书应用上下文切换接口
/// </summary>
/// <remarks>
/// <para>提供在不同飞书应用上下文之间切换的能力。</para>
/// <para>当系统配置了多个飞书应用时，可通过此接口快速切换当前使用的应用上下文。</para>
/// <para>此接口由飞书HTTP客户端API服务实现，用于支持多应用场景下的上下文管理。</para>
/// <para>
/// <b>Mud.HttpUtils 3.0.0 适配（上游 BC-27）</b>：上游 <c>IAppContextSwitcher</c> 已移除
/// <c>UseApp(string)</c> / <c>UseDefaultApp()</c> / <c>BeginScope(string)</c> 三个成员，
/// 生成器（非 HttpClient 模式）也不再默认发射它们。本接口<b>显式重声明</b>这三个成员以保持
/// <c>Mud.Feishu</c> 的对外契约不变 —— 该写法触发上游「接口自行声明即豁免」机制，
/// 使本仓库 188+ 个生成实现类继续发射这三个成员，行为与 2.0.9 完全一致。
/// </para>
/// <para>
/// <b>迁移建议</b>：新代码请使用 <see cref="IAppScopeSwitcher.UseAppScope(string)"/> /
/// <see cref="IAppScopeSwitcher.UseDefaultAppScope"/>（守卫完全相同，且返回 <see cref="IDisposable"/>、
/// 释放时<b>自动归还</b>上下文）。上述三个成员将在 <c>Mud.Feishu</c> 的下一个大版本随上游一并移除。
/// </para>
/// </remarks>
public interface IFeishuAppContextSwitcher : IAppContextSwitcher, IAppScopeSwitcher
{
    // ───────────── 以下三个成员为「Mud.HttpUtils 3.0.0 适配」新增的接续声明 ─────────────
    // 上游 BC-27 已从 IAppContextSwitcher 移除这三个成员。本接口显式重声明以保持对外契约不变；
    // 签名必须与上游原签名逐字一致（IMudAppContext UseApp(string) / IMudAppContext UseDefaultApp() /
    // IDisposable BeginScope(string)），否则上游生成器的「按名字 + 签名谓词」豁免判定不成立，
    // 188 个生成类会因成员缺失落入契约补全（HTTPCLIENT024（Error） + NotSupportedException 占位）。
    // 本组成员将在本 SDK 的下一个大版本随上游一并移除（届时下游须改用 IAppScopeSwitcher）。

    /// <summary>切换到指定应用上下文（<b>无作用域</b>：不会自动归还上下文）。</summary>
    /// <param name="appKey">应用标识。</param>
    /// <returns>切换后的应用上下文实例。</returns>
    [Obsolete("请改用 IAppScopeSwitcher.UseAppScope(appKey)：守卫完全相同（格式校验 + 授权判定 + 未注册授权器默认拒绝），且释放时自动归还上下文。Mud.Feishu 将在下一个大版本移除本成员。")]
    IMudAppContext UseApp(string appKey);

    /// <summary>切换到默认应用上下文（<b>无作用域</b>：不会自动归还上下文）。</summary>
    /// <returns>默认应用上下文实例。</returns>
    [Obsolete("请改用 IAppScopeSwitcher.UseDefaultAppScope()：守卫完全相同，且释放时自动归还上下文。Mud.Feishu 将在下一个大版本移除本成员。")]
    IMudAppContext UseDefaultApp();

    /// <summary>切换到指定应用并以作用域自动归还上下文。</summary>
    /// <param name="appKey">应用标识。</param>
    /// <returns>释放时恢复之前上下文的作用域对象。</returns>
    [Obsolete("请改用 IAppScopeSwitcher.UseAppScope(appKey)：二者为同一实现（逐行等价的别名），且返回类型同为 IDisposable。Mud.Feishu 将在下一个大版本移除本成员。")]
    IDisposable BeginScope(string appKey);
}
