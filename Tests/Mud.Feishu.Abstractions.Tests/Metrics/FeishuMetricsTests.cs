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
/// <remarks>
/// 必须与其它直接读取 <c>FeishuMetrics</c> 计数的测试类同属
/// <see cref="FeishuMetricsCollection"/>：MeterListener 为进程级观测通道，
/// 并行运行会使测量串扰进本类"精确计数"断言。
/// </remarks>
[Collection(FeishuMetricsCollection.Name)]
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

    /// <summary>
    /// 从 <see cref="Measurement{T}.Tags"/>（ReadOnlySpan，不可按 key 索引）中取出指定标签值。
    /// </summary>
    private static object? GetTag(Measurement<int> measurement, string key)
    {
        foreach (var tag in measurement.Tags)
        {
            if (tag.Key == key)
            {
                return tag.Value;
            }
        }

        return null;
    }

    [Fact]
    public void WebSocketMetricsSource_ShouldReturnProvidedValues()
    {
        var expectedCount = 5;

        using var registration = FeishuMetrics.RegisterWebSocketMetricsSource(
            appKeyProvider: () => "test_app",
            activeConnectionsProvider: () => expectedCount,
            pendingMessagesProvider: () => 0);

        var measurements = FeishuMetrics.ObserveWebSocketConnections().ToList();
        measurements.Should().HaveCount(1);
        measurements[0].Value.Should().Be(expectedCount);
        GetTag(measurements[0], FeishuMetrics.Tags.AppKey).Should().Be("test_app");
    }

    /// <summary>
    /// P2-3 核心：多应用（多个注册实例）必须<b>聚合</b>而非互相覆盖。
    /// </summary>
    [Fact]
    public void WebSocketMetricsSource_ShouldAggregateMultipleApps_WhenMultipleRegistered()
    {
        using var first = FeishuMetrics.RegisterWebSocketMetricsSource(() => "app_1", () => 1, () => 0);
        using var second = FeishuMetrics.RegisterWebSocketMetricsSource(() => "app_2", () => 1, () => 7);

        var connections = FeishuMetrics.ObserveWebSocketConnections().ToList();
        var backlog = FeishuMetrics.ObserveWebSocketBacklog().ToList();

        connections.Should().HaveCount(2, "同一进程内多个应用必须各上报一条序列，而不是只保留最后一个");
        connections.Select(m => GetTag(m, FeishuMetrics.Tags.AppKey))
            .Should().BeEquivalentTo(new object?[] { "app_1", "app_2" });
        backlog.Should().HaveCount(2);
        backlog.Single(m => Equals(GetTag(m, FeishuMetrics.Tags.AppKey), "app_2")).Value.Should().Be(7);
    }

    [Fact]
    public void WebSocketMetricsSource_ShouldRemoveEntry_WhenRegistrationDisposed()
    {
        var registration = FeishuMetrics.RegisterWebSocketMetricsSource(() => "app_dispose", () => 1, () => 2);
        FeishuMetrics.ObserveWebSocketConnections().Should().HaveCount(1);

        registration.Dispose();

        FeishuMetrics.ObserveWebSocketConnections().Should().BeEmpty("注销后不得残留（否则静态集合会持有已释放实例）");
    }

    [Fact]
    public void WebSocketMetricsSource_ShouldBeIdempotent_WhenDisposedTwice()
    {
        var registration = FeishuMetrics.RegisterWebSocketMetricsSource(() => "app_twice", () => 1, () => 0);

        var act = () =>
        {
            registration.Dispose();
            registration.Dispose();
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void WebSocketMetricsSource_ShouldReadAppKeyDynamically_WhenProviderValueChanges()
    {
        var appKey = "app_before";
        using var registration = FeishuMetrics.RegisterWebSocketMetricsSource(() => appKey, () => 1, () => 0);

        appKey = "app_after";

        var measurement = FeishuMetrics.ObserveWebSocketConnections().Single();
        GetTag(measurement, FeishuMetrics.Tags.AppKey).Should().Be("app_after",
            "AppKey 提供器在每次采集时读取，支持配置热更新");
    }

    [Fact]
    public void WebSocketMetricsSource_ShouldSkipFaultedSource_WhenProviderThrows()
    {
        using var healthy = FeishuMetrics.RegisterWebSocketMetricsSource(() => "app_ok", () => 3, () => 0);
        using var faulted = FeishuMetrics.RegisterWebSocketMetricsSource(
            () => throw new InvalidOperationException("提供器故障"),
            () => 1,
            () => 0);

        var measurements = FeishuMetrics.ObserveWebSocketConnections().ToList();

        measurements.Should().HaveCount(1, "单个指标源故障不得中断采集，也不得把异常抛进 OTel 回调");
        measurements[0].Value.Should().Be(3);
    }

    [Fact]
    public void RegisterWebSocketMetricsSource_ShouldThrow_WhenProviderIsNull()
    {
        var act = () => FeishuMetrics.RegisterWebSocketMetricsSource(null!, () => 0, () => 0);

        act.Should().Throw<ArgumentNullException>().WithParameterName("appKeyProvider");
    }

    [Fact]
    public void WebSocketGauges_ShouldBeRegistered()
    {
        FeishuMetrics.WebSocketConnectionGauge.Should().NotBeNull();
        FeishuMetrics.WebSocketBacklogGauge.Should().NotBeNull();
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
    public void WebSocketBacklogMetricsSource_ShouldReturnProvidedValues()
    {
        var expectedBacklog = 3;

        using var registration = FeishuMetrics.RegisterWebSocketMetricsSource(
            appKeyProvider: () => "test_app",
            activeConnectionsProvider: () => 0,
            pendingMessagesProvider: () => expectedBacklog);

        var measurements = FeishuMetrics.ObserveWebSocketBacklog().ToList();
        measurements.Should().HaveCount(1);
        measurements[0].Value.Should().Be(expectedBacklog);
    }

    [Fact]
    public void ObserveWebSocketConnections_ShouldReturnEmpty_WhenNoSourceRegistered()
    {
        // 说明：其余用例均以 using 释放注册，故此处观测结果应为空（验证"无源"路径不抛异常）
        FeishuMetrics.ObserveWebSocketConnections().Should().BeEmpty();
        FeishuMetrics.ObserveWebSocketBacklog().Should().BeEmpty();
    }
}
