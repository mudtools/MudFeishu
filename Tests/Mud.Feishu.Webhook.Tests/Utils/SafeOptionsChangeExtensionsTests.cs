// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Mud.Feishu.Webhook.Configuration;
using Mud.Feishu.Webhook.Utils;

namespace Mud.Feishu.Webhook.Tests.Utils;

/// <summary>
/// W-02：<c>SafeOptionsChangeExtensions.OnChangeSafe</c> 的"零异常外泄"契约。
/// </summary>
/// <remarks>
/// <para>
/// 这是 <c>HotReloadCallbackContractGuards</c>（源码扫描）的**行为侧**互补：守卫保证"所有回调
/// 都经该扩展点注册"，本组用例保证"经该扩展点注册后，回调里任何异常都不会跑到配置重载线程上"。
/// </para>
/// <para>
/// 为什么必须行为化：<c>IOptionsMonitor.OnChange</c> 的回调由配置重载线程**同步**调用且框架不兜底，
/// 未捕获的异常会沿 <c>IConfiguration</c> 变更通知链上抛并终止进程——只靠源码字符串断言无法证明
/// "真的没抛出去"。
/// </para>
/// </remarks>
public class SafeOptionsChangeExtensionsTests
{
    [Fact]
    public void OnChange_ShouldNotThrow_WhenNewOptionsInvalid()
    {
        // Arrange - 模拟"配置热更成非法值"：回调体内读 CurrentValue 会抛
        // OptionsValidationException/InvalidOperationException（真实崩溃路径）。
        var monitor = new ThrowingCurrentValueMonitor();
        var logger = new Mock<ILogger>();

        monitor.OnChangeSafe(logger.Object, (_, _) => _ = monitor.CurrentValue);

        // Act
        var act = () => monitor.Raise(new FeishuWebhookOptions());

        // Assert
        act.Should().NotThrow("回调异常绝不能外泄到配置重载线程（.NET Core 上会终止进程）");
        logger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("配置变更回调执行失败")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "吞掉异常可以保住进程，但必须留下可诊断的记录");
    }

    [Fact]
    public void OnChange_ShouldNotThrow_WhenCallbackThrowsArbitraryException()
    {
        var monitor = new ThrowingCurrentValueMonitor();
        var logger = new Mock<ILogger>();

        monitor.OnChangeSafe(logger.Object, (_, _) => throw new InvalidOperationException("回调体内部炸了"));

        var act = () => monitor.Raise(new FeishuWebhookOptions { MaxConcurrentEvents = 7 });

        act.Should().NotThrow();
        logger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.Is<Exception>(e => e is InvalidOperationException),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void OnChange_ShouldNotThrow_WhenLoggerIsNull()
    {
        // 无日志宿主（未注册 ILogger）也必须保持"绝不上抛"，只是静默丢弃
        var monitor = new ThrowingCurrentValueMonitor();
        monitor.OnChangeSafe(logger: null, (_, _) => throw new InvalidOperationException("boom"));

        var act = () => monitor.Raise(new FeishuWebhookOptions());

        act.Should().NotThrow();
    }

    [Fact]
    public void OnChange_ShouldPassNewOptionsAndSourceName_ToCallback()
    {
        // 回调只能用**参数**给出的新值（不得回读 CurrentValue），故参数必须原样透传
        var monitor = new ThrowingCurrentValueMonitor();
        FeishuWebhookOptions? observed = null;
        string? observedName = null;

        monitor.OnChangeSafe(new Mock<ILogger>().Object, (newOptions, name) =>
        {
            observed = newOptions;
            observedName = name;
        });

        var expected = new FeishuWebhookOptions { MaxConcurrentEvents = 42 };
        monitor.Raise(expected, "appsettings.json");

        observed.Should().BeSameAs(expected);
        observedName.Should().Be("appsettings.json");
    }

    [Fact]
    public void OnChangeSafe_ShouldReturnNull_WhenUnderlyingMonitorReturnsNull()
    {
        // 现有代码以 `_onChangeSubscription?.Dispose()` 消费返回值，null 必须被容忍
        var monitor = new ThrowingCurrentValueMonitor { OnChangeReturnsNull = true };

        monitor.OnChangeSafe(new Mock<ILogger>().Object, (_, _) => { })
            .Should().BeNull();
    }

    /// <summary>
    /// <see cref="IOptionsMonitor{TOptions}"/> 替身：<see cref="CurrentValue"/> 可配置为抛异常
    /// （模拟热更后配置校验失败），并能手动触发已注册的回调。
    /// </summary>
    private sealed class ThrowingCurrentValueMonitor : IOptionsMonitor<FeishuWebhookOptions>
    {
        private Action<FeishuWebhookOptions, string?>? _listener;

        public bool CurrentValueThrows { get; set; } = true;

        public bool OnChangeReturnsNull { get; set; }

        public FeishuWebhookOptions CurrentValue => CurrentValueThrows
            ? throw new InvalidOperationException("配置校验失败（模拟热更后的非法配置）")
            : new FeishuWebhookOptions();

        public FeishuWebhookOptions Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<FeishuWebhookOptions, string?> listener)
        {
            _listener = listener;
            return OnChangeReturnsNull ? null : new NoopDisposable();
        }

        public void Raise(FeishuWebhookOptions newOptions, string? name = null) => _listener!(newOptions, name);

        private sealed class NoopDisposable : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}
