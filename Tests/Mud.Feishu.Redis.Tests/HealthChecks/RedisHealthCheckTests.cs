// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Mud.Feishu.Redis.HealthChecks;
using System.Net;

namespace Mud.Feishu.Redis.Tests.HealthChecks;

public class RedisHealthCheckTests
{
    private readonly Mock<IConnectionMultiplexer> _connectionMultiplexerMock;
    private readonly Mock<IDatabase> _databaseMock;
    private readonly Mock<IServer> _serverMock;

    public RedisHealthCheckTests()
    {
        _connectionMultiplexerMock = new Mock<IConnectionMultiplexer>();
        _databaseMock = new Mock<IDatabase>();
        _serverMock = new Mock<IServer>();

        _connectionMultiplexerMock
            .Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_databaseMock.Object);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenRedisIsHealthy_ShouldReturnHealthy()
    {
        _databaseMock
            .Setup(x => x.PingAsync(It.IsAny<CommandFlags>()))
            .ReturnsAsync(TimeSpan.FromMilliseconds(5));

        _connectionMultiplexerMock
            .Setup(x => x.GetEndPoints(It.IsAny<bool>()))
            .Returns([new DnsEndPoint("localhost", 6379)]);

        _connectionMultiplexerMock
            .Setup(x => x.GetServer(It.IsAny<EndPoint>(), It.IsAny<object>()))
            .Returns(_serverMock.Object);

        _serverMock
            .Setup(x => x.IsConnected)
            .Returns(true);

        var healthCheck = new RedisHealthCheck(_connectionMultiplexerMock.Object);
        var context = new HealthCheckContext { Registration = new HealthCheckRegistration("redis", healthCheck, null, null) };

        var result = await healthCheck.CheckHealthAsync(context);

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal("Redis is healthy", result.Description);
        Assert.True(result.Data.ContainsKey("latency"));
        Assert.True(result.Data.ContainsKey("connectedEndpoints"));
    }

    [Fact]
    public async Task CheckHealthAsync_WhenRedisThrowsException_ShouldReturnUnhealthy()
    {
        _databaseMock
            .Setup(x => x.PingAsync(It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisException("Connection refused"));

        var healthCheck = new RedisHealthCheck(_connectionMultiplexerMock.Object);
        var context = new HealthCheckContext { Registration = new HealthCheckRegistration("redis", healthCheck, null, null) };

        var result = await healthCheck.CheckHealthAsync(context);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("Redis connection failed", result.Description);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenRedisThrowsGenericException_ShouldReturnUnhealthy()
    {
        _databaseMock
            .Setup(x => x.PingAsync(It.IsAny<CommandFlags>()))
            .ThrowsAsync(new InvalidOperationException("Unexpected error"));

        var healthCheck = new RedisHealthCheck(_connectionMultiplexerMock.Object);
        var context = new HealthCheckContext { Registration = new HealthCheckRegistration("redis", healthCheck, null, null) };

        var result = await healthCheck.CheckHealthAsync(context);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("Redis health check failed", result.Description);
    }
}
