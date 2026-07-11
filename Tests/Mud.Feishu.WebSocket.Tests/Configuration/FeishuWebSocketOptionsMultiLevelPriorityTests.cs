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
/// FeishuWebSocketOptions 多级配置优先级测试类
/// 测试不同配置源的优先级：代码配置 > 配置文件 > 默认值
/// </summary>
public class FeishuWebSocketOptionsMultiLevelPriorityTests
{
    [Fact]
    public void ConfigurationPriority_ShouldPreferCodeConfiguration_OverFileConfiguration()
    {
        // Arrange
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(new Dictionary<string, string>
        {
            ["FeishuWebSocket:HeartbeatIntervalMs"] = "30000",
            ["FeishuWebSocket:AutoReconnect"] = "false"
        });
        
        var configuration = configurationBuilder.Build();
        
        var services = new ServiceCollection();
        services.Configure<FeishuWebSocketOptions>(configuration.GetSection("FeishuWebSocket"));
        
        // 通过 Configure 方法覆盖配置文件中的设置（代码配置优先级更高）
        services.Configure<FeishuWebSocketOptions>(options =>
        {
            options.HeartbeatIntervalMs = 25000; // 覆盖配置文件中的 30000
            options.AutoReconnect = true;        // 覆盖配置文件中的 false
        });

        var serviceProvider = services.BuildServiceProvider();
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<FeishuWebSocketOptions>>();

        // Act
        var options = optionsMonitor.CurrentValue;

        // Assert - 代码配置优先级高于文件配置
        options.HeartbeatIntervalMs.Should().Be(25000);
        options.AutoReconnect.Should().BeTrue();
    }

    [Fact]
    public void ConfigurationPriority_ShouldUseFileConfiguration_WhenNoCodeConfiguration()
    {
        // Arrange
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(new Dictionary<string, string>
        {
            ["FeishuWebSocket:HeartbeatIntervalMs"] = "20000",
            ["FeishuWebSocket:AutoReconnect"] = "true",
            ["FeishuWebSocket:MaxReconnectAttempts"] = "3"
        });
        
        var configuration = configurationBuilder.Build();
        
        var services = new ServiceCollection();
        services.Configure<FeishuWebSocketOptions>(configuration.GetSection("FeishuWebSocket"));

        var serviceProvider = services.BuildServiceProvider();
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<FeishuWebSocketOptions>>();

        // Act
        var options = optionsMonitor.CurrentValue;

        // Assert - 使用配置文件中的值
        options.HeartbeatIntervalMs.Should().Be(20000);
        options.AutoReconnect.Should().BeTrue();
        options.MaxReconnectAttempts.Should().Be(3);
    }

    [Fact]
    public void ConfigurationPriority_ShouldUseDefaultValues_WhenNoConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.Configure<FeishuWebSocketOptions>(options => { }); // 空配置

        var serviceProvider = services.BuildServiceProvider();
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<FeishuWebSocketOptions>>();

        // Act
        var options = optionsMonitor.CurrentValue;

