// -----------------------------------------------------------------------
//  作者:Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Mud.Feishu.AI.Tests.Extensions;

/// <summary>
/// 服务注册扩展：AddFeishuAgent 可解析、键控模型客户端工厂校验、配置节绑定（Phase 0 §7）。
/// </summary>
public class FeishuAgentServiceCollectionExtensionsTests
{
    [Fact]
    public void AddFeishuAgent_ShouldResolveAgent_WithKeyedChatClient()
    {
        var services = new ServiceCollection();
        // 端点指向环回地址（HTTPS 白名单的环回例外）——构造 OpenAIClient 不发起网络请求。
        services.AddFeishuOpenAIChatClient("test-model", "glm-4-flash", "sk-test", "http://localhost:9999/v4");
        services.AddFeishuAgent(configure: o =>
        {
            o.Instructions = "测试指令";
            o.ModelServiceKey = "test-model";
        });

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<FeishuAgent>().Should().NotBeNull();
        provider.GetRequiredService<AIAgent>().Should().BeSameAs(provider.GetRequiredService<FeishuAgent>());
        provider.GetRequiredService<IConversationStore>().Should().BeOfType<MemoryConversationStore>();
    }

    [Fact]
    public void AddFeishuAgent_ShouldResolveWithUnkeyedChatClient_WhenNoServiceKey()
    {
        var services = new ServiceCollection();
        var mockClient = new Mock<IChatClient>();
        services.AddSingleton<IChatClient>(mockClient.Object);
        services.AddFeishuAgent(configure: o => o.Instructions = "x");

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<FeishuAgent>().Should().NotBeNull();
    }

    [Fact]
    public void AddFeishuAgent_ShouldFailFast_WhenKeyedClientMissing()
    {
        var services = new ServiceCollection();
        services.AddFeishuAgent(configure: o =>
        {
            o.Instructions = "x";
            o.ModelServiceKey = "not-registered";
        });

        using var provider = services.BuildServiceProvider();
        var act = () => provider.GetRequiredService<FeishuAgent>();

        act.Should().Throw<InvalidOperationException>("键控 IChatClient 缺失须在工厂解析时失败（Phase 0 §8）");
    }

    [Fact]
    public void AddFeishuAgent_ShouldFailValidation_WhenInstructionsMissing()
    {
        var services = new ServiceCollection();
        var mockClient = new Mock<IChatClient>();
        services.AddSingleton<IChatClient>(mockClient.Object);
        services.AddFeishuAgent(); // 未设置 Instructions

        using var provider = services.BuildServiceProvider();
        var act = () => provider.GetRequiredService<FeishuAgent>();

        act.Should().Throw<OptionsValidationException>("IValidateOptions 经 Options 管线触发（netstandard2.0 首次解析触发）");
    }

