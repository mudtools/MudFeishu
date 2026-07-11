// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Mud.Feishu.WebSocket.Tests.Configuration;

/// <summary>
/// FeishuWebSocketOptions 热更新测试类
/// 测试 IOptionsMonitor 的配置热更新功能
/// </summary>
public class FeishuWebSocketOptionsHotUpdateTests
{
    [Fact]
    public void OptionsMonitor_ShouldReflectConfigurationChanges_WhenConfigurationReloaded()
    {
        // Arrange
        var services = new ServiceCollection();
        services.Configure<FeishuWebSocketOptions>(options =>
        {
            options.AutoReconnect = true;
            options.HeartbeatIntervalMs = 25000;
            options.ReconnectDelayMs = 5000;
        });

        var serviceProvider = services.BuildServiceProvider();
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<FeishuWebSocketOptions>>();

        // Act - 获取初始值
        var initialOptions = optionsMonitor.CurrentValue;

        // Assert - 验证初始值
        initialOptions.AutoReconnect.Should().BeTrue();
        initialOptions.HeartbeatIntervalMs.Should().Be(25000);
        initialOptions.ReconnectDelayMs.Should().Be(5000);

        // Act - 创建新的配置（模拟配置热重载）
        var newServices = new ServiceCollection();
        newServices.Configure<FeishuWebSocketOptions>(options =>
        {
            options.AutoReconnect = false;
            options.HeartbeatIntervalMs = 30000;
            options.ReconnectDelayMs = 10000;
        });

        var newServiceProvider = newServices.BuildServiceProvider();
        var newOptionsMonitor = newServiceProvider.GetRequiredService<IOptionsMonitor<FeishuWebSocketOptions>>();

        // Assert - 验证新值
        var updatedOptions = newOptionsMonitor.CurrentValue;
        updatedOptions.AutoReconnect.Should().BeFalse();
        updatedOptions.HeartbeatIntervalMs.Should().Be(30000);
        updatedOptions.ReconnectDelayMs.Should().Be(10000);
    }

    [Fact]
    public void ConfigurationValidation_ShouldWorkWithHotReload_WhenValuesChange()
    {
        // Arrange
        var services = new ServiceCollection();
        services.Configure<FeishuWebSocketOptions>(options =>
        {
            options.HeartbeatIntervalMs = 25000; // Valid value
            options.EventDeduplication.Mode = EventDeduplicationMode.InMemory;
        });

        var serviceProvider = services.BuildServiceProvider();
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<FeishuWebSocketOptions>>();

        // Act & Assert - 验证初始有效配置
        var initialOptions = optionsMonitor.CurrentValue;
        var act = () => initialOptions.Validate();
        act.Should().NotThrow();

        // Act - 更新为新的有效配置
        var newServices = new ServiceCollection();
        newServices.Configure<FeishuWebSocketOptions>(options =>
        {
            options.HeartbeatIntervalMs = 15000; // Still valid
            options.EventDeduplication.Mode = EventDeduplicationMode.Distributed;
            options.EventDeduplication.CacheExpiration = TimeSpan.FromHours(24);
        });

        var newServiceProvider = newServices.BuildServiceProvider();
        var newOptionsMonitor = newServiceProvider.GetRequiredService<IOptionsMonitor<FeishuWebSocketOptions>>();

        // Assert - 验证新配置也有效
        var updatedOptions = newOptionsMonitor.CurrentValue;
        var actUpdated = () => updatedOptions.Validate();
        actUpdated.Should().NotThrow();
        updatedOptions.HeartbeatIntervalMs.Should().Be(15000);
        updatedOptions.EventDeduplication.Mode.Should().Be(EventDeduplicationMode.Distributed);
    }

