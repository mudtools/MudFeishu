// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Mud.Feishu.Abstractions;
using Mud.Feishu.Webhook;
using Mud.Feishu.Webhook.Configuration;

namespace Mud.Feishu.Webhook.Tests.Extensions;

/// <summary>
/// FeishuWebhookServiceBuilder 契约测试（P1-1/R2）：
/// PostConfigure 随 IOptionsMonitor 缓存重建重放，注册+冻结必须只在首次执行。
/// </summary>
public class FeishuWebhookServiceBuilderTests
{
    private sealed class TestAppHandler : IFeishuEventHandler
    {
        public string SupportedEventType => "test.event";
        public Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    /// <summary>
    /// 手动触发的 Options 变更令牌源（模拟配置热更）。
    /// ConfigurationReloadToken 为单次性令牌：触发后必须换发新实例，
    /// 否则 OptionsManager 在热更重注册时会命中已消费的令牌导致运行中止。
    /// </summary>
    private sealed class ManualChangeTokenSource : IOptionsChangeTokenSource<FeishuWebhookOptions>
    {
        private ConfigurationReloadToken _token = new();
        public string Name => Options.DefaultName;
        public IChangeToken GetChangeToken() => _token;
        public void FireChange() => Interlocked.Exchange(ref _token, new ConfigurationReloadToken())?.OnReload();
    }

    /// <summary>首次 Options 构建时合法、第二次（热更重建）注入非法值的配置器</summary>
    private sealed class SecondBuildInvalidatingConfigurer : IConfigureOptions<FeishuWebhookOptions>
    {
        private int _runs;
        public void Configure(FeishuWebhookOptions options)
        {
            if (Interlocked.Increment(ref _runs) >= 2)
                options.EventHandlingTimeoutMs = 100; // Validate 要求 >= 1000
        }
    }

    [Fact]
    public void Build_RegistryRegistration_ShouldRunExactlyOnce_AcrossOptionsRebuilds()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var changeTokenSource = new ManualChangeTokenSource();
        services.AddSingleton<IOptionsChangeTokenSource<FeishuWebhookOptions>>(changeTokenSource);

        services.CreateFeishuWebhookServiceBuilder(options =>
            {
                options.EventHandlingTimeoutMs = 5000;
            })
            .AddHandler<TestAppHandler>("app-001")
            .Build();

        using var provider = services.BuildServiceProvider();
        var monitor = provider.GetRequiredService<IOptionsMonitor<FeishuWebhookOptions>>();
        var handlerRegistry = provider.GetRequiredService<FeishuWebhookHandlerRegistry>();

        // 首次 Options 构建：注册+冻结执行（首个 CurrentValue 访问触发）
        monitor.CurrentValue.Should().NotBeNull();
        handlerRegistry.HasAny("app-001").Should().BeTrue("首次 Options 构建应完成应用处理器注册");
        handlerRegistry.GetAll("app-001").Should().ContainSingle(t => t == typeof(TestAppHandler));

        // Act：模拟热更——change token 触发缓存失效，下一次 CurrentValue 重建并重放 PostConfigure
        changeTokenSource.FireChange();

        // Assert：修复前此处抛 InvalidOperationException("注册表已冻结...")，热更后所有请求 500；
        // 修复后 CurrentValue 正常解析
        monitor.CurrentValue.Should().NotBeNull();

        // 注册表内容不变：未被重复注册、未被解冻绕过（仍只有 1 个处理器类型）
        handlerRegistry.GetAll("app-001").Should().ContainSingle(t => t == typeof(TestAppHandler),
            "热更重放不得重复注册处理器");

        // 再次热更仍稳定
        changeTokenSource.FireChange();
        monitor.CurrentValue.Should().NotBeNull();
        handlerRegistry.GetAll("app-001").Should().ContainSingle(t => t == typeof(TestAppHandler));
    }

    [Fact]
    public void Build_OptionsValidation_ShouldRunOnEveryOptionsRebuild()
    {
        // Arrange：验证逻辑必须留在一次性守卫之外——每次重建都应执行 options.Validate()
        var services = new ServiceCollection();
        services.AddLogging();
        var changeTokenSource = new ManualChangeTokenSource();
        services.AddSingleton<IOptionsChangeTokenSource<FeishuWebhookOptions>>(changeTokenSource);

        services.CreateFeishuWebhookServiceBuilder(options =>
            {
                options.EventHandlingTimeoutMs = 5000;
            })
            .AddHandler<TestAppHandler>("app-001")
            .Build();

        // 注意：必须注册在 Build() 之后——builder 的 configure lambda 在 Build() 时才进入
        // ServiceCollection；本配置器若先注册，会在热更重建时被 builder lambda 覆盖回合法值。
        services.AddSingleton<IConfigureOptions<FeishuWebhookOptions>, SecondBuildInvalidatingConfigurer>();

        using var provider = services.BuildServiceProvider();
        var monitor = provider.GetRequiredService<IOptionsMonitor<FeishuWebhookOptions>>();

        // 首次构建：配置合法
        monitor.CurrentValue.Should().NotBeNull();

        // Act：热更触发第二次构建（配置器注入非法 EventHandlingTimeoutMs）
        // 注：容器内存在 OnChange 监听器时，OptionsMonitor 会在失效回调中急切重建，
        // 校验异常因此同步浮出于 FireChange（AggregateException 包裹）；否则延迟到
        // 下一次 CurrentValue 访问。两种浮出位置语义等价——重建必触发校验。
        var fireEx = Record.Exception(changeTokenSource.FireChange);

        // Assert：Validate 在热更路径上仍被执行——非法配置被拒绝
        if (fireEx is null)
        {
            var act = () => monitor.CurrentValue;
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*EventHandlingTimeoutMs*");
        }
        else
        {
            fireEx.Should().BeOfType<AggregateException>(
                "急切重建路径上，校验异常经 ChangeToken 回调聚合后浮出");
            fireEx.InnerException.Should().BeOfType<InvalidOperationException>()
                .Which.Message.Should().Contain("EventHandlingTimeoutMs");
        }
    }
}
