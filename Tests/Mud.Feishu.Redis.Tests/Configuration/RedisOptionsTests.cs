// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Redis.Configuration;

namespace Mud.Feishu.Redis.Tests.Configuration;

/// <summary>
/// RedisOptions 单元测试
/// </summary>
public class RedisOptionsTests
{
    [Fact]
    public void RedisOptions_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var options = new RedisOptions();

        // Assert
        Assert.Equal("localhost:6379", options.ServerAddress);
        Assert.Equal(string.Empty, options.Password);
        Assert.Equal(TimeSpan.FromHours(48), options.EventCacheExpiration);
        Assert.Equal(TimeSpan.FromMinutes(5), options.NonceTtl);
        Assert.Equal(TimeSpan.FromHours(48), options.SeqIdCacheExpiration);
        Assert.Equal("feishu:event:", options.EventKeyPrefix);
        Assert.Equal("feishu:nonce:", options.NonceKeyPrefix);
        Assert.Equal("feishu:seqid:", options.SeqIdKeyPrefix);
        Assert.Equal(5000, options.ConnectTimeout);
        Assert.Equal(5000, options.SyncTimeout);
        Assert.False(options.Ssl);
        Assert.False(options.AllowAdmin);
        Assert.True(options.AbortOnConnectFail);
        Assert.Equal(3, options.ConnectRetry);
        Assert.Null(options.DefaultDatabase);
        Assert.Null(options.ClientName);
    }

    [Fact]
    public void RedisOptions_SetCustomValues_ShouldWork()
    {
        // Arrange
        var options = new RedisOptions
        {
            ServerAddress = "redis.example.com:6380",
            Password = "test_password",
            EventCacheExpiration = TimeSpan.FromHours(12),
            NonceTtl = TimeSpan.FromMinutes(10),
            SeqIdCacheExpiration = TimeSpan.FromHours(48),
            EventKeyPrefix = "custom:event:",
            NonceKeyPrefix = "custom:nonce:",
            SeqIdKeyPrefix = "custom:seqid:",
            ConnectTimeout = 10000,
            SyncTimeout = 10000,
            Ssl = true,
            AllowAdmin = false,
            AbortOnConnectFail = false,
            ConnectRetry = 5,
            DefaultDatabase = 1,
            ClientName = "TestClient"
        };

        // Assert
        Assert.Equal("redis.example.com:6380", options.ServerAddress);
        Assert.Equal("test_password", options.Password);
        Assert.Equal(TimeSpan.FromHours(12), options.EventCacheExpiration);
        Assert.Equal(TimeSpan.FromMinutes(10), options.NonceTtl);
        Assert.Equal(TimeSpan.FromHours(48), options.SeqIdCacheExpiration);
        Assert.Equal("custom:event:", options.EventKeyPrefix);
        Assert.Equal("custom:nonce:", options.NonceKeyPrefix);
        Assert.Equal("custom:seqid:", options.SeqIdKeyPrefix);
        Assert.Equal(10000, options.ConnectTimeout);
        Assert.Equal(10000, options.SyncTimeout);
        Assert.True(options.Ssl);
        Assert.False(options.AllowAdmin);
        Assert.False(options.AbortOnConnectFail);
        Assert.Equal(5, options.ConnectRetry);
        Assert.Equal(1, options.DefaultDatabase);
        Assert.Equal("TestClient", options.ClientName);
    }

    [Fact]
    public void RedisOptions_SetServerAddress_ShouldAcceptDifferentFormats()
    {
        // Arrange & Act
        var options1 = new RedisOptions { ServerAddress = "localhost:6379" };
        var options2 = new RedisOptions { ServerAddress = "127.0.0.1:6379" };
        var options3 = new RedisOptions { ServerAddress = "redis.example.com:6380" };
        var options4 = new RedisOptions { ServerAddress = "rediss://secure.redis.com:6380" };

        // Assert
        Assert.Equal("localhost:6379", options1.ServerAddress);
        Assert.Equal("127.0.0.1:6379", options2.ServerAddress);
        Assert.Equal("redis.example.com:6380", options3.ServerAddress);
        Assert.Equal("rediss://secure.redis.com:6380", options4.ServerAddress);
    }

    [Fact]
    public void RedisOptions_SetKeyPrefixes_ShouldAllowCustomPrefixes()
    {
        // Arrange & Act
        var options = new RedisOptions
        {
            EventKeyPrefix = "myapp:events:",
            NonceKeyPrefix = "myapp:nonces:",
            SeqIdKeyPrefix = "myapp:seqids:"
        };

        // Assert
        Assert.Equal("myapp:events:", options.EventKeyPrefix);
        Assert.Equal("myapp:nonces:", options.NonceKeyPrefix);
        Assert.Equal("myapp:seqids:", options.SeqIdKeyPrefix);
    }

    [Fact]
    public void RedisOptions_SetTimeouts_ShouldAcceptValidValues()
    {
        // Arrange & Act
        var options = new RedisOptions
        {
            ConnectTimeout = 15000,
            SyncTimeout = 20000
        };

        // Assert
        Assert.Equal(15000, options.ConnectTimeout);
        Assert.Equal(20000, options.SyncTimeout);
    }

    [Fact]
    public void RedisOptions_SetCacheExpirations_ShouldAcceptValidTimeSpans()
    {
        // Arrange & Act
        var options = new RedisOptions
        {
            EventCacheExpiration = TimeSpan.FromDays(7),
            NonceTtl = TimeSpan.FromSeconds(30),
            SeqIdCacheExpiration = TimeSpan.FromDays(30)
        };

        // Assert
        Assert.Equal(TimeSpan.FromDays(7), options.EventCacheExpiration);
        Assert.Equal(TimeSpan.FromSeconds(30), options.NonceTtl);
        Assert.Equal(TimeSpan.FromDays(30), options.SeqIdCacheExpiration);
    }
}
