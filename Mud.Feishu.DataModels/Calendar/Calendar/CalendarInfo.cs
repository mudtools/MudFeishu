// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Calendar;

/// <summary>
/// 日历信息
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Calendar")]
public class CalendarInfo : CalendarBaseData
{
    /// <summary>
    /// <para>日历 ID。后续可以通过该 ID 查询、更新或删除日历信息。更多信息可参见[日历 ID 字段说明](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/calendar-v4/calendar/introduction)。</para>
    /// <para>必填：是</para>
    /// <para>示例值：feishu.cn_xxxxxxxxxx@group.calendar.feishu.cn</para>
    /// </summary>
    [JsonPropertyName("calendar_id")]
    public string CalendarId { get; set; } = string.Empty;


    /// <summary>
    /// <para>日历类型。</para>
    /// <para>必填：否</para>
    /// <para>示例值：shared</para>
    /// <para>可选值：<list type="bullet">
    /// <item>unknown：未知类型</item>
    /// <item>primary：用户或应用的主日历</item>
    /// <item>shared：由用户或应用创建的共享日历</item>
    /// <item>google：用户绑定的谷歌日历</item>
    /// <item>resource：会议室日历</item>
    /// <item>exchange：用户绑定的 Exchange 日历</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }


    /// <summary>
    /// <para>对于当前身份，日历是否已经被标记为删除。</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// <para>默认值：false</para>
    /// </summary>
    [JsonPropertyName("is_deleted")]
    public bool? IsDeleted { get; set; }

    /// <summary>
    /// <para>当前日历是否是第三方数据。三方日历及日程只支持读，不支持写入。</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// <para>默认值：false</para>
    /// </summary>
    [JsonPropertyName("is_third_party")]
    public bool? IsThirdParty { get; set; }

    /// <summary>
    /// <para>当前身份对于该日历的访问权限。</para>
    /// <para>必填：否</para>
    /// <para>示例值：owner</para>
    /// <para>可选值：<list type="bullet">
    /// <item>unknown：未知权限</item>
    /// <item>free_busy_reader：游客，只能看到忙碌、空闲信息</item>
    /// <item>reader：订阅者，可查看所有日程详情</item>
    /// <item>writer：编辑者，可创建及修改日程</item>
    /// <item>owner：管理员，可管理日历及共享设置</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("role")]
    public string? Role { get; set; }
}
