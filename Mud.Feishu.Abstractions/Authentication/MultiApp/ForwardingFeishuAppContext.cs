// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.Authentication.MultiApp;

/// <summary>
/// TMR2-P0-1：默认应用上下文（<see cref="IFeishuAppContext"/>）的转发代理。
/// </summary>
/// <remarks>
/// <para>
/// 与 <see cref="ForwardingTenantTokenManager"/> 同构：每次成员访问现取当前默认应用上下文，
/// 使 <c>SetDefaultApp</c> / 配置热更新 / <c>RemoveApp</c> 的变更对已注入消费者<b>立即生效</b>。
/// </para>
/// <para>
/// 修复前为「实例桥接」——DI 工厂委托返回 <c>GetDefaultApp()</c> 的<b>实例快照</b>，容器永久缓存：
/// 默认应用切换后注入方继续使用旧应用的 <c>Config</c>/令牌/认证客户端（跨应用凭据误用），
/// 热更新重建默认应用后注入方持有已进入退休队列、宽限期后被 Dispose 的上下文（ODE）。
/// </para>
/// <para>
/// <b>不参与生成代码的 <c>UseApp</c>/<c>BeginScope</c> 路径</b>：该职责归
/// <see cref="IFeishuAppContextSwitcher"/>（由生成的 API 客户端实现），
/// <see cref="FeishuAppContext"/> 本身不实现该接口，因此替换为代理对功能面无影响，仅消除"钉死"。
/// </para>
/// <para>
/// 代理自身<b>无状态、不持有令牌与资源</b>，<see cref="IDisposable.Dispose"/> 为显式 no-op
/// （<c>IFeishuAppContext : IDisposable</c> 强制要求实现）；真实释放由
/// <c>FeishuAppManager</c> → <c>FeishuAppContextRetirement</c> 承担。
/// </para>
/// <para>
/// AOT：纯接口转发，无反射/动态代码，<c>IL2026</c>/<c>IL3050</c> 无涉；netstandard2.0 起全 TFM 可用。
/// </para>
/// </remarks>
internal sealed class ForwardingFeishuAppContext : IFeishuAppContext
{
    private readonly Func<IFeishuAppContext> _resolve;

    /// <summary>
    /// 初始化转发代理。
    /// </summary>
    /// <param name="resolve">现取当前默认应用上下文的解析委托（每次成员访问调用）。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="resolve"/> 为 null 时抛出。</exception>
    public ForwardingFeishuAppContext(Func<IFeishuAppContext> resolve)
        => _resolve = resolve ?? throw new ArgumentNullException(nameof(resolve));

    // === IMudAppContext 成员（现取转发） ===

    /// <inheritdoc />
    public string AppKey => _resolve().AppKey;

    /// <inheritdoc />
    public IEnhancedHttpClient HttpClient => _resolve().HttpClient;

    /// <inheritdoc />
    public ITokenManager GetTokenManager(string tokenType) => _resolve().GetTokenManager(tokenType);

    /// <inheritdoc />
    public T GetTokenManager<T>() where T : class, ITokenManager => _resolve().GetTokenManager<T>();

    /// <inheritdoc />
    public T? GetService<T>() where T : class => _resolve().GetService<T>();

    // === IFeishuAppContext 成员（现取转发） ===

    /// <inheritdoc />
    public FeishuAppConfig Config => _resolve().Config;

    /// <inheritdoc />
    public IFeishuAuthentication Authentication => _resolve().Authentication;

    /// <inheritdoc />
    public ITenantTokenManager TenantTokenManager => _resolve().TenantTokenManager;

    /// <inheritdoc />
    public IAppTokenManager AppTokenManager => _resolve().AppTokenManager;

    /// <inheritdoc />
    public IFeishuUserTokenManager UserTokenManager => _resolve().UserTokenManager;

    /// <summary>
    /// 显式 no-op：代理不持有任何资源，真实释放归退休队列生命周期。
    /// R1-1 风险：宿主若把注入的 <see cref="IFeishuAppContext"/> 强转为具体
    /// <see cref="FeishuAppContext"/> 将抛 <see cref="InvalidCastException"/>；
    /// 置 <c>FeishuAppOptions.ForwardDefaultAppContext = false</c> 可回退实例快照语义。
    /// </summary>
    void IDisposable.Dispose()
    {
    }
}
