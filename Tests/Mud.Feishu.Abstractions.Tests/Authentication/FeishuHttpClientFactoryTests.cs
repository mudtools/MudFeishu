// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Mud.Feishu.Abstractions.Authentication.MultiApp;
using System.Net;
using System.Reflection;
using System.Text.Encodings.Web;
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
        // TMX-19（组件 2.0.5）：快照构造已 internal，统一改用 IOptionsMonitor 公共构造（TMR-07 热更新路径）
        var executor = new TokenRecoveryExecutor(
            new Mock<ITokenManager>().Object,
            new Mock<IUserTokenManager>().Object,
            currentUserContext: null,
            optionsMonitor: Mock.Of<IOptionsMonitor<TokenRecoveryOptions>>(m => m.CurrentValue == new TokenRecoveryOptions()));

        var client = factory.Create("cli_a", executor);

        client.Should().BeOfType<TokenRecoveryEnhancedClient>();
    }

    /// <summary>
    /// F-05 防退化守卫：<see cref="EnhancedHttpClientOptions.Clone"/>（上游 3.0.5 公开 API）
    /// 必须复制全部公共可写属性，且 <c>FeishuHttpClientFactory.CreateOptions</c> 走该路径。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 原「硬编码属性清单契约」（2.0.4 时代守护手写 Clone）已废弃：手写清单在上游 3.0.5 新增
    /// <c>JsonEncoder</c> 时即漏拷（阶段 0 门禁实测红于本类
    /// <c>EnhancedHttpClientOptions_PropertySet_ShouldMatchClonedContract</c>），证明清单式守护
    /// 不可持续。现改为两层反射断言：
    /// </para>
    /// <para>
    /// ① <b>哨兵覆盖完整性</b>：枚举全部公共可写属性，要求哨兵源均为非默认值——上游新增属性而
    /// 下方初始化器未登记哨兵时在此失败，强制补齐；<br/>
    /// ② <b>全字段复制</b>：Clone 后逐属性一致（浅拷贝共享引用）。
    /// </para>
    /// </remarks>
    [Fact]
    public void Clone_ShouldCopyAllWritableProperties_WhenSourceHasNonDefaultSentinels()
    {
        // 哨兵源：所有公共可写属性均赋予非默认值（上游新增属性时，下方覆盖完整性断言失败并提示补哨兵）。
        var source = new EnhancedHttpClientOptions
        {
            Logger = Mock.Of<ILogger>(),
            RequestInterceptors = new[] { Mock.Of<IHttpRequestInterceptor>() },
            ResponseInterceptors = new[] { Mock.Of<IHttpResponseInterceptor>() },
            SensitiveDataMasker = Mock.Of<ISensitiveDataMasker>(),
            AppAccessAuthorizer = Mock.Of<IAppAccessAuthorizer>(),
            AllowCustomBaseUrls = true,
            RequestBodySerialization = RequestBodySerializationMode.Buffered,
            ExceptionRedactor = Mock.Of<IExceptionRedactor>(),
            MaxExceptionContentLength = 2048,
            CaptureRequestContent = true,
            UrlResolution = UrlResolutionMode.Rfc3986,
            MaxSuccessResponseBytes = 4096,
            HttpRequestMessageOptions = new Dictionary<string, object?> { ["sentinel"] = "value" },
#if NET6_0_OR_GREATER
            HttpVersion = new Version(2, 0),
            HttpVersionPolicy = System.Net.Http.HttpVersionPolicy.RequestVersionExact,
#endif
#if NET8_0_OR_GREATER
            JsonTypeInfoResolver = Mock.Of<System.Text.Json.Serialization.Metadata.IJsonTypeInfoResolver>(),
#endif
            JsonEncoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        var clone = source.Clone();

        var writable = typeof(EnhancedHttpClientOptions)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite)
            .ToList();
        writable.Should().NotBeEmpty();

        // ① 覆盖完整性：哨兵源不得残留默认值（上游新增可写属性而哨兵未同步时在此失败）。
        foreach (var prop in writable)
        {
            var value = prop.GetValue(source);
            if (prop.PropertyType.IsValueType)
            {
                value.Should().NotBe(Activator.CreateInstance(prop.PropertyType),
                    $"哨兵源必须为属性 {prop.Name} 赋非默认值（上游新增属性需在此测试初始化器中登记哨兵）");
            }
            else
            {
                value.Should().NotBeNull($"哨兵源必须为引用属性 {prop.Name} 赋非 null 值（上游新增属性需在此测试初始化器中登记哨兵）");
            }
        }

        // ② 全字段复制：Clone 后逐属性一致。
        foreach (var prop in writable)
        {
            prop.GetValue(clone).Should().Be(prop.GetValue(source),
                $"Clone 必须复制属性 {prop.Name}（F-05：CreateOptions 使用上游公开 Clone，禁止回退手写字段清单式拷贝）");
        }
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
