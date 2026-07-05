// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Mud.Feishu.Abstractions;
using Mud.Feishu.Redis.Configuration;
using Mud.Feishu.Redis.Extensions;

namespace Mud.Feishu.Redis.Tests.Extensions;

/// <summary>
/// Redis 服务注册扩展方法测试 - 验证 ServiceRegistration-Fix-Refactor-Plan 中的 SR-P2-2 修复点
/// </summary>
/// <remarks>
/// SR-P2-2：AddFeishuRedisDeduplicators 必须在 AddFeishuApp 之前调用。
/// 此前若颠倒顺序，Redis TokenStore 因 TryAddSingleton 语义（已存在则跳过）而无法覆盖默认 Memory 实现，
/// 且不会有任何错误抛出（静默失败）。
/// </remarks>
public class RedisFeishuServiceBuilderExtensionsTests
{
    /// <summary>
    /// 构造测试用飞书应用配置列表
    /// </summary>
    private static List<FeishuAppConfig> CreateFeishuAppConfigs() => new()
    {
        new FeishuAppConfig
        {
            AppKey = "default",
            AppId = "cli_default_id_1234567890",
            AppSecret = "default_secret_123456",
            IsDefault = true
        }
    };

    /// <summary>
    /// SR-P2-2 验证：当 AddFeishuApp 已调用时，AddFeishuRedisDeduplicators(Action&lt;RedisOptions&gt;) 重载应抛出 InvalidOperationException。
    /// 业务场景：防止用户因调用顺序颠倒导致 Redis TokenStore 静默失败。
    /// 使用 Action 重载避免依赖 Microsoft.Extensions.Configuration.Memory 包。
    /// </summary>
    [Fact]
    public void AddFeishuRedisDeduplicators_ShouldThrow_WhenAddFeishuAppAlreadyCalled_WithActionConfigure()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act - 先调用 AddFeishuApp（错误顺序）
        services.AddFeishuApp(CreateFeishuAppConfigs());

        // Assert - 再调用 AddFeishuRedisDeduplicators 应抛异常
        Action<RedisOptions> configureOptions = _ => { };
        var act = () => services.AddFeishuRedisDeduplicators(configureOptions);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*AddFeishuRedisDeduplicators 必须在 AddFeishuApp 之前调用*",
                "应在 AddFeishuApp 已调用时抛出 InvalidOperationException 提示调用顺序错误");
    }

    /// <summary>
    /// SR-P2-2 验证：当 AddFeishuApp 未调用时，AddFeishuRedisDeduplicators 不应抛出调用顺序异常。
    /// 业务场景：正确的调用顺序 - 先 AddFeishuRedisDeduplicators，再 AddFeishuApp。
    /// 注意：实际初始化 Redis 连接会尝试连接 localhost:6379，若不可达可能抛出其他异常，但不应是调用顺序相关的 InvalidOperationException。
    /// </summary>
    [Fact]
    public void AddFeishuRedisDeduplicators_ShouldNotThrowCallOrderException_WhenAddFeishuAppNotCalled()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act - 仅调用 AddFeishuRedisDeduplicators（未调用 AddFeishuApp）
        // 注意：可能会因 Redis 连接失败抛出其他异常，但不应抛出调用顺序相关的 InvalidOperationException
        try
        {
            Action<RedisOptions> configureOptions = _ => { };
            services.AddFeishuRedisDeduplicators(configureOptions);
        }
        catch (InvalidOperationException ex)
        {
            // 若抛出 InvalidOperationException，必须不是调用顺序错误
            ex.Message.Should().NotContain("AddFeishuRedisDeduplicators 必须在 AddFeishuApp 之前调用",
                "未调用 AddFeishuApp 时不应触发调用顺序检测");
        }
        catch (Exception)
        {
            // 接受 Redis 连接失败等其他异常
        }
    }

    /// <summary>
    /// SR-P2-2 验证：当 configureOptions 为 null 时，应抛出 ArgumentNullException 而非调用顺序异常。
    /// </summary>
    [Fact]
    public void AddFeishuRedisDeduplicators_ShouldThrowArgumentNullException_WhenConfigureOptionsIsNull()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        var act = () => services.AddFeishuRedisDeduplicators((Action<RedisOptions>)null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("configureOptions");
    }
}

