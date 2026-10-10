// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Configuration;

namespace Mud.Feishu.Agent.Demo.Tests;

/// <summary>
/// <see cref="DocAgentSettings"/> 的配置面治理：fail-fast、HTTPS 端点、白名单互斥。
/// </summary>
public class DocAgentSettingsTests
{
    /// <summary>
    /// 缺任一必填项即抛错，且异常消息**指明配置文件键**（否则排障只能靠猜）。
    /// 模型与飞书凭证统一取自 <c>FeishuDemo</c> 节，故错误消息指向 <c>FeishuDemo:*</c>。
    /// </summary>
    /// <param name="missing">被抽掉的键名（节 <c>FeishuDemo</c> 下）。</param>
    [Theory]
    [InlineData(DocAgentSettings.KeyModelId)]
    [InlineData(DocAgentSettings.KeyApiKey)]
    [InlineData(DocAgentSettings.KeyAppId)]
    [InlineData(DocAgentSettings.KeyAppSecret)]
    public void FromConfiguration_ShouldFailFast_WhenMissingRequired(string missing)
    {
        var values = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            [$"{FeishuDemoSettings.SectionName}:{DocAgentSettings.KeyModelId}"] = "test-model",
            [$"{FeishuDemoSettings.SectionName}:{DocAgentSettings.KeyApiKey}"] = "sk-test",
            [$"{FeishuDemoSettings.SectionName}:{DocAgentSettings.KeyAppId}"] = "cli_test",
            [$"{FeishuDemoSettings.SectionName}:{DocAgentSettings.KeyAppSecret}"] = "secret",
        };
        values[$"{FeishuDemoSettings.SectionName}:{missing}"] = null;

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();

        var act = () => DocAgentSettings.FromConfiguration(configuration);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{FeishuDemoSettings.SectionName}:{missing}*", "错误消息必须指明缺哪个配置键");
    }

    /// <summary>非 HTTPS 且非环回端点必须被拒（与 SDK 的 EnsureHttpsEndpoint 同口径，但错误更早更友好）。</summary>
    [Fact]
    public void Validate_ShouldRejectNonHttpsEndpoint()
    {
        var settings = TestDoubles.CreateSettings() with { Endpoint = "http://api.example.com/v1" };

        var act = () => settings.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{FeishuDemoSettings.SectionName}:{DocAgentSettings.KeyEndpoint}*");
    }

    /// <summary>环回地址走明文是允许的（本地自建模型端点）。</summary>
    [Fact]
    public void Validate_ShouldAcceptLoopbackEndpoint()
    {
        var settings = TestDoubles.CreateSettings() with { Endpoint = "http://127.0.0.1:11434/v1" };

        var act = () => settings.Validate();

        act.Should().NotThrow();
    }

    /// <summary>读写白名单必须互斥（SDK 侧也会 fail-fast，但错误信息更晚）。</summary>
    [Fact]
    public void ReadonlyTools_Should_NotIntersectWriteTools()
    {
        DocAgentSettings.ReadonlyTools.Intersect(DocAgentSettings.WriteTools, StringComparer.Ordinal)
            .Should().BeEmpty("读写分离：同一工具不得同时出现在只读与写类白名单");

        DocAgentSettings.ReadonlyTools.Should().OnlyHaveUniqueItems();
        DocAgentSettings.WriteTools.Should().OnlyHaveUniqueItems();
    }

    /// <summary>非法策略字面量必须在装配前被拒。</summary>
    [Fact]
    public void Validate_ShouldRejectUnknownPolicy()
    {
        var settings = TestDoubles.CreateSettings() with { Policy = "whatever" };

        var act = () => settings.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{DocAgentSettings.SectionName}:{DocAgentSettings.KeyPolicy}*");
    }

    /// <summary>appKey 含 <c>:</c> 会破坏会话键可读性（键以 <c>:</c> 分段）。</summary>
    [Fact]
    public void Validate_ShouldRejectAppKeyWithSeparator()
    {
        var settings = TestDoubles.CreateSettings() with { AppKey = "demo:app" };

        var act = () => settings.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{DocAgentSettings.SectionName}:{DocAgentSettings.KeyAppKey}*");
    }

    /// <summary>密钥掩码不得泄漏原文（安全验收 S4 的可执行证据）。</summary>
    [Fact]
    public void Mask_ShouldNeverExposeFullSecret()
    {
        var masked = DocAgentSettings.Mask("sk-super-secret-value");

        masked.Should().NotContain("super-secret");
        DocAgentSettings.Mask(null).Should().Be("（未设置）");
    }
}