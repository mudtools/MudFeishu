// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Mud.HttpUtils;
using Mud.HttpUtils.Observability;
using Xunit;

namespace Mud.Feishu.Abstractions.Tests.Authentication;

public class DebugMultiClientTests
{
    [Fact]
    public void AddMudHttpClient_TwoNamedClients_ShouldBothCreateSuccessfully()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMudHttpClient("client-a", client => client.BaseAddress = new Uri("https://a.example.com"));
        services.AddMudHttpClient("client-b", client => client.BaseAddress = new Uri("https://b.example.com"));
        using var provider = services.BuildServiceProvider();

        var factory = provider.GetRequiredService<IHttpClientFactory>();

        var clientA = factory.CreateClient("client-a");
        clientA.Should().NotBeNull();

        var clientB = factory.CreateClient("client-b");
        clientB.Should().NotBeNull();
    }

    [Fact]
    public void NativeAddHttpClient_TwoNamedClients_ShouldBothCreateSuccessfully()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpClient("client-a", client => client.BaseAddress = new Uri("https://a.example.com"))
            .AddHttpMessageHandler(() => new TracingDelegatingHandler());
        services.AddHttpClient("client-b", client => client.BaseAddress = new Uri("https://b.example.com"))
            .AddHttpMessageHandler(() => new TracingDelegatingHandler());
        using var provider = services.BuildServiceProvider();

        var factory = provider.GetRequiredService<IHttpClientFactory>();

        var clientA = factory.CreateClient("client-a");
        clientA.Should().NotBeNull();

        var clientB = factory.CreateClient("client-b");
        clientB.Should().NotBeNull();
    }
}
