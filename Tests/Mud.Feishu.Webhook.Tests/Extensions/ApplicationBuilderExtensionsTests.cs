// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Mud.Feishu.Webhook.Configuration;

namespace Mud.Feishu.Webhook.Tests.Extensions;

/// <summary>
/// W-04：<c>UseFeishuWebhook()</c> 的 fail-fast 契约。
/// </summary>
/// <remarks>
/// 历史形态：<c>Apps</c> 为空时中间件对任何 <c>/{prefix}/{appKey}</c> 请求都走
/// <c>_next</c>（→ 404），宿主"启动成功、日志干净、一个事件都收不到"，而飞书侧
/// 表现为持续重推直至放弃。fail-fast 放在"挂中间件"这一刻，既表达了"我要收事件"的意图，
/// 也不误伤"只注入 IFeishuWebhookService 手动驱动事件"的宿主。
/// </remarks>
public class ApplicationBuilderExtensionsTests
{
    private static IServiceProvider BuildProvider(FeishuWebhookOptions options)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IOptionsMonitor<FeishuWebhookOptions>>(
            new StaticOptionsMonitor(options));
        return services.BuildServiceProvider();
    }

    [Fact]
    public void UseFeishuWebhook_ShouldThrow_WhenAppsEmpty()
    {
        var app = new ApplicationBuilder(BuildProvider(new FeishuWebhookOptions()));

        var act = () => app.UseFeishuWebhook();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Apps 为空*");
    }

    [Fact]
    public void UseFeishuWebhook_ShouldNotThrow_WhenAppsConfigured()
    {
        var options = new FeishuWebhookOptions
        {
            Apps = new Dictionary<string, FeishuAppWebhookOptions>
            {
                ["app1"] = new FeishuAppWebhookOptions
                {
                    VerificationToken = "token",
                    EncryptKey = "0123456789abcdef0123456789abcdef"
                }
            }
        };

        var app = new ApplicationBuilder(BuildProvider(options));

        var act = () => app.UseFeishuWebhook();

        act.Should().NotThrow("配置齐全时必须能正常挂载");
    }

    /// <summary>只读的 OptionsMonitor 替身（UseFeishuWebhook 只读一次 CurrentValue）。</summary>
    private sealed class StaticOptionsMonitor : IOptionsMonitor<FeishuWebhookOptions>
    {
        public StaticOptionsMonitor(FeishuWebhookOptions value) => CurrentValue = value;

        public FeishuWebhookOptions CurrentValue { get; }

        public FeishuWebhookOptions Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<FeishuWebhookOptions, string?> listener) => null;
    }
}
