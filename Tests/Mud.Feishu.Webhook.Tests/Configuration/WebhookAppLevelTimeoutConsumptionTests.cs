// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Mud.Feishu.Webhook.Configuration;

namespace Mud.Feishu.Webhook.Tests.Configuration;

// R5/X9：本文件专门验证 LegacyGlobalTimeoutOnly 这一 Obsolete 过渡开关的绑定与运行期语义
// （Obsolete 化只是下线预告，不得顺带切断绑定）。
#pragma warning disable CS0618

/// <summary>
/// B1：应用级 EventHandlingTimeoutMs 运行时消费 + LegacyGlobalTimeoutOnly 兼容闸。
/// </summary>
public class WebhookAppLevelTimeoutConsumptionTests
{
    private const int GlobalTimeoutMs = 30000;
    private const int AppTimeoutMs = 5000;

    [Fact]
    public void ResolveEventHandlingTimeoutMs_ShouldUseAppLevelValue_WhenPositive()
    {
        var options = new FeishuWebhookOptions { EventHandlingTimeoutMs = GlobalTimeoutMs };
        var app = new FeishuAppWebhookOptions { EventHandlingTimeoutMs = AppTimeoutMs };

        options.ResolveEventHandlingTimeoutMs(app).Should().Be(AppTimeoutMs);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(-1)]
    [InlineData(0)]
    public void ResolveEventHandlingTimeoutMs_ShouldInheritGlobal_WhenAppValueIsInheritSentinel(int? appTimeout)
    {
        var options = new FeishuWebhookOptions { EventHandlingTimeoutMs = GlobalTimeoutMs };
        var app = new FeishuAppWebhookOptions { EventHandlingTimeoutMs = appTimeout };

        options.ResolveEventHandlingTimeoutMs(app).Should().Be(GlobalTimeoutMs);
    }

    [Fact]
    public void ResolveEventHandlingTimeoutMs_ShouldUseGlobal_WhenAppConfigIsNull()
    {
        var options = new FeishuWebhookOptions { EventHandlingTimeoutMs = GlobalTimeoutMs };

        options.ResolveEventHandlingTimeoutMs(null).Should().Be(GlobalTimeoutMs);
    }

    [Fact]
    public void ResolveEventHandlingTimeoutMs_ShouldIgnoreAppOverride_WhenLegacyGlobalTimeoutOnlyTrue()
    {
        var options = new FeishuWebhookOptions
        {
            EventHandlingTimeoutMs = GlobalTimeoutMs,
            LegacyGlobalTimeoutOnly = true
        };
        var app = new FeishuAppWebhookOptions { EventHandlingTimeoutMs = AppTimeoutMs };

        options.ResolveEventHandlingTimeoutMs(app).Should().Be(GlobalTimeoutMs,
            "Legacy 兼容闸必须恢复修复前的全局-only 语义");
    }

    [Fact]
    public void Bind_ShouldMapLegacyGlobalTimeoutOnly_WhenConfigured()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeishuWebhook:LegacyGlobalTimeoutOnly"] = "true"
            })
            .Build();

        var options = new FeishuWebhookOptions();
        configuration.GetSection("FeishuWebhook").Bind(options);

        options.LegacyGlobalTimeoutOnly.Should().BeTrue();
    }

    [Fact]
    public void Bind_ShouldDefaultLegacyGlobalTimeoutOnlyToFalse()
    {
        var options = new FeishuWebhookOptions();
        options.LegacyGlobalTimeoutOnly.Should().BeFalse();
    }
}
