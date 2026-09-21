// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Mud.Feishu.Abstractions.Metrics;
using System.Diagnostics.Metrics;

namespace Mud.Feishu.Abstractions.Tests.Metrics;

/// <summary>
/// FeishuMetricsHelper 单元测试。
/// </summary>
/// <remarks>
/// P2-5：<c>RecordEventOutcome</c> 的 <c>error.type</c> 标签必须在 success 路径上同样保留。
/// 历史实现为 <c>if (!success &amp;&amp; errorType != null)</c>，导致
/// unhandled / timeout_recovered / mark_completed_failed 等 success 路径限定标签全部落空，
/// WHF-07 / WHF-09 / T2-2 的指标设计随之失效。
/// </remarks>
[Collection(FeishuMetricsCollection.Name)]
public class FeishuMetricsHelperTests
{
    private const string EventHandlingInstrumentName = "feishu.event.handling";

    /// <summary>
    /// 在 <paramref name="act"/> 执行期间采集 feishu.event.handling 的度量记录。
    /// </summary>
    private static List<(long Value, Dictionary<string, object?> Tags)> CaptureEventHandling(Action act)
    {
        var records = new List<(long, Dictionary<string, object?>)>();

        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, l) =>
            {
                if (instrument.Meter.Name == FeishuMetrics.MeterName &&
                    instrument.Name == EventHandlingInstrumentName)
                {
                    l.EnableMeasurementEvents(instrument);
                }
            }
        };

        listener.SetMeasurementEventCallback<long>((_, value, tags, _) =>
        {
            var captured = new Dictionary<string, object?>();
            foreach (var tag in tags)
            {
                captured[tag.Key] = tag.Value;
            }
            records.Add((value, captured));
        });

        listener.Start();
        act();
        listener.Dispose();

        return records;
    }

    [Fact]
    public void RecordEventOutcome_ShouldKeepErrorTypeTag_WhenSuccess()
    {
        // Act
        var records = CaptureEventHandling(() =>
            FeishuMetricsHelper.RecordEventOutcome("app1", "drive.file.edit_v1", success: true, "mark_completed_failed"));

        // Assert
        records.Should().ContainSingle();
        records[0].Value.Should().Be(1);
        records[0].Tags[FeishuMetrics.Tags.Outcome].Should().Be("success");
        records[0].Tags.Should().ContainKey(FeishuMetrics.Tags.ErrorType,
            "P2-5：success 路径的限定标签不得被丢弃");
        records[0].Tags[FeishuMetrics.Tags.ErrorType].Should().Be("mark_completed_failed");
        records[0].Tags[FeishuMetrics.Tags.EventType].Should().Be("drive.file.edit_v1");
        records[0].Tags[FeishuMetrics.Tags.AppKey].Should().Be("app1");
    }

    [Fact]
    public void RecordEventOutcome_ShouldKeepErrorTypeTag_WhenFailure()
    {
        // Act
        var records = CaptureEventHandling(() =>
            FeishuMetricsHelper.RecordEventOutcome("app1", "drive.file.edit_v1", success: false, "OperationCanceledException"));

        // Assert
        records.Should().ContainSingle();
        records[0].Tags[FeishuMetrics.Tags.Outcome].Should().Be("failure");
        records[0].Tags[FeishuMetrics.Tags.ErrorType].Should().Be("OperationCanceledException");
    }

    [Fact]
    public void RecordEventOutcome_ShouldNotAddErrorTypeTag_WhenErrorTypeIsNull()
    {
        // Act
        var records = CaptureEventHandling(() =>
            FeishuMetricsHelper.RecordEventOutcome("app1", "drive.file.edit_v1", success: true));

        // Assert
        records.Should().ContainSingle();
        records[0].Tags.Should().NotContainKey(FeishuMetrics.Tags.ErrorType,
            "未提供限定标签时不得引入空标签（否则会污染维度基数）");
    }
}
