// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ApprovalForm;

/// <summary>
/// 日期区间控件
/// </summary>
public class DateIntervalWidget() : WidgetBase<DateIntervalValue>("dateInterval")
{
}

/// <summary>
/// 表示日期间隔值的数据模型，包含开始时间、结束时间和间隔
/// </summary>
public class DateIntervalValue
{
    /// <summary>
    /// 间隔的开始时间
    /// </summary>
    [JsonPropertyName("start")]
    public DateTimeOffset Start { get; set; }

    /// <summary>
    /// 间隔的结束时间
    /// </summary>
    [JsonPropertyName("end")]
    public DateTimeOffset End { get; set; }

    /// <summary>
    /// 时间间隔值（单位可能根据具体使用场景而定）
    /// </summary>
    [JsonPropertyName("interval")]
    public double Interval { get; set; }
}