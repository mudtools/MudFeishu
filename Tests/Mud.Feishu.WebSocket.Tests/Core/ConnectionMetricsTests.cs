// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Mud.Feishu.Abstractions.Metrics;
using System.Diagnostics.Metrics;

namespace Mud.Feishu.WebSocket.Tests.Core;

/// <summary>
/// ConnectionMetrics 连接指标管理器测试类
/// </summary>
public class ConnectionMetricsTests
{
    private readonly Mock<ILogger<ConnectionMetrics>> _loggerMock;
    private readonly ConnectionMetrics _connectionMetrics;
    private readonly MeterListener _meterListener;
    private readonly Dictionary<string, long> _counterValues;
    private readonly Dictionary<string, List<double>> _histogramValues;

    public ConnectionMetricsTests()
    {
        _loggerMock = new Mock<ILogger<ConnectionMetrics>>();
        _connectionMetrics = new ConnectionMetrics(_loggerMock.Object);
        _counterValues = new Dictionary<string, long>();
        _histogramValues = new Dictionary<string, List<double>>();

        // 设置指标监听器来捕获指标值
        _meterListener = new MeterListener();
        _meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == "Mud.Feishu")
            {
                if (instrument is Counter<long> counter)
                {
                    listener.EnableMeasurementEvents(counter);
                }
                else if (instrument is Histogram<double> histogram)
                {
                    listener.EnableMeasurementEvents(histogram);
                }
            }
        };

        _meterListener.SetMeasurementEventCallback<long>((instrument, value, tags, state) =>
        {
            var key = instrument.Name;
            if (!_counterValues.ContainsKey(key))
            {
                _counterValues[key] = 0;
            }
            _counterValues[key] += value;
        });

        _meterListener.SetMeasurementEventCallback<double>((instrument, value, tags, state) =>
        {
            var key = instrument.Name;
            if (!_histogramValues.ContainsKey(key))
            {
                _histogramValues[key] = new List<double>();
            }
            _histogramValues[key].Add(value);
        });

        _meterListener.Start();
    }

    [Fact]
    public void RecordMessageSent_ShouldIncrementCounters_AndCallFeishuMetricsHelper()
    {
        // Arrange
        var bytes = 100;

        // Act
        _connectionMetrics.RecordMessageSent(bytes);

        // Assert
        var stats = _connectionMetrics.GetCurrentStats();
        stats.MessagesSent.Should().Be(1);
        stats.BytesSent.Should().Be(bytes);

        // 验证 FeishuMetricsHelper 是否被调用（通过指标监听器）
        _counterValues.Should().ContainKey("feishu_websocket_messages_sent_total");
        _counterValues["feishu_websocket_messages_sent_total"].Should().Be(1);
        _counterValues.Should().ContainKey("feishu_websocket_bytes_sent_total");
        _counterValues["feishu_websocket_bytes_sent_total"].Should().Be(bytes);
    }

    [Fact]
    public void RecordMessageReceived_ShouldIncrementCounters_AndCallFeishuMetricsHelper()
    {
        // Arrange
        var bytes = 200;

        // Act
        _connectionMetrics.RecordMessageReceived(bytes);

        // Assert
        var stats = _connectionMetrics.GetCurrentStats();
        stats.MessagesReceived.Should().Be(1);
        stats.BytesReceived.Should().Be(bytes);

        // 验证 FeishuMetricsHelper 是否被调用（通过指标监听器）
        _counterValues.Should().ContainKey("feishu_websocket_messages_received_total");
        _counterValues["feishu_websocket_messages_received_total"].Should().Be(1);
        _counterValues.Should().ContainKey("feishu_websocket_bytes_received_total");
        _counterValues["feishu_websocket_bytes_received_total"].Should().Be(bytes);
    }

    [Fact]
    public void RecordConnectionClosed_ShouldIncrementErrorCounter_AndCallFeishuMetricsHelper()
    {
        // Act
        _connectionMetrics.RecordConnectionClosed();

        // Assert
        var stats = _connectionMetrics.GetCurrentStats();
        stats.ConnectionErrors.Should().Be(1);

        // 验证 FeishuMetricsHelper 是否被调用（通过指标监听器）
        _counterValues.Should().ContainKey("feishu_websocket_connection_errors_total");
        _counterValues["feishu_websocket_connection_errors_total"].Should().Be(1);
    }

    [Fact]
    public void RecordAuthenticationError_ShouldIncrementErrorCounter_AndCallFeishuMetricsHelper()
    {
        // Act
        _connectionMetrics.RecordAuthenticationError();

        // Assert
        var stats = _connectionMetrics.GetCurrentStats();
        stats.AuthenticationErrors.Should().Be(1);

        // 验证 FeishuMetricsHelper 是否被调用（通过指标监听器）
        _counterValues.Should().ContainKey("feishu_websocket_authentication_errors_total");
        _counterValues["feishu_websocket_authentication_errors_total"].Should().Be(1);
    }

    [Fact]
    public void RecordMessageProcessing_ShouldRecordDuration_WhenUsingFeishuMetricsHelper()
    {
        // Act
        using (FeishuMetricsHelper.RecordWebSocketMessageProcessing())
        {
            // 模拟消息处理时间
            Thread.Sleep(50);
        }

        // Assert
        // 验证 FeishuMetricsHelper 是否被调用（通过指标监听器）
        _histogramValues.Should().ContainKey("feishu_websocket_message_processing_duration_ms");
        _histogramValues["feishu_websocket_message_processing_duration_ms"].Should().HaveCount(1);
        _histogramValues["feishu_websocket_message_processing_duration_ms"][0].Should().BeGreaterThan(0);
    }

    [Fact]
    public void Reset_ShouldClearAllCounters()
    {
        // Arrange
        _connectionMetrics.RecordMessageSent(100);
        _connectionMetrics.RecordMessageReceived(200);
        _connectionMetrics.RecordConnectionClosed();
        _connectionMetrics.RecordAuthenticationError();

        // Act
        _connectionMetrics.Reset();

        // Assert
        var stats = _connectionMetrics.GetCurrentStats();
        stats.MessagesSent.Should().Be(0);
        stats.MessagesReceived.Should().Be(0);
        stats.BytesSent.Should().Be(0);
        stats.BytesReceived.Should().Be(0);
        stats.ConnectionErrors.Should().Be(0);
        stats.AuthenticationErrors.Should().Be(0);
        stats.AverageProcessingTimeMs.Should().Be(0);
    }

    [Fact]
    public void GetCurrentStats_ShouldReturnCorrectStatistics()
    {
        // Arrange
        _connectionMetrics.RecordMessageSent(100);
        _connectionMetrics.RecordMessageSent(150);
        _connectionMetrics.RecordMessageReceived(200);
        _connectionMetrics.RecordMessageReceived(250);
        _connectionMetrics.RecordConnectionClosed();
        _connectionMetrics.RecordAuthenticationError();

        // Act
        var stats = _connectionMetrics.GetCurrentStats();

        // Assert
        stats.MessagesSent.Should().Be(2);
        stats.MessagesReceived.Should().Be(2);
        stats.BytesSent.Should().Be(250); // 100 + 150
        stats.BytesReceived.Should().Be(450); // 200 + 250
        stats.ConnectionErrors.Should().Be(1);
        stats.AuthenticationErrors.Should().Be(1);
        stats.Uptime.Should().Be(TimeSpan.Zero); // 因为没有调用 RecordConnectionEstablished
    }

    [Fact]
    public void RecordConnectionEstablished_ShouldSetConnectionStartTime()
    {
        // Act
        _connectionMetrics.RecordConnectionEstablished();
        var stats = _connectionMetrics.GetCurrentStats();

        // Assert
        stats.Uptime.Should().BeGreaterThan(TimeSpan.Zero);
        stats.UptimeSeconds.Should().BeGreaterThan(0);
    }

    [Fact]
    public void MetricsUpdatedEvent_ShouldBeRaised_WhenProcessingMessages()
    {
        // Arrange
        var eventRaised = false;
        ConnectionMetricsEventArgs? eventArgs = null;

        _connectionMetrics.MetricsUpdated += (sender, args) =>
        {
            eventRaised = true;
            eventArgs = args;
        };

        // Act - 处理100条消息以触发事件
        for (int i = 0; i < 100; i++)
        {
            var stopwatch = _connectionMetrics.StartMessageProcessing();
            Thread.Sleep(1);
            _connectionMetrics.EndMessageProcessing(stopwatch);
        }

        // Assert
        eventRaised.Should().BeTrue();
        eventArgs.Should().NotBeNull();
        eventArgs!.Statistics.Should().NotBeNull();
    }

    [Fact]
    public void EndMessageProcessing_ShouldUpdateProcessingTimeStatistics()
    {
        // Arrange
        var stopwatch1 = _connectionMetrics.StartMessageProcessing();
        Thread.Sleep(10);
        _connectionMetrics.EndMessageProcessing(stopwatch1);

        var stopwatch2 = _connectionMetrics.StartMessageProcessing();
        Thread.Sleep(20);
        _connectionMetrics.EndMessageProcessing(stopwatch2);

        // Act
        var stats = _connectionMetrics.GetCurrentStats();

        // Assert
        stats.AverageProcessingTimeMs.Should().BeGreaterThan(0);
    }

    [Fact]
    public void MultipleCallsToRecordMethods_ShouldIncrementCountersCorrectly()
    {
        // Arrange
        var iterations = 5;

        // Act
        for (int i = 0; i < iterations; i++)
        {
            _connectionMetrics.RecordMessageSent(100);
            _connectionMetrics.RecordMessageReceived(200);
        }

        // Assert
        var stats = _connectionMetrics.GetCurrentStats();
        stats.MessagesSent.Should().Be(iterations);
        stats.MessagesReceived.Should().Be(iterations);
        stats.BytesSent.Should().Be(iterations * 100);
        stats.BytesReceived.Should().Be(iterations * 200);

        // 验证 FeishuMetricsHelper 是否被调用（通过指标监听器）
        _counterValues["feishu_websocket_messages_sent_total"].Should().Be(iterations);
        _counterValues["feishu_websocket_messages_received_total"].Should().Be(iterations);
    }
}