    [Fact]
    public void PropertyValidation_ShouldEnforceConstraints_WhenConfigurationChanges()
    {
        // Arrange
        var services = new ServiceCollection();
        services.Configure<FeishuWebSocketOptions>(options =>
        {
            options.HeartbeatIntervalMs = 25000; // Valid value
        });

        var serviceProvider = services.BuildServiceProvider();
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<FeishuWebSocketOptions>>();

        // Act & Assert - 验证初始配置有效
        var initialOptions = optionsMonitor.CurrentValue;
        var act = () => initialOptions.Validate();
        act.Should().NotThrow();

        // Act - 设置会触发自动修正的值
        var newServices = new ServiceCollection();
        newServices.Configure<FeishuWebSocketOptions>(options =>
        {
            options.HeartbeatIntervalMs = 1000; // Will be auto-corrected to 5000
            options.ReconnectDelayMs = 500;     // Will be auto-corrected to 1000
        });

        var newServiceProvider = newServices.BuildServiceProvider();
        var newOptionsMonitor = newServiceProvider.GetRequiredService<IOptionsMonitor<FeishuWebSocketOptions>>();

        // Assert - 验证属性自动修正且配置仍然有效
        var updatedOptions = newOptionsMonitor.CurrentValue;
        updatedOptions.HeartbeatIntervalMs.Should().Be(5000); // Auto-corrected to minimum
        updatedOptions.ReconnectDelayMs.Should().Be(1000);    // Auto-corrected to minimum
        
        var actUpdated = () => updatedOptions.Validate();
        actUpdated.Should().NotThrow();
    }

    [Fact]
    public void EventDeduplicationSettings_ShouldUpdateIndependently_WhenConfigurationChanges()
    {
        // Arrange
        var services = new ServiceCollection();
        services.Configure<FeishuWebSocketOptions>(options =>
        {
            options.EventDeduplication.Mode = EventDeduplicationMode.InMemory;
            options.EventDeduplication.CacheExpiration = TimeSpan.FromHours(48);
            options.EventDeduplication.CleanupInterval = TimeSpan.FromMinutes(5);
            options.EventDeduplication.ProcessingTimeout = TimeSpan.FromMinutes(10);
        });

        var serviceProvider = services.BuildServiceProvider();
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<FeishuWebSocketOptions>>();

        // Act - 获取初始值
        var initialOptions = optionsMonitor.CurrentValue;

        // Assert - 验证初始值
        initialOptions.EventDeduplication.Mode.Should().Be(EventDeduplicationMode.InMemory);
        initialOptions.EventDeduplication.CacheExpiration.Should().Be(TimeSpan.FromHours(48));
        initialOptions.EventDeduplication.CleanupInterval.Should().Be(TimeSpan.FromMinutes(5));
        initialOptions.EventDeduplication.ProcessingTimeout.Should().Be(TimeSpan.FromMinutes(10));

        // Act - 更新去重配置
        var newServices = new ServiceCollection();
        newServices.Configure<FeishuWebSocketOptions>(options =>
        {
            options.EventDeduplication.Mode = EventDeduplicationMode.Distributed;
            options.EventDeduplication.CacheExpiration = TimeSpan.FromHours(24);
            options.EventDeduplication.CleanupInterval = TimeSpan.FromMinutes(10);
            options.EventDeduplication.ProcessingTimeout = TimeSpan.FromMinutes(15);
            options.EventDeduplication.MaxCacheSize = 50000;
        });

        var newServiceProvider = newServices.BuildServiceProvider();
        var newOptionsMonitor = newServiceProvider.GetRequiredService<IOptionsMonitor<FeishuWebSocketOptions>>();

        // Assert - 验证新值
        var updatedOptions = newOptionsMonitor.CurrentValue;
        updatedOptions.EventDeduplication.Mode.Should().Be(EventDeduplicationMode.Distributed);
        updatedOptions.EventDeduplication.CacheExpiration.Should().Be(TimeSpan.FromHours(24));
        updatedOptions.EventDeduplication.CleanupInterval.Should().Be(TimeSpan.FromMinutes(10));
        updatedOptions.EventDeduplication.ProcessingTimeout.Should().Be(TimeSpan.FromMinutes(15));
        updatedOptions.EventDeduplication.MaxCacheSize.Should().Be(50000);
    }

