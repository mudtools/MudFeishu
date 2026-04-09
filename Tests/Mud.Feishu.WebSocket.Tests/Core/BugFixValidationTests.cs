// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Mud.Feishu.Abstractions;
using Mud.Feishu.WebSocket.Core;
using Mud.Feishu.WebSocket.SocketEventArgs;

namespace Mud.Feishu.WebSocket.Tests.Core;

/// <summary>
/// Bug修复验证测试 - 验证审核报告中的问题是否已修复
/// </summary>
public class BugFixValidationTests
{
    private readonly ILogger<FeishuWebSocketClient> _logger;
    private readonly Mock<IFeishuEventHandlerFactory> _eventHandlerFactoryMock;
    private readonly FeishuWebSocketOptions _options;

    public BugFixValidationTests()
    {
        _logger = NullLogger<FeishuWebSocketClient>.Instance;
        _eventHandlerFactoryMock = new Mock<IFeishuEventHandlerFactory>();
        _eventHandlerFactoryMock
            .Setup(x => x.GetHandler(It.IsAny<string>()))
            .Returns(Mock.Of<IFeishuEventHandler>());

        _options = new FeishuWebSocketOptions
        {
            EnableLogging = false
        };
    }

    #region Bug修复1: 心跳超时阈值常量化

    [Fact]
    public void HeartbeatTimeoutThreshold_ShouldBeDefined_AsConstant()
    {
        const string fieldName = "HeartbeatTimeoutThreshold";

        var type = typeof(HeartbeatManager);
        var field = type.GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var value = field?.GetValue(null);

        value.Should().Be(3);
        field?.IsLiteral.Should().BeTrue();
    }

    #endregion

    #region Bug修复2: IAsyncDisposable接口实现

    [Fact]
    public void FeishuWebSocketManager_ShouldImplement_IAsyncDisposable()
    {
        // Arrange & Act
        var type = typeof(FeishuWebSocketManager);

        // Assert
        typeof(IAsyncDisposable).IsAssignableFrom(type).Should().BeTrue("FeishuWebSocketManager 应实现 IAsyncDisposable 接口");
    }

    [Fact]
    public void FeishuWebSocketManager_ShouldHave_DisposeAsync_Method()
    {
        // Arrange
        var type = typeof(FeishuWebSocketManager);

        // Act
        var disposeAsyncMethod = type.GetMethod("DisposeAsync");

        // Assert
        disposeAsyncMethod.Should().NotBeNull("DisposeAsync 方法应存在");
        disposeAsyncMethod!.ReturnType.Should().Be(typeof(ValueTask));
    }

    #endregion

    #region Bug修复3: 心跳间隔默认值

    [Fact]
    public void HeartbeatIntervalMs_DefaultValue_ShouldBe25000()
    {
        // Arrange
        var options = new FeishuWebSocketOptions();

        // Assert
        options.HeartbeatIntervalMs.Should().Be(25000, "飞书要求心跳间隔在25秒内");
    }

    [Fact]
    public void HeartbeatIntervalMs_ShouldHaveMinimum_5000()
    {
        // Arrange
        var options = new FeishuWebSocketOptions();

        // Act - 尝试设置小于最小值的值
        options.HeartbeatIntervalMs = 1000;

        // Assert
        options.HeartbeatIntervalMs.Should().Be(5000, "最小心跳间隔应为5秒");
    }

    #endregion

    #region Bug修复4: 证书验证配置选项

    [Fact]
    public void FeishuWebSocketOptions_ShouldHave_CertificateValidationOptions()
    {
        // Arrange
        var options = new FeishuWebSocketOptions();

        // Assert
        options.ValidateServerCertificate.Should().BeTrue("默认应验证服务器证书");
        options.AllowSelfSignedCertificates.Should().BeFalse("默认不允许自签名证书");
        options.CustomCertificateValidationCallback.Should().BeNull();
    }

    [Fact]
    public void FeishuWebSocketOptions_ShouldAllow_CustomCertificateValidationCallback()
    {
        // Arrange
        var options = new FeishuWebSocketOptions();

        // Act
        options.CustomCertificateValidationCallback = (sender, cert, chain, errors) => true;

        // Assert
        options.CustomCertificateValidationCallback.Should().NotBeNull();
    }

    [Fact]
    public void FeishuWebSocketOptions_ShouldAllow_DisableCertificateValidation()
    {
        // Arrange
        var options = new FeishuWebSocketOptions();

        // Act
        options.ValidateServerCertificate = false;

        // Assert
        options.ValidateServerCertificate.Should().BeFalse();
    }

    #endregion

    #region 功能完善: 消息队列满告警

    [Fact]
    public void FeishuWebSocketClient_ShouldHave_QueueOverflowEvent()
    {
        // Arrange
        var client = CreateClient();

        // Act & Assert
        var errorEvent = typeof(FeishuWebSocketClient).GetEvent("Error");
        errorEvent.Should().NotBeNull();

        // 验证可以订阅Error事件
        WebSocketErrorEventArgs? receivedArgs = null;
        client.Error += (sender, args) => receivedArgs = args;

        // 此测试验证事件可以订阅，实际队列满逻辑需要集成测试
        receivedArgs.Should().BeNull();
    }

    #endregion

    #region 代码质量: 常量化验证

    [Fact]
    public void MessageSequenceValidator_ShouldHave_SequenceGapThreshold_Constant()
    {
        // Arrange
        const string fieldName = "SequenceGapThreshold";

        // Act
        var type = typeof(MessageSequenceValidator);
        var field = type.GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var value = field?.GetValue(null);

        // Assert
        value.Should().Be(10);
    }

    [Fact]
    public void MessageSequenceValidator_ShouldHave_CleanupIntervalMinutes_Constant()
    {
        // Arrange
        const string fieldName = "CleanupIntervalMinutes";

        // Act
        var type = typeof(MessageSequenceValidator);
        var field = type.GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var value = field?.GetValue(null);

        // Assert
        value.Should().Be(30);
    }

    #endregion

    private FeishuWebSocketClient CreateClient()
    {
        var loggerFactory = NullLoggerFactory.Instance;
        return new FeishuWebSocketClient(
            _logger,
            _eventHandlerFactoryMock.Object,
            loggerFactory,
            null,
            _options);
    }
}
