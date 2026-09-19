// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Mud.Feishu.Abstractions;

namespace Mud.Feishu.WebSocket.Tests.Core;

/// <summary>
/// TMR-P0-2（F2）WebSocket 现取默认应用构造回归测试。
/// </summary>
/// <remarks>
/// 构造设计约束：旧 4 参签名保留（源级兼容宿主手工 new）；新增 5 参构造（含
/// IFeishuAppManager）供 DI 最长构造选择——两个构造保持<b>不同元数</b>，
/// 避免 null 字面量二义与 MS.DI 多构造歧义。
/// </remarks>
public class FeishuWebSocketManagerTmrTests
{
    private readonly Mock<ILogger<FeishuWebSocketManager>> _loggerMock = new();
    private readonly Mock<IFeishuAppContext> _appContextMock = new();
    private readonly Mock<IFeishuAppManager> _appManagerMock = new();
    private readonly Mock<IFeishuWebSocketClient> _clientMock = new();
    private readonly Mock<IOptionsMonitor<FeishuWebSocketOptions>> _optionsMonitorMock = new();

    public FeishuWebSocketManagerTmrTests()
    {
        _optionsMonitorMock.Setup(x => x.CurrentValue).Returns(new FeishuWebSocketOptions());
        _appContextMock.SetupGet(x => x.Config).Returns(new Mud.Feishu.Abstractions.FeishuAppConfig
        {
            AppKey = "default",
            AppId = "cli_default",
            AppSecret = "secret_default"
        });
    }

    [Fact]
    public void Constructor_WithAppManager_ShouldCreateInstance()
    {
        // Act：5 参构造（DI 最长构造选择路径，现取语义）。
        var manager = new FeishuWebSocketManager(
            _loggerMock.Object,
            _appManagerMock.Object,
            _appContextMock.Object,
            _optionsMonitorMock.Object,
            _clientMock.Object);

        // Assert
        manager.Should().NotBeNull();
        manager.IsConnected.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithNullAppManager_ShouldThrowArgumentNullException()
    {
        // Act + Assert
        var action = () => new FeishuWebSocketManager(
            _loggerMock.Object,
            (IFeishuAppManager)null!,
            _appContextMock.Object,
            _optionsMonitorMock.Object,
            _clientMock.Object);

        action.Should().Throw<ArgumentNullException>().WithParameterName("appManager");
    }

    [Fact]
    public void Constructor_WithLegacySignature_ShouldCreateInstance_AndWarnFallback()
    {
        // Act：旧 4 参签名（向后兼容手工 new）——无现取通道，构造时告警但不抛。
        var manager = new FeishuWebSocketManager(
            _loggerMock.Object,
            _appContextMock.Object,
            _optionsMonitorMock.Object,
            _clientMock.Object);

        // Assert：实例可用；回退告警已记录（IFeishuAppManager 缺失为可观测的降级路径）。
        manager.Should().NotBeNull();
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("IFeishuAppManager")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
