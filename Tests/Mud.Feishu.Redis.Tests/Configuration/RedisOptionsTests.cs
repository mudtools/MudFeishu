// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
// -----------------------------------------------------------------------

using Mud.Feishu.Redis.Configuration;

namespace Mud.Feishu.Redis.Tests.Configuration;

/// <summary>
/// RedisOptions 单元测试（R4：仅嵌套 Connection/Advanced）
/// </summary>
public class RedisOptionsTests
{
    [Fact]
    public void RedisOptions_DefaultValues_ShouldBeCorrect()
    {
        var options = new RedisOptions();

        Assert.Equal("localhost:6379", options.Connection.ServerAddress);
        Assert.Equal(string.Empty, options.Connection.Password);
        Assert.Equal("feishu:nonce:", options.NonceKeyPrefix);
        Assert.Equal("feishu:seqid:", options.SeqIdKeyPrefix);
        Assert.Equal("feishu:event:", options.EventKeyPrefix);
        Assert.Equal(TimeSpan.FromHours(48), options.EventCacheExpiration);
        Assert.Equal(TimeSpan.FromHours(48), options.SeqIdCacheExpiration);
        // WHF-R2/A4：NonceTtl 默认从 5 分钟调整为 10 分钟（2 × 300s 上限容差）
Assert.Equal(TimeSpan.FromMinutes(10), options.NonceTtl);
        Assert.Equal(5000, options.Connection.ConnectTimeout);
        Assert.Equal(5000, options.Connection.SyncTimeout);
        Assert.False(options.Connection.Ssl);
        Assert.False(options.Advanced.AllowAdmin);
        Assert.True(options.Connection.AbortOnConnectFail);
        Assert.Equal(3, options.Connection.ConnectRetry);
        Assert.Null(options.Connection.DefaultDatabase);
        Assert.Null(options.Advanced.ClientName);
    }

    [Fact]
    public void RedisOptions_SetCustomValues_ShouldWork()
    {
        var options = new RedisOptions
        {
            Connection = new RedisConnectionOptions
            {
                ServerAddress = "redis.example.com:6380",
                Password = "test_password",
                ConnectTimeout = 10000,
                SyncTimeout = 10000,
                Ssl = true,
                AbortOnConnectFail = false,
                ConnectRetry = 5,
                DefaultDatabase = 1
            },
            Advanced = new RedisAdvancedOptions
            {
                AllowAdmin = false,
                ClientName = "TestClient"
            },
            NonceTtl = TimeSpan.FromMinutes(10),
            NonceKeyPrefix = "custom:nonce:",
            SeqIdKeyPrefix = "custom:seqid:"
        };

        Assert.Equal("redis.example.com:6380", options.Connection.ServerAddress);
        Assert.Equal("test_password", options.Connection.Password);
        Assert.Equal(TimeSpan.FromMinutes(10), options.NonceTtl);
        Assert.Equal("custom:nonce:", options.NonceKeyPrefix);
        Assert.Equal("custom:seqid:", options.SeqIdKeyPrefix);
        Assert.Equal(10000, options.Connection.ConnectTimeout);
        Assert.Equal(10000, options.Connection.SyncTimeout);
        Assert.True(options.Connection.Ssl);
        Assert.False(options.Advanced.AllowAdmin);
        Assert.False(options.Connection.AbortOnConnectFail);
        Assert.Equal(5, options.Connection.ConnectRetry);
        Assert.Equal(1, options.Connection.DefaultDatabase);
        Assert.Equal("TestClient", options.Advanced.ClientName);
    }

    [Fact]
    public void RedisOptions_SetServerAddress_ShouldAcceptDifferentFormats()
    {
        var options1 = new RedisOptions { Connection = new RedisConnectionOptions { ServerAddress = "localhost:6379" } };
        var options2 = new RedisOptions { Connection = new RedisConnectionOptions { ServerAddress = "127.0.0.1:6379" } };
        var options3 = new RedisOptions { Connection = new RedisConnectionOptions { ServerAddress = "redis.example.com:6380" } };
        var options4 = new RedisOptions { Connection = new RedisConnectionOptions { ServerAddress = "rediss://secure.redis.com:6380" } };

        Assert.Equal("localhost:6379", options1.Connection.ServerAddress);
        Assert.Equal("127.0.0.1:6379", options2.Connection.ServerAddress);
        Assert.Equal("redis.example.com:6380", options3.Connection.ServerAddress);
        Assert.Equal("rediss://secure.redis.com:6380", options4.Connection.ServerAddress);
    }

