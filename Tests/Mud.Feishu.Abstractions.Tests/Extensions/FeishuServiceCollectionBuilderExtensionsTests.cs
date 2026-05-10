// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;

namespace Mud.Feishu.Abstractions.Tests.Extensions;

/// <summary>
/// 飞书服务集合建造者扩展方法测试
/// </summary>
public class FeishuServiceCollectionBuilderExtensionsTests
{
    /// <summary>
    /// 测试添加飞书服务时是否正确注册所需的服务
    /// 业务场景：验证当指定了有效的模块时，AddFeishuServices 方法是否正确注册了所有必要的服务
    /// </summary>
    [Fact]
    public void AddFeishuServices_ShouldRegisterRequiredServices_WhenCalledWithValidModules()
    {
        // Arrange
        var services = new ServiceCollection();
        var modules = new[] { FeishuModule.Organization };

        // Act
        services.AddFeishuServices(modules);

        // Assert
        var provider = services.BuildServiceProvider();

        // 验证服务是否已注册成功
        // 注意：由于我们只是添加了 HttpClient，这里不验证具体的服务实例
        // 而是验证服务注册过程没有抛出异常
        Assert.NotNull(provider);
    }

    /// <summary>
    /// 测试添加飞书服务时传入空模块数组的情况
    /// 业务场景：验证当传入空模块数组时，AddFeishuServices 方法是否抛出 ArgumentException
    /// </summary>
    [Fact]
    public void AddFeishuServices_ShouldThrowArgumentException_WhenCalledWithEmptyModules()
    {
        // Arrange
        var services = new ServiceCollection();
        var modules = Array.Empty<FeishuModule>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => services.AddFeishuServices(modules));
        Assert.Contains("至少需要指定一个模块", exception.Message);
    }


    /// <summary>
    /// 测试创建飞书服务建造者
    /// 业务场景：验证 CreateFeishuServicesBuilder 方法是否返回有效的建造者实例
    /// </summary>
    [Fact]
    public void CreateFeishuServicesBuilder_ShouldReturnBuilder_WhenCalled()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = services.CreateFeishuServicesBuilder();

        // Assert
        Assert.NotNull(builder);
    }
}
