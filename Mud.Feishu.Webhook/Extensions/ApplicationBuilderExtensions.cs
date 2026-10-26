// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Webhook;
using Mud.Feishu.Webhook.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 应用程序构建器扩展方法
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// 使用飞书多应用 Webhook 中间件（推荐）
    /// </summary>
    /// <param name="builder">应用程序构建器</param>
    /// <returns>应用程序构建器</returns>
    /// <exception cref="InvalidOperationException">
    /// <c>FeishuWebhook:Apps</c> 为空——挂载了中间件却没有任何应用可路由（W-04）。
    /// </exception>
    /// <remarks>
    /// <para>
    /// <b>为什么在这里 fail-fast</b>：<c>services.CreateFeishuWebhookServiceBuilder(...)</c> 只注册服务、
    /// 不承诺收事件，而本方法才表达"我要收事件"。因此"<c>Apps</c> 为空"只对**挂载了中间件**的宿主是错误——
    /// 既不会误伤"只注入 <see cref="IFeishuWebhookService"/> 手动驱动事件"的高级用法，
    /// 也不会让"配好了但一个事件都收不到"（原表现为全量 404）静默存在。
    /// </para>
    /// <para>
    /// 读取一次 <c>CurrentValue</c> 会触发完整的 Options 构建链（<c>PostConfigure</c> →
    /// <c>Validate()</c> → 前缀归一化 → D1 生产锁），因此配置类错误同样在此刻暴露为启动失败。
    /// </para>
    /// </remarks>
    public static IApplicationBuilder UseFeishuWebhook(this IApplicationBuilder builder)
    {
        if (builder is null) throw new ArgumentNullException(nameof(builder));

        var options = builder.ApplicationServices
            .GetRequiredService<IOptionsMonitor<FeishuWebhookOptions>>().CurrentValue;

        if (options.Apps.Count == 0)
        {
            throw new InvalidOperationException(
                "FeishuWebhook:Apps 为空——中间件将不处理任何 /{prefix}/{appKey} 请求，" +
                "飞书侧表现为持续重推直至丢弃事件。请二选一修复：" +
                "① services.CreateFeishuWebhookServiceBuilder(configuration)（从 FeishuWebhook:Apps 绑定）；" +
                "② services.CreateFeishuWebhookServiceBuilder(o => o.Apps[\"my-app\"] = new FeishuAppWebhookOptions { ... })。" +
                "若本宿主仅使用 IFeishuWebhookService 手动驱动事件，请勿调用 UseFeishuWebhook()。");
        }

        return builder.UseMiddleware<FeishuMultiAppMiddleware>();
    }

    /// <summary>
    /// 使用飞书 Webhook 限流中间件
    /// </summary>
    /// <param name="builder">应用程序构建器</param>
    /// <returns>应用程序构建器</returns>
    /// <remarks>
    /// 使用示例：
    /// <code>
    /// app.UseFeishuRateLimit()
    ///    .UseFeishuWebhook();
    /// </code>
    /// 注意：限流中间件应该在 Webhook 中间件之前注册
    /// </remarks>
    public static IApplicationBuilder UseFeishuRateLimit(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<FeishuRateLimitMiddleware>();
    }
}
