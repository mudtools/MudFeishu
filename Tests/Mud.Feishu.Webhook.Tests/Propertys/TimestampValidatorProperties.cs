// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FsCheck;
using FsCheck.Xunit;
using Mud.Feishu.Webhook.Configuration;
using Mud.Feishu.Webhook.Services;
using Mud.Feishu.Webhook.Utilities;
using SystemRandom = System.Random;

namespace Mud.Feishu.Webhook.Tests.Propertys;

/// <summary>
/// 时间戳验证器属性测试
/// 使用 FsCheck 进行基于属性的测试，验证时间戳验证器的正确性属性
/// </summary>
[Trait("Feature", "feishu-validator-refactoring")]
public class TimestampValidatorProperties
{
    private readonly Mock<ILogger<TimestampValidator>> _loggerMock;
    private readonly Mock<IOptionsMonitor<FeishuWebhookOptions>> _optionsMonitorMock;
    private readonly Mock<IEnvironmentService> _environmentServiceMock;

    public TimestampValidatorProperties()
    {
        _loggerMock = new Mock<ILogger<TimestampValidator>>();
        _optionsMonitorMock = new Mock<IOptionsMonitor<FeishuWebhookOptions>>();
        _environmentServiceMock = new Mock<IEnvironmentService>();

        // Setup default options
        var defaultOptions = new FeishuWebhookOptions
        {
            TimestampToleranceSeconds = 300
        };
        _optionsMonitorMock.Setup(x => x.CurrentValue).Returns(defaultOptions);

        // Setup environment service to return non-production for tests
        _environmentServiceMock.Setup(x => x.IsProduction).Returns(false);
        _environmentServiceMock.Setup(x => x.IsDevelopment).Returns(true);
        _environmentServiceMock.Setup(x => x.EnvironmentName).Returns("Development");
    }

    /// <summary>
    /// 属性 4: 时间戳格式识别
    /// **验证需求: 2.2**
    /// 对于任何有效的时间戳（秒级或毫秒级），TimestampValidator 应该能够正确识别格式并进行验证
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Property", "4: 时间戳格式识别")]
    public Property TimestampValidator_ShouldIdentifyTimestampFormats()
    {
        return Prop.ForAll(
            GenerateValidTimestampData(),
            data =>
            {
                // Arrange
                var validator = new TimestampValidator(_loggerMock.Object, _optionsMonitorMock.Object, _environmentServiceMock.Object);

                // Act
                var result = validator.ValidateTimestamp(data.Timestamp, data.ToleranceSeconds);

                // Assert - 对于有效的时间戳，验证器应该能够正确处理
                // 秒级时间戳（10位）和毫秒级时间戳（13位）都应该被正确识别
                if (data.Timestamp == 0)
                {
                    // 零时间戳应该被允许（跳过验证）
                    return result;
                }
                else if (data.IsWithinTolerance)
                {
                    // 在容错范围内的时间戳应该验证通过
                    return result;
                }
                else
                {
                    // 超出容错范围的时间戳应该验证失败
                    return !result;
                }
            });
    }

    /// <summary>
    /// 属性 5: 时间戳容错验证
    /// **验证需求: 2.3**
    /// 对于任何时间戳和容错配置的组合，验证结果应该基于时间差是否在容错范围内
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Property", "5: 时间戳容错验证")]
    public Property TimestampValidator_ShouldValidateWithinTolerance()
    {
        return Prop.ForAll(
            GenerateTimestampToleranceData(),
            data =>
            {
                // Arrange
                var validator = new TimestampValidator(_loggerMock.Object, _optionsMonitorMock.Object, _environmentServiceMock.Object);

                // Act
                var result = validator.ValidateTimestamp(data.Timestamp, data.ToleranceSeconds);

                // Assert
                if (data.Timestamp == 0)
                {
                    // 零时间戳应该总是通过验证
                    return result;
                }

                var now = DateTimeOffset.UtcNow;
                DateTimeOffset requestTime;

                // 根据时间戳格式计算请求时间
                if (data.Timestamp < 10000000000) // 秒级时间戳
                {
                    requestTime = DateTimeOffset.FromUnixTimeSeconds(data.Timestamp);
                }
                else // 毫秒级时间戳
                {
                    requestTime = DateTimeOffset.FromUnixTimeMilliseconds(data.Timestamp);
                }

                var diff = Math.Abs((now - requestTime).TotalSeconds);
                var shouldBeValid = diff <= data.ToleranceSeconds;

                return result == shouldBeValid;
            });
    }

