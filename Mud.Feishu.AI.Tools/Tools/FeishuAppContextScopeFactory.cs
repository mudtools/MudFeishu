// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Tools;

/// <summary>
/// <see cref="IFeishuAppContextScopeFactory"/> 默认实现（R3-5：零业务客户端依赖 + 无 Transient 捕获）。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么不经业务域客户端</b>：生成的 HTTP 客户端注册为<b>瞬时</b>（<c>AddTransient</c>），而本工厂
/// 以单例注册——旧实现直接持有 <c>IFeishuTenantV1Message</c>，既构成 Captive Dependency（TMA-13），
/// 又要求宿主必须注册 IM 域客户端，使「按域装配」（如只调 <c>AddFeishuBitableTools()</c>）在解析
/// <c>FeishuToolBinding</c> 时失败。
/// </para>
/// <para>
/// 本实现改为依赖两个<b>单例</b>：<c>IAppContextHolder</c>（切换器本体，
/// <c>AddTokenProvider</c> 已 <c>TryAddSingleton</c> 注册）与 <c>IFeishuAppManager</c>（appKey → 应用上下文）。
/// 二者与业务域无关，故任何按域装配组合都能解析。
/// </para>
/// <para>
/// <b>授权门禁同源（已知取舍，登记于 R3-5）</b>：<c>Mud.HttpUtils</c> 未提供
/// 「<c>string appKey</c> → 带门禁作用域」的公共辅助类型，故这里的四步门禁是生成器所发
/// <c>XxxClient.BeginScope(string appKey)</c> 的<b>同构复制</b>：
/// 格式校验 → 授权器非空 → <c>CanSwitchTo</c> → <c>BeginScope(context)</c>。
/// 生成器模板若演进，须同步本处；一致性由用例锁定。
/// </para>
/// </remarks>
internal sealed class FeishuAppContextScopeFactory : IFeishuAppContextScopeFactory
{
    /// <summary>
    /// 显式声明<b>公共</b>构造函数（而非主构造函数）：本类型由 DI 注册为
    /// <c>IFeishuAppContextScopeFactory</c> 的实现，而内置容器只通过
    /// <c>Type.GetConstructors()</c>（公共构造函数）激活类型——主构造函数的可访问性跟随类型
    /// （<c>internal</c>）时会在解析期报「未找到公共构造函数」。
    /// </summary>
    /// <param name="contextHolder">上下文切换器（单例）。</param>
    /// <param name="appManager">应用管理器（单例，appKey → 应用上下文）。</param>
    /// <param name="appAuthorizer">应用切换授权器（可空；为空时按 fail-closed 拒绝切换）。</param>
    public FeishuAppContextScopeFactory(
        Mud.HttpUtils.IAppContextHolder contextHolder,
        Mud.Feishu.Abstractions.IFeishuAppManager appManager,
        Mud.HttpUtils.IAppAccessAuthorizer? appAuthorizer = null)
    {
        _contextHolder = contextHolder ?? throw new ArgumentNullException(nameof(contextHolder));
        _appManager = appManager ?? throw new ArgumentNullException(nameof(appManager));
        _appAuthorizer = appAuthorizer;
    }

    /// <summary>
    /// appKey 格式非法的提示（与生成器所发客户端逐字对齐，便于宿主识别同一类失败）。
    /// </summary>
    private const string InvalidAppKeyMessage =
        "appKey 格式非法：只能由字母、数字、'.'、'_'、'-' 组成，首字符必须是字母或数字，长度不超过 128。";

    /// <summary>
    /// 未注册授权器时的提示（fail-closed：不静默放行）。
    /// </summary>
    private const string MissingAuthorizerMessage =
        "多应用切换需要授权器：请注册 IAppAccessAuthorizer 实现（例如 services.AddSingleton<IAppAccessAuthorizer, YourAuthorizer>()）；"
        + "若为单应用或完全受信场景，请显式注册 Mud.HttpUtils.AllowAllAppAccessAuthorizer 以表明放行意图。";

    private readonly Mud.HttpUtils.IAppContextHolder _contextHolder;
    private readonly Mud.Feishu.Abstractions.IFeishuAppManager _appManager;
    private readonly Mud.HttpUtils.IAppAccessAuthorizer? _appAuthorizer;

    /// <inheritdoc />
    public IDisposable BeginScope(string appKey)
    {
        if (!Mud.HttpUtils.AppKey.IsValid(appKey))
        {
            throw new ArgumentException(InvalidAppKeyMessage, nameof(appKey));
        }

        if (_appAuthorizer is null)
        {
            throw new InvalidOperationException(MissingAuthorizerMessage);
        }

        if (!_appAuthorizer.CanSwitchTo(appKey))
        {
            throw new UnauthorizedAccessException(
                "当前调用主体无权切换到目标应用（IAppAccessAuthorizer.CanSwitchTo 返回 false）。");
        }

        return _contextHolder.BeginScope(_appManager.GetApp(appKey));
    }
}