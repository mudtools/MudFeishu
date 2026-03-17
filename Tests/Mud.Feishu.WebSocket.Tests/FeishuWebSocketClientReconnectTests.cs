// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证 位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Mud.Feishu.Abstractions;

namespace Mud.Feishu.WebSocket.Tests;

/// <summary>
/// FeishuWebSocketClient 重连限制逻辑测试类
/// </summary>
public class FeishuWebSocketClientReconnectTests
{
    private readonly ILogger<FeishuWebSocketClient> _logger;
    private readonly Mock<IFeishuEventHandlerFactory> _eventHandlerFactoryMock;
    private readonly FeishuWebSocketOptions _options;

    public FeishuWebSocketClientReconnectTests()
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

    /// <summary>
    /// 使用反射辅助获取私有字段值
    /// </summary>
    private static T GetPrivateField<T>(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (T)field!.GetValue(instance)!;
    }

    /// <summary>
    /// 使用反射辅助调用私有方法
    /// </summary>
    private static object? InvokePrivateMethod(object instance, string methodName, params object[] parameters)
    {
        var method = instance.GetType().GetMethod(methodName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return method?.Invoke(instance, parameters);
    }

    /// <summary>
    /// 使用反射辅助设置私有字段值
    /// </summary>
    private static void SetPrivateField(object instance, string fieldName, object value)
    {
        var field = instance.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field!.SetValue(instance, value);
    }

    [Fact]
    public void MaxTotalReconnectAttempts_ShouldBeDefined_As20()
    {
        // Arrange
        const string fieldName = "MaxTotalReconnectAttempts";

        // Act - 通过反射获取常量值
        var type = typeof(FeishuWebSocketClient);
        var field = type.GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var value = field?.GetValue(null);

        // Assert
        value.Should().Be(20);
    }

    [Fact]
    public void MaxReconnectTotalTime_ShouldBeDefined_As30Minutes()
    {
        // Arrange
        const string fieldName = "MaxReconnectTotalTime";

        // Act
        var type = typeof(FeishuWebSocketClient);
        var field = type.GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var value = (TimeSpan?)field?.GetValue(null);

        // Assert
        value.Should().Be(TimeSpan.FromMinutes(30));
    }

    [Fact]
    public void ConnectionState_ShouldBeThreadSafe_UsingPrivateLock()
    {
        // Arrange
        const string fieldName = "_connectionStateLock";

        // Act
        var type = typeof(FeishuWebSocketClient);
        var field = type.GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Assert
        field.Should().NotBeNull();
        field!.FieldType.Should().Be(typeof(object));
    }

    [Fact]
    public void FeishuWebSocketClient_ShouldInitialize_ConnectionStateFields()
    {
        // Arrange & Act
        var client = CreateClient();

        // Assert - 验证连接状态相关字段存在
        var type = typeof(FeishuWebSocketClient);
        var connectionStateField = type.GetField("_connectionState",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var totalReconnectAttemptsField = type.GetField("_totalReconnectAttempts",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var firstReconnectAttemptField = type.GetField("_firstReconnectAttempt",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        connectionStateField.Should().NotBeNull();
        totalReconnectAttemptsField.Should().NotBeNull();
        firstReconnectAttemptField.Should().NotBeNull();
    }

    [Fact]
    public void ConnectionState_ShouldInitialize_ToZero_ByDefault()
    {
        // Arrange
        var client = CreateClient();

        // Act
        var connectionState = GetPrivateField<int>(client, "_connectionState");

        // Assert - 0 表示未连接状态
        connectionState.Should().Be(0);
    }

    [Fact]
    public void TotalReconnectAttempts_ShouldInitialize_ToZero_ByDefault()
    {
        // Arrange
        var client = CreateClient();

        // Act
        var reconnectAttempts = GetPrivateField<int>(client, "_totalReconnectAttempts");

        // Assert
        reconnectAttempts.Should().Be(0);
    }

    [Fact]
    public void FirstReconnectAttempt_ShouldInitialize_ToMinValue()
    {
        // Arrange
        var client = CreateClient();

        // Act
        var firstAttempt = GetPrivateField<DateTime>(client, "_firstReconnectAttempt");

        // Assert
        firstAttempt.Should().Be(DateTime.MinValue);
    }

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

/// <summary>
/// 连接状态枚举 - 与 FeishuWebSocketClient 中的定义对应
/// </summary>
public enum TestConnectionState
{
    Disconnected = 0,
    Connected = 1,
    Connecting = 2,
    Reconnecting = 3
}
