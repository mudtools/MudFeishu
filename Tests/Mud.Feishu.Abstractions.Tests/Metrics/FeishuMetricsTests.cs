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
        var eventType = "im.message.receive_v1";
        var handlerType = "webhook";

        using (FeishuMetricsHelper.RecordEventHandling(eventType, handlerType))
        {
            Thread.Sleep(50);
        }

        _counterValues.Should().ContainKey("feishu_event_handling_total");
        _counterValues["feishu_event_handling_total"].Should().Be(1);
        _histogramValues.Should().ContainKey("feishu_event_handling_duration_ms");
        _histogramValues["feishu_event_handling_duration_ms"].Should().HaveCount(1);
        _histogramValues["feishu_event_handling_duration_ms"][0].Should().BeGreaterThan(0);
    }

    [Fact]
    public void RecordEventHandlingSuccess_ShouldIncrementCounter()
    {
        var eventType = "im.message.receive_v1";

        FeishuMetricsHelper.RecordEventHandlingSuccess(eventType);

        _counterValues.Should().ContainKey("feishu_event_handling_success_total");
        _counterValues["feishu_event_handling_success_total"].Should().Be(1);
    }

    [Fact]
    public void RecordEventHandlingFailure_ShouldIncrementCounter()
    {
        var eventType = "im.message.receive_v1";
        var errorType = "timeout";

        FeishuMetricsHelper.RecordEventHandlingFailure(eventType, errorType);

        _counterValues.Should().ContainKey("feishu_event_handling_failure_total");
        _counterValues["feishu_event_handling_failure_total"].Should().Be(1);
    }

    [Fact]
    public void RecordEventDeduplicationHit_ShouldIncrementCounter()
    {
        var dedupType = "event_id";

        FeishuMetricsHelper.RecordEventDeduplicationHit(dedupType);

        _counterValues.Should().ContainKey("feishu_event_deduplication_hit_total");
        _counterValues["feishu_event_deduplication_hit_total"].Should().Be(1);
    }

    [Fact]
    public void RecordHttpRequest_ShouldIncrementCounter_AndRecordDuration()
    {
        var method = "GET";
        var url = "https://open.feishu.cn/open-apis/auth/v3/tenant_access_token/internal";

        using (FeishuMetricsHelper.RecordHttpRequest(method, url))
        {
            Thread.Sleep(50);
        }

        _counterValues.Should().ContainKey("feishu_http_request_total");
        _counterValues["feishu_http_request_total"].Should().Be(1);
        _histogramValues.Should().ContainKey("feishu_http_request_duration_ms");
        _histogramValues["feishu_http_request_duration_ms"].Should().HaveCount(1);
        _histogramValues["feishu_http_request_duration_ms"][0].Should().BeGreaterThan(0);
    }

    [Fact]
    public void WebSocketConnectionCount_ShouldReturnProviderValue()
    {
        var expectedCount = 5;

        FeishuMetrics.WebSocketConnectionCountProvider = () => expectedCount;

        FeishuMetrics.WebSocketConnectionCountProvider.Should().NotBeNull();
        FeishuMetrics.WebSocketConnectionCountProvider().Should().Be(expectedCount);
    }

    [Fact]
    public void RecordWebSocketMessageProcessing_ShouldRecordDuration()
    {
        using (FeishuMetricsHelper.RecordWebSocketMessageProcessing())
        {
            Thread.Sleep(50);
        }

        _histogramValues.Should().ContainKey("feishu_websocket_message_processing_duration_ms");
        _histogramValues["feishu_websocket_message_processing_duration_ms"].Should().HaveCount(1);
        _histogramValues["feishu_websocket_message_processing_duration_ms"][0].Should().BeGreaterThan(0);
    }
}
