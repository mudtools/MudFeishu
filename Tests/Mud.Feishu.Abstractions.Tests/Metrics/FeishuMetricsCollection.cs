// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.Tests.Metrics;

/// <summary>
/// 指标类测试的 xUnit 集合定义。
/// </summary>
/// <remarks>
/// <para>
/// <see cref="System.Diagnostics.Metrics.MeterListener"/> 是<b>进程级</b>观测通道：
/// 一旦启用某个 instrument，监听器会收到<b>该进程内所有</b>测量（不区分发起测试类）。
/// </para>
/// <para>
/// 而 <c>FeishuMetrics</c> 的 instrument 是 <c>static readonly</c> 单例，
/// 多个指标测试类若并行运行，会互相把测量计入对方的计数器，
/// 导致"断言精确计数"的用例随机失败（曾观测：期望 1、实际 3）。
/// </para>
/// <para>
/// 因此所有直接读取 <c>FeishuMetrics</c> 计数的测试类都必须加入本集合——
/// 同一集合内的测试类由 xUnit 串行执行。新增此类测试时请一并标注
/// <c>[Collection(FeishuMetricsCollection.Name)]</c>。
/// </para>
/// </remarks>
[CollectionDefinition(Name)]
public class FeishuMetricsCollection
{
    /// <summary>集合名称。</summary>
    public const string Name = "FeishuMetrics";
}
