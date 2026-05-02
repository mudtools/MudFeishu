// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Mud.Feishu.Abstractions.Metrics;

/// <summary>
/// Meter 扩展方法
/// </summary>
public static class MeterExtensions
{
    /// <summary>
    /// 创建一个可释放的持续时间记录器
    /// </summary>
    public static IDisposable RecordDuration(this Histogram<double> histogram)
    {
        return new DurationRecorder(histogram, default);
    }

    /// <summary>
    /// 创建一个带标签的可释放持续时间记录器
    /// </summary>
    public static IDisposable RecordDuration(this Histogram<double> histogram, TagList tags)
    {
        return new DurationRecorder(histogram, tags);
    }

    private class DurationRecorder : IDisposable
    {
        private readonly Histogram<double> _histogram;
        private readonly Stopwatch _stopwatch;
        private readonly TagList _tags;

        public DurationRecorder(Histogram<double> histogram, TagList tags)
        {
            _histogram = histogram;
            _tags = tags;
            _stopwatch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            _histogram.Record(_stopwatch.ElapsedMilliseconds, _tags);
        }
    }
}
