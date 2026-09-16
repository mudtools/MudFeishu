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
            PingInterval = 120,  // 秒 → 钳制到 30 秒上界
            ReconnectInterval = 60,
            ReconnectCount = 3,
            ReconnectNonce = 10
        };

        // Act
        manager.OnPongReceived(config);

        // Assert - WS-07 修复：PingInterval 被钳制到 5~30 秒区间，写入私有字段 _heartbeatIntervalMs
        // 120 秒被钳制为 30 秒 = 30000 毫秒，不再回写 Options
        var heartbeatField = typeof(HeartbeatManager).GetField("_heartbeatIntervalMs", BindingFlags.NonPublic | BindingFlags.Instance);
        heartbeatField.Should().NotBeNull();
        heartbeatField!.GetValue(manager).Should().Be(30000);
        // Options 实例不被运行时改写
        options.HeartbeatIntervalMs.Should().Be(25000);
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
    public void OnPongReceived_WithClientConfig_ShouldNotUpdateReconnectDelay()
    {
        // Arrange - WS-07 修复：ReconnectDelayMs 属于本地运维策略，禁止被运行时改写
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

        // Assert - 服务端建议的重连间隔被忽略，使用本地配置
        options.ReconnectDelayMs.Should().Be(5000);
    }

    [Fact]
    public void OnPongReceived_WithReconnectCountMinusOne_ShouldNotUpdateMaxReconnectAttempts()
    {
        // Arrange - WS-07 修复：MaxReconnectAttempts 属于本地运维策略，禁止被运行时改写
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

        // Assert - 服务端建议的重连次数被忽略，使用本地配置
        options.MaxReconnectAttempts.Should().Be(5);
    }

    [Fact]
    public void OnPongReceived_WithReconnectCountPositive_ShouldNotUpdateMaxReconnectAttempts()
    {
        // Arrange - WS-07 修复：MaxReconnectAttempts 属于本地运维策略，禁止被运行时改写
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

        // Assert - 服务端建议的重连次数被忽略，使用本地配置
        options.MaxReconnectAttempts.Should().Be(5);
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