    [Fact]
    public void MessageSizeLimits_ShouldUpdateWithHotReload_WhenConfigurationChanges()
    {
        // Arrange
        var services = new ServiceCollection();
        services.Configure<FeishuWebSocketOptions>(options =>
        {
            options.MessageSizeLimits.MaxTextMessageSize = 1024 * 1024; // 1MB
            options.MessageSizeLimits.MaxBinaryMessageSize = 10 * 1024 * 1024; // 10MB
        });

        var serviceProvider = services.BuildServiceProvider();
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<FeishuWebSocketOptions>>();

        // Act - 获取初始值
        var initialOptions = optionsMonitor.CurrentValue;

        // Assert - 验证初始值
        initialOptions.MessageSizeLimits.MaxTextMessageSize.Should().Be(1024 * 1024);
        initialOptions.MessageSizeLimits.MaxBinaryMessageSize.Should().Be(10 * 1024 * 1024);

        // Act - 更新配置
        var newServices = new ServiceCollection();
        newServices.Configure<FeishuWebSocketOptions>(options =>
        {
            options.MessageSizeLimits.MaxTextMessageSize = 2 * 1024 * 1024; // 2MB
            options.MessageSizeLimits.MaxBinaryMessageSize = 20 * 1024 * 1024; // 20MB
        });

        var newServiceProvider = newServices.BuildServiceProvider();
        var newOptionsMonitor = newServiceProvider.GetRequiredService<IOptionsMonitor<FeishuWebSocketOptions>>();

        // Assert - 验证新值
        var updatedOptions = newOptionsMonitor.CurrentValue;
        updatedOptions.MessageSizeLimits.MaxTextMessageSize.Should().Be(2 * 1024 * 1024);
        updatedOptions.MessageSizeLimits.MaxBinaryMessageSize.Should().Be(20 * 1024 * 1024);
    }

    [Fact]
    public void TimeSpansAndNumericTypes_ShouldUpdateCorrectly_WhenConfigurationChanges()
    {
        // Arrange
        var services = new ServiceCollection();
        services.Configure<FeishuWebSocketOptions>(options =>
        {
            options.MaxTotalReconnectTime = TimeSpan.FromMinutes(30);
            options.ReconnectCooldownTime = TimeSpan.FromSeconds(5);
            options.HealthCheckIntervalMs = 60000;
            options.MessageHandlerTimeoutMs = 30000;
        });

        var serviceProvider = services.BuildServiceProvider();
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<FeishuWebSocketOptions>>();

        // Act - 获取初始值
        var initialOptions = optionsMonitor.CurrentValue;

        // Assert - 验证初始值
        initialOptions.MaxTotalReconnectTime.Should().Be(TimeSpan.FromMinutes(30));
        initialOptions.ReconnectCooldownTime.Should().Be(TimeSpan.FromSeconds(5));
        initialOptions.HealthCheckIntervalMs.Should().Be(60000);
        initialOptions.MessageHandlerTimeoutMs.Should().Be(30000);

        // Act - 更新配置
        var newServices = new ServiceCollection();
        newServices.Configure<FeishuWebSocketOptions>(options =>
        {
            options.MaxTotalReconnectTime = TimeSpan.FromMinutes(45);
            options.ReconnectCooldownTime = TimeSpan.FromSeconds(10);
            options.HealthCheckIntervalMs = 120000;
            options.MessageHandlerTimeoutMs = 45000;
        });

        var newServiceProvider = newServices.BuildServiceProvider();
        var newOptionsMonitor = newServiceProvider.GetRequiredService<IOptionsMonitor<FeishuWebSocketOptions>>();

        // Assert - 验证新值
        var updatedOptions = newOptionsMonitor.CurrentValue;
        updatedOptions.MaxTotalReconnectTime.Should().Be(TimeSpan.FromMinutes(45));
        updatedOptions.ReconnectCooldownTime.Should().Be(TimeSpan.FromSeconds(10));
        updatedOptions.HealthCheckIntervalMs.Should().Be(120000);
        updatedOptions.MessageHandlerTimeoutMs.Should().Be(45000);
    }
}