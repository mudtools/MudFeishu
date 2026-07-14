// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Mud.Feishu.Abstractions.Tests.Extensions;

/// <summary>
/// FeishuServiceBuilder 单元测试
/// </summary>
/// <remarks>
/// 覆盖以下修复点：
/// - NEW-SR-07：AddModule 自定义模块顺序修复（不在 _registrars 中的模块不应被静默标记为 added）
/// - NEW-SR-08：Build 验证 IFeishuAppManager 注册（未注册时抛 InvalidOperationException）
/// </remarks>
public class FeishuServiceBuilderTests
{
    // ============================================================
    // NEW-SR-07：AddModule 自定义模块顺序修复
    // ============================================================

    /// <summary>
    /// NEW-SR-07 验证：调用 AddModule 传入不在 _registrars 中的自定义模块时，
    /// 不应将其标记为已添加（TryAdd 不应被调用）。
    /// 修复前：AddModule 先调用 _configuration.TryAdd(module) 标记为已添加，
    /// 再查 _registrars，导致自定义模块被静默标记 added 但不执行注册；
    /// 后续 RegisterModule 因 TryAdd 返回 false 再次跳过。
    /// 修复后：仅在 registrar 存在时才标记 added 并执行注册。
    /// </summary>
    [Fact]
    public void AddModule_WithCustomModuleNotInRegistrars_ShouldNotMarkAsAdded()
    {
        // Arrange：自定义模块值（不在 _registrars 中）
        var customModule = (FeishuModule)999;
        var services = new ServiceCollection();
        services.AddLogging();
        // 通过公共扩展方法创建 FeishuServiceBuilder（构造函数为 internal）
        var builder = services.CreateFeishuServicesBuilder();

        // Act：调用 AddModules 传入不存在的自定义模块（间接调用 private AddModule）
        builder.AddModules(customModule);

        // Assert：可以通过 RegisterModule 注册同一自定义模块（说明未被静默标记为 added）
        var customRegistrarMock = new Mock<IFeishuModuleRegistrar>();
        customRegistrarMock.SetupGet(r => r.Module).Returns(customModule);
        customRegistrarMock.Setup(r => r.Register(It.IsAny<IServiceCollection>())).Verifiable();

        builder.RegisterModule(customRegistrarMock.Object);

        // 验证 customRegistrar.Register 被调用（说明 AddModule 未静默标记 added）
        customRegistrarMock.Verify(r => r.Register(It.IsAny<IServiceCollection>()), Times.Once,
            "NEW-SR-07 修复：AddModule 对不在 _registrars 中的自定义模块不应标记为 added，应允许 RegisterModule 后续注册");
    }

    // ============================================================
    // NEW-SR-08：Build 验证 IFeishuAppManager 注册
    // ============================================================

    /// <summary>
    /// NEW-SR-08 验证：Build 时若未注册 IFeishuAppManager，应抛出 InvalidOperationException。
    /// 业务场景：用户可能写出 services.AddFeishu(builder => builder.AddMessageApi()) 而未调用 AddFeishuApp，
    /// 修复前 Build() 通过但运行时解析 IFeishuV1Message 会因 IFeishuAppManager 未注册而抛异常；
    /// 修复后 Build() 提前校验并给出明确错误消息（含示例代码）。
    /// </summary>
    [Fact]
    public void Build_WhenFeishuAppManagerNotRegistered_ShouldThrowInvalidOperationException()
    {
        // Arrange：仅添加模块，未调用 AddFeishuApp 注册 IFeishuAppManager
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoryCache();
        // 通过公共扩展方法创建 builder（构造函数为 internal）
        var builder = services.CreateFeishuServicesBuilder();

        // Act：仅添加一个模块（不通过 AddFeishuApp 链式调用）
        builder.AddMessageApi();

        // Assert：Build 应抛出 InvalidOperationException，消息包含示例代码
        var act = () => builder.Build();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*未注册 IFeishuAppManager*")
            .WithMessage("*AddFeishuApp*");
    }

    /// <summary>
    /// NEW-SR-08 验证：Build 时若已注册 IFeishuAppManager，不应抛出异常。
    /// </summary>
    [Fact]
    public void Build_WhenFeishuAppManagerRegistered_ShouldNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoryCache();
        services.AddSingleton<IFeishuAppManager>(new Mock<IFeishuAppManager>().Object);
        // 通过公共扩展方法创建 builder（构造函数为 internal）
        var builder = services.CreateFeishuServicesBuilder();

        // Act
        builder.AddMessageApi();
        var result = builder.Build();

        // Assert
        result.Should().NotBeNull();
    }
}
