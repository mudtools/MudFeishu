// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Mud.Feishu.Abstractions.Authentication.MultiApp;
using Xunit;

namespace Mud.Feishu.Abstractions.Tests.Authentication;

/// <summary>
/// 验证 <see cref="FeishuHttpClientFactory"/> 的装配收敛行为（ARC-2 Step 1）。
/// </summary>
/// <remarks>
/// 修复前 <c>FeishuAppManager.CreateEnhancedHttpClientOptions</c> 手工 <c>new EnhancedHttpClientOptions</c>，
/// 完全忽略 DI 中注册的 <see cref="EnhancedHttpClientOptions"/> 编程式基线，
/// 导致 10 个字段静默取默认值，与 <c>AddMudHttpClient</c> 注册路径形成配置双源。
/// </remarks>
public class FeishuHttpClientFactoryTests
{
    private static IServiceProvider BuildProvider(Action<EnhancedHttpClientOptions>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddHttpClient();
        if (configure != null)
        {
            services.Configure(configure);
        }
        return services.BuildServiceProvider();
    }

    /// <summary>
    /// DI 中注册的 EnhancedHttpClientOptions 基线必须被完整继承（修复前全部丢失）。
    /// </summary>
    [Fact]
    public void CreateOptions_ShouldInheritBaseline_WhenEnhancedHttpClientOptionsConfigured()
    {
        var provider = BuildProvider(o =>
        {
            o.MaxSuccessResponseBytes = 4096;
            o.CaptureRequestContent = true;
            o.AllowCustomBaseUrls = true;
            o.MaxExceptionContentLength = 2048;
        });

        var factory = new FeishuHttpClientFactory(provider);
        var options = factory.CreateOptions("cli_a");

        options.MaxSuccessResponseBytes.Should().Be(4096);
        options.CaptureRequestContent.Should().BeTrue();
        options.AllowCustomBaseUrls.Should().BeTrue();
        options.MaxExceptionContentLength.Should().Be(2048);
    }

    /// <summary>
    /// 未注册基线时，全部字段应为类型默认值，且不得抛异常。
    /// </summary>
    [Fact]
    public void CreateOptions_ShouldReturnDefaults_WhenNoBaselineConfigured()
    {
        var factory = new FeishuHttpClientFactory(BuildProvider());

        var options = factory.CreateOptions("cli_a");

        options.Should().NotBeNull();
        options.MaxSuccessResponseBytes.Should().Be(0);
        options.CaptureRequestContent.Should().BeFalse();
    }

    /// <summary>
    /// 不同应用必须得到彼此独立的客户端实例（命名客户端 feishu-{appKey} 一一对应）。
    /// </summary>
    [Fact]
    public void CreateBasic_ShouldReturnDistinctInstances_PerApp()
    {
        var factory = new FeishuHttpClientFactory(BuildProvider());

        var a = factory.CreateBasic("cli_a");
        var b = factory.CreateBasic("cli_b");

        a.Should().NotBeSameAs(b);
        factory.CreateBasic("cli_a").Should().NotBeSameAs(a);
    }

    /// <summary>
    /// Create 必须不接受 null 恢复执行器。
    /// </summary>
    [Fact]
    public void Create_ShouldThrow_WhenRecoveryExecutorIsNull()
    {
        var factory = new FeishuHttpClientFactory(BuildProvider());

        Assert.Throws<ArgumentNullException>(() => factory.Create("cli_a", null!));
    }

    /// <summary>
    /// Create 应返回 TokenRecoveryEnhancedClient（官方推荐路径）。
    /// </summary>
    [Fact]
    public void Create_ShouldReturnTokenRecoveryClient_WhenExecutorProvided()
    {
        var provider = BuildProvider();
        var factory = new FeishuHttpClientFactory(provider);
        var executor = new TokenRecoveryExecutor(
            new Mock<ITokenManager>().Object,
            new Mock<IUserTokenManager>().Object);

        var client = factory.Create("cli_a", executor);

        client.Should().BeOfType<TokenRecoveryEnhancedClient>();
    }

    /// <summary>
    /// 防退化守卫：<see cref="FeishuHttpClientFactory"/> 的 Clone 必须覆盖
    /// <see cref="EnhancedHttpClientOptions"/> 的**全部**公共属性。
    /// </summary>
    /// <remarks>
    /// 组件升级（如 2.0.4 → 2.0.5）若新增配置属性而本仓库 Clone 未同步补全，
    /// 会导致新属性在 MudFeishu 路径上被静默丢弃（且不会有任何编译错误）。
    /// 本用例把属性清单固化为契约：组件新增字段时此处会失败，强制同步 Clone。
    /// 期望清单需与 <c>EnhancedHttpClientOptions</c> 中按 TFM 的 <c>#if</c> 条件保持一致。
    /// </remarks>
    [Fact]
    public void EnhancedHttpClientOptions_PropertySet_ShouldMatchClonedContract()
    {
        var expected = new List<string>
        {
            "AllowCustomBaseUrls",
            "AppAccessAuthorizer",
            "CaptureRequestContent",
            "ExceptionRedactor",
            "HttpRequestMessageOptions",
            "Logger",
            "MaxExceptionContentLength",
            "MaxSuccessResponseBytes",
            "RequestBodySerialization",
            "RequestInterceptors",
            "ResponseInterceptors",
            "SensitiveDataMasker",
            "UrlResolution",
#if NET6_0_OR_GREATER
            "HttpVersion",
            "HttpVersionPolicy",
#endif
#if NET8_0_OR_GREATER
            "JsonTypeInfoResolver",
#endif
        };

        var actual = typeof(EnhancedHttpClientOptions)
            .GetProperties()
            .Select(p => p.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        actual.Should().Equal(expected.OrderBy(n => n, StringComparer.Ordinal).ToList(),
            "组件新增/删除配置属性时必须同步更新 FeishuHttpClientFactory.Clone 与本清单，否则新属性会被静默丢弃");
    }

    /// <summary>
    /// 客户端名称必须与 AddMudHttpClient 注册的命名保持一致。
    /// </summary>
    [Theory]
    [InlineData("cli_a", "feishu-cli_a")]
    [InlineData("cli_b", "feishu-cli_b")]
    public void BuildClientName_ShouldMatchRegistration(string appKey, string expected)
    {
        FeishuHttpClientFactory.BuildClientName(appKey).Should().Be(expected);
    }
}
