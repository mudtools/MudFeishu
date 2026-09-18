// -----------------------------------------------------------------------
//  作者:Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging.Abstractions;
using Mud.Feishu.Redis.Services;
using StackExchange.Redis;

namespace Mud.Feishu.Redis.Tests.Services;

/// <summary>
/// RedisConnectionWarmupService 单元测试（WHF-10：启动期连接预热）
/// </summary>
public class RedisConnectionWarmupServiceTests
{
    private readonly Mock<IConnectionMultiplexer> _connectionMultiplexerMock;
    private readonly Mock<IDatabase> _databaseMock;

    public RedisConnectionWarmupServiceTests()
    {
        _connectionMultiplexerMock = new Mock<IConnectionMultiplexer>();
        _databaseMock = new Mock<IDatabase>();

        _connectionMultiplexerMock
            .Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_databaseMock.Object);
        _connectionMultiplexerMock
            .Setup(x => x.GetEndPoints(It.IsAny<bool>()))
            .Returns(new System.Net.EndPoint[] { new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 6379) });
    }

    [Fact]
    public async Task StartAsync_WhenPingSucceeds_ShouldComplete()
    {
        // Arrange - WHF-10：解析 IConnectionMultiplexer 并 PING，把连接建立成本移到启动期
        _databaseMock
            .Setup(x => x.PingAsync(It.IsAny<CommandFlags>()))
            .ReturnsAsync(TimeSpan.FromMilliseconds(1));
        var service = new RedisConnectionWarmupService(
            _connectionMultiplexerMock.Object,
            NullLogger<RedisConnectionWarmupService>.Instance);

        // Act & Assert - 不抛出即视为预热成功
        await service.StartAsync(CancellationToken.None);

        _databaseMock.Verify(x => x.PingAsync(It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task StartAsync_WhenPingFails_AndAbortOnConnectFailTrue_ShouldThrow()
    {
        // Arrange - fail-fast：AbortOnConnectFail=true 时预热失败终止启动
        _databaseMock
            .Setup(x => x.PingAsync(It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisServerException("PONG failed"));
        var service = new RedisConnectionWarmupService(
            _connectionMultiplexerMock.Object,
            NullLogger<RedisConnectionWarmupService>.Instance,
            abortOnConnectFail: true);

        // Act & Assert
        await Assert.ThrowsAsync<RedisServerException>(
            async () => await service.StartAsync(CancellationToken.None));
    }

    [Fact]
    public async Task StartAsync_WhenPingFails_AndAbortOnConnectFailFalse_ShouldNotThrow()
    {
        // Arrange - AbortOnConnectFail=false 时仅告警，交由连接层自动重连
        _databaseMock
            .Setup(x => x.PingAsync(It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisServerException("PONG failed"));
        var service = new RedisConnectionWarmupService(
            _connectionMultiplexerMock.Object,
            NullLogger<RedisConnectionWarmupService>.Instance,
            abortOnConnectFail: false);

        // Act & Assert - 不抛出，不阻断启动
        await service.StartAsync(CancellationToken.None);
    }
}
