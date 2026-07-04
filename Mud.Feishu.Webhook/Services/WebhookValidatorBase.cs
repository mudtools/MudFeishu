// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Webhook.Services;

/// <summary>
/// Webhook 验证器公共基类
/// <para>提供应用键（AppKey）管理和日志记录的通用功能，消除各验证器中的重复代码</para>
/// </summary>
/// <remarks>
/// 各子验证器（TimestampValidator、NonceValidator、SignatureValidator、SubscriptionValidator、CompositeFeishuEventValidator）
/// 通过继承此基类自动获得 <see cref="CurrentAppKey"/> 属性和 <see cref="SetCurrentAppKey"/> 方法，
/// 同时满足各验证器接口中对 <c>SetCurrentAppKey</c> 方法的定义。
/// </remarks>
public abstract class WebhookValidatorBase
{
    /// <summary>
    /// 应用键上下文访问器
    /// </summary>
    protected readonly IWebhookAppKeyAccessor AppKeyAccessor;

    /// <summary>
    /// 日志记录器
    /// </summary>
    protected readonly ILogger Logger;

    /// <summary>
    /// 初始化 Webhook 验证器基类
    /// </summary>
    /// <param name="appKeyAccessor">应用键上下文访问器</param>
    /// <param name="logger">日志记录器</param>
    protected WebhookValidatorBase(IWebhookAppKeyAccessor appKeyAccessor, ILogger logger)
    {
        AppKeyAccessor = appKeyAccessor ?? throw new ArgumentNullException(nameof(appKeyAccessor));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 获取当前应用键（优先从 IWebhookAppKeyAccessor 获取）
    /// </summary>
    protected string? CurrentAppKey => AppKeyAccessor.CurrentAppKey;

    /// <summary>
    /// 设置当前应用键（多应用场景）
    /// </summary>
    /// <param name="appKey">应用键，用于多应用场景下的上下文标识</param>
    public void SetCurrentAppKey(string appKey)
    {
        AppKeyAccessor.SetAppKey(appKey);
        Logger.LogDebug("设置当前应用键: {AppKey}", appKey);
    }
}