        // Assert - 使用默认值
        options.AutoReconnect.Should().BeTrue();
        options.MaxReconnectAttempts.Should().Be(5);
        options.HeartbeatIntervalMs.Should().Be(25000);
        options.EnableLogging.Should().BeTrue();
    }

    [Fact]
    public void ConfigurationPriority_ShouldHandlePartialOverrides_WhenMixedConfiguration()
    {
        // Arrange
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(new Dictionary<string, string>
        {
            ["FeishuWebSocket:HeartbeatIntervalMs"] = "30000", // 只配置部分属性
            ["FeishuWebSocket:AutoReconnect"] = "false"
        });
        
        var configuration = configurationBuilder.Build();
        
        var services = new ServiceCollection();
        services.Configure<FeishuWebSocketOptions>(configuration.GetSection("FeishuWebSocket"));
        
        // 部分覆盖
        services.Configure<FeishuWebSocketOptions>(options =>
        {
            options.HeartbeatIntervalMs = 35000; // 覆盖文件配置
            // 不覆盖 AutoReconnect，保留文件配置中的 false
        });

        var serviceProvider = services.BuildServiceProvider();
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<FeishuWebSocketOptions>>();

        // Act
        var options = optionsMonitor.CurrentValue;

        // Assert - 混合配置：HeartbeatIntervalMs 使用代码配置，AutoReconnect 使用文件配置，其他未配置的属性使用默认值
        options.HeartbeatIntervalMs.Should().Be(35000); // 代码配置
        options.AutoReconnect.Should().BeFalse();       // 文件配置
        options.MaxReconnectAttempts.Should().Be(5);    // 默认值
        options.EnableLogging.Should().BeTrue();        // 默认值
    }

    [Fact]
    public void EventDeduplicationConfiguration_ShouldFollowSamePriorityRules()
    {
        // Arrange
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(new Dictionary<string, string>
        {
            ["FeishuWebSocket:EventDeduplication:Mode"] = "Distributed",
            ["FeishuWebSocket:EventDeduplication:CacheExpiration"] = "24:00:00"
        });
        
        var configuration = configurationBuilder.Build();
        
        var services = new ServiceCollection();
        services.Configure<FeishuWebSocketOptions>(configuration.GetSection("FeishuWebSocket"));
        
        // 代码配置覆盖
        services.Configure<FeishuWebSocketOptions>(options =>
        {
            options.EventDeduplication.Mode = EventDeduplicationMode.InMemory;
            options.EventDeduplication.CacheExpiration = TimeSpan.FromHours(48);
        });

        var serviceProvider = services.BuildServiceProvider();
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<FeishuWebSocketOptions>>();

        // Act
        var options = optionsMonitor.CurrentValue;

        // Assert - 代码配置优先级高于文件配置
        options.EventDeduplication.Mode.Should().Be(EventDeduplicationMode.InMemory);
        options.EventDeduplication.CacheExpiration.Should().Be(TimeSpan.FromHours(48));
    }

    [Fact]
    public void MessageSizeLimitsConfiguration_ShouldFollowSamePriorityRules()
    {
        // Arrange
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(new Dictionary<string, string>
        {
            ["FeishuWebSocket:MessageSizeLimits:MaxTextMessageSize"] = "2097152",  // 2MB
            ["FeishuWebSocket:MessageSizeLimits:MaxBinaryMessageSize"] = "31457280" // 30MB
        });
        
        var configuration = configurationBuilder.Build();
        
        var services = new ServiceCollection();
        services.Configure<FeishuWebSocketOptions>(configuration.GetSection("FeishuWebSocket"));
        
        // 代码配置部分覆盖
        services.Configure<FeishuWebSocketOptions>(options =>
        {
            options.MessageSizeLimits.MaxTextMessageSize = 3145728; // 3MB，覆盖文件配置
            // 不覆盖 MaxBinaryMessageSize，保留文件配置
        });

        var serviceProvider = services.BuildServiceProvider();
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<FeishuWebSocketOptions>>();

        // Act
        var options = optionsMonitor.CurrentValue;

        // Assert - 部分覆盖测试
        options.MessageSizeLimits.MaxTextMessageSize.Should().Be(3145728);   // 代码配置
        options.MessageSizeLimits.MaxBinaryMessageSize.Should().Be(31457280); // 文件配置
    }

    [Fact]
    public void ConfigurationValidation_ShouldWorkWithAllPriorityLevels()
    {
        // Arrange - 文件配置包含无效值
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(new Dictionary<string, string>
        {
            ["FeishuWebSocket:HeartbeatIntervalMs"] = "1000", // 低于最小值，会被自动修正
            ["FeishuWebSocket:MaxReconnectAttempts"] = "15"   // 高于默认值，但合法
        });
        
        var configuration = configurationBuilder.Build();
        
        var services = new ServiceCollection();
        services.Configure<FeishuWebSocketOptions>(configuration.GetSection("FeishuWebSocket"));
        
        // 代码配置覆盖部分值
        services.Configure<FeishuWebSocketOptions>(options =>
        {
            options.HeartbeatIntervalMs = 15000; // 覆盖文件中的无效值
        });

        var serviceProvider = services.BuildServiceProvider();
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<FeishuWebSocketOptions>>();

        // Act
        var options = optionsMonitor.CurrentValue;

        // Assert - 验证配置有效
        var act = () => options.Validate();
        act.Should().NotThrow();
        
        // 验证优先级：代码配置覆盖文件配置
        options.HeartbeatIntervalMs.Should().Be(15000);      // 代码配置
        options.MaxReconnectAttempts.Should().Be(15);        // 文件配置
    }

    [Fact]
    public void NestedConfiguration_ShouldOverrideCorrectly_WhenMultiLevelSetup()
    {
        // Arrange - 三层配置：默认值 < 文件配置 < 代码配置
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(new Dictionary<string, string>
        {
            ["FeishuWebSocket:EventDeduplication:Mode"] = "Distributed",
            ["FeishuWebSocket:EventDeduplication:CacheExpiration"] = "12:00:00",
            ["FeishuWebSocket:EventDeduplication:CleanupInterval"] = "00:03:00",
            ["FeishuWebSocket:EventDeduplication:MaxCacheSize"] = "75000"
        });
        
        var configuration = configurationBuilder.Build();
        
        var services = new ServiceCollection();
        services.Configure<FeishuWebSocketOptions>(configuration.GetSection("FeishuWebSocket"));
        
        // 代码配置部分覆盖嵌套属性
        services.Configure<FeishuWebSocketOptions>(options =>
        {
            // 覆盖部分 EventDeduplication 属性
            options.EventDeduplication.Mode = EventDeduplicationMode.InMemory;
            options.EventDeduplication.CacheExpiration = TimeSpan.FromHours(36);
            // 保留文件配置中的 CleanupInterval 和 MaxCacheSize
        });

        var serviceProvider = services.BuildServiceProvider();
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<FeishuWebSocketOptions>>();

        // Act
        var options = optionsMonitor.CurrentValue;

        // Assert - 验证嵌套配置的优先级
        options.EventDeduplication.Mode.Should().Be(EventDeduplicationMode.InMemory);    // 代码配置
        options.EventDeduplication.CacheExpiration.Should().Be(TimeSpan.FromHours(36));  // 代码配置
        options.EventDeduplication.CleanupInterval.Should().Be(TimeSpan.FromMinutes(3));  // 文件配置
        options.EventDeduplication.MaxCacheSize.Should().Be(75000);                     // 文件配置
    }
}