    [Fact]
    public void AddFeishuAgent_ShouldBindConfigurationSection()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeishuAgent:Instructions"] = "来自配置的指令",
                ["FeishuAgent:MaxHistoryMessages"] = "7",
            })
            .Build();

        var services = new ServiceCollection();
        var mockClient = new Mock<IChatClient>();
        services.AddSingleton<IChatClient>(mockClient.Object);
        services.AddFeishuAgent(configuration);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<FeishuAgentOptions>>().Value;

        options.Instructions.Should().Be("来自配置的指令");
        options.MaxHistoryMessages.Should().Be(7);
    }

    [Fact]
    public void AddFeishuAgent_ShouldBindConversationOptionsSection()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeishuConversation:SessionTtl"] = "02:00:00",
            })
            .Build();

        var services = new ServiceCollection();
        var mockClient = new Mock<IChatClient>();
        services.AddSingleton<IChatClient>(mockClient.Object);
        services.AddFeishuAgent(configuration);

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IOptions<FeishuConversationOptions>>()
            .Value.SessionTtl.Should().Be(TimeSpan.FromHours(2),
                "FeishuConversation 节是 Memory/Redis 双后端的单一 TTL 阈值源");
    }

    /// <summary>探针工具源：记录收到的 <see cref="IServiceProvider"/> 以断言「根作用域」契约。</summary>
    private sealed class ProbeToolSource : FeishuAgentToolSource
    {
        public IServiceProvider? ObservedProvider { get; private set; }

        public override IReadOnlyList<AIFunction> GetTools(IServiceProvider serviceProvider)
        {
            ObservedProvider = serviceProvider;
            return [];
        }
    }

    /// <summary>Scoped 协作件（用于验证工具源拿到的是根作用域、解析不到 Scoped 服务）。</summary>
    private sealed class ScopedProbe;

    /// <summary>
    /// P2-10：工具源聚合发生在<b>根作用域</b>（Agent 是 Singleton），实现只能解析 Singleton。
    /// </summary>
    /// <remarks>
    /// 原方案建议「聚合点改用 <c>CreateScope()</c>」——那会把「启动期响亮失败」换成
    /// 「Scoped 实例被单例捕获、scope 释放后悬空」的静默缺陷，故本轮不采用，改为把契约测住：
    /// 以 <c>ValidateScopes = true</c> 构筑宿主，断言工具源拿到的 provider <b>解析不到</b> Scoped 服务。
    /// </remarks>
    [Fact]
    public void AddFeishuAgent_ToolSource_ShouldReceiveRootScopeOnly()
    {
        var services = new ServiceCollection();
        var mockClient = new Mock<IChatClient>();
        services.AddSingleton<IChatClient>(mockClient.Object);
        services.AddScoped<ScopedProbe>();
        var source = new ProbeToolSource();
        services.AddSingleton<FeishuAgentToolSource>(source);
        services.AddFeishuAgent(configure: o => o.Instructions = "x");

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        provider.GetRequiredService<FeishuAgent>().Should().NotBeNull();

        source.ObservedProvider.Should().NotBeNull("工具源必须被真实调用（否则本守卫是假绿）");
        var act = () => source.ObservedProvider!.GetService(typeof(ScopedProbe));
        act.Should().Throw<InvalidOperationException>(
            "工具源收到的是根作用域：解析 Scoped 协作件必须响亮失败（FeishuAgentToolSource 契约：只能解析 Singleton）");
    }

    [Fact]
    public void AddFeishuOpenAIChatClient_ShouldRejectNonHttpsPublicEndpoint()
    {
        var services = new ServiceCollection();
        var act = () => services.AddFeishuOpenAIChatClient("k", "m", "sk", "http://api.example.com/v4");

        act.Should().Throw<InvalidOperationException>("Base URL 白名单安全默认不得削弱");
    }

    [Fact]
    public void AddFeishuOpenAIChatClient_ShouldAllowLoopbackHttp()
    {
        var services = new ServiceCollection();
        var act = () => services.AddFeishuOpenAIChatClient("k", "m", "sk", "http://127.0.0.1:8080/v4");

        act.Should().NotThrow();
    }

    /// <summary>
    /// R5-7：IPv6 环回字面量的两种合法书写都必须命中「环回地址例外」。
    /// </summary>
    /// <remarks>
    /// <c>System.Uri</c> 会把 IPv6 字面量规范化为<b>带方括号、零压缩展开</b>的形态
    /// （<c>http://[::1]:8000/v1</c> ⇒ Host = <c>[0000:0000:0000:0000:0000:0000:0000:0001]</c>），
    /// 故「<c>Host == "::1"</c>」的字面量比较与 <c>IPAddress.TryParse("[::1]")</c> 都必然失败。
    /// 注意：无方括号的 <c>http://::1:8000/v1</c> 会被 <see cref="Uri"/> 直接判为非法格式，
    /// 不能作为用例数据（那不是「合法写法」，而是解析异常）。
    /// </remarks>
    [Theory]
    [InlineData("http://[::1]:8000/v1")]
    [InlineData("http://[0:0:0:0:0:0:0:1]:9000/v4")]
    public void AddFeishuOpenAIChatClient_ShouldAllowLoopbackHttp_ForIpv6Literal(string endpoint)
    {
        var services = new ServiceCollection();
        var act = () => services.AddFeishuOpenAIChatClient("k", "m", "sk", endpoint);

        act.Should().NotThrow("环回地址例外不得因 IPv6 书写形态而静默失效");
    }

    [Fact]
    public void AddFeishuOpenAIChatClient_ShouldRejectBlankArguments()
    {
        var services = new ServiceCollection();
        var act = () => services.AddFeishuOpenAIChatClient("", "m", "sk");

        act.Should().Throw<ArgumentException>();
    }
}
