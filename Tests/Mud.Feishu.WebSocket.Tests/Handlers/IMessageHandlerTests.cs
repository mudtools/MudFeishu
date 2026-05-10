// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证 位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Mud.Feishu.WebSocket.Handlers;

namespace Mud.Feishu.WebSocket.Tests.Handlers;

/// <summary>
/// JsonMessageHandler 消息处理器基类测试
/// </summary>
public class JsonMessageHandlerTests
{
    private readonly Mock<ILogger<TestJsonMessageHandler>> _loggerMock;
    private readonly TestJsonMessageHandler _handler;

    public JsonMessageHandlerTests()
    {
        _loggerMock = new Mock<ILogger<TestJsonMessageHandler>>();
        _handler = new TestJsonMessageHandler(_loggerMock.Object);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TestJsonMessageHandler(null!));
    }

    [Fact]
    public void Constructor_ShouldSetLogger_WhenLoggerIsValid()
    {
        // Arrange & Act
        var handler = new TestJsonMessageHandler(_loggerMock.Object);

        // Assert - Should not throw
    }

    [Fact]
    public void SafeDeserialize_ShouldReturnNull_WhenJsonIsInvalid()
    {
        // Arrange
        var invalidJson = "{ invalid json }";

        // Act
        var result = _handler.TestSafeDeserialize<TestData>(invalidJson);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void SafeDeserialize_ShouldReturnNull_WhenJsonIsEmpty()
    {
        // Arrange
        var emptyJson = "";

        // Act
        var result = _handler.TestSafeDeserialize<TestData>(emptyJson);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void SafeDeserialize_ShouldThrowArgumentNullException_WhenJsonIsNull()
    {
        // Arrange
        string? nullJson = null;

        // Act & Assert
        // SafeDeserialize catches JsonException but not ArgumentNullException
        // When json is null, JsonSerializer.Deserialize throws ArgumentNullException
        Assert.Throws<ArgumentNullException>(() => _handler.TestSafeDeserialize<TestData>(nullJson));
    }

    [Fact]
    public void SafeDeserialize_ShouldDeserializeValidJson()
    {
        // Arrange
        var testData = new TestData
        {
            Id = "test-id",
            Name = "test-name",
            Value = 123
        };
        var json = JsonSerializer.Serialize(testData, JsonOptions.Default);

        // Act
        var result = _handler.TestSafeDeserialize<TestData>(json);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(testData.Id);
        result.Name.Should().Be(testData.Name);
        result.Value.Should().Be(testData.Value);
    }

    [Fact]
    public void SafeDeserialize_ShouldHandleNullProperties()
    {
        // Arrange
        var json = "{\"id\":null,\"name\":null,\"value\":0}";

        // Act
        var result = _handler.TestSafeDeserialize<TestData>(json);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().BeNull();
        result.Name.Should().BeNull();
        result.Value.Should().Be(0);
    }

    [Fact]
    public void SafeDeserialize_ShouldHandleMissingProperties()
    {
        // Arrange
        var json = "{}";

        // Act
        var result = _handler.TestSafeDeserialize<TestData>(json);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().BeNull();
        result.Name.Should().BeNull();
        result.Value.Should().Be(0);
    }

    [Fact]
    public void SafeDeserialize_ShouldHandleMalformedJson()
    {
        // Arrange
        var malformedJsons = new[]
        {
            "{",
            "}",
            "{{}}",
            "[{",
            "{]",
            "invalid json string",
            "null",
            "undefined"
        };

        foreach (var json in malformedJsons)
        {
            // Act
            var result = _handler.TestSafeDeserialize<TestData>(json);

            // Assert
            result.Should().BeNull($"Should return null for malformed JSON: {json}");
        }
    }

    [Fact]
    public void SafeDeserialize_ShouldHandleLargeJson()
    {
        // Arrange
        var largeData = new TestData
        {
            Id = new string('a', 10000),
            Name = new string('b', 10000),
            Value = int.MaxValue
        };
        var json = JsonSerializer.Serialize(largeData, JsonOptions.Default);

        // Act
        var result = _handler.TestSafeDeserialize<TestData>(json);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(largeData.Id);
        result.Name.Should().Be(largeData.Name);
        result.Value.Should().Be(largeData.Value);
    }

    [Fact]
    public void SafeDeserialize_ShouldHandleSpecialCharacters()
    {
        // Arrange
        var testData = new TestData
        {
            Id = "id-with-特殊字符-!@#$%^&*()",
            Name = "name-with-\n\t\r and \"quotes\"",
            Value = 123
        };
        var json = JsonSerializer.Serialize(testData, JsonOptions.Default);

        // Act
        var result = _handler.TestSafeDeserialize<TestData>(json);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(testData.Id);
        result.Name.Should().Be(testData.Name);
        result.Value.Should().Be(testData.Value);
    }

    [Fact]
    public void SafeDeserialize_ShouldHandleUnicodeCharacters()
    {
        // Arrange
        var testData = new TestData
        {
            Id = "id-with-中文-日本語-한글",
            Name = "name-with-😀🎉🚀",
            Value = 123
        };
        var json = JsonSerializer.Serialize(testData, JsonOptions.Default);

        // Act
        var result = _handler.TestSafeDeserialize<TestData>(json);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(testData.Id);
        result.Name.Should().Be(testData.Name);
        result.Value.Should().Be(testData.Value);
    }

    [Fact]
    public void SafeDeserialize_ShouldHandleNumericValues()
    {
        // Arrange
        var testData = new NumericData
        {
            IntValue = int.MaxValue,
            LongValue = long.MaxValue,
            DoubleValue = 12345.6789,
            DecimalValue = 98765.4321m
        };
        var json = JsonSerializer.Serialize(testData, JsonOptions.Default);

        // Act
        var result = _handler.TestSafeDeserialize<NumericData>(json);

        // Assert
        result.Should().NotBeNull();
        result!.IntValue.Should().Be(testData.IntValue);
        result.LongValue.Should().Be(testData.LongValue);
        result.DoubleValue.Should().BeApproximately(testData.DoubleValue, 0.0001);
        result.DecimalValue.Should().Be(testData.DecimalValue);
    }

    [Fact]
    public void SafeDeserialize_ShouldHandleBooleanValues()
    {
        // Arrange
        var testData = new BooleanData
        {
            BoolValue = true,
            FalseValue = false
        };
        var json = JsonSerializer.Serialize(testData, JsonOptions.Default);

        // Act
        var result = _handler.TestSafeDeserialize<BooleanData>(json);

        // Assert
        result.Should().NotBeNull();
        result!.BoolValue.Should().BeTrue();
        result.FalseValue.Should().BeFalse();
    }

    // Test class to expose protected method
    public class TestJsonMessageHandler : JsonMessageHandler
    {
        public TestJsonMessageHandler(ILogger logger) : base(logger)
        {
        }

        public T? TestSafeDeserialize<T>(string json) where T : class
        {
            return SafeDeserialize<T>(json);
        }

        public override bool CanHandle(string messageType) => true;
        public override Task HandleAsync(string message, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    // Test data models
    public class TestData
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public int Value { get; set; }
    }

    public class NumericData
    {
        public int IntValue { get; set; }
        public long LongValue { get; set; }
        public double DoubleValue { get; set; }
        public decimal DecimalValue { get; set; }
    }

    public class BooleanData
    {
        public bool BoolValue { get; set; }
        public bool FalseValue { get; set; }
    }
}
