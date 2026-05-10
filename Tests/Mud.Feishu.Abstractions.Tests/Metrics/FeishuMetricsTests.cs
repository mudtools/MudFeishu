// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using Mud.Feishu.Abstractions.Metrics;

namespace Mud.Feishu.Abstractions.Tests.Metrics;

/// <summary>
/// FeishuMetrics 指标系统测试类
/// </summary>
public class FeishuMetricsTests
{
    private readonly MeterListener _meterListener;
    private readonly ConcurrentDictionary<string, long> _counterValues;
    private readonly ConcurrentDictionary<string, List<double>> _histogramValues;
    private readonly object _histogramLock = new();

    public FeishuMetricsTests()
    {
        _counterValues = new ConcurrentDictionary<string, long>();
        _histogramValues = new ConcurrentDictionary<string, List<double>>();

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
                else if (instrument is ObservableGauge<int> gauge)
                {
                    listener.EnableMeasurementEvents(gauge);
                }
            }
        };

        _meterListener.SetMeasurementEventCallback<long>((instrument, value, tags, state) =>
        {
            var key = instrument.Name;
            _counterValues.AddOrUpdate(key, value, (_, existing) => existing + value);
        });

        _meterListener.SetMeasurementEventCallback<double>((instrument, value, tags, state) =>
        {
            var key = instrument.Name;
            var list = _histogramValues.GetOrAdd(key, _ => new List<double>());
            lock (_histogramLock)
            {
                list.Add(value);
            }
        });

        _meterListener.Start();
    }

    [Fact]
    public void RecordTokenFetch_ShouldIncrementCounters()
    {
        // Arrange
        var tokenType = "app";

        // Act
        using (FeishuMetricsHelper.RecordTokenFetch(tokenType, false))
        {
            // 模拟令牌获取过程
        }

        // Assert
        _counterValues.Should().ContainKey("feishu_token_fetch_total");
        _counterValues["feishu_token_fetch_total"].Should().Be(1);
        _counterValues.Should().ContainKey("feishu_token_cache_miss_total");
        _counterValues["feishu_token_cache_miss_total"].Should().Be(1);
    }

    [Fact]
    public void RecordTokenFetch_FromCache_ShouldIncrementCacheHitCounter()
    {
        // Arrange
        var tokenType = "user";

        // Act
        using (FeishuMetricsHelper.RecordTokenFetch(tokenType, true))
        {
            // 模拟令牌获取过程
        }

        // Assert
        _counterValues.Should().ContainKey("feishu_token_fetch_total");
        _counterValues["feishu_token_fetch_total"].Should().Be(1);
        _counterValues.Should().ContainKey("feishu_token_cache_hit_total");
        _counterValues["feishu_token_cache_hit_total"].Should().Be(1);
    }

    [Fact]
    public void RecordTokenRefresh_ShouldIncrementCounter()
    {
        // Arrange
        var tokenType = "tenant";

        // Act
        FeishuMetricsHelper.RecordTokenRefresh(tokenType);

        // Assert
        _counterValues.Should().ContainKey("feishu_token_refresh_total");
        _counterValues["feishu_token_refresh_total"].Should().Be(1);
    }

    [Fact]
    public void RecordEventHandling_ShouldIncrementCounter_AndRecordDuration()
    {
        // Arrange
        var eventType = "im.message.receive_v1";
        var handlerType = "webhook";

        // Act
        using (FeishuMetricsHelper.RecordEventHandling(eventType, handlerType))
        {
            // 模拟事件处理过程
            Thread.Sleep(50);
        }

        // Assert
        _counterValues.Should().ContainKey("feishu_event_handling_total");
        _counterValues["feishu_event_handling_total"].Should().Be(1);
        _histogramValues.Should().ContainKey("feishu_event_handling_duration_ms");
        _histogramValues["feishu_event_handling_duration_ms"].Should().HaveCount(1);
        _histogramValues["feishu_event_handling_duration_ms"][0].Should().BeGreaterThan(0);
    }

    [Fact]
    public void RecordEventHandlingSuccess_ShouldIncrementCounter()
    {
        // Arrange
        var eventType = "im.message.receive_v1";

        // Act
        FeishuMetricsHelper.RecordEventHandlingSuccess(eventType);

        // Assert
        _counterValues.Should().ContainKey("feishu_event_handling_success_total");
        _counterValues["feishu_event_handling_success_total"].Should().Be(1);
    }

    [Fact]
    public void RecordEventHandlingFailure_ShouldIncrementCounter()
    {
        // Arrange
        var eventType = "im.message.receive_v1";
        var errorType = "timeout";

        // Act
        FeishuMetricsHelper.RecordEventHandlingFailure(eventType, errorType);

        // Assert
        _counterValues.Should().ContainKey("feishu_event_handling_failure_total");
        _counterValues["feishu_event_handling_failure_total"].Should().Be(1);
    }

    [Fact]
    public void RecordEventDeduplicationHit_ShouldIncrementCounter()
    {
        // Arrange
        var dedupType = "event_id";

        // Act
        FeishuMetricsHelper.RecordEventDeduplicationHit(dedupType);

        // Assert
        _counterValues.Should().ContainKey("feishu_event_deduplication_hit_total");
        _counterValues["feishu_event_deduplication_hit_total"].Should().Be(1);
    }

    [Fact]
    public void RecordHttpRequest_ShouldIncrementCounter_AndRecordDuration()
    {
        // Arrange
        var method = "GET";
        var url = "https://open.feishu.cn/open-apis/auth/v3/tenant_access_token/internal";

        // Act
        using (FeishuMetricsHelper.RecordHttpRequest(method, url))
        {
            // 模拟 HTTP 请求过程
            Thread.Sleep(50);
        }

        // Assert
        _counterValues.Should().ContainKey("feishu_http_request_total");
        _counterValues["feishu_http_request_total"].Should().Be(1);
        _histogramValues.Should().ContainKey("feishu_http_request_duration_ms");
        _histogramValues["feishu_http_request_duration_ms"].Should().HaveCount(1);
        _histogramValues["feishu_http_request_duration_ms"][0].Should().BeGreaterThan(0);
    }

    [Fact]
    public void RecordHttpRequestSuccess_ShouldIncrementCounter()
    {
        // Arrange
        var method = "POST";
        var statusCode = 200;

        // Act
        FeishuMetricsHelper.RecordHttpRequestSuccess(method, statusCode);

        // Assert
        _counterValues.Should().ContainKey("feishu_http_request_success_total");
        _counterValues["feishu_http_request_success_total"].Should().Be(1);
    }

    [Fact]
    public void RecordHttpRequestFailure_ShouldIncrementCounter()
    {
        // Arrange
        var method = "GET";
        var statusCode = 401;
        var errorType = "unauthorized";

        // Act
        FeishuMetricsHelper.RecordHttpRequestFailure(method, statusCode, errorType);

        // Assert
        _counterValues.Should().ContainKey("feishu_http_request_failure_total");
        _counterValues["feishu_http_request_failure_total"].Should().Be(1);
    }

    [Fact]
    public void WebSocketConnectionCount_ShouldReturnProviderValue()
    {
        // Arrange
        var expectedCount = 5;

        // Act
        FeishuMetrics.WebSocketConnectionCountProvider = () => expectedCount;

        // Assert
        // 验证提供器设置成功
        FeishuMetrics.WebSocketConnectionCountProvider.Should().NotBeNull();
        FeishuMetrics.WebSocketConnectionCountProvider().Should().Be(expectedCount);
    }

    [Fact]
    public void RecordWebSocketMessageSent_ShouldIncrementCounters()
    {
        // Arrange
        var bytes = 100;

        // Act
        FeishuMetricsHelper.RecordWebSocketMessageSent(bytes);

        // Assert
        _counterValues.Should().ContainKey("feishu_websocket_messages_sent_total");
        _counterValues["feishu_websocket_messages_sent_total"].Should().Be(1);
        _counterValues.Should().ContainKey("feishu_websocket_bytes_sent_total");
        _counterValues["feishu_websocket_bytes_sent_total"].Should().Be(bytes);
    }

    [Fact]
    public void RecordWebSocketMessageReceived_ShouldIncrementCounters()
    {
        // Arrange
        var bytes = 200;

        // Act
        FeishuMetricsHelper.RecordWebSocketMessageReceived(bytes);

        // Assert
        _counterValues.Should().ContainKey("feishu_websocket_messages_received_total");
        _counterValues["feishu_websocket_messages_received_total"].Should().Be(1);
        _counterValues.Should().ContainKey("feishu_websocket_bytes_received_total");
        _counterValues["feishu_websocket_bytes_received_total"].Should().Be(bytes);
    }

    [Fact]
    public void RecordWebSocketConnectionError_ShouldIncrementCounter()
    {
        // Act
        FeishuMetricsHelper.RecordWebSocketConnectionError();

        // Assert
        _counterValues.Should().ContainKey("feishu_websocket_connection_errors_total");
        _counterValues["feishu_websocket_connection_errors_total"].Should().Be(1);
    }

    [Fact]
    public void RecordWebSocketAuthenticationError_ShouldIncrementCounter()
    {
        // Act
        FeishuMetricsHelper.RecordWebSocketAuthenticationError();

        // Assert
        _counterValues.Should().ContainKey("feishu_websocket_authentication_errors_total");
        _counterValues["feishu_websocket_authentication_errors_total"].Should().Be(1);
    }

    [Fact]
    public void RecordWebSocketMessageProcessing_ShouldRecordDuration()
    {
        // Act
        using (FeishuMetricsHelper.RecordWebSocketMessageProcessing())
        {
            // 模拟消息处理过程
            Thread.Sleep(50);
        }

        // Assert
        _histogramValues.Should().ContainKey("feishu_websocket_message_processing_duration_ms");
        _histogramValues["feishu_websocket_message_processing_duration_ms"].Should().HaveCount(1);
        _histogramValues["feishu_websocket_message_processing_duration_ms"][0].Should().BeGreaterThan(0);
    }

    [Fact]
    public void MultipleCallsToMetricsMethods_ShouldIncrementCountersCorrectly()
    {
        // Arrange
        var iterations = 3;

        // Act
        for (int i = 0; i < iterations; i++)
        {
            FeishuMetricsHelper.RecordWebSocketMessageSent(100);
            FeishuMetricsHelper.RecordWebSocketMessageReceived(200);
        }

        // Assert
        _counterValues["feishu_websocket_messages_sent_total"].Should().Be(iterations);
        _counterValues["feishu_websocket_messages_received_total"].Should().Be(iterations);
        _counterValues["feishu_websocket_bytes_sent_total"].Should().Be(iterations * 100);
        _counterValues["feishu_websocket_bytes_received_total"].Should().Be(iterations * 200);
    }
}
