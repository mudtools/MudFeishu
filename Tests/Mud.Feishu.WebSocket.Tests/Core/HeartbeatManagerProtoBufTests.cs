// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Mud.Feishu.DataModels.WsEndpoint;
using ProtoBuf;
using System.Reflection;

namespace Mud.Feishu.WebSocket.Tests.Core;

/// <summary>
/// HeartbeatManager ProtoBuf 心跳和 ClientConfig 动态配置测试
/// </summary>
public class HeartbeatManagerProtoBufTests
{
    private static HeartbeatManager CreateHeartbeatManager(
        FeishuWebSocketOptions? options = null,
        Func<byte[], CancellationToken, Task>? sendCallback = null)
    {
        options ??= new FeishuWebSocketOptions { EnableLogging = false };
        sendCallback ??= (_, _) => Task.CompletedTask;

        return new HeartbeatManager(
            NullLogger<HeartbeatManager>.Instance,
            options,
            sendCallback);
    }

    #region ProtoBuf 二进制心跳

    [Fact]
    public async Task StartHeartbeatAsync_ShouldSendProtoBufBinaryPing_WhenConnected()
    {
        // Arrange
        byte[]? sentData = null;
        var options = new FeishuWebSocketOptions
        {
            HeartbeatIntervalMs = 5000,
            EnableLogging = false
        };

        var manager = new HeartbeatManager(
            NullLogger<HeartbeatManager>.Instance,
            options,
            (data, _) => { sentData = data; return Task.CompletedTask; });

        manager.SetServiceId(1001);

        using var cts = new CancellationTokenSource();
        var heartbeatTask = manager.StartHeartbeatAsync(cts.Token);

        // 等待心跳发送（间隔 5000ms，需要等待）
        await Task.Delay(6000);

        // Act
        cts.Cancel();
        await heartbeatTask;

        // Assert
        sentData.Should().NotBeNull();
        var frame = Serializer.Deserialize<EventProtoData>(new MemoryStream(sentData!));
        frame.Service.Should().Be(1001);
        frame.Method.Should().Be(FrameBuilder.MethodControl);
        frame.MessageType.Should().Be(MessageType.Ping);
    }

    [Fact]
    public void SetServiceId_ShouldStoreServiceId()
    {
        // Arrange
        var manager = CreateHeartbeatManager();

        // Act
        manager.SetServiceId(2002);

        // Assert - 验证通过反射
        var serviceIdField = typeof(HeartbeatManager).GetField("_serviceId", BindingFlags.NonPublic | BindingFlags.Instance);
        serviceIdField.Should().NotBeNull();
        var value = serviceIdField!.GetValue(manager);
        value.Should().Be(2002);
    }

    #endregion

    #region PongReceived + ClientConfig 动态配置

    [Fact]
    public void OnPongReceived_WithClientConfig_ShouldUpdateHeartbeatInterval()
    {
        // Arrange
        var options = new FeishuWebSocketOptions
        {
            HeartbeatIntervalMs = 25000,
            EnableLogging = false
        };
        var manager = CreateHeartbeatManager(options);

        var config = new ClientConfigInfo
        {
            PingInterval = 120,  // 秒
            ReconnectInterval = 60,
            ReconnectCount = 3,
            ReconnectNonce = 10
        };

        // Act
        manager.OnPongReceived(config);

        // Assert - PingInterval 从秒转换为毫秒
        options.HeartbeatIntervalMs.Should().Be(120000);
    }

    [Fact]
    public void OnPongReceived_WithNullConfig_ShouldNotUpdateOptions()
    {
        // Arrange
        var options = new FeishuWebSocketOptions
        {
            HeartbeatIntervalMs = 25000,
            EnableLogging = false
        };
        var manager = CreateHeartbeatManager(options);

        // Act
        manager.OnPongReceived(null);

        // Assert - 配置不变
        options.HeartbeatIntervalMs.Should().Be(25000);
    }

    [Fact]
    public void OnPongReceived_WithClientConfig_ShouldUpdateReconnectDelay()
    {
        // Arrange
        var options = new FeishuWebSocketOptions
        {
            ReconnectDelayMs = 5000,
            EnableLogging = false
        };
        var manager = CreateHeartbeatManager(options);

        var config = new ClientConfigInfo
        {
            ReconnectInterval = 120 // 秒
        };

        // Act
        manager.OnPongReceived(config);

        // Assert
        options.ReconnectDelayMs.Should().Be(120000);
    }

    [Fact]
    public void OnPongReceived_WithReconnectCountMinusOne_ShouldSetZeroForInfiniteReconnect()
    {
        // Arrange
        var options = new FeishuWebSocketOptions
        {
            MaxReconnectAttempts = 5,
            EnableLogging = false
        };
        var manager = CreateHeartbeatManager(options);

        var config = new ClientConfigInfo
        {
            ReconnectCount = -1 // Java SDK: 无限重连
        };

        // Act
        manager.OnPongReceived(config);

        // Assert - .NET: MaxReconnectAttempts=0 表示无限重连
        options.MaxReconnectAttempts.Should().Be(0);
    }

    [Fact]
    public void OnPongReceived_WithReconnectCountPositive_ShouldUpdateMaxReconnectAttempts()
    {
        // Arrange
        var options = new FeishuWebSocketOptions
        {
            MaxReconnectAttempts = 5,
            EnableLogging = false
        };
        var manager = CreateHeartbeatManager(options);

        var config = new ClientConfigInfo
        {
            ReconnectCount = 10
        };

        // Act
        manager.OnPongReceived(config);

        // Assert
        options.MaxReconnectAttempts.Should().Be(10);
    }

    #endregion

    #region 构造函数验证

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenSendBinaryCallbackIsNull()
    {
        var act = () => new HeartbeatManager(
            NullLogger<HeartbeatManager>.Instance,
            new FeishuWebSocketOptions(),
            null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("sendBinaryCallback");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenOptionsIsNull()
    {
        var act = () => new HeartbeatManager(
            NullLogger<HeartbeatManager>.Instance,
            null!,
            (_, _) => Task.CompletedTask);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    #endregion
}
