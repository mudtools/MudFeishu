// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Calendar;


/// <summary>
/// 查询日历列表响应体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Calendar")]
public class QueryCalendarsResult : ApiPageListResult
{
    /// <summary>
    /// <para>增量同步标记。当 has_more 为 false 时，会同步返回新的 sync_token，下次请求需要带上 sync_token 增量获取日历变更数据。</para>
    /// <para>**注意**：返回的 sync_token 在 90 天内有效。</para>
    /// <para>必填：否</para>
    /// <para>示例值：ListCalendarsSyncToken_xxx</para>
    /// </summary>
    [JsonPropertyName("sync_token")]
    public string? SyncToken { get; set; }

    /// <summary>
    /// <para>分页加载的日历数据列表。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("calendar_list")]
    public CalendarInfo[]? CalendarLists { get; set; }
}
