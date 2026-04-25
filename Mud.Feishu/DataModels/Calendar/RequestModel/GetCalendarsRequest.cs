// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Calendar;


/// <summary>
/// 批量查询日历信息请求体
/// </summary>
public class GetCalendarsRequest
{
    /// <summary>
    /// <para>日历 ID 列表，多个 ID 传入格式为 `["xxx1", "xxx2"]`。</para>
    /// <para>创建共享日历时会返回日历 ID。你也可以调用以下接口获取某一日历的 ID。</para>
    /// <para>- [查询主日历信息](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/calendar-v4/calendar/primary)</para>
    /// <para>- [查询日历列表](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/calendar-v4/calendar/list)</para>
    /// <para>- [搜索日历](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/calendar-v4/calendar/search)</para>
    /// <para>必填：是</para>
    /// <para>最大长度：10</para>
    /// <para>最小长度：1</para>
    /// </summary>
    [JsonPropertyName("calendar_ids")]
    public string[] CalendarIds { get; set; } = [];
}