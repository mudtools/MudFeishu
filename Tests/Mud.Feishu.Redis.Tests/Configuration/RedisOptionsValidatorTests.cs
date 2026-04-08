// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Options;
using Mud.Feishu.Redis.Configuration;
using Xunit;

namespace Mud.Feishu.Redis.Tests.Configuration;

/// <summary>
/// RedisOptions 验证器单元测试
/// </summary>
public class RedisOptionsValidatorTests
{
    private readonly RedisOptionsValidator _validator;

    public RedisOptionsValidatorTests()
    {
        _validator = new RedisOptionsValidator();
    }

    [Fact]
    public void Validate_WithNullOptions_ShouldReturnFail()
    {
        var result = _validator.Validate(null, null!);

        Assert.True(result.Failed);
        Assert.Contains("不能为 null", result.FailureMessage);
    }

    [Fact]
    public void Validate_WithValidOptions_ShouldReturnSuccess()
    {
        var options = new RedisOptions
        {
            ServerAddress = "localhost:6379"
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_WithEmptyServerAddress_ShouldReturnFail()
    {
        var options = new RedisOptions
        {
            ServerAddress = ""
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("ServerAddress", result.FailureMessage);
    }

    [Fact]
    public void Validate_WithInvalidServerAddressFormat_ShouldReturnFail()
    {
        var options = new RedisOptions
        {
            ServerAddress = "invalid_address"
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("格式无效", result.FailureMessage);
    }

    [Fact]
    public void Validate_WithRedisProtocol_ShouldReturnSuccess()
    {
        var options = new RedisOptions
        {
            ServerAddress = "redis://localhost:6379"
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_WithRedissProtocol_ShouldReturnSuccess()
    {
        var options = new RedisOptions
        {
            ServerAddress = "rediss://secure.redis.com:6380"
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_WithInvalidConnectTimeout_ShouldReturnFail()
    {
        var options = new RedisOptions
        {
            ServerAddress = "localhost:6379",
            ConnectTimeout = 500
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("ConnectTimeout", result.FailureMessage);
    }

    [Fact]
    public void Validate_WithInvalidSyncTimeout_ShouldReturnFail()
    {
        var options = new RedisOptions
        {
            ServerAddress = "localhost:6379",
            SyncTimeout = 500
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("SyncTimeout", result.FailureMessage);
    }

    [Fact]
    public void Validate_WithNegativeConnectRetry_ShouldReturnFail()
    {
        var options = new RedisOptions
        {
            ServerAddress = "localhost:6379",
            ConnectRetry = -1
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("ConnectRetry", result.FailureMessage);
    }
}
