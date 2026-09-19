// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.Configuration;
using Mud.Feishu.Abstractions.Services;

namespace Mud.Feishu.Webhook.Tests.Configuration;

/// <summary>
/// B2/R1.2：Webhook 内存去重工厂——默认对齐 Consts；有 DeduplicationOptions 时被注入。
/// </summary>
public class WebhookDeduplicationFactoryOptionsTests
{
    private sealed class NoOpFeishuEventHandler : Mud.Feishu.Abstractions.IFeishuEventHandler
    {
        public string SupportedEventType => "test.event";
        public Task HandleAsync(Mud.Feishu.Abstractions.EventData eventData, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private static ServiceProvider BuildWebhookServices(Action<IServiceCollection>? preConfigure = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        preConfigure?.Invoke(services);
        services.CreateFeishuWebhookServiceBuilder(options =>
            {
                options.EventHandlingTimeoutMs = 30000;
            })
            .AddHandler<NoOpFeishuEventHandler>()
            .Build();
        return services.BuildServiceProvider();
    }

    private static TimeSpan GetPrivateTimeSpan(object instance, string fieldName)
    {
        // 私有字段声明在基类 MemoryDeduplicator<string> 上
        var field = typeof(MemoryDeduplicator<string>)
            .GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?? throw new InvalidOperationException($"未找到字段 {fieldName}");
        return (TimeSpan)field.GetValue(instance)!;
    }

    [Fact]
    public void WebhookDeduplicator_ShouldUseConstsDefaults_WhenNoDeduplicationOptionsRegistered()
    {
        using var sp = BuildWebhookServices();

        var dedup = sp.GetRequiredService<IFeishuEventDeduplicator>();

        dedup.Should().BeOfType<FeishuEventDeduplicator>();
        // MemoryDeduplicator 私有默认 24h/5min 与 Consts 48h/10min 不一致；
        // 工厂必须显式对齐 Consts。
        GetPrivateTimeSpan(dedup, "_cacheExpiration").Should().Be(
            TimeSpan.FromMilliseconds(Consts.DefaultCacheExpirationMs),
            "Webhook 内存去重 TTL 必须与 Consts.DefaultCacheExpirationMs 对齐");
        GetPrivateTimeSpan(dedup, "_processingTimeout").Should().Be(
            TimeSpan.FromMilliseconds(Consts.DefaultProcessingTimeoutMs),
            "Webhook 内存去重 ProcessingTimeout 必须与 Consts.DefaultProcessingTimeoutMs 对齐");
    }

    [Fact]
    public void WebhookDeduplicator_ShouldPreferConfiguredDeduplicationOptions_WhenRegistered()
    {
        var customTtl = TimeSpan.FromHours(2);
        var customProcessing = TimeSpan.FromMinutes(3);

        using var sp = BuildWebhookServices(services =>
        {
            services.Configure<DeduplicationOptions>(o =>
            {
                o.CacheExpiration = customTtl;
                o.ProcessingTimeout = customProcessing;
            });
        });

        var dedup = sp.GetRequiredService<IFeishuEventDeduplicator>();
        dedup.Should().BeOfType<FeishuEventDeduplicator>();

        GetPrivateTimeSpan(dedup, "_cacheExpiration").Should().Be(customTtl);
        GetPrivateTimeSpan(dedup, "_processingTimeout").Should().Be(customProcessing);
    }
}
