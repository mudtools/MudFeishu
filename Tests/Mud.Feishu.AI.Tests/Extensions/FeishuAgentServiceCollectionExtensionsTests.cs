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

    [Fact]
    public void AddFeishuOpenAIChatClient_ShouldRejectBlankArguments()
    {
        var services = new ServiceCollection();
        var act = () => services.AddFeishuOpenAIChatClient("", "m", "sk");

        act.Should().Throw<ArgumentException>();
    }
}
