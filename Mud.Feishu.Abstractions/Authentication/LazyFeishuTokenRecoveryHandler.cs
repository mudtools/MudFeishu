// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mud.Feishu.Abstractions.Authentication;

/// <summary>
/// 飞书令牌恢复延迟解析处理器
/// </summary>
/// <remarks>
/// <para>
/// C-1 修复：此处理器解决 <c>TokenRecoveryDelegatingHandler</c> 注册时的循环依赖问题。
/// </para>
/// <para>
/// 问题背景：<c>FeishuAppManager</c> 构造函数中调用 <c>CreateAppContext</c> → <c>CreateHttpClient</c> →
/// <c>IHttpClientFactory.CreateClient</c>，此时 <c>IFeishuAppManager</c> 尚未完成构造，
/// 若 Handler 工厂直接解析 <c>IFeishuAppManager</c> 将导致循环依赖异常。
/// </para>
/// <para>
/// 解决方案：此包装器在构造时不解析任何服务，仅存储 <c>IServiceProvider</c> 和 <c>appKey</c>。
/// 在首次 HTTP 请求（<see cref="SendAsync"/>）时才延迟解析 <c>IFeishuAppManager</c> 并创建实际的
/// <c>TokenRecoveryDelegatingHandler</c>，此时 <c>IFeishuAppManager</c> 已完成构造。
/// </para>
/// </remarks>
internal sealed class LazyFeishuTokenRecoveryHandler : DelegatingHandler
{
    private readonly IServiceProvider _serviceProvider;
    private readonly string _appKey;
    private TokenRecoveryDelegatingHandler? _recoveryHandler;
    private readonly object _lock = new();

    /// <summary>
    /// 初始化延迟解析的飞书令牌恢复处理器
    /// </summary>
    /// <param name="serviceProvider">DI 服务提供者</param>
    /// <param name="appKey">应用唯一标识，用于解析对应的 TokenManager</param>
    public LazyFeishuTokenRecoveryHandler(IServiceProvider serviceProvider, string appKey)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        if (string.IsNullOrWhiteSpace(appKey))
            throw new ArgumentException("AppKey 不能为空", nameof(appKey));
        _appKey = appKey;
    }

    /// <summary>
    /// 延迟创建实际的 TokenRecoveryDelegatingHandler 并将其插入到 InnerHandler 管道中。
    /// </summary>
    /// <remarks>
    /// DelegatingHandler.SendAsync 是 protected internal 方法，不能跨程序集直接调用。
    /// 正确做法是将 TokenRecoveryDelegatingHandler 插入到 InnerHandler 链中，
    /// 然后通过 base.SendAsync 让管道自动委托调用。
    /// </remarks>
    private void EnsureHandler()
    {
        if (_recoveryHandler != null)
            return;

        lock (_lock)
        {
            if (_recoveryHandler != null)
                return;

            var appManager = _serviceProvider.GetRequiredService<IFeishuAppManager>();
            var appContext = appManager.GetApp(_appKey);

            var handler = new TokenRecoveryDelegatingHandler(
                appContext.TenantTokenManager,
                appContext.UserTokenManager as IUserTokenManager,
                _serviceProvider.GetService<ICurrentUserContext>(),
                _serviceProvider.GetService<IOptions<TokenRecoveryOptions>>()?.Value,
                _serviceProvider.GetService<ILogger<TokenRecoveryDelegatingHandler>>());

            // 将 TokenRecoveryDelegatingHandler 插入到 InnerHandler 链中：
            // 原管道: this -> [originalInnerHandler]
            // 新管道: this -> TokenRecoveryDelegatingHandler -> [originalInnerHandler]
            handler.InnerHandler = InnerHandler;
            InnerHandler = handler;

            _recoveryHandler = handler;
        }
    }

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        EnsureHandler();
        // base.SendAsync 会调用 InnerHandler.SendAsync，即 TokenRecoveryDelegatingHandler.SendAsync
        return base.SendAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // 释放实际的恢复处理器
            // 注意：TokenRecoveryDelegatingHandler.Dispose 会调用 InnerHandler.Dispose，
            // 但 HttpMessageHandler.Dispose 内部有 _disposed 标记，双重释放是安全的幂等操作
            _recoveryHandler?.Dispose();
        }

        base.Dispose(disposing);
    }
}
