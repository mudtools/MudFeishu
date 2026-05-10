// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Webhook.Utilities;
using Xunit;

namespace Mud.Feishu.Webhook.Tests.Utilities;

/// <summary>
/// EnvironmentService 单元测试
/// </summary>
public class EnvironmentServiceTests
{
    #region IsProduction 测试

    [Fact]
    public void IsProduction_WhenEnvironmentIsProduction_ShouldReturnTrue()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
        var service = new EnvironmentService();

        // Act
        var result = service.IsProduction;

        // Assert
        Assert.True(result);
        Assert.Equal("Production", service.EnvironmentName);
    }

    [Fact]
    public void IsProduction_WhenEnvironmentIsDevelopment_ShouldReturnFalse()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        var service = new EnvironmentService();

        // Act
        var result = service.IsProduction;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsProduction_WhenEnvironmentIsStaging_ShouldReturnFalse()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Staging");
        var service = new EnvironmentService();

        // Act
        var result = service.IsProduction;

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IsDevelopment 测试

    [Fact]
    public void IsDevelopment_WhenEnvironmentIsDevelopment_ShouldReturnTrue()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        var service = new EnvironmentService();

        // Act
        var result = service.IsDevelopment;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsDevelopment_WhenEnvironmentIsProduction_ShouldReturnFalse()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
        var service = new EnvironmentService();

        // Act
        var result = service.IsDevelopment;

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IsStaging 测试

    [Fact]
    public void IsStaging_WhenEnvironmentIsStaging_ShouldReturnTrue()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Staging");
        var service = new EnvironmentService();

        // Act
        var result = service.IsStaging;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsStaging_WhenEnvironmentIsProduction_ShouldReturnFalse()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
        var service = new EnvironmentService();

        // Act
        var result = service.IsStaging;

        // Assert
        Assert.False(result);
    }

    #endregion

    #region 默认环境测试

    [Fact]
    public void EnvironmentName_WhenNotSet_ShouldDefaultToProduction()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
        var service = new EnvironmentService();

        // Act
        var result = service.EnvironmentName;

        // Assert
        Assert.Equal("Production", result);
        Assert.True(service.IsProduction);
    }

    #endregion

    #region 大小写不敏感测试

    [Fact]
    public void IsProduction_WhenLowerCase_ShouldReturnTrue()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "production");
        var service = new EnvironmentService();

        // Act
        var result = service.IsProduction;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsProduction_WhenMixedCase_ShouldReturnTrue()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "PRODUCTION");
        var service = new EnvironmentService();

        // Act
        var result = service.IsProduction;

        // Assert
        Assert.True(result);
    }

    #endregion
}
