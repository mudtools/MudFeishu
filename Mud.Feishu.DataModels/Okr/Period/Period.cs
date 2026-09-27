// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Okr;

/// <summary>
/// OKR 周期
/// </summary>
public class Period
{
    /// <summary>
    /// <para>周期 ID</para>
    /// <para>示例值：7071200999834255380</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>周期中文名称</para>
    /// <para>示例值：2022 年 1 月 - 3 月</para>
    /// </summary>
    [JsonPropertyName("zh_name")]
    public string? ZhName { get; set; }

    /// <summary>
    /// <para>周期英文名称</para>
    /// <para>示例值：Jan - Mar 2022</para>
    /// </summary>
    [JsonPropertyName("en_name")]
    public string? EnName { get; set; }

    /// <summary>
    /// <para>启用状态：0 默认、1 正常、2 失效、3 隐藏</para>
    /// <para>示例值：0</para>
    /// </summary>
    [JsonPropertyName("status")]
    public int? Status { get; set; }

    /// <summary>
    /// <para>周期开始时间（毫秒时间戳字符串）</para>
    /// <para>示例值：1546272000000</para>
    /// </summary>
    [JsonPropertyName("period_start_time")]
    public string? PeriodStartTime { get; set; }

    /// <summary>
    /// <para>周期结束时间（毫秒时间戳字符串）</para>
    /// <para>示例值：1577721600000</para>
    /// </summary>
    [JsonPropertyName("period_end_time")]
    public string? PeriodEndTime { get; set; }
}
