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
    public void RecordEventHandling_ShouldIncrementCounter_AndRecordDuration()
    {
        var appKey = "test_app";
        var eventType = "im.message.receive_v1";
        var handlerType = "webhook";

        using (FeishuMetricsHelper.RecordEventHandling(appKey, eventType, handlerType))
        {
            Thread.Sleep(50);
        }

        _counterValues.Should().ContainKey("feishu.event.handling");
        _counterValues["feishu.event.handling"].Should().Be(1);
        _histogramValues.Should().ContainKey("feishu.event.handling.duration");
        _histogramValues["feishu.event.handling.duration"].Should().HaveCount(1);
        _histogramValues["feishu.event.handling.duration"][0].Should().BeGreaterThan(0);
    }

    [Fact]
    public void RecordEventOutcome_WhenSuccess_ShouldIncrementCounter()
    {
        var appKey = "test_app";
        var eventType = "im.message.receive_v1";

        FeishuMetricsHelper.RecordEventOutcome(appKey, eventType, success: true);

        _counterValues.Should().ContainKey("feishu.event.handling");
        _counterValues["feishu.event.handling"].Should().Be(1);
    }

    [Fact]
    public void RecordEventOutcome_WhenFailure_ShouldIncrementCounter()
    {
        var appKey = "test_app";
        var eventType = "im.message.receive_v1";
        var errorType = "timeout";

        FeishuMetricsHelper.RecordEventOutcome(appKey, eventType, success: false, errorType);

        _counterValues.Should().ContainKey("feishu.event.handling");
        _counterValues["feishu.event.handling"].Should().Be(1);
    }

    [Fact]
    public void RecordEventDeduplication_ShouldIncrementCounter()
    {
        var appKey = "test_app";
        var dedupType = "event_id";

        FeishuMetricsHelper.RecordEventDeduplication(appKey, dedupType, hit: true);

        _counterValues.Should().ContainKey("feishu.event.deduplication");
        _counterValues["feishu.event.deduplication"].Should().Be(1);
    }

    [Fact]
    public void WebSocketConnectionObserver_ShouldReturnProvidedValues()
    {
        var expectedCount = 5;

        FeishuMetrics.WebSocketConnectionObserver = () =>
        {
            return new[]
            {
                new Measurement<int>(
                    expectedCount,
                    new KeyValuePair<string, object?>(FeishuMetrics.Tags.AppKey, "test_app"))
            };
        };

        FeishuMetrics.WebSocketConnectionObserver.Should().NotBeNull();
        var measurements = FeishuMetrics.WebSocketConnectionObserver!().ToList();
        measurements.Should().HaveCount(1);
        measurements[0].Value.Should().Be(expectedCount);
    }

    [Fact]
    public void RecordWebSocketMessageProcessing_ShouldRecordDuration()
    {
        var appKey = "test_app";

        using (FeishuMetricsHelper.RecordWebSocketMessageProcessing(appKey, "text"))
        {
            Thread.Sleep(50);
        }

        _histogramValues.Should().ContainKey("feishu.websocket.message.duration");
        _histogramValues["feishu.websocket.message.duration"].Should().HaveCount(1);
        _histogramValues["feishu.websocket.message.duration"][0].Should().BeGreaterThan(0);
    }

    [Fact]
    public void RecordWebhookRequest_ShouldIncrementCounter_AndRecordDuration()
    {
        var appKey = "test_app";

        using (FeishuMetricsHelper.RecordWebhookRequest(appKey))
        {
            Thread.Sleep(50);
        }

        _counterValues.Should().ContainKey("feishu.webhook.request");
        _counterValues["feishu.webhook.request"].Should().Be(1);
        _histogramValues.Should().ContainKey("feishu.webhook.request.duration");
        _histogramValues["feishu.webhook.request.duration"].Should().HaveCount(1);
        _histogramValues["feishu.webhook.request.duration"][0].Should().BeGreaterThan(0);
    }

    [Fact]
    public void RecordWebSocketReconnect_ShouldIncrementCounter()
    {
        var appKey = "test_app";

        FeishuMetricsHelper.RecordWebSocketReconnect(appKey, success: true);

        _counterValues.Should().ContainKey("feishu.websocket.reconnect");
        _counterValues["feishu.websocket.reconnect"].Should().Be(1);
    }

    [Fact]
    public void WebSocketBacklogObserver_ShouldReturnProvidedValues()
    {
        var expectedBacklog = 3;

        FeishuMetrics.WebSocketBacklogObserver = () =>
        {
            return new[]
            {
                new Measurement<int>(
                    expectedBacklog,
                    new KeyValuePair<string, object?>(FeishuMetrics.Tags.AppKey, "test_app"))
            };
        };

        FeishuMetrics.WebSocketBacklogObserver.Should().NotBeNull();
        var measurements = FeishuMetrics.WebSocketBacklogObserver!().ToList();
        measurements.Should().HaveCount(1);
        measurements[0].Value.Should().Be(expectedBacklog);
    }
}
