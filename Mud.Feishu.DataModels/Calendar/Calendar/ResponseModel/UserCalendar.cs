// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Calendar;

/// <summary>
/// 用户日历信息
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Calendar")]
public class UserCalendar
{
    /// <summary>
    /// <para>日历实体信息。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("calendar")]
    public CalendarInfo? Calendar { get; set; }


    /// <summary>
    /// <para>日历创建者的 User ID。了解用户不同类型的 ID，可参见[用户身份概述](https://open.feishu.cn/document/home/user-identity-introduction/introduction)。</para>
    /// <para>必填：否</para>
    /// <para>示例值：ou_xxxxxx</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }
}
