// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mud.Feishu.WebSocket.Tests.Core;

/// <summary>
/// 消息序号验证器测试类
/// 测试消息序号的连续性、重复性检测和去重逻辑
/// </summary>
public class MessageSequenceValidatorTests
{
    private readonly Mock<ILogger<MessageSequenceValidator>> _loggerMock;
    private readonly Mud.Feishu.WebSocket.FeishuWebSocketOptions _options;
    private readonly MessageSequenceValidator _validator;

    public MessageSequenceValidatorTests()
    {
        _loggerMock = new Mock<ILogger<MessageSequenceValidator>>();
        _options = new Mud.Feishu.WebSocket.FeishuWebSocketOptions
        {
            EnableLogging = true
        };
        _validator = new MessageSequenceValidator(_loggerMock.Object, _options);
    }

    /// <summary>
    /// 测试第一条消息的序号验证
    /// 业务场景：当接收到第一条消息时，由于没有历史序号，应返回 Valid
    /// </summary>
    [Fact]
    public void ValidateSequence_ShouldReturnValid_WhenFirstMessage()
    {
        // Arrange
        var sequenceNumber = 12345UL;

        // Act
        var result = _validator.ValidateSequence(sequenceNumber);

        // Assert
        result.Should().Be(SequenceValidationResult.Valid);
    }

    /// <summary>
    /// 测试不同序号场景的验证逻辑
    /// 业务场景：验证连续序号、重复序号、序号回退、小间隔序号和大间隔序号的处理
    /// </summary>
    /// <param name="firstSequence">第一条消息的序号</param>
    /// <param name="secondSequence">第二条消息的序号</param>
    /// <param name="expectedFirstResult">第一条消息的预期验证结果</param>
    /// <param name="expectedSecondResult">第二条消息的预期验证结果</param>
    /// <param name="scenarioDescription">场景描述</param>
    [Theory]
    [InlineData(12345UL, 12346UL, SequenceValidationResult.Valid, SequenceValidationResult.Valid, "连续序号")]
    [InlineData(12345UL, 12345UL, SequenceValidationResult.Valid, SequenceValidationResult.Duplicate, "重复序号")]
    [InlineData(12345UL, 12344UL, SequenceValidationResult.Valid, SequenceValidationResult.Rollback, "序号回退")]
    [InlineData(12345UL, 12347UL, SequenceValidationResult.Valid, SequenceValidationResult.Valid, "小间隔序号")]
    [InlineData(12345UL, 12360UL, SequenceValidationResult.Valid, SequenceValidationResult.MessageLoss, "大间隔序号")]
    public void ValidateSequence_ShouldHandleDifferentSequenceScenarios(
        ulong firstSequence,
        ulong secondSequence,
        SequenceValidationResult expectedFirstResult,
        SequenceValidationResult expectedSecondResult,
        string scenarioDescription)
    {
        // Arrange - 创建新的验证器实例以确保测试隔离
        var validator = new MessageSequenceValidator(_loggerMock.Object, _options);

        // Act
        var firstResult = validator.ValidateSequence(firstSequence);
        var secondResult = validator.ValidateSequence(secondSequence);

        // Assert
        firstResult.Should().Be(expectedFirstResult, $"场景: {scenarioDescription}");
        secondResult.Should().Be(expectedSecondResult, $"场景: {scenarioDescription}");
    }

    /// <summary>
    /// 测试重置验证器状态
    /// 业务场景：当WebSocket连接重新建立时，需要重置验证器状态，以接受新的序号序列
    /// </summary>
    [Fact]
    public void Reset_ShouldClearState()
    {
        // Arrange
        var sequenceNumber = 12345UL;
        _validator.ValidateSequence(sequenceNumber);

        // Act
        _validator.Reset();
        var result = _validator.ValidateSequence(sequenceNumber);

        // Assert
        result.Should().Be(SequenceValidationResult.Valid); // 重置后应重新接受相同的序号
    }

    /// <summary>
    /// 测试连续多条消息的序号验证
    /// 业务场景：验证连续接收多条消息时，序号验证的正确性
    /// </summary>
    [Fact]
    public void ValidateSequence_ShouldHandleMultipleMessages()
    {
        // Arrange
        var sequenceNumbers = new[] { 10000UL, 10001UL, 10002UL, 10003UL, 10004UL };

        // Act & Assert
        foreach (var seq in sequenceNumbers)
        {
            var result = _validator.ValidateSequence(seq);
            result.Should().Be(SequenceValidationResult.Valid);
        }
    }

    /// <summary>
    /// 测试多条消息中的重复序号检测
    /// 业务场景：验证在连续接收的消息中，重复序号能够被正确检测并标记为 Duplicate
    /// </summary>
    [Fact]
    public void ValidateSequence_ShouldDetectDuplicatesInMultipleMessages()
    {
        // Arrange
        var sequenceNumbers = new[] { 10000UL, 10001UL, 10002UL, 10001UL, 10003UL };

        // Act
        var results = sequenceNumbers.Select(seq => _validator.ValidateSequence(seq)).ToList();

        // Assert
        results[0].Should().Be(SequenceValidationResult.Valid);
        results[1].Should().Be(SequenceValidationResult.Valid);
        results[2].Should().Be(SequenceValidationResult.Valid);
        results[3].Should().Be(SequenceValidationResult.Duplicate); // 重复序号
        results[4].Should().Be(SequenceValidationResult.Valid);
    }
}
