// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Calendar;


/// <summary>
/// <para>日程开始时间。</para>
/// </summary>
public class CalendarTimeInfo
{
    /// <summary>
    /// <para>开始时间，仅全天日程使用该字段，[RFC 3339](https://datatracker.ietf.org/doc/html/rfc3339) 格式，例如，2018-09-01。</para>
    /// <para>**注意**：该参数不能与 `timestamp` 同时指定。</para>
    /// <para>必填：否</para>
    /// <para>示例值：2018-09-01</para>
    /// </summary>
    [JsonPropertyName("date")]
    public string? Date { get; set; }

    /// <summary>
    /// <para>秒级时间戳，用于设置具体的开始时间。例如，1602504000 表示 2020/10/12 20:00:00（UTC +8 时区）。</para>
    /// <para>**注意**：该参数不能与 `date` 同时指定。</para>
    /// <para>必填：否</para>
    /// <para>示例值：1602504000</para>
    /// </summary>
    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; set; }

    /// <summary>
    /// <para>时区。使用 IANA Time Zone Database 标准，例如 Asia/Shanghai。</para>
    /// <para>- 全天日程时区固定为UTC +0</para>
    /// <para>- 非全天日程时区默认为 Asia/Shanghai</para>
    /// <para>必填：否</para>
    /// <para>示例值：Asia/Shanghai</para>
    /// </summary>
    [JsonPropertyName("timezone")]
    public string? Timezone { get; set; }
}