    [Fact]
    public void RedisOptions_SetKeyPrefixes_ShouldAllowCustomPrefixes()
    {
        var options = new RedisOptions
        {
            NonceKeyPrefix = "myapp:nonces:",
            SeqIdKeyPrefix = "myapp:seqids:"
        };

        Assert.Equal("myapp:nonces:", options.NonceKeyPrefix);
        Assert.Equal("myapp:seqids:", options.SeqIdKeyPrefix);
    }

    [Fact]
    public void RedisOptions_SetTimeouts_ShouldAcceptValidValues()
    {
        var options = new RedisOptions
        {
            Connection = new RedisConnectionOptions
            {
                ConnectTimeout = 15000,
                SyncTimeout = 20000
            }
        };

        Assert.Equal(15000, options.Connection.ConnectTimeout);
        Assert.Equal(20000, options.Connection.SyncTimeout);
    }

    [Fact]
    public void RedisOptions_SetCacheExpirations_ShouldAcceptValidTimeSpans()
    {
        var options = new RedisOptions
        {
            NonceTtl = TimeSpan.FromSeconds(30),
        };

        Assert.Equal(TimeSpan.FromSeconds(30), options.NonceTtl);
    }

    [Fact]
    public void Validate_ShouldNotThrow_WithDefaultValues()
    {
        var options = new RedisOptions();
        options.Validate();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_ShouldThrow_WhenServerAddressIsNullOrWhitespace(string? serverAddress)
    {
        var options = new RedisOptions
        {
            Connection = new RedisConnectionOptions { ServerAddress = serverAddress! }
        };

        var ex = Assert.Throws<InvalidOperationException>(() => options.Validate());
        Assert.Contains("Connection.ServerAddress", ex.Message);
    }

    [Theory]
    [InlineData("invalidformat")]
    [InlineData("justhost")]
    [InlineData("no-port-here")]
    public void Validate_ShouldThrow_WhenServerAddressMissingColonOrScheme(string serverAddress)
    {
        var options = new RedisOptions
        {
            Connection = new RedisConnectionOptions { ServerAddress = serverAddress }
        };

        var ex = Assert.Throws<InvalidOperationException>(() => options.Validate());
        Assert.Contains("Connection.ServerAddress", ex.Message);
    }

    [Theory]
    [InlineData("localhost:6379")]
    [InlineData("127.0.0.1:6379")]
    [InlineData("redis.example.com:6380")]
    [InlineData("redis://localhost:6379")]
    [InlineData("rediss://secure.redis.com:6380")]
    [InlineData("REDIS://localhost:6379")]
    [InlineData("REDISS://secure.redis.com:6380")]
    public void Validate_ShouldAccept_WhenServerAddressIsValidFormat(string serverAddress)
    {
        var options = new RedisOptions
        {
            Connection = new RedisConnectionOptions { ServerAddress = serverAddress }
        };

        options.Validate();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenConnectTimeoutLessThan1000()
    {
        var options = new RedisOptions
        {
            Connection = new RedisConnectionOptions { ConnectTimeout = 500 }
        };

        var ex = Assert.Throws<InvalidOperationException>(() => options.Validate());
        Assert.Contains("Connection.ConnectTimeout", ex.Message);
    }

    [Fact]
    public void Validate_ShouldAccept_WhenConnectTimeoutIsExactly1000()
    {
        var options = new RedisOptions
        {
            Connection = new RedisConnectionOptions { ConnectTimeout = 1000 }
        };

        options.Validate();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenSyncTimeoutLessThan1000()
    {
        var options = new RedisOptions
        {
            Connection = new RedisConnectionOptions { SyncTimeout = 999 }
        };

        var ex = Assert.Throws<InvalidOperationException>(() => options.Validate());
        Assert.Contains("Connection.SyncTimeout", ex.Message);
    }

    [Fact]
    public void Validate_ShouldAccept_WhenSyncTimeoutIsExactly1000()
    {
        var options = new RedisOptions
        {
            Connection = new RedisConnectionOptions { SyncTimeout = 1000 }
        };

        options.Validate();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenConnectRetryIsNegative()
    {
        var options = new RedisOptions
        {
            Connection = new RedisConnectionOptions { ConnectRetry = -1 }
        };

        var ex = Assert.Throws<InvalidOperationException>(() => options.Validate());
        Assert.Contains("Connection.ConnectRetry", ex.Message);
    }

    [Fact]
    public void Validate_ShouldAccept_WhenConnectRetryIsZero()
    {
        var options = new RedisOptions
        {
            Connection = new RedisConnectionOptions { ConnectRetry = 0 }
        };

        options.Validate();
    }
}
