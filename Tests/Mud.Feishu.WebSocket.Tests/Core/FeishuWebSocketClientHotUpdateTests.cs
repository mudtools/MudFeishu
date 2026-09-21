// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Mud.Feishu.Abstractions;

namespace Mud.Feishu.WebSocket.Tests.Core;

/// <summary>
/// P2-2 回归测试：客户端运行期配置视图的真实性。
/// </summary>
/// <remarks>
/// 改造前 <c>FeishuWebSocketClient</c> 持有 <c>IOptionsMonitor</c> 但<b>从不读取</b>（F4 声称的热更新未落地）。
/// 现在 <c>AppKey</c>（指标维度）与 <c>AuthGateTimeoutMs</c>（认证闸门）通过私有属性 <c>Options</c> 在
/// <b>每次读取时</b>取 <c>CurrentValue</c>；其余配置项在构造期固化到子组件（口径已在 XML 注释中写明）。
/// <para>
/// 说明（备查）：这里使用可控的 <see cref="IOptionsMonitor{T}"/> 替身而非"真实配置重载"路径——
/// <c>InMemoryConfigurationProvider</c> 在构造期把初始数据拷贝进 <c>Data</c> 且 <c>Load()</c> 为空实现，
/// 因此"改字典 + Reload()"不会生效；用替身可以精确断言"每次读取 CurrentValue"这一契约本身。
/// </para>
/// </remarks>
public class FeishuWebSocketClientHotUpdateTests
{
    private static readonly PropertyInfo RuntimeOptionsProperty =
        typeof(FeishuWebSocketClient).GetProperty("Options", BindingFlags.NonPublic | BindingFlags.Instance)!;

    /// <summary>
    /// 可控的 <see cref="IOptionsMonitor{T}"/> 替身（<c>CurrentValue</c> 可在用例中直接替换）。
    /// </summary>
    private sealed class StubOptionsMonitor : IOptionsMonitor<FeishuWebSocketOptions>
    {
        public StubOptionsMonitor(FeishuWebSocketOptions currentValue) => CurrentValue = currentValue;

        public FeishuWebSocketOptions CurrentValue { get; set; }

        public FeishuWebSocketOptions Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<FeishuWebSocketOptions, string?> listener) => new NoopDisposable();

        private sealed class NoopDisposable : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }

    private static FeishuWebSocketOptions GetRuntimeOptions(FeishuWebSocketClient client)
        => (FeishuWebSocketOptions)RuntimeOptionsProperty.GetValue(client)!;

    private static FeishuWebSocketOptions GetCapturedSnapshot(FeishuWebSocketClient client)
        => (FeishuWebSocketOptions)typeof(FeishuWebSocketClient)
            .GetField("_options", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(client)!;

    private static FeishuWebSocketClient CreateClient(IOptionsMonitor<FeishuWebSocketOptions>? optionsMonitor)
    {
        var factoryMock = new Mock<IFeishuEventHandlerFactory>();
        factoryMock.Setup(x => x.GetHandler(It.IsAny<string>())).Returns(Mock.Of<IFeishuEventHandler>());
        factoryMock.Setup(x => x.GetHandlers(It.IsAny<string>()))
            .Returns(new List<IFeishuEventHandler>());

        return new FeishuWebSocketClient(
            NullLogger<FeishuWebSocketClient>.Instance,
            factoryMock.Object,
            NullLoggerFactory.Instance,
            optionsMonitor: optionsMonitor);
    }

    [Fact]
    public void Options_ShouldFollowMonitor_WhenConfigurationChanges()
    {
        // Arrange
        var monitor = new StubOptionsMonitor(new FeishuWebSocketOptions
        {
            AppKey = "app-before",
            AuthGateTimeoutMs = 0
        });
        var client = CreateClient(monitor);

        GetRuntimeOptions(client).AppKey.Should().Be("app-before");

        // Act：模拟配置热更新（下一次采集/闸门判断必须取到新值）
        monitor.CurrentValue = new FeishuWebSocketOptions
        {
            AppKey = "app-after",
            AuthGateTimeoutMs = 1500
        };

        // Assert
        GetRuntimeOptions(client).AppKey.Should().Be("app-after", "AppKey 指标维度支持热更新");
        GetRuntimeOptions(client).AuthGateTimeoutMs.Should().Be(1500, "认证闸门等待上限支持热更新");
    }

    [Fact]
    public void CapturedSnapshot_ShouldNotChange_WhenConfigurationChanges()
    {
        // Arrange：构造期快照已固化到各子组件（MessageRouter/BinaryMessageProcessor/HeartbeatManager 等），
        // 本用例固定"快照不变、运行期视图变化"这一区分，防止后续误以为全体配置都支持热更新。
        var monitor = new StubOptionsMonitor(new FeishuWebSocketOptions
        {
            AppKey = "app-before",
            AuthGateTimeoutMs = 0
        });
        var client = CreateClient(monitor);

        monitor.CurrentValue = new FeishuWebSocketOptions
        {
            AppKey = "app-after",
            AuthGateTimeoutMs = 1500
        };

        // Act
        var snapshot = GetCapturedSnapshot(client);

        // Assert
        snapshot.AppKey.Should().Be("app-before", "构造期快照不随配置变更（相关子组件需重启才生效）");
        snapshot.AuthGateTimeoutMs.Should().Be(0);
        GetRuntimeOptions(client).AppKey.Should().Be("app-after");
    }

    [Fact]
    public void Options_ShouldFallBackToSnapshot_WhenMonitorNotProvided()
    {
        // Arrange：兼容存量调用方（不传 IOptionsMonitor）
        var client = CreateClient(optionsMonitor: null);

        // Act & Assert：不得抛异常，且等价于构造期快照
        GetRuntimeOptions(client).AppKey.Should().Be(GetCapturedSnapshot(client).AppKey);
        GetRuntimeOptions(client).AuthGateTimeoutMs.Should().Be(GetCapturedSnapshot(client).AuthGateTimeoutMs);
    }
}