    /// <summary>
    /// 属性 6: 时间戳超限日志记录
    /// **验证需求: 2.5**
    /// 对于任何超出容错范围的时间戳，系统应该拒绝请求并记录警告日志
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Property", "6: 时间戳超限日志记录")]
    public Property TimestampValidator_ShouldLogWarningForOutOfRangeTimestamps()
    {
        return Prop.ForAll(
            GenerateOutOfRangeTimestampData(),
            data =>
            {
                // Arrange
                var loggerMock = new Mock<ILogger<TimestampValidator>>();
                var optionsMonitorMock = new Mock<IOptionsMonitor<FeishuWebhookOptions>>();
                var environmentServiceMock = new Mock<IEnvironmentService>();
                var defaultOptions = new FeishuWebhookOptions { TimestampToleranceSeconds = 300 };
                optionsMonitorMock.Setup(x => x.CurrentValue).Returns(defaultOptions);
                environmentServiceMock.Setup(x => x.IsProduction).Returns(false);
                environmentServiceMock.Setup(x => x.IsDevelopment).Returns(true);

                var validator = new TimestampValidator(loggerMock.Object, optionsMonitorMock.Object, environmentServiceMock.Object);

                // Act
                var result = validator.ValidateTimestamp(data.Timestamp, data.ToleranceSeconds);

                // Assert
                if (data.Timestamp == 0)
                {
                    // 零时间戳不应该记录警告日志
                    return result; // 应该返回 true
                }

                // 超出容错范围的时间戳应该：
                // 1. 验证失败（返回 false）
                // 2. 记录警告日志
                if (!result)
                {
                    // 验证是否记录了警告日志
                    loggerMock.Verify(
                        x => x.Log(
                            LogLevel.Warning,
                            It.IsAny<EventId>(),
                            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("时间戳超出容错范围")),
                            It.IsAny<Exception>(),
                            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                        Times.AtLeastOnce);
                    return true;
                }

                return false; // 超出范围的时间戳不应该验证通过
            });
    }

    /// <summary>
    /// 生成有效的时间戳测试数据
    /// </summary>
    private static Arbitrary<ValidTimestampTestData> GenerateValidTimestampData()
    {
        return Arb.From(
            from timestampType in Gen.Elements("seconds", "milliseconds", "zero")
            from toleranceSeconds in Gen.Choose(60, 600) // 1-10分钟的容错时间
            select GenerateTimestampByType(timestampType, toleranceSeconds, true));
    }

    /// <summary>
    /// 生成时间戳容错测试数据
    /// </summary>
    private static Arbitrary<TimestampToleranceTestData> GenerateTimestampToleranceData()
    {
        return Arb.From(
            from timestampType in Gen.Elements("seconds", "milliseconds", "zero")
            from toleranceSeconds in Gen.Choose(60, 600)
            from withinTolerance in Gen.Elements(true, false)
            select GenerateToleranceTestData(timestampType, toleranceSeconds, withinTolerance));
    }

    /// <summary>
    /// 生成超出范围的时间戳测试数据
    /// </summary>
    private static Arbitrary<OutOfRangeTimestampTestData> GenerateOutOfRangeTimestampData()
    {
        return Arb.From(
            from timestampType in Gen.Elements("seconds", "milliseconds", "zero")
            from toleranceSeconds in Gen.Choose(60, 300)
            select GenerateOutOfRangeTestData(timestampType, toleranceSeconds));
    }

    /// <summary>
    /// 根据类型生成时间戳
    /// </summary>
    private static ValidTimestampTestData GenerateTimestampByType(string type, int tolerance, bool withinTolerance)
    {
        var now = DateTimeOffset.UtcNow;
        long timestamp;
        bool isWithinTolerance;

        switch (type)
        {
            case "zero":
                timestamp = 0;
                isWithinTolerance = true; // 零时间戳总是有效
                break;
            case "seconds":
                if (withinTolerance)
                {
                    // 生成在容错范围内的秒级时间戳
                    var offsetSeconds = SystemRandom.Shared.Next(-tolerance + 10, tolerance - 10);
                    timestamp = now.AddSeconds(offsetSeconds).ToUnixTimeSeconds();
                    isWithinTolerance = true;
                }
                else
                {
                    // 生成超出容错范围的秒级时间戳
                    var offsetSeconds = SystemRandom.Shared.Next(tolerance + 10, tolerance + 3600);
                    timestamp = now.AddSeconds(-offsetSeconds).ToUnixTimeSeconds();
                    isWithinTolerance = false;
                }
                break;
            case "milliseconds":
                if (withinTolerance)
                {
                    // 生成在容错范围内的毫秒级时间戳
                    var offsetSeconds = SystemRandom.Shared.Next(-tolerance + 10, tolerance - 10);
                    timestamp = now.AddSeconds(offsetSeconds).ToUnixTimeMilliseconds();
                    isWithinTolerance = true;
                }
                else
                {
                    // 生成超出容错范围的毫秒级时间戳
                    var offsetSeconds = SystemRandom.Shared.Next(tolerance + 10, tolerance + 3600);
                    timestamp = now.AddSeconds(-offsetSeconds).ToUnixTimeMilliseconds();
                    isWithinTolerance = false;
                }
                break;
            default:
                timestamp = now.ToUnixTimeSeconds();
                isWithinTolerance = true;
                break;
        }

        return new ValidTimestampTestData
        {
            Timestamp = timestamp,
            ToleranceSeconds = tolerance,
            IsWithinTolerance = isWithinTolerance
        };
    }

