// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Mud.Feishu.WebSocket.Tests.Configuration;

/// <summary>
/// EventDeduplicationOptions TimeSpan 绑定陷阱测试
/// 验证 "24:00:00" 在 IConfiguration 绑定时的解释行为
/// </summary>
public class EventDeduplicationOptionsTimeSpanTrapTests
{
    [Fact]
    public void Bind_CacheExpiration_24Hours_ShouldBe24HoursNot24Days()
    {
        // Arrange - 验证 IConfiguration 绑定中 "24:00:00" 的实际解释行为
        // 注意：Microsoft.Extensions.Configuration 的 ConfigurationBinder 使用 TypeDescriptor 转换，
        // 将 "24:00:00" 解释为 24 天 (24.00:00:00)，而非 24 小时 (1.00:00:00)。
        // 正确的 24 小时格式应为 "1.00:00:00"。
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeishuWebSocket:EventDeduplication:CacheExpiration"] = "24:00:00"
            })
            .Build();

        var services = new ServiceCollection();
        services.Configure<FeishuWebSocketOptions>(config.GetSection("FeishuWebSocket"));
        var sp = services.BuildServiceProvider();

        // Act
        var options = sp.GetRequiredService<IOptions<FeishuWebSocketOptions>>().Value;

        // Assert - 记录陷阱：IConfiguration 绑定将 "24:00:00" 解释为 24 天
        // setter 的最小值校验（60 秒）无法拦截此值（24 天 >> 60 秒），故配置静默生效为 24 天
        options.EventDeduplication.CacheExpiration.Should().Be(TimeSpan.FromDays(24));

        // 验证正确的 24 小时格式 "1.00:00:00"
        var correctConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeishuWebSocket:EventDeduplication:CacheExpiration"] = "1.00:00:00"
            })
            .Build();

        var correctServices = new ServiceCollection();
        correctServices.Configure<FeishuWebSocketOptions>(correctConfig.GetSection("FeishuWebSocket"));
        var correctSp = correctServices.BuildServiceProvider();
        var correctOptions = correctSp.GetRequiredService<IOptions<FeishuWebSocketOptions>>().Value;

        correctOptions.EventDeduplication.CacheExpiration.Should().Be(TimeSpan.FromHours(24));
    }

    [Fact]
    public void Bind_CacheExpiration_48Hours_ShouldBindCorrectly()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeishuWebSocket:EventDeduplication:CacheExpiration"] = "2.00:00:00"
            })
            .Build();

        var services = new ServiceCollection();
        services.Configure<FeishuWebSocketOptions>(config.GetSection("FeishuWebSocket"));
        var sp = services.BuildServiceProvider();

        // Act
        var options = sp.GetRequiredService<IOptions<FeishuWebSocketOptions>>().Value;

        // Assert
        options.EventDeduplication.CacheExpiration.Should().Be(TimeSpan.FromHours(48));
    }

    [Fact]
    public void Bind_CacheExpiration_Below60Seconds_ShouldClampTo60Seconds()
    {
        // Arrange - 30 秒低于最小值 60 秒
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeishuWebSocket:EventDeduplication:CacheExpiration"] = "00:00:30"
            })
            .Build();

        var services = new ServiceCollection();
        services.Configure<FeishuWebSocketOptions>(config.GetSection("FeishuWebSocket"));
        var sp = services.BuildServiceProvider();

        // Act
        var options = sp.GetRequiredService<IOptions<FeishuWebSocketOptions>>().Value;

        // Assert - setter 应将 30 秒提升至 60 秒
        options.EventDeduplication.CacheExpiration.Should().Be(TimeSpan.FromSeconds(60));
    }

    [Fact]
    public void Bind_MaxCacheSize_NegativeValue_ShouldClampToZero()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeishuWebSocket:EventDeduplication:MaxCacheSize"] = "-100"
            })
            .Build();

        var services = new ServiceCollection();
        services.Configure<FeishuWebSocketOptions>(config.GetSection("FeishuWebSocket"));
        var sp = services.BuildServiceProvider();

        // Act
        var options = sp.GetRequiredService<IOptions<FeishuWebSocketOptions>>().Value;

        // Assert - setter 应将负数提升至 0
        options.EventDeduplication.MaxCacheSize.Should().Be(0);
    }

    [Fact]
    public void Bind_Mode_None_WithCustomCacheSettings_ShouldThrowOnValidate()
    {
        // Arrange - Mode=None 时设置非默认 CacheExpiration 应在 Validate 时抛异常
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeishuWebSocket:EventDeduplication:Mode"] = "None",
                ["FeishuWebSocket:EventDeduplication:CacheExpiration"] = "12:00:00"
            })
            .Build();

        var services = new ServiceCollection();
        services.Configure<FeishuWebSocketOptions>(config.GetSection("FeishuWebSocket"));
        var sp = services.BuildServiceProvider();

        // Act
        var options = sp.GetRequiredService<IOptions<FeishuWebSocketOptions>>().Value;

        // Assert
        var act = () => options.Validate();
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*EventDeduplication.Mode 设置为 None*");
    }
}
