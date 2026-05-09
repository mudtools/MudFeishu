// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Configuration;
using Xunit;

namespace Mud.Feishu.Abstractions.Tests.Configuration;

public class DeduplicationOptionsTests
{
    [Fact]
    public void Default_ShouldReturnOptionsWithDefaultValues()
    {
        var options = DeduplicationOptions.Default;

        Assert.NotNull(options);
        Assert.Equal(TimeSpan.FromHours(48), options.CacheExpiration);
        Assert.Equal(TimeSpan.FromMinutes(10), options.ProcessingTimeout);
        Assert.Equal(TimeSpan.FromMinutes(5), options.CleanupInterval);
        Assert.True(options.AllowProcessingOnFallback);
        Assert.Equal(Consts.DefaultRetryCount, options.MaxRetryCount);
    }

    [Fact]
    public void Default_ShouldReturnNewInstance()
    {
        var options1 = DeduplicationOptions.Default;
        var options2 = DeduplicationOptions.Default;

        Assert.NotSame(options1, options2);
    }

    [Fact]
    public void CacheExpiration_WhenSetToValidValue_ShouldUpdate()
    {
        var options = new DeduplicationOptions();
        var newExpiration = TimeSpan.FromHours(72);

        options.CacheExpiration = newExpiration;

        Assert.Equal(newExpiration, options.CacheExpiration);
    }

    [Fact]
    public void CacheExpiration_WhenSetToLessThanOneMinute_ShouldUseMinimum()
    {
        var options = new DeduplicationOptions();

        options.CacheExpiration = TimeSpan.FromSeconds(30);

        Assert.Equal(TimeSpan.FromMinutes(1), options.CacheExpiration);
    }

    [Fact]
    public void ProcessingTimeout_WhenSetToValidValue_ShouldUpdate()
    {
        var options = new DeduplicationOptions();
        var newTimeout = TimeSpan.FromMinutes(30);

        options.ProcessingTimeout = newTimeout;

        Assert.Equal(newTimeout, options.ProcessingTimeout);
    }

    [Fact]
    public void ProcessingTimeout_WhenSetToLessThanTenSeconds_ShouldUseMinimum()
    {
        var options = new DeduplicationOptions();

        options.ProcessingTimeout = TimeSpan.FromSeconds(5);

        Assert.Equal(TimeSpan.FromSeconds(10), options.ProcessingTimeout);
    }

    [Fact]
    public void CleanupInterval_WhenSetToValidValue_ShouldUpdate()
    {
        var options = new DeduplicationOptions();
        var newInterval = TimeSpan.FromMinutes(10);

        options.CleanupInterval = newInterval;

        Assert.Equal(newInterval, options.CleanupInterval);
    }

    [Fact]
    public void CleanupInterval_WhenSetToLessThanThirtySeconds_ShouldUseMinimum()
    {
        var options = new DeduplicationOptions();

        options.CleanupInterval = TimeSpan.FromSeconds(15);

        Assert.Equal(TimeSpan.FromSeconds(30), options.CleanupInterval);
    }

    [Fact]
    public void MaxRetryCount_ShouldHaveDefaultValue()
    {
        var options = new DeduplicationOptions();

        Assert.Equal(3, options.MaxRetryCount);
    }

    [Fact]
    public void InitialRetryDelay_ShouldHaveDefaultValue()
    {
        var options = new DeduplicationOptions();

        Assert.Equal(TimeSpan.FromSeconds(1), options.InitialRetryDelay);
    }

    [Fact]
    public void MaxRetryDelay_ShouldHaveDefaultValue()
    {
        var options = new DeduplicationOptions();

        Assert.Equal(TimeSpan.FromSeconds(30), options.MaxRetryDelay);
    }

    [Fact]
    public void KeyPrefix_ShouldHaveDefaultValue()
    {
        var options = new DeduplicationOptions();

        Assert.Equal("feishu:event:", options.KeyPrefix);
    }

    [Fact]
    public void EnableVerboseLogging_ShouldHaveDefaultValue()
    {
        var options = new DeduplicationOptions();

        Assert.False(options.EnableVerboseLogging);
    }

    [Fact]
    public void AllProperties_ShouldBeSettable()
    {
        var options = new DeduplicationOptions
        {
            CacheExpiration = TimeSpan.FromHours(24),
            ProcessingTimeout = TimeSpan.FromMinutes(15),
            CleanupInterval = TimeSpan.FromMinutes(3),
            AllowProcessingOnFallback = false,
            MaxRetryCount = 5,
            InitialRetryDelay = TimeSpan.FromMilliseconds(200),
            MaxRetryDelay = TimeSpan.FromSeconds(10),
            KeyPrefix = "custom:prefix:",
            EnableVerboseLogging = true
        };

        Assert.Equal(TimeSpan.FromHours(24), options.CacheExpiration);
        Assert.Equal(TimeSpan.FromMinutes(15), options.ProcessingTimeout);
        Assert.Equal(TimeSpan.FromMinutes(3), options.CleanupInterval);
        Assert.False(options.AllowProcessingOnFallback);
        Assert.Equal(5, options.MaxRetryCount);
        Assert.Equal(TimeSpan.FromMilliseconds(200), options.InitialRetryDelay);
        Assert.Equal(TimeSpan.FromSeconds(10), options.MaxRetryDelay);
        Assert.Equal("custom:prefix:", options.KeyPrefix);
        Assert.True(options.EnableVerboseLogging);
    }

    [Fact]
    public void HighReliability_ShouldReturnCorrectConfiguration()
    {
        var options = DeduplicationOptions.HighReliability;

        Assert.Equal(TimeSpan.FromHours(72), options.CacheExpiration);
        Assert.Equal(TimeSpan.FromMinutes(5), options.ProcessingTimeout);
        Assert.False(options.AllowProcessingOnFallback);
        Assert.Equal(5, options.MaxRetryCount);
    }

    [Fact]
    public void HighAvailability_ShouldReturnCorrectConfiguration()
    {
        var options = DeduplicationOptions.HighAvailability;

        Assert.Equal(TimeSpan.FromHours(48), options.CacheExpiration);
        Assert.Equal(TimeSpan.FromMinutes(15), options.ProcessingTimeout);
        Assert.True(options.AllowProcessingOnFallback);
        Assert.Equal(3, options.MaxRetryCount);
    }
}