    /// <summary>
    /// 生成容错测试数据
    /// </summary>
    private static TimestampToleranceTestData GenerateToleranceTestData(string type, int tolerance, bool withinTolerance)
    {
        var now = DateTimeOffset.UtcNow;
        long timestamp;

        switch (type)
        {
            case "zero":
                timestamp = 0;
                break;
            case "seconds":
                if (withinTolerance)
                {
                    var offsetSeconds = SystemRandom.Shared.Next(-tolerance + 10, tolerance - 10);
                    timestamp = now.AddSeconds(offsetSeconds).ToUnixTimeSeconds();
                }
                else
                {
                    var offsetSeconds = SystemRandom.Shared.Next(tolerance + 10, tolerance + 3600);
                    timestamp = now.AddSeconds(-offsetSeconds).ToUnixTimeSeconds();
                }
                break;
            case "milliseconds":
                if (withinTolerance)
                {
                    var offsetSeconds = SystemRandom.Shared.Next(-tolerance + 10, tolerance - 10);
                    timestamp = now.AddSeconds(offsetSeconds).ToUnixTimeMilliseconds();
                }
                else
                {
                    var offsetSeconds = SystemRandom.Shared.Next(tolerance + 10, tolerance + 3600);
                    timestamp = now.AddSeconds(-offsetSeconds).ToUnixTimeMilliseconds();
                }
                break;
            default:
                timestamp = now.ToUnixTimeSeconds();
                break;
        }

        return new TimestampToleranceTestData
        {
            Timestamp = timestamp,
            ToleranceSeconds = tolerance
        };
    }

    /// <summary>
    /// 生成超出范围的测试数据
    /// </summary>
    private static OutOfRangeTimestampTestData GenerateOutOfRangeTestData(string type, int tolerance)
    {
        var now = DateTimeOffset.UtcNow;
        long timestamp;

        switch (type)
        {
            case "zero":
                timestamp = 0;
                break;
            case "seconds":
                // 生成明显超出容错范围的秒级时间戳
                var offsetSeconds = SystemRandom.Shared.Next(tolerance + 60, tolerance + 7200); // 超出1小时到2小时
                timestamp = now.AddSeconds(-offsetSeconds).ToUnixTimeSeconds();
                break;
            case "milliseconds":
                // 生成明显超出容错范围的毫秒级时间戳
                var offsetSecondsMs = SystemRandom.Shared.Next(tolerance + 60, tolerance + 7200);
                timestamp = now.AddSeconds(-offsetSecondsMs).ToUnixTimeMilliseconds();
                break;
            default:
                timestamp = now.AddSeconds(-(tolerance + 3600)).ToUnixTimeSeconds();
                break;
        }

        return new OutOfRangeTimestampTestData
        {
            Timestamp = timestamp,
            ToleranceSeconds = tolerance
        };
    }

    /// <summary>
    /// 有效时间戳测试数据
    /// </summary>
    public class ValidTimestampTestData
    {
        public long Timestamp { get; set; }
        public int ToleranceSeconds { get; set; }
        public bool IsWithinTolerance { get; set; }
    }

    /// <summary>
    /// 时间戳容错测试数据
    /// </summary>
    public class TimestampToleranceTestData
    {
        public long Timestamp { get; set; }
        public int ToleranceSeconds { get; set; }
    }

    /// <summary>
    /// 超出范围时间戳测试数据
    /// </summary>
    public class OutOfRangeTimestampTestData
    {
        public long Timestamp { get; set; }
        public int ToleranceSeconds { get; set; }
    }
}