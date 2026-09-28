// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mud.Feishu.Abstractions.Configuration;
using Mud.Feishu.Abstractions.Extensions;

namespace Mud.Feishu.Abstractions.Tests.Configuration;

/// <summary>
/// R5.4-R1：F1（代码路径激活统一节）+ F11（前缀两两互异）防回归。
/// </summary>
public class R54DeduplicationActivationTests
{
    [Fact]
    public void AddFeishuDeduplicationOptions_ProfileOverload_ShouldActivateUnifiedSection_WithoutPhysicalSection()
    {
        // F1：此前 Profile 重载从不写 IsConfiguredFromConfiguration，下游三包以该标志为唯一激活闸门
        // → Profile/字段级配置静默无效。
        var services = new ServiceCollection();
        services.AddFeishuDeduplicationOptions(DeduplicationProfile.HighReliability);

        var options = services.BuildServiceProvider()
            .GetRequiredService<IOptions<FeishuDeduplicationOptions>>().Value;

        options.IsConfiguredFromConfiguration.Should().BeTrue(
            "显式调用 AddFeishuDeduplicationOptions(Profile) 等价于声明使用统一节，须激活下游消费闸门");
        options.Profile.Should().Be(FeishuDeduplicationOptions.ProfileHighReliability);
    }

    [Fact]
    public void AddFeishuDeduplicationOptions_ProfileOverload_WithOverride_ShouldStillActivate()
    {
        var services = new ServiceCollection();
        services.AddFeishuDeduplicationOptions(DeduplicationProfile.Default, o =>
        {
            o.Event = new DeduplicationEntryOptions { Ttl = TimeSpan.FromHours(3) };
        });

        var options = services.BuildServiceProvider()
            .GetRequiredService<IOptions<FeishuDeduplicationOptions>>().Value;

        options.IsConfiguredFromConfiguration.Should().BeTrue();
        options.Event!.Ttl.Should().Be(TimeSpan.FromHours(3));
    }

    [Fact]
    public void DeduplicationPrefixes_ShouldFail_WhenAnyTwoAreIdentical()
    {
        // F11：TMA2-20 要求三前缀两两互异；此前仅拦「全部相同」（Distinct().Count()==1），
        // 「两个相同 + 一个不同」可绕过 → 多租户键空间部分重叠。
        var options = new FeishuDeduplicationOptions
        {
            Mode = FeishuDeduplicationOptions.ModeDistributed,
            Event = new DeduplicationEntryOptions { KeyPrefix = "tenant-a:feishu:event:" },
            Nonce = new NonceDeduplicationOptions { KeyPrefix = "tenant-a:feishu:event:" }, // 与 Event 相同
            SeqId = new SeqIdDeduplicationOptions { KeyPrefix = "tenant-a:feishu:seqid:" }
        };

        var result = new FeishuDeduplicationOptionsValidator().Validate(null, options);

        result.Failed.Should().BeTrue("Event/Nonce 前缀相同须失败（两两互异）");
        result.FailureMessage.Should().Contain("两两互异");
    }

    [Fact]
    public void DeduplicationPrefixes_ShouldSucceed_WhenAllDistinct()
    {
        var options = new FeishuDeduplicationOptions
        {
            Mode = FeishuDeduplicationOptions.ModeDistributed,
            Event = new DeduplicationEntryOptions { KeyPrefix = "t-a:event:" },
            Nonce = new NonceDeduplicationOptions { KeyPrefix = "t-a:nonce:" },
            SeqId = new SeqIdDeduplicationOptions { KeyPrefix = "t-a:seqid:" }
        };

        var result = new FeishuDeduplicationOptionsValidator().Validate(null, options);

        result.Succeeded.Should().BeTrue();
    }
}