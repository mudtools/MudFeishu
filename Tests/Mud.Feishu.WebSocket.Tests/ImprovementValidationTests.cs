// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Mud.Feishu.WebSocket;
using System.Net.Sockets;
using System.Net.WebSockets;
using Xunit;

namespace Mud.Feishu.WebSocket.Tests;

/// <summary>
/// 改进验证测试 - 验证高优先级改进项目的实施效果
/// </summary>
public class ImprovementValidationTests
{
    private readonly ILogger<ErrorRecoveryStrategy> _logger;

    public ImprovementValidationTests()
    {
        _logger = NullLogger<ErrorRecoveryStrategy>.Instance;
    }

    /// <summary>
    /// 测试错误恢复策略 - WebSocket异常分析
    /// </summary>
    [Fact]
    public void ErrorRecoveryStrategy_WebSocketException_ShouldAnalyzeCorrectly()
    {
        // Arrange
        var strategy = new ErrorRecoveryStrategy(_logger);
        var exception = new WebSocketException(WebSocketError.ConnectionClosedPrematurely, "Connection closed");

        // Act
        var result = strategy.AnalyzeError(exception, "Test context");

        // Assert
        Assert.Equal("WebSocketException", result.ErrorType);
        Assert.True(result.IsRecoverable);
        Assert.Equal("立即重连", result.RecoveryRecommendation);
        Assert.Equal(TimeSpan.FromSeconds(1), result.SuggestedDelay);
    }

    /// <summary>
    /// 测试错误恢复策略 - Socket异常分析
    /// </summary>
    [Fact]
    public void ErrorRecoveryStrategy_SocketException_ShouldAnalyzeCorrectly()
    {
        // Arrange
        var strategy = new ErrorRecoveryStrategy(_logger);
        var exception = new SocketException((int)SocketError.ConnectionRefused);

        // Act
        var result = strategy.AnalyzeError(exception, "Connection test");

        // Assert
        Assert.Equal("SocketException", result.ErrorType);
        Assert.True(result.IsRecoverable);
        Assert.Equal("网络连接问题，重试连接", result.RecoveryRecommendation);
        Assert.Equal(TimeSpan.FromSeconds(5), result.SuggestedDelay);
    }

    /// <summary>
    /// 测试心跳间隔配置 - 最小值验证
    /// </summary>
    [Fact]
    public void FeishuWebSocketOptions_HeartbeatInterval_ShouldEnforceMinimum()
    {
        // Arrange
        var options = new FeishuWebSocketOptions();

        // Act
        options.HeartbeatIntervalMs = 1000; // 尝试设置小于最小值

        // Assert
        Assert.Equal(5000, options.HeartbeatIntervalMs); // 应该被强制设为最小值5000
    }

    /// <summary>
    /// 测试心跳间隔配置 - 正常值设置
    /// </summary>
    [Fact]
    public void FeishuWebSocketOptions_HeartbeatInterval_ShouldAcceptValidValue()
    {
        // Arrange
        var options = new FeishuWebSocketOptions();

        // Act
        options.HeartbeatIntervalMs = 30000;

        // Assert
        Assert.Equal(30000, options.HeartbeatIntervalMs);
    }

    /// <summary>
    /// 测试配置验证 - 心跳间隔验证
    /// </summary>
    [Fact]
    public void FeishuWebSocketOptions_Validate_ShouldThrowForInvalidHeartbeat()
    {
        // Arrange
        var options = new FeishuWebSocketOptions();
        // 通过反射设置无效值，绕过属性的最小值限制
        var field = typeof(FeishuWebSocketOptions).GetField("_heartbeatIntervalMs",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(options, 1000);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(options.Validate);
        Assert.Contains("HeartbeatIntervalMs必须至少为5000毫秒", exception.Message);
    }
}