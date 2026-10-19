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
using Xunit;

namespace Mud.Feishu.Abstractions.Tests.Authentication;

/// <summary>
/// 锁定组件 <c>TokenRecoveryEnhancedClient.WithBaseAddress</c> 的「同类型克隆」能力。
/// </summary>
/// <remarks>
/// <para>
/// 组件 3.0.9 起该类型重写了内部的按基地址克隆实现，<c>WithBaseAddress(...)</c> 返回的仍是
/// 带令牌恢复能力的同类型实例；此前基类实现返回普通增强客户端，令牌恢复能力被静默丢弃。
/// </para>
/// <para>
/// 本用例只锁「同类型 + 基地址生效」两点：命令式换基地址与「每请求重跑配置委托」的配置热更新
/// 语义不同，后者的行为由 <see cref="FeishuClientEndpointHotReloadTests"/> 锁定。
/// </para>
/// </remarks>
public class TokenRecoveryClientCapabilityTests
{
    private static IServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddHttpClient();
        return services.BuildServiceProvider();
    }

    private static IEnhancedHttpClient CreateTokenRecoveryClient(IServiceProvider provider)
    {
        var factory = new FeishuHttpClientFactory(provider);
        var executor = new TokenRecoveryExecutor(
            new Mock<ITokenManager>().Object,
            new Mock<IUserTokenManager>().Object,
            currentUserContext: null,
            optionsMonitor: Mock.Of<IOptionsMonitor<TokenRecoveryOptions>>(m => m.CurrentValue == new TokenRecoveryOptions()));
        return factory.Create("cli_a", executor);
    }

    /// <summary>
    /// 换基地址后实例类型不变：令牌恢复能力（401 自动恢复）不得因克隆而丢失。
    /// </summary>
    [Fact]
    public void WithBaseAddress_OnTokenRecoveryClient_PreservesSameType()
    {
        var client = CreateTokenRecoveryClient(BuildProvider());
        client.Should().BeOfType<TokenRecoveryEnhancedClient>();

        var derived = client.WithBaseAddress(new Uri("https://open.larksuite.com"));

        derived.Should().BeOfType<TokenRecoveryEnhancedClient>("换基地址必须返回同类型实例，否则令牌恢复能力静默丢失");
        derived.Should().NotBeSameAs(client, "克隆语义返回新实例");
    }

    /// <summary>
    /// 换基地址后 <see cref="IEnhancedHttpClient.BaseAddress"/> 必须指向新地址。
    /// </summary>
    [Fact]
    public void WithBaseAddress_OnTokenRecoveryClient_AppliesNewBaseAddress()
    {
        var client = CreateTokenRecoveryClient(BuildProvider());
        var target = new Uri("https://open.feishu.cn");

        var derived = client.WithBaseAddress(target);

        derived.BaseAddress.Should().Be(target);
    }
}
